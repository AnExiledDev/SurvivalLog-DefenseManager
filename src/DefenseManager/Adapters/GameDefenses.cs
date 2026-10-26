using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DefenseManager.Core;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;
using UnityEngine;
using Il2CppInts = Il2CppSystem.Collections.Generic.List<int>;
using Material = DefenseManager.Core.Material;

namespace DefenseManager.Adapters;

/// <summary>
/// The game's side of a defense press. Every read goes through the game's logic layer: the home's furniture in the
/// defense slot (the traps and barricades outside; doors and windows sit in other slots), their durability, the tower
/// repair cost and the condition check behind each defense's Repair button. A job is the same action the defense's
/// own menu lines up. Each call fails soft: an exception is logged once per kind of call and answered as a refusal.
/// </summary>
internal sealed class GameDefenses : IDefenseGame
{
    internal const int DefenseSlot = 10;
    private const int RepairFunc = 9121;
    private const int DefaultRemoveFunc = 299;
    private const int DefaultRepairAction = 9121;
    private const int DefaultRemoveAction = 199;
    private static string NoWorld => Texts.NoWorld.Pick(GameLanguage.IsChinese());
    private static string ReadFailed => Texts.ReadFailed.Pick(GameLanguage.IsChinese());
    private static string NoRepair => Texts.NoRepair.Pick(GameLanguage.IsChinese());
    private static string CostUnreadable => Texts.CostUnreadable.Pick(GameLanguage.IsChinese());
    private static string CheckFailed => Texts.CheckFailed.Pick(GameLanguage.IsChinese());

    private static readonly IReadOnlyDictionary<int, int> NoCost = new Dictionary<int, int>();

    private readonly Action<string> _log;
    private readonly HashSet<string> _reportedFailures = new();

    /// <summary>Defense id to the actions its own menu would line up, from the latest read. Every press reads before it queues.</summary>
    private readonly Dictionary<long, (int Repair, int Remove)> _actions = new();

    private static readonly Func<Task<(IconIndex Icons, string Note)>> ScanIcons = WebIcons.StartScan;

    private readonly IconLoad _icons;
    private readonly GameYard _yard;
    private int _loggedDefenseCount = -1;

    // The panel reads every second. What a defense or a material shows (its name, icon and the actions behind its
    // buttons) comes from table rows that never change, so each is read once per table, language and icon set.
    private readonly RowCache<TableKey, FurnitureFacts> _furniture = new();
    private readonly RowCache<TableKey, ItemFacts> _items = new();
    private int? _staminaRule;

    /// <summary>The world as last found, so the Layout check, made every frame, does not ask the game for it each time.</summary>
    private BattleLogicWorld? _world;

    public GameDefenses(Action<string> log)
    {
        _log = log;
        _icons = new IconLoad(log);
        _yard = new GameYard(log);
    }

    /// <summary>What a furniture row gives a defense, or null for a row that is not a defense.</summary>
    private sealed record FurnitureFacts(string Name, string? Icon, bool IsElectrical, bool HasRepair, int RepairConditionSetId, int RepairAction, int RemoveAction);

    private sealed record ItemFacts(string Name, string? Icon);

    public DefenseRead Read()
    {
        try
        {
            return ReadSurvey();
        }
        catch (Exception ex)
        {
            ReportOnce("read", ex);

            return new DefenseRead.NotReady(ReadFailed);
        }
    }

    /// <summary>
    /// Lines up the defense's repair or removal the way its menu buttons do, without starting it. A removal lined up
    /// here skips the menu's are-you-sure box: that box belongs to the menu, not to the action.
    /// </summary>
    public bool TryQueue(Job job, long defenseId)
    {
        try
        {
            var role = GameSingleton.Existing<BattleLogicWorld>()?._AgentManager?.GetLeadingRole();

            if (role == null || !_actions.TryGetValue(defenseId, out var actions)) return false;

            var actionId = job switch
            {
                Job.Repair => actions.Repair,
                Job.Remove => actions.Remove,
                _ => throw new ArgumentOutOfRangeException(nameof(job), job, "unknown job"),
            };

            role.AddLeadingRoleAction(actionId, defenseId, Vector3.zero, Quaternion.identity);
            _log($"lined up {job} of defense {defenseId} (action {actionId})");

            return true;
        }
        catch (Exception ex)
        {
            ReportOnce("queue", ex);

            return false;
        }
    }

    /// <summary>
    /// Starts the lined-up jobs as one batch. When that throws, the line is cleared so the next press does not read the
    /// stranded jobs as work in progress. That empties only the role's not-yet-started action cache, which held just
    /// this press's jobs; actions already in the character's queue are not touched.
    /// </summary>
    public bool TryStart()
    {
        LeadingRole? role = null;

        try
        {
            role = GameSingleton.Existing<BattleLogicWorld>()?._AgentManager?.GetLeadingRole();

            if (role == null) return false;

            role.DoLeadingRoleActionCache();

            return true;
        }
        catch (Exception ex)
        {
            ReportOnce("start", ex);

            if (role != null) ClearLine(role);

            return false;
        }
    }

    /// <summary>
    /// Where the player is: in no game, on an expedition, or at home with the defenses. Cheap enough for every refresh:
    /// the home check reads two map ids off the character, and looks up two map rows only when they differ. A failed
    /// home check counts as away.
    /// </summary>
    public Where Locate()
    {
        try
        {
            var game = LoadedGame();
            _world = game?.World;

            if (game == null) return Where.NoGame;

            return IsHome(game.Value.Agents) ? Where.Home : Where.Away;
        }
        catch (Exception ex)
        {
            ReportOnce("game check", ex);

            return Where.NoGame;
        }
    }

    /// <summary>
    /// True while the game's Layout screen (its plan mode) is up. Layout pauses the game's logic, so jobs lined up there
    /// are kept but wait until the player leaves it. Asked every frame, so the world is kept between frames rather than
    /// asked of the game each time (each ask makes a new wrapper object); <see cref="Locate"/>, run every second, picks
    /// up a new world after a load, and a world the game has let go is dropped at once.
    /// </summary>
    public bool IsInLayout()
    {
        try
        {
            // The world's address and its plan mode flag are read as fields: the game's IsInstanceCreated and IsPlanMode
            // only return them, but through interop each call boxes its answer, every frame.
            var pointer = GameSingleton.Pointer<BattleLogicWorld>();

            if (pointer == IntPtr.Zero)
            {
                _world = null;

                return false;
            }

            if (_world is not { } world || world.Pointer != pointer) _world = world = BaseSingleton<BattleLogicWorld>.Instance;

            return world._IsPlanMode_k__BackingField;
        }
        catch (Exception ex)
        {
            ReportOnce("layout check", ex);

            return false;
        }
    }

    private DefenseRead ReadSurvey()
    {
        var game = LoadedGame();

        if (game == null) return new DefenseRead.NoGame();

        var (world, config, agents, role) = game.Value;
        var actions = world._ActionManager;
        var conditions = world._ConditionSetManager;

        if (actions == null || conditions == null) return new DefenseRead.NotReady(NoWorld);

        _actions.Clear();
        _icons.Start(ScanIcons);

        var isChinese = GameLanguage.IsChinese();
        var tables = new TableKey(config.Pointer.ToInt64(), isChinese, _icons.Current);

        if (_furniture.Follow(tables)) _staminaRule = null;

        _items.Follow(tables);

        var defenses = ReadDefenses(agents, config, actions, conditions, world._PowerManager, role.InstanceId, tables.Icons);
        var materials = ReadMaterials(config, defenses, tables.Icons);
        var isWaveLocked = world._TowerWaveManager?.IsWaveOperationLocked() ?? false;
        var survey = new Survey(defenses, PositionOf(role), materials, ReadQueueOnStart(role, actions), isWaveLocked, IsHome(agents), ReadStamina(config, role), isChinese, _yard.Read(world, config, GamePower.ReadLoad(world)));

        LogDefenseCount(defenses.Count);

        return new DefenseRead.Ready(survey);
    }

    /// <summary>Null on the title screen, while a save loads and in the prologue.</summary>
    internal static (BattleLogicWorld World, ConfigManager Config, AgentManager Agents, LeadingRole Role)? LoadedGame()
    {
        var world = GameSingleton.Existing<BattleLogicWorld>();
        var config = GameSingleton.Existing<ConfigManager>();
        var agents = world?._AgentManager;
        var role = agents?.GetLeadingRole();

        if (world == null || config == null || agents == null || role == null || !IsAfterDisaster(world)) return null;

        return (world, config, agents, role);
    }

    /// <summary>
    /// False in the prologue, before the disaster. This is the game's own test (SceneRatManager.IsPostDisaster reads
    /// the same field), done here without the game's singleton lookup.
    /// </summary>
    private static bool IsAfterDisaster(BattleLogicWorld world) => world._GameTimeManager?.WorldStateType == WorldStateType.PostDisaster;

    /// <summary>One defense that cannot be read is left out of the panel; the rest still are.</summary>
    private List<Defense> ReadDefenses(AgentManager agents, ConfigManager config, ActionManager actions, ConditionSetManager conditions, PowerManager? power, long roleId, IconIndex icons)
    {
        var found = agents.GetHomeFurnituresBySlotType(DefenseSlot);
        var defenses = new List<Defense>();

        for (var i = 0; found != null && i < found.Count; i++)
        {
            var furniture = found[i];

            if (furniture == null || furniture.IsDead) continue;

            try
            {
                if (ReadDefense(furniture, config, actions, conditions, power, roleId, icons) is { } defense) defenses.Add(defense);
            }
            catch (Exception ex)
            {
                ReportOnce("defense read", ex);
            }
        }

        return defenses;
    }

    /// <summary>
    /// The slot is checked against the furniture table too: the game's own Repair button test
    /// (Furniture.ShouldShowTowerRepairBtn) does the same before it reads the durability.
    /// </summary>
    private Defense? ReadDefense(Furniture furniture, ConfigManager config, ActionManager actions, ConditionSetManager conditions, PowerManager? power, long roleId, IconIndex icons)
    {
        var configId = furniture.AgentConfigId;
        var facts = FactsOf(config, configId, icons);

        if (facts == null) return null;

        var durability = AgentTools.GetAgentComponent<FurnitureDurabilityComponent>(furniture);

        if (durability == null) return null;

        var id = furniture.InstanceId;
        var current = durability.CurrentDurability;
        var max = durability.MaxDurability;
        var (cost, blockedReason) = max > 0 && current < max ? ReadRepair(furniture, config, conditions, facts, current, max) : (NoCost, null);
        _actions[id] = (facts.RepairAction, facts.RemoveAction);

        return new Defense(
            id,
            configId,
            facts.Name,
            facts.Icon,
            current,
            max,
            durability.IsBrokenTowerDevice(),
            PositionOf(furniture),
            cost,
            blockedReason,
            actions.HasActionInQueue(roleId, facts.RepairAction, id),
            IsRemovalQueued(actions, roleId, id, facts.RemoveAction),
            facts.IsElectrical ? ReadPower(furniture, power) : null);
    }

    /// <summary>
    /// A furniture type's name, icon, power and the actions behind its Repair and Remove buttons, read once per table,
    /// language and icon set. Null, and kept as null, for a row that is not a defense.
    /// </summary>
    private FurnitureFacts? FactsOf(ConfigManager config, int configId, IconIndex icons)
    {
        if (_furniture.TryGet(configId, out var known)) return known;

        var row = FurnitureRow(config, configId);

        if (row == null || row.SlotType != DefenseSlot) return _furniture.Add(configId, null);

        var repairFunc = Contains(row.FurnitureFunc, RepairFunc) ? FurnitureFunc(config, RepairFunc) : null;
        var removeFunc = FurnitureFunc(config, First(row.RemoveFunc) ?? DefaultRemoveFunc);

        return _furniture.Add(configId, new FurnitureFacts(
            NameOf(config, row, configId),
            icons.Resolve(row.ICON),
            row.IsElectrical,
            repairFunc != null,
            repairFunc?.ConditionSetId ?? 0,
            First(repairFunc?.ActionIds) ?? DefaultRepairAction,
            First(removeFunc?.ActionIds) ?? DefaultRemoveAction));
    }

    /// <summary>The furniture table's row, read the way the game's own lookup reads it, without its error line for a missing id.</summary>
    internal static Config_Furniture? FurnitureRow(ConfigManager config, int configId)
    {
        var rows = config._Config_Furniture_Dict;

        return rows != null && rows.TryGetValue(configId, out var row) ? row : null;
    }

    private static Config_FurnitureFunc? FurnitureFunc(ConfigManager config, int funcId)
    {
        var funcs = config._Config_FurnitureFunc_Dict;

        return funcs != null && funcs.TryGetValue(funcId, out var func) ? func : null;
    }

    /// <summary>The defense's name in the player's language, falling back to the table's own text, then to its furniture id.</summary>
    internal static string NameOf(ConfigManager config, Config_Furniture row, int configId) =>
        Translate(config, row.Name) ?? NonEmpty(row.Name_Local) ?? NonEmpty(row.Name) ?? $"Defense {configId}";

    /// <summary>
    /// The switch as the game has it and whether the game counts the defense as working. A read that throws shows the
    /// defense as switched off and not working, so the panel offers its settings but never claims the mod runs it.
    /// </summary>
    private DefensePower ReadPower(Furniture furniture, PowerManager? power)
    {
        try
        {
            var device = AgentTools.GetAgentComponent<TowerDeviceComponent>(furniture);

            return new DefensePower(power?.IsConsumerOn(furniture.InstanceId) ?? false, device?.IsOperational() ?? false);
        }
        catch (Exception ex)
        {
            ReportOnce("power state", ex);

            return new DefensePower(IsOn: false, IsOperational: false);
        }
    }

    /// <summary>The game files a removal under its own id or the one it maps it to for the time of day, so either counts.</summary>
    private static bool IsRemovalQueued(ActionManager actions, long roleId, long defenseId, int removeAction)
    {
        var mapped = ActionManager.MapDemolishActionByWorldState(removeAction);

        return actions.HasActionInQueue(roleId, removeAction, defenseId) || (mapped != removeAction && actions.HasActionInQueue(roleId, mapped, defenseId));
    }

    /// <summary>
    /// What a full repair costs now and the game's reason for refusing it, null when it would allow it. A cost or a
    /// check that cannot be read blocks the repair rather than letting it through on a guess.
    /// </summary>
    private (IReadOnlyDictionary<int, int> Cost, string? BlockedReason) ReadRepair(Furniture furniture, ConfigManager config, ConditionSetManager conditions, FurnitureFacts facts, int current, int max)
    {
        if (!facts.HasRepair) return (NoCost, NoRepair);

        var cost = ReadCost(furniture, max - current, max);

        if (cost == null) return (NoCost, CostUnreadable);

        return (cost, WhyRepairRefused(config, conditions, facts.RepairConditionSetId, furniture.InstanceId));
    }

    /// <summary>
    /// The cost the repair action itself charges. The game answers null when the repair takes no material, so that
    /// reads as free; zero counts are dropped.
    /// </summary>
    private IReadOnlyDictionary<int, int>? ReadCost(Furniture furniture, int missing, int max)
    {
        try
        {
            var device = AgentTools.GetAgentComponent<TowerDeviceComponent>(furniture);
            var built = TowerRepairMaterial.BuildCost(furniture.AgentConfigId, device, missing, max);
            var cost = new Dictionary<int, int>();

            if (built == null) return cost;

            foreach (var itemId in built.Keys)
            {
                var count = built[itemId];

                if (count > 0) cost[itemId] = count;
            }

            return cost;
        }
        catch (Exception ex)
        {
            ReportOnce("repair cost", ex);

            return null;
        }
    }

    /// <summary>
    /// The condition set behind the Repair button, run the way the game's action check runs it: CheckConditionSet
    /// decides, and CheckFurnitureFuncCondition only supplies the line to show. The flag in that second call's result
    /// read as "blocked" on every repair (Il2CppInterop misreads a bool field of a generic struct), so it is never
    /// used; a block without a line comes back as an empty reason.
    /// </summary>
    private string? WhyRepairRefused(ConfigManager config, ConditionSetManager conditions, int conditionSetId, long defenseId)
    {
        if (conditionSetId <= 0) return null;

        try
        {
            if (conditions.CheckConditionSet(conditionSetId, defenseId, false)) return null;

            var reason = conditions.CheckFurnitureFuncCondition(conditionSetId, defenseId).Item2;

            return Translate(config, reason) ?? NonEmpty(reason) ?? "";
        }
        catch (Exception ex)
        {
            ReportOnce("repair check", ex);

            return CheckFailed;
        }
    }

    /// <summary>
    /// The character's stamina, read the way the game's condition check reads it (ConditionSetManager.c:935-990), and
    /// the least stamina the repair's condition set lets a repair start with. Null when either cannot be read, so only
    /// the game's own check applies.
    /// </summary>
    private RepairStamina? ReadStamina(ConfigManager config, LeadingRole role)
    {
        try
        {
            var neededToStart = _staminaRule ??= ReadStaminaRule(config);
            var attributes = AgentTools.GetAgentComponent<AttributeComponent>(role);

            if (neededToStart <= 0 || attributes == null) return null;

            var have = attributes.IsSingle(AttrName.Stamina) ? attributes.GetTotalValue_Float(AttrName.Stamina) : attributes.GetTotalValue_Int(AttrName.Stamina);

            return new RepairStamina(have, neededToStart);
        }
        catch (Exception ex)
        {
            ReportOnce("stamina", ex);

            return null;
        }
    }

    /// <summary>The repair's stamina rule from the tables, 0 when it has none. Kept with the other table reads.</summary>
    private static int ReadStaminaRule(ConfigManager config)
    {
        var conditionSetId = FurnitureFunc(config, RepairFunc)?.ConditionSetId ?? 0;
        var conditionSets = config._Config_ConditionSet_Dict;

        return conditionSetId > 0 && conditionSets != null && conditionSets.TryGetValue(conditionSetId, out var conditionSet) ? StaminaRule(conditionSet) : 0;
    }

    /// <summary>The stamina the condition set asks for before a repair starts; it is a minimum, not what the repair spends.</summary>
    private static int StaminaRule(Config_ConditionSet? conditionSet)
    {
        var keys = conditionSet?.AttriKey;
        var numbers = conditionSet?.Number;

        for (var i = 0; keys != null && numbers != null && i < keys.Count && i < numbers.Count; i++)
        {
            if (keys[i] == (int)AttrName.Stamina) return numbers[i];
        }

        return 0;
    }

    /// <summary>The materials the defenses' repairs use, with what the game counts as available for a tower repair.</summary>
    private Dictionary<int, Material> ReadMaterials(ConfigManager config, List<Defense> defenses, IconIndex icons)
    {
        var materials = new Dictionary<int, Material>();

        for (var i = 0; i < defenses.Count; i++)
        {
            foreach (var itemId in defenses[i].Cost.Keys)
            {
                if (materials.ContainsKey(itemId)) continue;

                var item = ItemOf(config, itemId, icons);

                materials[itemId] = new Material(item.Name, item.Icon, TowerRepairMaterial.CountAvailable(itemId));
            }
        }

        return materials;
    }

    /// <summary>A material's name and icon, read once per table, language and icon set; only its stock is read every time.</summary>
    private ItemFacts ItemOf(ConfigManager config, int itemId, IconIndex icons)
    {
        if (_items.TryGet(itemId, out var known) && known != null) return known;

        var items = config._Config_Item_Dict;
        var item = items != null && items.TryGetValue(itemId, out var found) ? found : null;
        var name = item == null ? null : Translate(config, item.ItemName) ?? NonEmpty(item.ItemName_Local) ?? NonEmpty(item.ItemName);
        var facts = new ItemFacts(name ?? $"Item {itemId}", icons.Resolve(NonEmpty(item?.WebIcon) ?? NonEmpty(item?.Icon)));

        _items.Add(itemId, facts);

        return facts;
    }

    /// <summary>
    /// What starting the batch does to the character's queue. The game's start (ActionCacheComponent.DoActionCache)
    /// clears the character's actions first only in the Default action mode; in Queue mode, which the game sets after
    /// the prologue, the batch joins the end of the queue. A mode that cannot be read counts as Default, the one that
    /// can lose something.
    /// </summary>
    private static QueueOnStart ReadQueueOnStart(LeadingRole role, ActionManager actions)
    {
        var mode = AgentTools.GetAgentComponent<ActionCacheComponent>(role)?.Mode ?? ActionMode.Default;

        var hasPlayerActions = actions.HasManualActionInQueue(role.InstanceId);

        if (mode == ActionMode.Queue) return hasPlayerActions ? QueueOnStart.AppendsBehindPlayerActions : QueueOnStart.Appends;

        return hasPlayerActions ? QueueOnStart.ReplacesPlayerActions : QueueOnStart.ReplacesOwnActions;
    }

    /// <summary>The defenses are on the home map, so a failed check counts as away and lines nothing up.</summary>
    private bool IsHome(AgentManager agents)
    {
        try
        {
            return agents.IsHomeMap(ExploreSearchStateManager.GetCurrentMapId());
        }
        catch (Exception ex)
        {
            ReportOnce("home check", ex);

            return false;
        }
    }

    /// <summary>An agent with no position stands at the origin: the walk order is then only a guess, never a refusal.</summary>
    internal static Spot PositionOf(BaseAgent agent)
    {
        var transform = AgentTools.GetAgentComponent<TransformComponent>(agent);

        if (transform == null) return new Spot(0, 0, 0);

        var position = transform.Position;

        return new Spot(position.x, position.y, position.z);
    }

    /// <summary>The panel reads every second, so the count is only logged when it changes.</summary>
    private void LogDefenseCount(int count)
    {
        if (count == _loggedDefenseCount) return;

        _loggedDefenseCount = count;
        _log($"{count} outdoor defenses at home");
    }

    private void ClearLine(LeadingRole role)
    {
        try
        {
            role.ClearLeadingRoleActionCache();
        }
        catch (Exception ex)
        {
            ReportOnce("clear line", ex);
        }
    }

    private static int? First(Il2CppInts? ids) => ids != null && ids.Count > 0 ? ids[0] : null;

    private static bool Contains(Il2CppInts? ids, int wanted)
    {
        for (var i = 0; ids != null && i < ids.Count; i++)
        {
            if (ids[i] == wanted) return true;
        }

        return false;
    }

    private static string? Translate(ConfigManager config, string? key)
    {
        if (string.IsNullOrEmpty(key)) return null;

        var text = config.GetLocalTxt(key);

        return string.IsNullOrEmpty(text) || text == key ? null : text;
    }

    private static string? NonEmpty(string? text) => string.IsNullOrWhiteSpace(text) ? null : text;

    private void ReportOnce(string operation, Exception ex)
    {
        if (_reportedFailures.Add(operation)) _log($"{operation} failed (each kind reported once): {ex}");
    }
}

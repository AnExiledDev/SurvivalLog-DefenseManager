using System;
using System.Collections.Generic;
using System.Linq;
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
    private const int DefenseSlot = 10;
    private const int RepairFunc = 9121;
    private const int DefaultRemoveFunc = 299;
    private const int DefaultRepairAction = 9121;
    private const int DefaultRemoveAction = 199;
    private const string NoWorld = "the game world is not loaded";
    private const string ReadFailed = "the game could not be read, see BepInEx/defense-manager.log";
    private const string NoRepair = "it has no repair";
    private const string CostUnreadable = "its repair cost could not be read";
    private const string CheckFailed = "the game's repair check failed, see BepInEx/defense-manager.log";

    private static readonly IReadOnlyDictionary<int, int> NoCost = new Dictionary<int, int>();

    private readonly Action<string> _log;
    private readonly HashSet<string> _reportedFailures = new();

    /// <summary>Defense id to the actions its own menu would line up, from the latest read. Every press reads before it queues.</summary>
    private readonly Dictionary<long, (int Repair, int Remove)> _actions = new();

    private IconIndex? _icons;
    private int _loggedDefenseCount = -1;

    public GameDefenses(Action<string> log)
    {
        _log = log;
    }

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

    /// <summary>True in a loaded game past the prologue, the only place the panel and its HUD button have work.</summary>
    public bool IsGameLoaded()
    {
        try
        {
            return LoadedGame() != null;
        }
        catch (Exception ex)
        {
            ReportOnce("game check", ex);

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

        var defenses = ReadDefenses(agents, config, actions, conditions, role.InstanceId);
        var materials = ReadMaterials(config, defenses);
        var isWaveLocked = world._TowerWaveManager?.IsWaveOperationLocked() ?? false;
        var survey = new Survey(defenses, PositionOf(role), materials, ReadQueueOnStart(role, actions), isWaveLocked, IsHome(agents));

        LogDefenseCount(defenses.Count);

        return new DefenseRead.Ready(survey);
    }

    /// <summary>Null on the title screen, while a save loads and in the prologue.</summary>
    private static (BattleLogicWorld World, ConfigManager Config, AgentManager Agents, LeadingRole Role)? LoadedGame()
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
    private List<Defense> ReadDefenses(AgentManager agents, ConfigManager config, ActionManager actions, ConditionSetManager conditions, long roleId)
    {
        var found = agents.GetHomeFurnituresBySlotType(DefenseSlot);
        var defenses = new List<Defense>();

        for (var i = 0; found != null && i < found.Count; i++)
        {
            var furniture = found[i];

            if (furniture == null || furniture.IsDead) continue;

            try
            {
                if (ReadDefense(furniture, config, actions, conditions, roleId) is { } defense) defenses.Add(defense);
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
    private Defense? ReadDefense(Furniture furniture, ConfigManager config, ActionManager actions, ConditionSetManager conditions, long roleId)
    {
        var configId = furniture.AgentConfigId;
        var row = config.Get_Config_Furniture(configId);
        var durability = AgentTools.GetAgentComponent<FurnitureDurabilityComponent>(furniture);

        if (row == null || row.SlotType != DefenseSlot || durability == null) return null;

        var id = furniture.InstanceId;
        var current = durability.CurrentDurability;
        var max = durability.MaxDurability;
        var repairFunc = Contains(row.FurnitureFunc, RepairFunc) ? config.Get_Config_FurnitureFunc(RepairFunc) : null;
        var repairAction = First(repairFunc?.ActionIds) ?? DefaultRepairAction;
        var removeAction = First(config.Get_Config_FurnitureFunc(First(row.RemoveFunc) ?? DefaultRemoveFunc)?.ActionIds) ?? DefaultRemoveAction;
        var (cost, blockedReason) = max > 0 && current < max ? ReadRepair(furniture, config, conditions, repairFunc, current, max) : (NoCost, null);
        _actions[id] = (repairAction, removeAction);

        return new Defense(
            id,
            configId,
            Translate(config, row.Name) ?? NonEmpty(row.Name_Local) ?? NonEmpty(row.Name) ?? $"Defense {configId}",
            Icons().Resolve(row.ICON),
            current,
            max,
            durability.IsBrokenTowerDevice(),
            PositionOf(furniture),
            cost,
            blockedReason,
            actions.HasActionInQueue(roleId, repairAction, id),
            IsRemovalQueued(actions, roleId, id, removeAction));
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
    private (IReadOnlyDictionary<int, int> Cost, string? BlockedReason) ReadRepair(Furniture furniture, ConfigManager config, ConditionSetManager conditions, Config_FurnitureFunc? repairFunc, int current, int max)
    {
        if (repairFunc == null) return (NoCost, NoRepair);

        var cost = ReadCost(furniture, max - current, max);

        if (cost == null) return (NoCost, CostUnreadable);

        return (cost, WhyRepairRefused(config, conditions, repairFunc.ConditionSetId, furniture.InstanceId));
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
    /// The condition set behind the Repair button, run the way its menu runs it. The game answers whether it blocks
    /// and the line it would show; a block without a line comes back as an empty reason, which still blocks.
    /// </summary>
    private string? WhyRepairRefused(ConfigManager config, ConditionSetManager conditions, int conditionSetId, long defenseId)
    {
        if (conditionSetId <= 0) return null;

        try
        {
            var check = conditions.CheckFurnitureFuncCondition(conditionSetId, defenseId);

            if (!check.Item1) return null;

            return Translate(config, check.Item2) ?? NonEmpty(check.Item2) ?? "";
        }
        catch (Exception ex)
        {
            ReportOnce("repair check", ex);

            return CheckFailed;
        }
    }

    /// <summary>The materials the defenses' repairs use, with what the game counts as available for a tower repair.</summary>
    private Dictionary<int, Material> ReadMaterials(ConfigManager config, IEnumerable<Defense> defenses)
    {
        var materials = new Dictionary<int, Material>();

        foreach (var itemId in defenses.SelectMany(defense => defense.Cost.Keys).Distinct())
        {
            var item = config.Get_Config_Item(itemId);
            var name = item == null ? null : Translate(config, item.ItemName) ?? NonEmpty(item.ItemName_Local) ?? NonEmpty(item.ItemName);
            var icon = Icons().Resolve(NonEmpty(item?.WebIcon) ?? NonEmpty(item?.Icon));

            materials[itemId] = new Material(name ?? $"Item {itemId}", icon, TowerRepairMaterial.CountAvailable(itemId));
        }

        return materials;
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

        if (mode == ActionMode.Queue) return QueueOnStart.Appends;

        return actions.HasManualActionInQueue(role.InstanceId) ? QueueOnStart.ReplacesPlayerActions : QueueOnStart.ReplacesOwnActions;
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
    private static Spot PositionOf(BaseAgent agent)
    {
        var transform = AgentTools.GetAgentComponent<TransformComponent>(agent);

        if (transform == null) return new Spot(0, 0, 0);

        var position = transform.Position;

        return new Spot(position.x, position.y, position.z);
    }

    private IconIndex Icons() => _icons ??= WebIcons.Load(_log);

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

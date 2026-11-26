using System;
using System.Collections.Generic;
using DefenseManager.Core;
using ExiledToolkit.Game;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle;
using GameCore.HotUpdate.Battle.Logic;

namespace DefenseManager.Adapters;

/// <summary>
/// Runs the Unlocks settings against the loaded run when play starts (a load, a new game or a trip) and when a setting
/// changes, on the schedule UnlockSchedule keeps. Only a story run after the disaster is touched: endless runs already
/// start with all of it, and challenge and tutorial runs keep the game's rules. Each distinct log line is written once,
/// so a grant the game refuses is not logged every pass.
/// </summary>
internal sealed class UnlockSession
{
    private static bool _isStarted;

    private readonly Action<string> _log;
    private readonly HashSet<string> _logged = new();
    private readonly UnlockSchedule _schedule;
    private UnlockSettings _settings = UnlockSettings.None;
    private IntPtr _world;

    public UnlockSession(Action<string> log, bool isHooked)
    {
        _log = log;
        _schedule = new UnlockSchedule(isHooked);
    }

    /// <summary>
    /// Hooks AgentManager.StartBattle, which runs at every start of play; its first 14 bytes have no relative jump or
    /// call. The disaster starts a new world, so it comes through here too. Returns false when the hook did not go in.
    /// </summary>
    public static bool HookStart(PatchKit hooks) =>
        hooks.Patch(typeof(UnlockSession), typeof(AgentManager), nameof(AgentManager.StartBattle), new[] { typeof(GameSaveData), typeof(GameMode) },
            postfix: nameof(AfterStart), consequence: "the Unlocks are checked every 5 seconds instead");

    private static void AfterStart() => _isStarted = true;

    public void Use(UnlockSettings settings)
    {
        _settings = settings;
        _schedule.Ask();
    }

    public void Update()
    {
        var now = DefenseTicker.Now();

        if (_isStarted)
        {
            _isStarted = false;
            _schedule.Started(now);
        }

        if (!_schedule.IsDue(now)) return;

        var game = GameUnlocks.StoryRun(out var isLoading);

        _schedule.Ran(now, isLoading);

        if (game == null) return;

        // A new run (another save loaded) logs its own gifts again.
        if (game.World != _world)
        {
            _world = game.World;
            _logged.Clear();
        }

        foreach (var line in DayOneUnlocks.Run(game, _settings))
        {
            if (_logged.Add(line)) _log("unlocks: " + line);
        }
    }
}

/// <summary>
/// The run's tags and craft table. A recipe the craft table knows is a key in its record list (the recipe's materials,
/// sorted and joined, as the game builds it); a recipe it may make is an id in its craft-unlock list. The game has no
/// way to forget either, so taking one back removes it from those lists, which are what the save stores.
/// </summary>
internal sealed class GameUnlocks : IUnlockGame
{
    private readonly TagManager _tags;
    private readonly ToolTableManager _table;
    private readonly DynamicEventManager? _events;
    private readonly ConfigManager _config;

    private GameUnlocks(IntPtr world, TagManager tags, ToolTableManager table, DynamicEventManager? events, ConfigManager config)
    {
        World = world;
        _tags = tags;
        _table = table;
        _events = events;
        _config = config;
    }

    public IntPtr World { get; }

    /// <summary>
    /// The loaded story run after the disaster, or null for anything else. isLoading says a run is there but not ready
    /// yet (still loading, or the world is not running), so it is worth asking again soon.
    /// </summary>
    public static GameUnlocks? StoryRun(out bool isLoading)
    {
        var main = GameSingleton.Existing<BattleMain>();
        var world = GameSingleton.Existing<BattleLogicWorld>();
        var save = GameSingleton.Existing<GameSaveManager>()?.gameSave;
        var config = GameSingleton.Existing<ConfigManager>();

        isLoading = main != null && !main.IsGameOver && (!main._IsLoadingFinished || world == null || !world.IsUpdate || save == null || config == null);

        if (main == null || isLoading || main.IsGameOver) return null;

        if (world == null || save == null || config == null) return null;

        if (world.GameModeKey != GameMode.Default || save.EndlessActive || save.ChallengeTargetId > 0) return null;

        if (world._GameTimeManager is not { } clock || clock.WorldStateType == WorldStateType.PreDisaster) return null;

        if (world._TagManager is not { } tags || world._ToolTableManager is not { } table) return null;

        return new GameUnlocks(world.Pointer, tags, table, world._DynamicEventManager, config);
    }

    public bool Has(Grant grant) => grant switch
    {
        Grant.Tag tag => _tags.HasTag(tag.Name),
        Grant.Recipe recipe => _config.Get_Config_ProductionList(recipe.Id) is { } row && _table.IsProductionRevealedByRecord(row),
        Grant.CraftUnlock craft => _table.HasCraftUnlockRecord(craft.Id),
        _ => false,
    };

    public void Give(IReadOnlyList<Grant> grants)
    {
        var tags = new Il2CppSystem.Collections.Generic.List<string>();
        var recipes = new Il2CppSystem.Collections.Generic.List<int>();
        var crafts = new Il2CppSystem.Collections.Generic.List<int>();

        foreach (var grant in grants)
        {
            switch (grant)
            {
                case Grant.Tag tag:
                    tags.Add(tag.Name);
                    break;
                case Grant.Recipe recipe:
                    recipes.Add(recipe.Id);
                    break;
                case Grant.CraftUnlock craft:
                    crafts.Add(craft.Id);
                    break;
            }
        }

        if (crafts.Count > 0) _table.AddProductionCraftUnlocks(crafts);
        if (recipes.Count > 0) _table.AddProductionRecords(recipes);
        if (tags.Count > 0) _tags.AddTags(tags);
    }

    public void Take(Grant grant)
    {
        switch (grant)
        {
            case Grant.Tag tag:
                var list = new Il2CppSystem.Collections.Generic.List<string>();

                list.Add(tag.Name);
                _tags.RemoveTags(list);
                break;
            case Grant.Recipe recipe:
                if (_config.Get_Config_ProductionList(recipe.Id) is { } row && ToolTableManager.ToProductionKey(row) is { } key)
                {
                    var records = _table._ProductionRecordList_k__BackingField;

                    while (records != null && records.Remove(key))
                    {
                    }
                }

                break;
            case Grant.CraftUnlock craft:
                var unlocked = _table._CraftUnlockedProductionIds_k__BackingField;

                while (unlocked != null && unlocked.Remove(craft.Id))
                {
                }

                break;
        }
    }

    public bool HasEventFired(int eventId) => _events != null && _events.HasEventFired(eventId);
}

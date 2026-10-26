using System;
using System.Collections.Generic;
using DefenseManager.Core;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;

namespace DefenseManager.Adapters;

/// <summary>
/// The game's side of the power pass: the home's outdoor defenses with their power switches, the zombie wave, the
/// living zombies' positions, and the game clock. A switch goes through PowerManager.SetConsumerSwitch, the call the
/// defense's own power toggle makes. Each call fails soft: an exception is logged once per kind of call and answered
/// as nothing read or nothing switched, and one defense that cannot be read is left out while the rest are not.
/// </summary>
internal sealed class GamePower : IPowerGame
{
    private readonly Action<string> _log;
    private readonly HashSet<string> _reportedFailures = new();

    // The pass runs twice a second; a type's name is a table lookup and a translation, so each is kept per table and
    // language. A type that has no power switch is kept as null and its defenses are not read at all.
    private readonly RowCache<TableKey, string> _names = new();

    // The zombies are read only if the pass asks about them (see PowerControl), from the game objects of the latest read.
    private readonly Func<IReadOnlyList<Spot>> _readZombies;
    private readonly Func<bool> _hasZombies;
    private AgentManager? _agents;
    private TowerWaveManager? _waves;

    public GamePower(Action<string> log)
    {
        _log = log;
        _readZombies = ReadZombies;
        _hasZombies = HasZombies;
    }

    public PowerScene? ReadPower()
    {
        try
        {
            return ReadScene();
        }
        catch (Exception ex)
        {
            ReportOnce("power read", ex);

            return null;
        }
    }

    /// <summary>
    /// The game ignores the call while its power switches are locked, so the switch is read back rather than assumed.
    /// </summary>
    public bool? TrySwitch(long defenseId, bool isOn)
    {
        try
        {
            var power = GameSingleton.Existing<BattleLogicWorld>()?._PowerManager;

            if (power == null) return null;

            power.SetConsumerSwitch(defenseId, isOn);

            return power.IsConsumerOn(defenseId);
        }
        catch (Exception ex)
        {
            ReportOnce("power switch", ex);

            return null;
        }
    }

    /// <summary>Null with no game loaded, in the prologue, or before the game's power system exists.</summary>
    private PowerScene? ReadScene()
    {
        var game = GameDefenses.LoadedGame();

        if (game == null) return null;

        var (world, config, agents, _) = game.Value;
        var power = world._PowerManager;
        var clock = world._GameTimeManager;
        var waves = world._TowerWaveManager;

        if (power == null || clock == null) return null;

        _agents = agents;
        _waves = waves;

        return new PowerScene(ReadUnits(agents, config, power), waves?.IsWaveActive() ?? false, new LazyList<Spot>(_readZombies), clock.GetTotalSeconds(), power.IsLocked, ReadLoad(world)) { HasZombies = _hasZombies };
    }

    /// <summary>
    /// The load, by the addresses of the game objects behind it. The world and the save data are both taken because it
    /// is not known whether the game builds a new world for every load, while a load reads its save into new save data.
    /// </summary>
    internal static GameLoad ReadLoad(BattleLogicWorld world)
    {
        var save = GameSingleton.Existing<GameSaveManager>()?.gameSave;

        return new GameLoad(world.Pointer.ToInt64(), save?.Pointer.ToInt64() ?? 0);
    }

    private List<PowerUnit> ReadUnits(AgentManager agents, ConfigManager config, PowerManager power)
    {
        var found = agents.GetHomeFurnituresBySlotType(GameDefenses.DefenseSlot);
        var units = new List<PowerUnit>();

        _names.Follow(new TableKey(config.Pointer.ToInt64(), GameLanguage.IsChinese(), IconIndex.Empty));

        for (var i = 0; found != null && i < found.Count; i++)
        {
            var furniture = found[i];

            if (furniture == null || furniture._IsDead_k__BackingField) continue;

            try
            {
                if (ReadUnit(furniture, config, power) is { } unit) units.Add(unit);
            }
            catch (Exception ex)
            {
                ReportOnce("power defense read", ex);
            }
        }

        return units;
    }

    /// <summary>
    /// A defense with no durability to read counts as broken, so it is never switched. Only electrical defenses are
    /// read: the pass switches nothing else, and a power button on any other defense answers that it has no switch,
    /// which is what a defense left out of the read answers too.
    /// </summary>
    private PowerUnit? ReadUnit(Furniture furniture, ConfigManager config, PowerManager power)
    {
        var configId = furniture._AgentConfigId_k__BackingField;

        if (NameOfSwitched(config, configId) is not { } name) return null;

        var id = furniture._InstanceId_k__BackingField;
        var durability = AgentTools.GetAgentComponent<FurnitureDurabilityComponent>(furniture);
        var device = AgentTools.GetAgentComponent<TowerDeviceComponent>(furniture);
        var isBroken = durability == null || durability.IsBrokenTowerDevice();
        var isOperational = device != null && device.IsOperational();

        return new PowerUnit(id, configId, name, GameDefenses.PositionOf(furniture), IsElectrical: true, isBroken, isOperational, power.IsConsumerOn(id));
    }

    /// <summary>The name of an electrical defense type, or null for a type that is not one.</summary>
    private string? NameOfSwitched(ConfigManager config, int configId)
    {
        if (_names.TryGet(configId, out var known)) return known;

        var row = GameDefenses.FurnitureRow(config, configId);
        var isSwitched = row != null && row.SlotType == GameDefenses.DefenseSlot && row.IsElectrical;

        return _names.Add(configId, isSwitched ? GameDefenses.NameOf(config, row!, configId) : null);
    }

    /// <summary>
    /// The living zombies at the base, read from the id list the wave manager keeps of them, which takes in stray
    /// zombies spawned between waves as well as a wave's (GetAliveZombieIds hands a list out as
    /// an IL2CPP IReadOnlyList, which is clumsier to walk from here). A zombie that is gone, dead or has no position is
    /// skipped, never put at the map's origin where it would wake the defenses there. A read that throws stops the
    /// pass, as a failed read of the rest of the scene does: switching on a guess about the zombies could turn a
    /// defense off with zombies at the door.
    /// </summary>
    private IReadOnlyList<Spot> ReadZombies()
    {
        try
        {
            var ids = _waves?.aliveZombieIds;
            var spots = new List<Spot>();

            for (var i = 0; _agents != null && ids != null && i < ids.Count; i++)
            {
                var zombie = _agents.GetAgent(ids[i]);

                if (zombie == null || zombie._IsDead_k__BackingField) continue;

                var transform = AgentTools.GetAgentComponent<TransformComponent>(zombie);

                if (transform == null) continue;

                var position = transform.Position;
                spots.Add(new Spot(position.x, position.y, position.z));
            }

            return spots;
        }
        catch (Exception ex)
        {
            ReportOnce("zombie read", ex);

            throw;
        }
    }

    /// <summary>
    /// Whether <see cref="ReadZombies"/> would find any zombie, without reading positions: it skips the same zombies and
    /// stops at the first one it would keep. A read that throws stops the pass, as there.
    /// </summary>
    private bool HasZombies()
    {
        try
        {
            var ids = _waves?.aliveZombieIds;

            for (var i = 0; _agents != null && ids != null && i < ids.Count; i++)
            {
                var zombie = _agents.GetAgent(ids[i]);

                if (zombie != null && !zombie._IsDead_k__BackingField && AgentTools.GetAgentComponent<TransformComponent>(zombie) != null) return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            ReportOnce("zombie read", ex);

            throw;
        }
    }

    private void ReportOnce(string operation, Exception ex)
    {
        if (_reportedFailures.Add(operation)) _log($"{operation} failed (each kind reported once): {ex}");
    }
}

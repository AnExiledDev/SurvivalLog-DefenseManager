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

    public GamePower(Action<string> log)
    {
        _log = log;
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

        return new PowerScene(ReadUnits(agents, config, power), waves?.IsWaveActive() ?? false, ReadZombies(agents, waves), clock.GetTotalSeconds(), power.IsLocked);
    }

    private List<PowerUnit> ReadUnits(AgentManager agents, ConfigManager config, PowerManager power)
    {
        var found = agents.GetHomeFurnituresBySlotType(GameDefenses.DefenseSlot);
        var units = new List<PowerUnit>();

        for (var i = 0; found != null && i < found.Count; i++)
        {
            var furniture = found[i];

            if (furniture == null || furniture.IsDead) continue;

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

    /// <summary>A defense with no durability to read counts as broken, so it is never switched.</summary>
    private static PowerUnit? ReadUnit(Furniture furniture, ConfigManager config, PowerManager power)
    {
        var configId = furniture.AgentConfigId;
        var row = config.Get_Config_Furniture(configId);

        if (row == null || row.SlotType != GameDefenses.DefenseSlot) return null;

        var id = furniture.InstanceId;
        var isElectrical = row.IsElectrical;
        var durability = AgentTools.GetAgentComponent<FurnitureDurabilityComponent>(furniture);
        var device = AgentTools.GetAgentComponent<TowerDeviceComponent>(furniture);
        var isBroken = durability == null || durability.IsBrokenTowerDevice();
        var isOperational = device != null && device.IsOperational();

        return new PowerUnit(id, configId, GameDefenses.NameOf(config, row, configId), GameDefenses.PositionOf(furniture), isElectrical, isBroken, isOperational, isElectrical && power.IsConsumerOn(id));
    }

    /// <summary>
    /// The wave's living zombies, read from the id list the wave keeps of them (GetAliveZombieIds hands a list out as
    /// an IL2CPP IReadOnlyList, which is clumsier to walk from here). A zombie that is gone, dead or has no position is
    /// skipped, never put at the map's origin where it would wake the defenses there.
    /// </summary>
    private static List<Spot> ReadZombies(AgentManager agents, TowerWaveManager? waves)
    {
        var ids = waves?.aliveZombieIds;
        var spots = new List<Spot>();

        for (var i = 0; ids != null && i < ids.Count; i++)
        {
            var zombie = agents.GetAgent(ids[i]);

            if (zombie == null || zombie.IsDead) continue;

            var transform = AgentTools.GetAgentComponent<TransformComponent>(zombie);

            if (transform == null) continue;

            var position = transform.Position;
            spots.Add(new Spot(position.x, position.y, position.z));
        }

        return spots;
    }

    private void ReportOnce(string operation, Exception ex)
    {
        if (_reportedFailures.Add(operation)) _log($"{operation} failed (each kind reported once): {ex}");
    }
}

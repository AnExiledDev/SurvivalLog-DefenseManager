using System;
using System.Collections.Generic;
using System.Linq;

namespace DefenseManager.Core;

/// <summary>
/// One outdoor defense as the power pass sees it. Only an electrical defense that is whole and working is switched;
/// the rest are left as the game has them.
/// </summary>
public sealed record PowerUnit(long Id, int ConfigId, string Name, Spot Position, bool IsElectrical, bool IsBroken, bool IsOperational, bool IsOn)
{
    public bool IsManaged => PowerPlanner.IsManaged(IsElectrical, IsBroken, IsOperational);

    public DefenseKey Key => DefenseKey.Of(ConfigId, Position);
}

/// <summary>
/// Which load of a game is running, told apart by the game objects a load builds: its world and its save data. Loading
/// a save again, even the same file, gives a new load whose defenses can reuse the last load's instance ids.
/// </summary>
public readonly record struct GameLoad(long World, long Save);

/// <summary>
/// The home's defenses and what decides their power: whether a zombie wave is running, where the living zombies are,
/// the game clock in game seconds, whether the game's power switches are locked (a switch then does nothing), and
/// which load of the game this is.
/// </summary>
public sealed record PowerScene(IReadOnlyList<PowerUnit> Defenses, bool IsWaveRunning, IReadOnlyList<Spot> Zombies, int Clock, bool IsLocked, GameLoad Load);

/// <summary>The game's power switches. None of these throws: a call that fails answers null.</summary>
public interface IPowerGame
{
    /// <summary>The defenses and their surroundings, or null when no game is loaded.</summary>
    PowerScene? ReadPower();

    /// <summary>Flips the defense's switch the way its own menu does; the state read back afterwards, or null when that failed.</summary>
    bool? TrySwitch(long defenseId, bool isOn);
}

/// <summary>Whether a mode wants the defense on, off, or left to the game (null), and why, for the log.</summary>
public readonly record struct PowerNeed(bool? IsOn, string Reason);

public static class PowerPlanner
{
    /// <summary>How long a Proximity defense stays on after the last zombie was in range: ten minutes on the game clock.</summary>
    public const int HoldSeconds = 600;

    /// <summary>The defenses the mod switches. Any other is left exactly as the game has it.</summary>
    public static bool IsManaged(bool isElectrical, bool isBroken, bool isOperational) => isElectrical && !isBroken && isOperational;

    /// <summary>
    /// Wave only answers any living zombie at the base, not just a wave's: a stray zombie that wanders in between waves
    /// is as much a threat.
    /// </summary>
    public static PowerNeed Need(PowerMode mode, bool isWaveRunning, bool areZombiesAtBase, bool isZombieNear, bool isHeld) => mode switch
    {
        PowerMode.AlwaysOn => new PowerNeed(true, "always on"),
        PowerMode.WaveOnly when isWaveRunning => new PowerNeed(true, "a wave is running"),
        PowerMode.WaveOnly when areZombiesAtBase => new PowerNeed(true, "zombies are at the base"),
        PowerMode.WaveOnly => new PowerNeed(false, "no wave is running and no zombies are at the base"),
        PowerMode.Proximity when isZombieNear => new PowerNeed(true, "a zombie is in range"),
        PowerMode.Proximity when isHeld => new PowerNeed(true, "a zombie was in range less than 10 game minutes ago"),
        PowerMode.Proximity => new PowerNeed(false, "no zombie in range for 10 game minutes"),
        PowerMode.GameDefault => new PowerNeed(null, "left to the game"),
        _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "unknown power mode"),
    };

    /// <summary>Range is measured along the ground: a zombie on a slope or a rooftop above a defense is still next to it.</summary>
    public static bool IsNear(Spot defense, IEnumerable<Spot> zombies, double range) =>
        zombies.Any(zombie => Square(zombie.X - defense.X) + Square(zombie.Z - defense.Z) <= range * range);

    /// <summary>A clock that went back (an earlier save loaded under the same name) ends the hold rather than stretching it.</summary>
    public static bool IsHeld(int? lastNearAt, int clock) => lastNearAt is { } at && clock >= at && clock - at < HoldSeconds;

    private static double Square(double value) => value * value;
}

/// <summary>A switch the pass made, or tried to: the defense, the state it asked for, why, and whether the game took it.</summary>
public sealed record PowerSwitch(PowerUnit Defense, bool IsOn, string Reason, bool IsDone);

/// <summary>
/// The power pass: every managed defense is put in the state its mode wants, and only those not already in it are
/// switched, since turning a defense on costs the grid a tick of power. It remembers, per defense, when a zombie was
/// last in range, for the Proximity hold. That memory is keyed by instance id, which only means one defense within one
/// load, so it is dropped whenever the game is loaded again.
/// </summary>
public sealed class PowerControl
{
    private readonly Dictionary<long, int> _lastNearAt = new();
    private readonly HashSet<long> _failing = new();
    private GameLoad? _load;

    /// <summary>A new save has its own clock and its own defenses, so nothing carries over.</summary>
    public void Forget()
    {
        _lastNearAt.Clear();
        _failing.Clear();
    }

    /// <summary>
    /// One pass. Returns the switches worth logging: each one made, and a switch the game refused, the first time only,
    /// so a defense that will not switch does not fill the log twice a second.
    /// </summary>
    public IReadOnlyList<PowerSwitch> Run(IPowerGame game, PowerPolicy policy, double range)
    {
        var scene = game.ReadPower();

        if (scene == null) return Array.Empty<PowerSwitch>();

        Follow(scene.Load);

        if (scene.IsLocked) return Array.Empty<PowerSwitch>();

        var switches = new List<PowerSwitch>();

        foreach (var unit in scene.Defenses.Where(unit => unit.IsManaged))
        {
            if (Settle(game, scene, unit, policy.ModeOf(unit.Key), range) is { } made) switches.Add(made);
        }

        return switches;
    }

    private void Follow(GameLoad load)
    {
        if (_load != load) Forget();

        _load = load;
    }

    private PowerSwitch? Settle(IPowerGame game, PowerScene scene, PowerUnit unit, PowerMode mode, double range)
    {
        var isNear = PowerPlanner.IsNear(unit.Position, scene.Zombies, range);

        if (isNear) _lastNearAt[unit.Id] = scene.Clock;

        var lastNearAt = _lastNearAt.TryGetValue(unit.Id, out var at) ? at : (int?)null;
        var need = PowerPlanner.Need(mode, scene.IsWaveRunning, scene.Zombies.Count > 0, isNear, PowerPlanner.IsHeld(lastNearAt, scene.Clock));

        if (need.IsOn is not { } isWanted || isWanted == unit.IsOn)
        {
            _failing.Remove(unit.Id);

            return null;
        }

        var isDone = game.TrySwitch(unit.Id, isWanted) == isWanted;
        var isFirstFailure = !isDone && _failing.Add(unit.Id);

        if (isDone) _failing.Remove(unit.Id);
        if (!isDone && !isFirstFailure) return null;

        return new PowerSwitch(unit, isWanted, $"{PowerModes.Label(mode)}, {need.Reason}", isDone);
    }
}

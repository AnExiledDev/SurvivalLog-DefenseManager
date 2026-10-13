using DefenseManager.Core;

namespace DefenseManager.Tests;

/// <summary>
/// The game's power side as the pass sees it: the home's defenses with their switches, the wave, the living zombies,
/// the game clock and the lock on the switches. Like the game, a switch is ignored while the switches are locked,
/// and each time a defense is switched on from off it powers up, which costs the grid; <see cref="PowerUps"/> counts
/// those, so a test can see a pass that switched a defense it had no need to.
/// </summary>
internal sealed class FakePowerGame : IPowerGame
{
    public const int Coil = 36101;
    public const int Lamp = 36102;
    public const int Barricade = 36001;

    public List<PowerUnit> Defenses { get; } = new();

    public bool IsWaveRunning { get; set; }

    public List<Spot> Zombies { get; } = new();

    /// <summary>The game clock, in game seconds.</summary>
    public int Clock { get; set; }

    public bool IsLocked { get; set; }

    public bool IsNoGame { get; set; }

    /// <summary>Which load of the game this is; a test loads the game again by changing it.</summary>
    public GameLoad Load { get; set; } = new(1, 1);

    /// <summary>Defenses whose switch the game will not move, whatever it is asked.</summary>
    public HashSet<long> Refuses { get; } = new();

    /// <summary>How many times each defense was switched on from off.</summary>
    public Dictionary<long, int> PowerUps { get; } = new();

    /// <summary>An electrical tesla coil, whole, working and switched off unless told otherwise.</summary>
    public static PowerUnit Unit(long id, int configId = Coil, double x = 0, double y = 0, double z = 0, bool isOn = false, bool isElectrical = true, bool isBroken = false, bool isOperational = true, string? name = null) =>
        new(id, configId, name ?? "Tesla coil", new Spot(x, y, z), isElectrical, isBroken, isOperational, isOn);

    public bool IsOn(long id) => Defenses.Single(defense => defense.Id == id).IsOn;

    public int PowerUpsOf(long id) => PowerUps.GetValueOrDefault(id);

    public PowerScene? ReadPower() => IsNoGame ? null : new PowerScene(Defenses.ToList(), IsWaveRunning, Zombies.ToList(), Clock, IsLocked, Load);

    public bool? TrySwitch(long defenseId, bool isOn)
    {
        var index = Defenses.FindIndex(defense => defense.Id == defenseId);

        if (index < 0) return null;

        var defense = Defenses[index];

        if (IsLocked || Refuses.Contains(defenseId)) return defense.IsOn;

        if (isOn && !defense.IsOn) PowerUps[defenseId] = PowerUpsOf(defenseId) + 1;

        Defenses[index] = defense with { IsOn = isOn };

        return isOn;
    }
}

/// <summary>The power settings file, which can be made unreadable or refuse a write.</summary>
internal sealed class FakePowerFile : IPowerFile
{
    public string Text { get; set; } = "";

    public bool IsUnreadable { get; set; }

    public bool FailsToWrite { get; set; }

    public string? Read() => IsUnreadable ? null : Text;

    public bool Replace(string text)
    {
        if (FailsToWrite) return false;

        Text = text;

        return true;
    }
}

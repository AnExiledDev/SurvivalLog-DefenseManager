using System;
using System.Linq;

namespace DefenseManager.Core;

/// <summary>How the mod powers an electrical defense.</summary>
public enum PowerMode
{
    AlwaysOn,

    /// <summary>Off between zombie waves, on while one is running.</summary>
    WaveOnly,

    /// <summary>On while a living zombie is in range, and for a while after the last one was.</summary>
    Proximity,

    /// <summary>Hands off: the game's own power switch decides, as if the mod were not there. Shown to the player as "Default".</summary>
    GameDefault,
}

/// <summary>The names a mode goes by: on the panel, and in the config and settings files.</summary>
public static class PowerModes
{
    /// <summary>Every name the config's DefaultMode accepts, the default first.</summary>
    public static readonly string[] ConfigNames = { ConfigName(PowerMode.WaveOnly), ConfigName(PowerMode.AlwaysOn), ConfigName(PowerMode.Proximity), ConfigName(PowerMode.GameDefault) };

    private static readonly PowerMode[] All = { PowerMode.AlwaysOn, PowerMode.WaveOnly, PowerMode.Proximity, PowerMode.GameDefault };

    public static string Label(PowerMode mode, bool isChinese = false) => mode switch
    {
        PowerMode.AlwaysOn => Texts.AlwaysOn.Pick(isChinese),
        PowerMode.WaveOnly => Texts.WaveOnly.Pick(isChinese),
        PowerMode.Proximity => Texts.Proximity.Pick(isChinese),
        PowerMode.GameDefault => Texts.GameDefault.Pick(isChinese),
        _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "unknown power mode"),
    };

    public static string ConfigName(PowerMode mode) => mode switch
    {
        PowerMode.AlwaysOn => "AlwaysOn",
        PowerMode.WaveOnly => "WaveOnly",
        PowerMode.Proximity => "Proximity",
        PowerMode.GameDefault => "Default",
        _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "unknown power mode"),
    };

    /// <summary>The mode a config or settings-file name stands for, case aside; null for anything else.</summary>
    public static PowerMode? Parse(string? name)
    {
        var trimmed = name?.Trim();

        return All.Where(mode => string.Equals(ConfigName(mode), trimmed, StringComparison.OrdinalIgnoreCase)).Select(mode => (PowerMode?)mode).FirstOrDefault();
    }
}

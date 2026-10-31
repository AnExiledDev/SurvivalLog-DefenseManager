using System;
using System.Globalization;

namespace DefenseManager.Core;

/// <summary>
/// The config values the panel's Settings tab shows. Key is the panel key's name, "" for none. AttackRate is the saved
/// value, which the game picks up at its next start. AllowDuringWaves lets defenses be repaired, removed, installed and
/// moved while a zombie wave is on. The three Unlocks give the door, window and fortification upgrades from day one.
/// </summary>
public sealed record PanelSettings(
    string Key,
    bool LayoutControls,
    PowerMode DefaultMode,
    float ProximityRange,
    float CrawlerDodge,
    float ArmoredCrawlerDodge,
    float AttackRate,
    bool AllowDuringWaves = false,
    bool DoorUnlocks = false,
    bool WindowUnlocks = false,
    bool FortificationUnlocks = false);

/// <summary>
/// One change from the Settings tab. The page sends a setting's name and the value it wants; a number is pulled into
/// the setting's range and rounded to its step, so a stepper pressed past the end stays at the end.
/// </summary>
public abstract record SettingChange
{
    public const float RangeMin = 1f;
    public const float RangeMax = 100f;
    public const float DodgeStep = 0.05f;
    public const float RateStep = 0.25f;

    private SettingChange()
    {
    }

    public sealed record LayoutControls(bool IsShown) : SettingChange;

    public sealed record DefaultMode(PowerMode Mode) : SettingChange;

    public sealed record ProximityRange(float Meters) : SettingChange;

    public sealed record CrawlerDodge(float Chance) : SettingChange;

    public sealed record ArmoredCrawlerDodge(float Chance) : SettingChange;

    public sealed record AttackRate(float Rate) : SettingChange;

    public sealed record AllowDuringWaves(bool IsAllowed) : SettingChange;

    public sealed record Unlock(UnlockGroup Group, bool IsOn) : SettingChange;

    /// <summary>The change a setting's name and value stand for, or null for a name or value that does not parse.</summary>
    public static SettingChange? Parse(string name, string value) => name switch
    {
        "layoutcontrols" => value switch
        {
            "1" => new LayoutControls(true),
            "0" => new LayoutControls(false),
            _ => null,
        },
        "wavework" => value switch
        {
            "1" => new AllowDuringWaves(true),
            "0" => new AllowDuringWaves(false),
            _ => null,
        },
        "doorunlock" => OnOff(value, isOn => new Unlock(UnlockGroup.Doors, isOn)),
        "windowunlock" => OnOff(value, isOn => new Unlock(UnlockGroup.Windows, isOn)),
        "fortunlock" => OnOff(value, isOn => new Unlock(UnlockGroup.Fortifications, isOn)),
        "defaultmode" => PowerModes.Parse(value) is { } mode ? new DefaultMode(mode) : null,
        "proximityrange" => TryNumber(value, out var meters) ? new ProximityRange((float)Math.Round(Math.Clamp(meters, RangeMin, RangeMax))) : null,
        "crawlerdodge" => TryNumber(value, out var chance) ? new CrawlerDodge(Stepped(chance, DodgeStep, 0f, 1f)) : null,
        "armoredcrawlerdodge" => TryNumber(value, out var chance) ? new ArmoredCrawlerDodge(Stepped(chance, DodgeStep, 0f, 1f)) : null,
        "attackrate" => TryNumber(value, out var rate) ? new AttackRate(Stepped(rate, RateStep, DeviceAttackRate.Slowest, DeviceAttackRate.Fastest)) : null,
        _ => null,
    };

    private static SettingChange? OnOff(string value, Func<bool, SettingChange> change) => value switch
    {
        "1" => change(true),
        "0" => change(false),
        _ => null,
    };

    private static bool TryNumber(string text, out double value) =>
        double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) && double.IsFinite(value);

    /// <summary>Kept in range and rounded to a whole number of steps, so 0.15 does not read back as 0.1500001.</summary>
    private static float Stepped(double value, float step, float min, float max)
    {
        var steps = Math.Round(Math.Clamp(value, min, max) / step, MidpointRounding.AwayFromZero);

        return (float)Math.Round(Math.Clamp(steps * step, min, max), 2);
    }
}

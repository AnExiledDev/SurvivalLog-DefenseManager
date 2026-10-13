using System;
using System.Linq;

namespace DefenseManager.Core;

/// <summary>What a power button on the panel changes: the one defense, or every defense of its type.</summary>
public enum PowerTarget
{
    Defense,
    Type,
}

/// <summary>
/// A press of a power button: moves the setting one step round its cycle and says what it is now. The page sends the
/// defense's instance id; it is turned into the defense's key (or its type) only now, from a fresh read, so the
/// setting lands on the defense that stands there at the moment of the press.
/// </summary>
public static class PowerPress
{
    public const string NoSaveText = "Power settings are kept per save, and this game has no save yet. Save the game first.";
    public const string UnreadableText = "The power settings file could not be read, so nothing was changed. See BepInEx/defense-manager.log.";
    public const string NoGameText = "Load a game first.";
    public const string NoSwitchText = "That defense has no power switch.";

    public static string Cycle(PowerTarget target, long defenseId, PowerScene? scene, PowerSlot slot, PowerMode global)
    {
        if (scene == null) return NoGameText;

        var unit = scene.Defenses.FirstOrDefault(candidate => candidate.Id == defenseId);

        if (unit == null || !unit.IsElectrical) return NoSwitchText;

        var (edit, done) = target switch
        {
            PowerTarget.Defense => CycleDefense(unit, slot, global),
            PowerTarget.Type => CycleType(unit, slot, global),
            _ => throw new ArgumentOutOfRangeException(nameof(target), target, "unknown power target"),
        };

        return edit switch
        {
            SettingsEdit.Saved => done,
            SettingsEdit.NotSaved => done + " It could not be written to the settings file, so it lasts until the game is closed.",
            SettingsEdit.NoSave => NoSaveText,
            SettingsEdit.Unreadable => UnreadableText,
            _ => throw new InvalidOperationException("unknown settings edit " + edit),
        };
    }

    private static (SettingsEdit Edit, string Done) CycleDefense(PowerUnit unit, PowerSlot slot, PowerMode global)
    {
        var next = PowerModes.Next(slot.Settings.DefenseSetting(unit.Key));
        var edit = slot.SetDefense(unit.Key, next);
        var typeMode = new PowerPolicy(slot.Settings, global).TypeMode(unit.ConfigId);
        var done = next is { } mode ? $"{unit.Name} set to {PowerModes.Label(mode)}." : $"{unit.Name} now follows its type setting ({PowerModes.Label(typeMode)}).";

        return (edit, done);
    }

    private static (SettingsEdit Edit, string Done) CycleType(PowerUnit unit, PowerSlot slot, PowerMode global)
    {
        var next = PowerModes.Next(slot.Settings.TypeSetting(unit.ConfigId));
        var edit = slot.SetType(unit.ConfigId, next);
        var done = next is { } mode ? $"Every {unit.Name} set to {PowerModes.Label(mode)}." : $"Every {unit.Name} now follows the global setting ({PowerModes.Label(global)}).";

        return (edit, done);
    }
}

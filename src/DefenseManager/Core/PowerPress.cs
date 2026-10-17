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
    public static string Cycle(PowerTarget target, long defenseId, PowerScene? scene, PowerSlot slot, PowerMode global, bool isChinese = false)
    {
        if (scene == null) return Texts.NoGame.Pick(isChinese);

        var unit = scene.Defenses.FirstOrDefault(candidate => candidate.Id == defenseId);

        if (unit == null || !unit.IsElectrical) return Texts.NoSwitch.Pick(isChinese);

        var (edit, done) = target switch
        {
            PowerTarget.Defense => CycleDefense(unit, slot, global, isChinese),
            PowerTarget.Type => CycleType(unit, slot, global, isChinese),
            _ => throw new ArgumentOutOfRangeException(nameof(target), target, "unknown power target"),
        };

        return edit switch
        {
            SettingsEdit.Saved => done,
            SettingsEdit.NotSaved => Texts.Sentences(isChinese, new[] { done, Texts.NotSaved.Pick(isChinese) }),
            SettingsEdit.NoSave => Texts.NoSave.Pick(isChinese),
            SettingsEdit.Unreadable => Texts.Unreadable.Pick(isChinese),
            _ => throw new InvalidOperationException("unknown settings edit " + edit),
        };
    }

    private static (SettingsEdit Edit, string Done) CycleDefense(PowerUnit unit, PowerSlot slot, PowerMode global, bool isChinese)
    {
        var next = PowerModes.Next(slot.Settings.DefenseSetting(unit.Key));
        var edit = slot.SetDefense(unit.Key, next);
        var typeMode = new PowerPolicy(slot.Settings, global).TypeMode(unit.ConfigId);

        var done = next is { } mode
            ? Texts.DefenseSet.Say(isChinese, unit.Name, PowerModes.Label(mode, isChinese))
            : Texts.DefenseFollowsType.Say(isChinese, unit.Name, PowerModes.Label(typeMode, isChinese));

        return (edit, done);
    }

    private static (SettingsEdit Edit, string Done) CycleType(PowerUnit unit, PowerSlot slot, PowerMode global, bool isChinese)
    {
        var next = PowerModes.Next(slot.Settings.TypeSetting(unit.ConfigId));
        var edit = slot.SetType(unit.ConfigId, next);

        var done = next is { } mode
            ? Texts.TypeSet.Say(isChinese, unit.Name, PowerModes.Label(mode, isChinese))
            : Texts.TypeFollowsGlobal.Say(isChinese, unit.Name, PowerModes.Label(global, isChinese));

        return (edit, done);
    }
}

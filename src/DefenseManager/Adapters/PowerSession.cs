using System;
using DefenseManager.Core;

namespace DefenseManager.Adapters;

/// <summary>
/// The power side of the mod: the pass that switches defenses about twice a second whenever a game is loaded (panel
/// shut and on an expedition included, but not on the Layout screen, where the game's logic is paused), the loaded
/// save's power settings, and the panel's power buttons.
/// </summary>
internal sealed class PowerSession
{
    private const float PassSeconds = 0.5f;
    private const int MaxSwitchesPerFrame = 1;

    private readonly GamePower _game;
    private readonly PowerSlot _slot;
    private readonly PowerControl _control = new();
    private PowerMode _global = PowerMode.WaveOnly;
    private double _range;
    private readonly Action<string> _log;

    private PowerPolicy? _policy;
    private float _nextPassAt;
    private bool _isContinuing;
    private string? _reportedUnreadableSave;

    public PowerSession(GamePower game, PowerSlot slot, Action<string> log)
    {
        _game = game;
        _slot = slot;
        _log = log;
    }

    /// <summary>Called on the main thread at start and after each change; the next pass goes by them.</summary>
    public void Use(PowerMode global, double range)
    {
        _global = global;
        _range = range;
    }

    public void Update(float now, Func<bool> isInLayout)
    {
        if (!_isContinuing)
        {
            if (now < _nextPassAt) return;

            // Kept on its own beat rather than counted from this frame, so it does not drift onto the frames of the
            // page check and the button check (see DefenseSession); a long stall starts the beat again.
            _nextPassAt += PassSeconds;

            if (_nextPassAt <= now) _nextPassAt = now + PassSeconds;
        }

        _isContinuing = false;

        if (isInLayout() || Policy() is not { } policy) return;

        // One switch a frame: each one the game takes recounts its power grid, rebuilds and sends its power panel's data
        // and writes a warning to its log, so a wave start that turns every defense on in one frame can stall it. The
        // rest follow on the next frames.
        var made = _control.Run(_game, policy, _range, MaxSwitchesPerFrame);
        _isContinuing = _control.IsUnfinished;

        for (var i = 0; i < made.Count; i++) _log(Describe(made[i]));
    }

    /// <summary>
    /// The loaded save's settings with the global default, or null when there are none to go by: no save loaded, or a
    /// settings file that cannot be read. Without them the pass does nothing, so a defense set to stay on is never
    /// switched by the global default instead.
    /// </summary>
    public PowerPolicy? Policy()
    {
        if (_slot.Follow(SaveSlot.CurrentKey())) _control.Forget();

        var load = _slot.Load();

        LogLoad(load);

        if (load is not (SettingsLoad.Read or SettingsLoad.AlreadyRead)) return null;

        // Asked for on every pass and every refresh; the settings are replaced, never changed in place, so the policy
        // is made again only when they or the global default are.
        if (_policy is not { } kept || !ReferenceEquals(kept.Settings, _slot.Settings) || kept.Global != _global) _policy = new PowerPolicy(_slot.Settings, _global);

        return _policy;
    }

    /// <summary>A power button press: the setting becomes the mode picked (null for none) and the reply says what it is now.</summary>
    public string Press(PowerTarget target, long defenseId, PowerMode? mode)
    {
        Policy();

        return PowerPress.Set(target, defenseId, mode, _game.ReadPower(), _slot, _global, GameLanguage.IsChinese());
    }

    private void LogLoad(SettingsLoad load)
    {
        switch (load)
        {
            case SettingsLoad.Read:
                _reportedUnreadableSave = null;
                _log($"power settings for save {_slot.Save}: {_slot.Settings.Defenses.Count} defense and {_slot.Settings.Types.Count} type settings");
                break;
            case SettingsLoad.Unreadable when _slot.Save != _reportedUnreadableSave:
                _reportedUnreadableSave = _slot.Save;
                _log($"power settings for save {_slot.Save} could not be read; no defense is switched until they can be");
                break;
        }
    }

    private static string Describe(PowerSwitch made)
    {
        var defense = made.Defense;
        var state = made.IsOn ? "on" : "off";

        return made.IsDone
            ? $"power: {defense.Name} ({defense.Id}) turned {state}, {made.Reason}"
            : $"power: {defense.Name} ({defense.Id}) did not turn {state} ({made.Reason}); the game kept its switch, reported once until it takes";
    }
}

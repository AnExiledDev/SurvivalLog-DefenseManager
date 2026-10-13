using System;
using DefenseManager.Core;
using UnityEngine;

namespace DefenseManager.Adapters;

/// <summary>
/// The panel's game side, run once a frame: its open state, the view it sends to page.js, the bar on the game's Layout
/// screen, the presses the page asks for, and the power pass. Page messages arrive on the web view's thread, so they
/// are queued and handled here, in this frame.
/// </summary>
internal sealed class DefenseSession
{
    private const float RefreshSeconds = 1f;
    private const string FailedStatus = "Defense Manager failed, see BepInEx/defense-manager.log.";

    private readonly GameDefenses _game;
    private readonly PowerSession _power;
    private readonly WebPage _page;
    private readonly KeyCode _panelKey;
    private readonly bool _isLayoutBarEnabled;
    private readonly Action<string> _log;

    private string _shownView = "";
    private float _nextRefreshAt;
    private float _nextLiveCheckAt;
    private bool? _sentLive;
    private bool? _loggedLive;
    private bool _isOpen;
    private bool _hasSentLayout;
    private string? _sentLayout;
    private bool _wasInLayout;
    private bool _hasReportedLayoutFailure;
    private bool _hasReportedPowerFailure;

    public DefenseSession(GameDefenses game, PowerSession power, WebPage page, KeyCode panelKey, bool isLayoutBarEnabled, Action<string> log)
    {
        _game = game;
        _power = power;
        _page = page;
        _panelKey = panelKey;
        _isLayoutBarEnabled = isLayoutBarEnabled;
        _log = log;
    }

    public void Update()
    {
        var now = Time.unscaledTime;
        _page.KeepInstalled(now);

        while (PageMessages.TryTake(out var command)) HandleSafely(command);

        UpdatePower(now);

        if (now >= _nextLiveCheckAt)
        {
            SendLive(now);
            SyncLayout();
        }
        else if (HasLayoutOpenedOrClosed())
        {
            SyncLayout();
        }

        if (_panelKey != KeyCode.None && Input.GetKeyDown(_panelKey))
        {
            Toggle();

            return;
        }

        if (!_isOpen) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Close();

            return;
        }

        if (now >= _nextRefreshAt) PushView();
    }

    /// <summary>A power pass that throws must not cost the panel its frame; the failure is logged once.</summary>
    private void UpdatePower(float now)
    {
        try
        {
            _power.Update(now, _game.IsInLayout);
        }
        catch (Exception ex)
        {
            if (!_hasReportedPowerFailure) _log($"power pass failed (reported once): {ex}");

            _hasReportedPowerFailure = true;
        }
    }

    /// <summary>One command that throws must not cost the rest of the frame its commands or the key checks.</summary>
    private void HandleSafely(PageCommand command)
    {
        try
        {
            Handle(command);
        }
        catch (Exception ex)
        {
            _log($"page command {command} failed: {ex}");
        }
    }

    private void Handle(PageCommand command)
    {
        switch (command)
        {
            case PageCommand.Toggle:
                Toggle();
                break;
            case PageCommand.Close:
                Close();
                break;
            case PageCommand.RepairAll:
                Press("repair all", () => DefenseRun.RepairAll(_game));
                break;
            case PageCommand.ClearBroken:
                Press("clear all broken", () => DefenseRun.ClearBroken(_game));
                break;
            case PageCommand.Repair repair:
                Press($"repair {repair.DefenseId}", () => DefenseRun.Repair(_game, repair.DefenseId));
                break;
            case PageCommand.Remove remove:
                Press($"remove {remove.DefenseId}", () => DefenseRun.Remove(_game, remove.DefenseId));
                break;
            case PageCommand.Power power:
                PressPower(PowerTarget.Defense, power.DefenseId);
                break;
            case PageCommand.PowerType powerType:
                PressPower(PowerTarget.Type, powerType.DefenseId);
                break;
            case PageCommand.Ready:
                _log("page script ready");
                _sentLive = null;
                _hasSentLayout = false;
                _nextLiveCheckAt = 0f;
                ReopenAfterReload();
                break;
            case PageCommand.NoScript:
                _page.Install();
                break;
            case PageCommand.Unknown unknown:
                _log("page said: " + unknown.Text);
                break;
            default:
                throw new InvalidOperationException("unhandled page command " + command);
        }
    }

    /// <summary>
    /// Tells the page whether the player is at home, so the HUD button shows only where it has work (not in the prologue,
    /// not on an expedition), and which key opens the panel. Sent when that changes; a reloaded page starts blank, so its
    /// ready message has it sent again.
    /// </summary>
    private void SendLive(float now)
    {
        _nextLiveCheckAt = now + RefreshSeconds;
        var where = _game.Locate();
        var isLive = Presence.ShowsButton(where);

        LogButtonChange(isLive, where);

        if (isLive == _sentLive) return;

        if (_page.Live(isLive, KeyName())) _sentLive = isLive;
    }

    /// <summary>
    /// Logged when the button's state changes, not when it is sent: a reloaded page or a failed send has it sent again
    /// without anything having changed.
    /// </summary>
    private void LogButtonChange(bool isLive, Where where)
    {
        if (isLive == _loggedLive) return;

        _loggedLive = isLive;
        _log($"HUD button {(isLive ? "shown" : "hidden")} ({where})");
    }

    /// <summary>
    /// Puts the bar on the game's Layout screen while that screen is up and takes it away after. Sent only when its view
    /// changed; a read that throws takes the bar away rather than leave it showing stale counts.
    /// </summary>
    private void SyncLayout()
    {
        var view = ReadLayoutView();

        if (_hasSentLayout && view == _sentLayout) return;
        if (!_page.Layout(view)) return;

        _hasSentLayout = true;
        _sentLayout = view;
    }

    /// <summary>
    /// Checked every frame, so the bar shows, with fresh counts, the frame Layout opens, and goes the frame it closes,
    /// rather than on the next one-second check.
    /// </summary>
    private bool HasLayoutOpenedOrClosed() => _isLayoutBarEnabled && _game.IsInLayout() != _wasInLayout;

    private string? ReadLayoutView()
    {
        try
        {
            _wasInLayout = _game.IsInLayout();

            return LayoutBar.View(_isLayoutBarEnabled, _wasInLayout, _game.Locate(), _game.Read);
        }
        catch (Exception ex)
        {
            if (!_hasReportedLayoutFailure) _log($"reading the defenses for the Layout bar failed (reported once): {ex}");

            _hasReportedLayoutFailure = true;

            return null;
        }
    }

    private string KeyName() => _panelKey == KeyCode.None ? "" : _panelKey.ToString();

    private void Toggle()
    {
        if (_isOpen) Close();
        else Open();
    }

    /// <summary>
    /// The view is read before anything is captured, so a failed read leaves the game's input alone. Away from home (the
    /// title screen, the prologue, an expedition) there is nothing to manage and the panel stays shut without a word.
    /// </summary>
    private void Open()
    {
        if (!TryReadView("open", out var view))
        {
            GiveUpOnFailedRead();

            return;
        }

        if (view == null) return;

        _isOpen = true;
        InputGate.PanelOpened();
        _shownView = view;
        _page.Open(view);
        _log("panel opened");
    }

    private void Close()
    {
        if (!_isOpen) return;

        _isOpen = false;
        InputGate.PanelClosed();
        _page.Close();
    }

    /// <summary>
    /// Sends the view only when it changed, so an idle panel costs one read a second and nothing more. A read that
    /// throws shuts the panel, so the game gets its input back instead of a panel that can no longer update; so does
    /// leaving home, for an expedition or out of the game.
    /// </summary>
    private void PushView()
    {
        if (!TryReadView("refresh", out var view))
        {
            GiveUpOnFailedRead();

            return;
        }

        if (view == null)
        {
            Close();

            return;
        }

        if (view == _shownView) return;

        _shownView = view;
        _page.Show(view);
    }

    /// <summary>A reloaded web view starts with the panel shut, so a panel the game side thinks is open is drawn again.</summary>
    private void ReopenAfterReload()
    {
        if (!_isOpen) return;

        if (!TryReadView("reopen", out var view))
        {
            GiveUpOnFailedRead();

            return;
        }

        if (view == null)
        {
            Close();

            return;
        }

        _shownView = view;
        _page.Open(view);
    }

    /// <summary>A panel that cannot read the defenses is shut rather than left holding the game's input.</summary>
    private void GiveUpOnFailedRead()
    {
        Close();
        _page.Status(FailedStatus);
    }

    /// <summary>False when the read threw; otherwise the view JSON, or null when the player is not at home to manage it.</summary>
    private bool TryReadView(string reason, out string? view)
    {
        try
        {
            view = ReadView();

            return true;
        }
        catch (Exception ex)
        {
            _log($"reading the defenses for {reason} failed: {ex}");
            view = null;

            return false;
        }
    }

    private string? ReadView()
    {
        _nextRefreshAt = Time.unscaledTime + RefreshSeconds;

        return Presence.PanelView(_game.Locate(), _game.Read, _power.Policy());
    }

    /// <summary>
    /// A power button: its reply goes to the status line, which also frees the page's buttons, and the panel is
    /// redrawn so the button shows its new setting at once. The switch itself follows on the next power pass.
    /// </summary>
    private void PressPower(PowerTarget target, long defenseId)
    {
        string message;

        try
        {
            message = _power.Press(target, defenseId);
            _log($"power {target} {defenseId}: {message}");
        }
        catch (Exception ex)
        {
            message = FailedStatus;
            _log($"power {target} {defenseId} failed: {ex}");
        }

        _page.Status(message);

        if (_isOpen) PushView();
    }

    /// <summary>
    /// Runs one button press and shows what it did: in the panel's status line while it is open, as a toast otherwise.
    /// The panel and the Layout bar are redrawn straight after, so a queued badge or a new count shows without waiting
    /// for the next refresh.
    /// </summary>
    private void Press(string name, Func<DefenseOutcome> press)
    {
        string? message;

        try
        {
            var outcome = press();
            message = OutcomeMessage.Describe(outcome, _game.IsInLayout());
            _log($"{name}: {outcome} -> {message ?? "(no message)"}");
        }
        catch (Exception ex)
        {
            message = FailedStatus;
            _log($"{name} failed: {ex}");
        }

        if (message != null) _page.Status(message);
        if (_isOpen) PushView();

        SyncLayout();
    }
}

using System;
using DefenseManager.Core;
using UnityEngine;

namespace DefenseManager.Adapters;

/// <summary>
/// The panel's game side, run once a frame: its open state, the view it sends to page.js, and the presses the page
/// asks for. Page messages arrive on the web view's thread, so they are queued and handled here, in this frame.
/// </summary>
internal sealed class DefenseSession
{
    private const float RefreshSeconds = 1f;
    private const string FailedStatus = "Defense Manager failed, see BepInEx/defense-manager.log.";

    private readonly GameDefenses _game;
    private readonly WebPage _page;
    private readonly KeyCode _panelKey;
    private readonly Action<string> _log;

    private string _shownView = "";
    private float _nextRefreshAt;
    private float _nextLiveCheckAt;
    private bool? _sentLive;
    private bool _isOpen;

    public DefenseSession(GameDefenses game, WebPage page, KeyCode panelKey, Action<string> log)
    {
        _game = game;
        _page = page;
        _panelKey = panelKey;
        _log = log;
    }

    public void Update()
    {
        var now = Time.unscaledTime;
        _page.KeepInstalled(now);

        while (PageMessages.TryTake(out var command)) HandleSafely(command);

        if (now >= _nextLiveCheckAt) SendLive(now);

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
            case PageCommand.Ready:
                _log("page script ready");
                _sentLive = null;
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
    /// Tells the page whether a game is loaded, so the HUD button shows only where it has work, and which key opens the
    /// panel. Sent when that changes; a reloaded page starts blank, so its ready message has it sent again.
    /// </summary>
    private void SendLive(float now)
    {
        _nextLiveCheckAt = now + RefreshSeconds;
        var isLive = _game.IsGameLoaded();

        if (isLive == _sentLive) return;

        if (_page.Live(isLive, KeyName())) _sentLive = isLive;
    }

    private string KeyName() => _panelKey == KeyCode.None ? "" : _panelKey.ToString();

    private void Toggle()
    {
        if (_isOpen) Close();
        else Open();
    }

    /// <summary>
    /// The view is read before anything is captured, so a failed read leaves the game's input alone. Outside a loaded
    /// game (the title screen, the prologue) there is nothing to manage and the panel stays shut without a word.
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
    /// leaving the game.
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

    /// <summary>False when the read threw; otherwise the view JSON, or null when there is no game to manage.</summary>
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
        var read = _game.Read();

        return read switch
        {
            DefenseRead.Ready ready => DefenseView.Build(ready.Survey),
            DefenseRead.NotReady notReady => DefenseView.Empty(OutcomeMessage.Describe(new DefenseOutcome.NotReady(notReady.Reason)) ?? notReady.Reason),
            DefenseRead.NoGame => null,
            _ => throw new InvalidOperationException("unknown defense read " + read),
        };
    }

    /// <summary>
    /// Runs one button press and shows what it did: in the panel's status line while it is open, as a toast otherwise.
    /// The panel is redrawn straight after, so a queued badge shows without waiting for the next refresh.
    /// </summary>
    private void Press(string name, Func<DefenseOutcome> press)
    {
        string? message;

        try
        {
            var outcome = press();
            message = OutcomeMessage.Describe(outcome);
            _log($"{name}: {outcome} -> {message ?? "(no message)"}");
        }
        catch (Exception ex)
        {
            message = FailedStatus;
            _log($"{name} failed: {ex}");
        }

        if (message != null) _page.Status(message);
        if (_isOpen) PushView();
    }
}

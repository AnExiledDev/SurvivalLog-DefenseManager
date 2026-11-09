using System;
using System.Diagnostics;
using DefenseManager.Core;
using ExiledToolkit.Game;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;

namespace DefenseManager.Adapters;

/// <summary>
/// Puts the player's crawler dodge chances into the game's TowerZombieDodgeVsDevice rules (in the base game crawlers
/// never get hit by the chainsaw). RollDodge is the only reader of those rules and re-reads them whenever the text
/// changes, so rewriting the text just before it reads is enough. Static because a Harmony patch has nowhere else to
/// look.
/// </summary>
internal static class DodgeRulesPatch
{
    private const string RulesId = "TowerZombieDodgeVsDevice";

    private static CrawlerDodge _dodge = new(CrawlerDodge.GameChance, CrawlerDodge.GameChance);
    private static readonly DodgeRulesKeeper Keeper = new();
    private static bool _hasFailed;
    private static int _checkedFrame = -1;
    private static long _nextCheckAt;
    private static Action<string> _log = _ => { };
    private static PatchKit? _hooks;

    public static void Apply(PatchKit hooks, Action<string> log)
    {
        _log = log;
        _hooks = hooks;
        hooks.Patch(
            typeof(DodgeRulesPatch),
            typeof(TowerCombatManager),
            nameof(TowerCombatManager.RollDodge),
            prefix: nameof(UsePlayersChances),
            done: "hooked the crawler dodge rules",
            consequence: "crawlers keep the game's dodge chances");
    }

    /// <summary>
    /// Called on the main thread at start and after each change. The rules are written again at the next dodge roll,
    /// always from the game's text (<see cref="DodgeRulesKeeper"/>).
    /// </summary>
    public static void Use(CrawlerDodge dodge)
    {
        _dodge = dodge;
        _checkedFrame = -1;
        _nextCheckAt = 0;
        _log($"crawler dodge chances set: crawler {dodge.Crawler}, armored crawler {dodge.ArmoredCrawler}");
    }

    private static void UsePlayersChances()
    {
        // RollDodge runs for every chainsaw target on every swing, so a write that keeps failing is tried and logged once,
        // and the rules are looked at no more than once a frame: each look is several calls into the game and copies the
        // rules text. Once they hold the player's chances they are looked at again only once a second, in case the game
        // loaded its own text back.
        if (_hasFailed) return;

        try
        {
            var now = Stopwatch.GetTimestamp();

            if (now < _nextCheckAt) return;

            var frame = UnityFrame.Count();

            if (frame == _checkedFrame) return;

            _checkedFrame = frame;

            var rules = GameSingleton.Existing<ConfigManager>()?.TryGet_Config_GlobalSetting(RulesId);
            var current = rules?.Params3;

            if (rules == null || string.IsNullOrEmpty(current)) return;

            _nextCheckAt = now + Stopwatch.Frequency;

            if (Keeper.Next(current, _dodge) is not { } written) return;

            rules.Params3 = written;
            _log($"dodge rules {current} -> {written}");
        }
        catch (Exception ex)
        {
            _hasFailed = true;
            _hooks?.Fail("setting the crawler dodge chances, the game's apply", ex);
        }
    }
}

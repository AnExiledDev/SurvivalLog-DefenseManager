using System;
using DefenseManager.Core;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;
using HarmonyLib;

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

    private static CrawlerDodge _dodge = new(0f, 0f);
    private static string? _written;
    private static bool _hasFailed;
    private static int _checkedFrame = -1;
    private static Action<string> _log = _ => { };

    public static void Apply(Harmony harmony, Action<string> log)
    {
        _log = log;

        try
        {
            harmony.Patch(
                AccessTools.Method(typeof(TowerCombatManager), nameof(TowerCombatManager.RollDodge)),
                prefix: new HarmonyMethod(typeof(DodgeRulesPatch), nameof(UsePlayersChances)));
            log("hooked the crawler dodge rules");
        }
        catch (Exception ex)
        {
            log($"could not hook TowerCombatManager.RollDodge, crawlers keep the game's dodge chances: {ex.Message}");
        }
    }

    /// <summary>
    /// Called on the main thread at start and after each change. The rules are written again at the next dodge roll;
    /// the rewrite replaces every crawler's chance, so it works on rules this mod already wrote.
    /// </summary>
    public static void Use(CrawlerDodge dodge)
    {
        _dodge = dodge;
        _written = null;
        _checkedFrame = -1;
        _log($"crawler dodge chances set: crawler {dodge.Crawler}, armored crawler {dodge.ArmoredCrawler}");
    }

    private static void UsePlayersChances()
    {
        // RollDodge runs for every chainsaw target on every swing, so a write that keeps failing is tried and logged once,
        // and the rules are looked at no more than once a frame: each look is several calls into the game.
        if (_hasFailed) return;

        try
        {
            var frame = UnityEngine.Time.frameCount;

            if (frame == _checkedFrame) return;

            _checkedFrame = frame;

            var rules = GameSingleton.Existing<ConfigManager>()?.TryGet_Config_GlobalSetting(RulesId);
            var current = rules?.Params3;

            if (rules == null || string.IsNullOrEmpty(current) || current == _written) return;

            _written = _dodge.Rewrite(current);
            rules.Params3 = _written;
            _log($"dodge rules {current} -> {_written}");
        }
        catch (Exception ex)
        {
            _hasFailed = true;
            _log($"could not set the crawler dodge chances, the game's own apply: {ex.Message}");
        }
    }
}

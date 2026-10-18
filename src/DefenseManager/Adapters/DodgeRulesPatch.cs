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
    private static Action<string> _log = _ => { };

    public static void Apply(Harmony harmony, CrawlerDodge dodge, Action<string> log)
    {
        _dodge = dodge;
        _log = log;

        try
        {
            harmony.Patch(
                AccessTools.Method(typeof(TowerCombatManager), nameof(TowerCombatManager.RollDodge)),
                prefix: new HarmonyMethod(typeof(DodgeRulesPatch), nameof(UsePlayersChances)));
            log($"crawler dodge chances set: crawler {dodge.Crawler}, armored crawler {dodge.ArmoredCrawler}");
        }
        catch (Exception ex)
        {
            log($"could not hook TowerCombatManager.RollDodge, crawlers keep the game's dodge chances: {ex.Message}");
        }
    }

    private static void UsePlayersChances()
    {
        try
        {
            var rules = GameSingleton.Existing<ConfigManager>()?.TryGet_Config_GlobalSetting(RulesId);
            var current = rules?.Params3;

            if (rules == null || string.IsNullOrEmpty(current) || current == _written) return;

            _written = _dodge.Rewrite(current);
            rules.Params3 = _written;
            _log($"dodge rules {current} -> {_written}");
        }
        catch (Exception ex)
        {
            _log($"could not set the crawler dodge chances, the game's own apply: {ex.Message}");
        }
    }
}

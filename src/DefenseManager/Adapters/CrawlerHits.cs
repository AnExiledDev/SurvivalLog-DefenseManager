using System;
using GameCore.HotUpdate.Battle.Logic;
using HarmonyLib;

namespace DefenseManager.Adapters;

/// <summary>
/// Stops zombies from dodging defense hits. The game's TowerZombieDodgeVsDevice table gives crawlers a dodge chance
/// against some defenses (crawlers ignore the chainsaw completely), and RollDodge is the one place it is read, so
/// answering "no dodge" there makes every damaging defense hit them. Static because a Harmony patch has nowhere else
/// to look.
/// </summary>
internal static class CrawlerHits
{
    public static void Apply(Harmony harmony, Action<string> log)
    {
        try
        {
            harmony.Patch(
                AccessTools.Method(typeof(TowerCombatManager), nameof(TowerCombatManager.RollDodge)),
                prefix: new HarmonyMethod(typeof(CrawlerHits), nameof(NeverDodge)));
            log("defenses hit crawlers: zombie dodges against defenses are off");
        }
        catch (Exception ex)
        {
            log($"could not hook TowerCombatManager.RollDodge, crawlers keep dodging some defenses: {ex.Message}");
        }
    }

    private static bool NeverDodge(ref bool __result)
    {
        __result = false;

        return false;
    }
}

using System;
using DefenseManager.Core;
using GameCore.HotUpdate.Battle.Logic;
using HarmonyLib;

namespace DefenseManager.Adapters;

/// <summary>
/// A defense device reads its time between strikes from its config when it is set up, and every later strike timer
/// is rolled from that base, so shortening the base once there covers every strike. Its wreck damage interval is set
/// in the same place and shortened the same way. Static because a Harmony patch has nowhere else to look.
/// </summary>
internal static class AttackRatePatch
{
    private static DeviceAttackRate _rate = DeviceAttackRate.From(1f);
    private static bool _hasFailed;
    private static Action<string> _log = _ => { };

    public static void Apply(Harmony harmony, DeviceAttackRate rate, Action<string> log)
    {
        _rate = rate;
        _log = log;

        if (rate.IsGameRate)
        {
            log("device attack rate left at the game's");

            return;
        }

        try
        {
            harmony.Patch(
                AccessTools.Method(typeof(TowerDeviceComponent), nameof(TowerDeviceComponent.OnInit)),
                postfix: new HarmonyMethod(typeof(AttackRatePatch), nameof(Shorten)));
            log($"device attack rate x{rate.Value:0.##}");
        }
        catch (Exception ex)
        {
            log($"could not hook TowerDeviceComponent.OnInit, devices strike at the game's rate: {ex.Message}");
        }
    }

    private static void Shorten(TowerDeviceComponent __instance)
    {
        if (_hasFailed || __instance == null) return;

        try
        {
            __instance._AttackIntervalSeconds_k__BackingField = _rate.Interval(__instance._AttackIntervalSeconds_k__BackingField);
            __instance._CurrentIntervalSeconds_k__BackingField = _rate.Interval(__instance._CurrentIntervalSeconds_k__BackingField);
            __instance._WreckIntervalSeconds_k__BackingField = _rate.Interval(__instance._WreckIntervalSeconds_k__BackingField);
        }
        catch (Exception ex)
        {
            _hasFailed = true;
            _log($"could not change a device's attack rate, devices strike at the game's rate: {ex.Message}");
        }
    }
}

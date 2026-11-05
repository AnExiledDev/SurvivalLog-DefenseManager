using System;
using DefenseManager.Core;
using GameCore.HotUpdate.Battle.Logic;
using HarmonyLib;

namespace DefenseManager.Adapters;

/// <summary>
/// A defense device reads its time between strikes from its config when it is set up, and every later strike timer
/// is rolled from that base, so shortening the base once there covers every strike. Its wreck damage interval is set
/// in the same place and shortened the same way. A new rate rescales every device already set up, from the rate it
/// was set up at, and devices set up later take the new rate. Static because a Harmony patch has nowhere else to look.
/// Main thread: the game sets devices up there and live settings apply there.
/// </summary>
internal static class AttackRatePatch
{
    private static DeviceAttackRate _rate = DeviceAttackRate.From(1f);
    private static bool _isHooked;
    private static bool _hasFailed;
    private static bool _hasRescaleFailed;
    private static Action<string> _log = _ => { };

    public static void Apply(Harmony harmony, Action<string> log)
    {
        _log = log;

        try
        {
            harmony.Patch(
                AccessTools.Method(typeof(TowerDeviceComponent), nameof(TowerDeviceComponent.OnInit)),
                postfix: new HarmonyMethod(typeof(AttackRatePatch), nameof(Shorten)));
            _isHooked = true;
        }
        catch (Exception ex)
        {
            log($"could not hook TowerDeviceComponent.OnInit, devices strike at the game's rate: {ex.Message}");
        }
    }

    /// <summary>Puts a rate in use: the devices already built change to it at once, and new ones take it.</summary>
    public static void Use(DeviceAttackRate rate)
    {
        var from = _rate;

        // The panel applies its own change and the live watch then applies it again; the second one changes nothing.
        if (from.Value == rate.Value) return;

        _rate = rate;

        if (!_isHooked || _hasFailed) return;

        var changed = Rescale(from, rate);

        _log((rate.IsGameRate ? "device attack rate at the game's" : $"device attack rate x{rate.Value:0.##}") +
            (changed > 0 ? $", {changed} built devices changed to it" : ""));
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

    /// <summary>
    /// Every device in the loaded world was set up at <paramref name="from"/>, by <see cref="Shorten"/>. The strike timer
    /// in progress is rescaled too, so the next strike already comes at the new rate.
    /// </summary>
    private static int Rescale(DeviceAttackRate from, DeviceAttackRate to)
    {
        if (_hasRescaleFailed) return 0;

        var agents = GameSingleton.Existing<BattleLogicWorld>()?._AgentManager?._agentList_k__BackingField;

        if (agents == null) return 0;

        var changed = 0;

        try
        {
            foreach (var agent in agents)
            {
                if (agent == null) continue;

                var device = AgentTools.GetAgentComponent<TowerDeviceComponent>(agent);

                if (device == null) continue;

                device._AttackIntervalSeconds_k__BackingField = to.Rescale(device._AttackIntervalSeconds_k__BackingField, from);
                device._CurrentIntervalSeconds_k__BackingField = to.Rescale(device._CurrentIntervalSeconds_k__BackingField, from);
                device._WreckIntervalSeconds_k__BackingField = to.Rescale(device._WreckIntervalSeconds_k__BackingField, from);
                changed++;
            }
        }
        catch (Exception ex)
        {
            // A device half way between two rates cannot be rescaled again, so later changes reach new devices only.
            _hasRescaleFailed = true;
            _log($"could not change the attack rate of the devices already built, they keep their rate until the game restarts: {ex.Message}");
        }

        return changed;
    }
}

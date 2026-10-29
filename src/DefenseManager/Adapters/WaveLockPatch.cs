using System;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;
using HarmonyLib;

namespace DefenseManager.Adapters;

/// <summary>
/// Lets the player work on defenses during a zombie wave when the option is on. The game's one lock test,
/// TowerWaveManager.IsWaveOperationLocked, is too short to hook and also keeps expeditions, robot repairs and the
/// wave's own events in line, so each place that locks defense work is opened on its own instead: the install and
/// move checks (InstallPlanManager.IsTowerSlotLockedByWave, read by the Layout screen and by the install and move
/// actions themselves), the removal check behind a defense's Remove button (TowerDefensePlanBridge's wave query), and
/// the Repair button, which the game greys out with its wave note. Robot repairs and expeditions keep the game's
/// lock. Static because a Harmony patch has nowhere else to look.
/// </summary>
internal static class WaveLockPatch
{
    private const int RepairFunc = 9121;

    private static Action<string> _log = _ => { };

    /// <summary>True while the player's option lets defense work go on during waves.</summary>
    public static bool AllowsWaves { get; private set; }

    public static void Apply(Harmony harmony, Action<string> log)
    {
        _log = log;
        Hook(harmony, typeof(InstallPlanManager), nameof(InstallPlanManager.IsTowerSlotLockedByWave), prefix: nameof(OpenSlot), what: "installing and moving");
        Hook(harmony, typeof(TowerDefensePlanBridge), nameof(TowerDefensePlanBridge.OnQueryWaveOperationLocked), prefix: nameof(OpenRemoval), what: "removing");
        Hook(harmony, typeof(Furniture), nameof(Furniture.BuildSingleBtnData), postfix: nameof(OpenRepairButton), what: "the Repair button");
    }

    public static void Use(bool allowsWaves)
    {
        AllowsWaves = allowsWaves;
        _log($"defense work during waves {(allowsWaves ? "allowed" : "locked as in the game")}");
    }

    private static void Hook(Harmony harmony, Type type, string method, string what, string? prefix = null, string? postfix = null)
    {
        try
        {
            harmony.Patch(
                AccessTools.Method(type, method),
                prefix: prefix == null ? null : new HarmonyMethod(typeof(WaveLockPatch), prefix),
                postfix: postfix == null ? null : new HarmonyMethod(typeof(WaveLockPatch), postfix));
            _log($"hooked the wave lock on {what}");
        }
        catch (Exception ex)
        {
            _log($"could not hook {type.Name}.{method}, {what} stays locked during waves: {ex.Message}");
        }
    }

    private static bool OpenSlot(ref bool __result)
    {
        if (!AllowsWaves) return true;

        __result = false;

        return false;
    }

    private static bool OpenRemoval(Il2CppSystem.Action<bool> callback)
    {
        if (!AllowsWaves) return true;

        try
        {
            callback?.Invoke(false);
        }
        catch (Exception ex)
        {
            _log($"answering the removal wave check failed: {ex.Message}");
        }

        return false;
    }

    /// <summary>
    /// The game greys the button out and sets its wave note; only a lock carrying that note is lifted, so a later
    /// check that greyed it for its own reason keeps it greyed.
    /// </summary>
    private static void OpenRepairButton(int funcId, BattleSelectBtnData __result)
    {
        if (!AllowsWaves || funcId != RepairFunc || __result == null || !__result.IsLocked) return;

        try
        {
            if (__result.LockTipText != GameKey.Text_TowerDefense_RepairWaveLocked) return;

            __result.IsLocked = false;
            __result.LockTipText = null;
        }
        catch (Exception ex)
        {
            _log($"opening the Repair button during a wave failed: {ex.Message}");
        }
    }
}

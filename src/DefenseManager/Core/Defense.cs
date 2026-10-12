using System.Collections.Generic;

namespace DefenseManager.Core;

/// <summary>
/// One outdoor defense (a trap or barricade, never a door or window) as the game holds it. Icon is the page-relative
/// path of its picture, or null. Cost is what a full repair takes right now, item id to count, and is empty when the
/// repair is free or none is needed. RepairBlockedReason is the game's own reason for refusing the repair (the check
/// behind the Repair button in its menu), null when the game would allow it. The two queued flags say whether the
/// character already has a repair or a removal of this defense lined up.
/// </summary>
public sealed record Defense(
    long Id,
    int ConfigId,
    string Name,
    string? Icon,
    int Current,
    int Max,
    bool IsBroken,
    Spot Position,
    IReadOnlyDictionary<int, int> Cost,
    string? RepairBlockedReason,
    bool IsRepairQueued,
    bool IsRemovalQueued)
{
    public bool IsDamaged => Max > 0 && Current < Max;
}

using System.Collections.Generic;
using System.Linq;

namespace DefenseManager.Core;

/// <summary>One material a repair is short of: the item, its name, and how many more it needs.</summary>
public sealed record Need(int ItemId, string ItemName, int Missing);

/// <summary>Whether a defense can be lined up for repair now, and if not, the one reason why.</summary>
public abstract record RepairVerdict
{
    private RepairVerdict()
    {
    }

    public sealed record Allowed : RepairVerdict;

    public sealed record Undamaged : RepairVerdict;

    /// <summary>A repair of it is already in the character's queue.</summary>
    public sealed record AlreadyQueued : RepairVerdict;

    /// <summary>It is queued for removal, and repairing what is about to be taken down would waste the materials.</summary>
    public sealed record QueuedForRemoval : RepairVerdict;

    /// <summary>The game's own check refuses the repair, with its reason.</summary>
    public sealed record Blocked(string Reason) : RepairVerdict;

    /// <summary>The materials left after earlier repairs do not cover it.</summary>
    public sealed record Short(IReadOnlyList<Need> Missing) : RepairVerdict;
}

/// <summary>Decides whether one defense's repair would be taken by the game, so no repair is queued only to be refused when it runs.</summary>
public static class RepairCheck
{
    public static RepairVerdict Judge(Defense defense, MaterialBudget budget, StaminaBudget stamina, Survey survey)
    {
        if (defense.IsRepairQueued) return new RepairVerdict.AlreadyQueued();
        if (defense.IsRemovalQueued) return new RepairVerdict.QueuedForRemoval();
        if (!defense.IsDamaged) return new RepairVerdict.Undamaged();
        if (defense.RepairBlockedReason is { } reason) return new RepairVerdict.Blocked(reason);
        if (!stamina.CoversOne) return new RepairVerdict.Blocked(Texts.OutOfStamina.Say(survey.IsChinese, stamina.PerRepair));

        var missing = budget.Missing(defense.Cost);

        if (missing.Count > 0) return new RepairVerdict.Short(missing.Select(entry => new Need(entry.Key, survey.MaterialName(entry.Key), entry.Value)).ToList());

        return new RepairVerdict.Allowed();
    }
}

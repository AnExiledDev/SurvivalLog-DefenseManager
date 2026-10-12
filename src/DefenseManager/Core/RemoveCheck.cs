namespace DefenseManager.Core;

/// <summary>Whether a defense can be lined up for removal now, and if not, why.</summary>
public abstract record RemoveVerdict
{
    private RemoveVerdict()
    {
    }

    public sealed record Allowed : RemoveVerdict;

    /// <summary>A removal of it is already in the character's queue.</summary>
    public sealed record AlreadyQueued : RemoveVerdict;

    /// <summary>A repair of it is queued; taking it down first would throw that repair's materials away.</summary>
    public sealed record QueuedForRepair : RemoveVerdict;
}

/// <summary>
/// Decides whether one defense may be queued for removal. The game asks the player to confirm a removal in the
/// defense's own menu; the panel's Remove button is the confirmation, so nothing else is asked.
/// </summary>
public static class RemoveCheck
{
    public static RemoveVerdict Judge(Defense defense)
    {
        if (defense.IsRemovalQueued) return new RemoveVerdict.AlreadyQueued();
        if (defense.IsRepairQueued) return new RemoveVerdict.QueuedForRepair();

        return new RemoveVerdict.Allowed();
    }
}

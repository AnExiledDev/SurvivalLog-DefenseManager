using System.Collections.Generic;

namespace DefenseManager.Core;

/// <summary>How one press of a panel button ended.</summary>
public abstract record DefenseOutcome
{
    private DefenseOutcome()
    {
    }

    /// <summary>No game to manage (title screen, loading, prologue); the press is ignored.</summary>
    public sealed record NoGame : DefenseOutcome;

    public sealed record NotReady(string Reason) : DefenseOutcome;

    /// <summary>A zombie wave has the defenses locked, so nothing was touched.</summary>
    public sealed record WaveLocked : DefenseOutcome;

    /// <summary>The character is away from home, where the defenses stand, so nothing was touched.</summary>
    public sealed record AwayFromHome : DefenseOutcome;

    /// <summary>Starting would have thrown away actions the player queued, so nothing was touched.</summary>
    public sealed record QueueBusy : DefenseOutcome;

    /// <summary>No defense needs the job: none damaged for a repair, none broken for a clear.</summary>
    public sealed record NothingToDo(Job Job) : DefenseOutcome;

    /// <summary>The defense a row button named is no longer there.</summary>
    public sealed record Gone : DefenseOutcome;

    public sealed record RepairRefused(string Name, RepairVerdict Verdict) : DefenseOutcome;

    public sealed record RemoveRefused(string Name, RemoveVerdict Verdict) : DefenseOutcome;

    /// <summary>The game would not line up the one job a row button asked for.</summary>
    public sealed record NotQueued(Job Job, string Name) : DefenseOutcome;

    /// <summary>Count jobs were lined up but the game would not start them.</summary>
    public sealed record StartFailed(Job Job, int Count) : DefenseOutcome;

    /// <summary>A row button's one job started.</summary>
    public sealed record Queued(Job Job, string Name) : DefenseOutcome;

    public sealed record Repaired(RepairTally Tally) : DefenseOutcome;

    /// <summary>The square an Install button named is not an owned, empty yard square any more.</summary>
    public sealed record SquareTaken : DefenseOutcome;

    /// <summary>The player has no package of that defense, in the yard or in Package Bank.</summary>
    public sealed record NoPackage : DefenseOutcome;

    /// <summary>The character is already installing something; the game runs one install at a time.</summary>
    public sealed record InstallBusy : DefenseOutcome;

    /// <summary>The package is in Package Bank but every yard package spot is taken, so it could not come out.</summary>
    public sealed record NoFreeSpot(string Name) : DefenseOutcome;

    /// <summary>Package Bank would not give the package back.</summary>
    public sealed record BankFailed(string Name) : DefenseOutcome;

    /// <summary>The game refused the install and showed its own reason. FromBank says the package came out of the bank first.</summary>
    public sealed record NotInstalled(string Name, bool FromBank) : DefenseOutcome;

    /// <summary>The character is on the way to install the defense. FromBank says the package came out of the bank first.</summary>
    public sealed record Installing(string Name, bool FromBank) : DefenseOutcome;

    public sealed record Cleared(ClearTally Tally) : DefenseOutcome;
}

/// <summary>
/// What Repair all did, counted in defenses. Queued started repairing; AlreadyQueued were in the queue from before.
/// The rest of the damaged ones were left, each for one reason: Short lacked materials (named, with what each is
/// missing), Blocked were refused by the game's own check (the first reason kept), BeingRemoved are queued for removal,
/// and Refused were turned down by the game's queue itself.
/// </summary>
public sealed record RepairTally(int Queued, int AlreadyQueued, IReadOnlyList<Shortfall> Short, int Blocked, string? BlockedReason, int BeingRemoved, int Refused);

/// <summary>
/// What Clear all broken did, counted in broken defenses. Queued started coming down; AlreadyQueued were queued for
/// removal from before; BeingRepaired are queued for repair and were kept; Refused were turned down by the game's queue.
/// </summary>
public sealed record ClearTally(int Queued, int AlreadyQueued, int BeingRepaired, int Refused);

/// <summary>A defense left unrepaired for lack of materials, and what it is missing.</summary>
public sealed record Shortfall(string Name, IReadOnlyList<Need> Missing);

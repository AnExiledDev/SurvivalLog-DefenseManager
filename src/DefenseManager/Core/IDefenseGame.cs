namespace DefenseManager.Core;

/// <summary>The two things the player can have the character do to a defense.</summary>
public enum Job
{
    Repair,
    Remove,
}

/// <summary>
/// The game as a defense press sees it. Each call is one read or one thing the player could do by hand from the
/// defense's own menu. None throws: a call that fails answers as if the game had said no.
/// </summary>
public interface IDefenseGame
{
    DefenseRead Read();

    /// <summary>Lines up one repair or removal of the defense for the character, without starting it. False when the game would not take it.</summary>
    bool TryQueue(Job job, long defenseId);

    /// <summary>Starts everything lined up as one batch, the way the game's own menu buttons do. False when that failed.</summary>
    bool TryStart();

    /// <summary>
    /// Takes one banked package of that defense out of Package Bank onto a free yard spot. The new package's id;
    /// <see cref="BankTakeOut.NoFreeSpot"/> when every spot is taken; <see cref="BankTakeOut.Failed"/> when none is
    /// banked, Package Bank is not installed, or it failed.
    /// </summary>
    long TakeOutOfBank(int configId);

    /// <summary>
    /// Has the character install the yard package on the yard square, as picking the square in the game's Layout screen
    /// does. False when the game refused; it then shows its own reason on screen.
    /// </summary>
    bool TryInstall(long bagId, string square);
}

/// <summary>What <see cref="IDefenseGame.TakeOutOfBank"/> answers when no package came out.</summary>
public static class BankTakeOut
{
    public const long Failed = 0;

    public const long NoFreeSpot = -1;
}

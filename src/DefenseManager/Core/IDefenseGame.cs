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
}

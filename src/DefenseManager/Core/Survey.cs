using System.Collections.Generic;

namespace DefenseManager.Core;

/// <summary>A repair material: its name, its picture (page-relative path or null) and how many the player has.</summary>
public sealed record Material(string Name, string? Icon, int Stock);

/// <summary>
/// Everything one press decides from: the home's outdoor defenses, where the character stands, the materials their
/// repairs use, what starting a batch does to the character's queue, whether a zombie wave has defenses locked, and
/// whether the character is at home where the defenses are.
/// </summary>
public sealed record Survey(
    IReadOnlyList<Defense> Defenses,
    Spot Character,
    IReadOnlyDictionary<int, Material> Materials,
    QueueOnStart QueueOnStart,
    bool IsWaveLocked,
    bool IsHome)
{
    /// <summary>A material the game did not describe is still named, so a message never shows a bare id alone.</summary>
    public string MaterialName(int itemId) => Materials.TryGetValue(itemId, out var material) ? material.Name : $"Item {itemId}";

    /// <summary>A material the game did not describe counts as none in stock, so no repair is promised on it.</summary>
    public int Stock(int itemId) => Materials.TryGetValue(itemId, out var material) ? material.Stock : 0;
}

/// <summary>The game's defenses for one press, or why they could not be read.</summary>
public abstract record DefenseRead
{
    private DefenseRead()
    {
    }

    public sealed record Ready(Survey Survey) : DefenseRead;

    public sealed record NotReady(string Reason) : DefenseRead;

    /// <summary>There is no game to manage: the title screen, a save still loading, or the prologue.</summary>
    public sealed record NoGame : DefenseRead;
}

/// <summary>What starting the lined-up actions does to the character's action queue.</summary>
public enum QueueOnStart
{
    /// <summary>The repairs or removals join the end of the queue and everything already in it stays.</summary>
    Appends,

    /// <summary>The queue is replaced, but it holds nothing the player queued: only what the character chose to do itself.</summary>
    ReplacesOwnActions,

    /// <summary>The queue is replaced and actions the player queued would be lost.</summary>
    ReplacesPlayerActions,
}

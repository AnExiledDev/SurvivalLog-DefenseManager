using System;
using System.Collections.Generic;

namespace DefenseManager.Core;

/// <summary>Which of the Unlocks settings are on.</summary>
public sealed record UnlockSettings(bool Doors, bool Windows, bool Fortifications)
{
    public static UnlockSettings None { get; } = new(false, false, false);

    public bool IsOn(UnlockGroup group) => group switch
    {
        UnlockGroup.Doors => Doors,
        UnlockGroup.Windows => Windows,
        UnlockGroup.Fortifications => Fortifications,
        _ => false,
    };
}

public enum UnlockGroup
{
    /// <summary>The door's Advanced Repair and Advanced Reinforce, with the Door Repair Kit and the strengthener recipes.</summary>
    Doors,

    /// <summary>The window's Advanced Repair, Advanced Reinforce and Reinforce Window, with the Window Repair Kit and the strengthener recipes.</summary>
    Windows,

    /// <summary>Reinforce Fortifications on doors and windows, with the Fortification Reinforcement Components recipe.</summary>
    Fortifications,
}

/// <summary>
/// One thing the game hands out in a run: a tag that shows a furniture option, a recipe the craft table knows, or a
/// recipe the craft table may make (a recipe with no craft level needs both).
/// </summary>
public abstract record Grant
{
    public const string MarkerPrefix = "DefenseManager_Gave_";

    private Grant()
    {
    }

    public sealed record Tag(string Name) : Grant
    {
        public override string Key => "Tag_" + Name;
    }

    public sealed record Recipe(int Id) : Grant
    {
        public override string Key => "Recipe_" + Id;
    }

    public sealed record CraftUnlock(int Id) : Grant
    {
        public override string Key => "Craft_" + Id;
    }

    public abstract string Key { get; }

    /// <summary>The tag saved with the run that says the mod gave this, so the mod only ever takes back its own gifts.</summary>
    public Tag Marker => new(MarkerPrefix + Key);
}

/// <summary>
/// A grant, the settings that want it, and where the game itself hands it out: the events whose rewards give it and
/// the tags (such as the work desk manual's) that make the craft table show it. A grant any of those already gave is
/// the game's, and turning a setting off leaves it.
/// </summary>
public sealed record UnlockGrant(Grant Grant, UnlockGroup[] Groups, int[] SourceEvents, string[] SourceTags);

/// <summary>What the game side of the unlocks can do. Markers are plain tags.</summary>
public interface IUnlockGame
{
    bool Has(Grant grant);

    /// <summary>Gives the grants together, so the game shows one new-recipe tip for a batch.</summary>
    void Give(IReadOnlyList<Grant> grants);

    void Take(Grant grant);

    /// <summary>True once the event has started in this run, running or finished.</summary>
    bool HasEventFired(int eventId);
}

/// <summary>
/// Gives the door, window and fortification upgrades from the start of a story run instead of on the game's days
/// (2 and 5 for doors, 13 and 14 for windows, 54 for fortifications), and takes back what it gave when a setting is
/// turned off. The game's own events still fire on their days; their rewards find the grant already there.
/// </summary>
public static class DayOneUnlocks
{
    private const string WorkDeskBook = "WorkDeskBook";

    // The work desk manual's events, which add WorkDeskBook and show the three repair recipes.
    private static readonly int[] ManualEvents = { 40031, 52031, 62031 };
    private static readonly int[] DoorRepairEvents = { 40411, 51501, 62411 };
    private static readonly int[] DoorReinforceEvents = { 40412, 51510, 62412, 40414, 51511, 62414 };
    private static readonly int[] WindowRepairEvents = { 40633, 52633, 62633 };
    private static readonly int[] WindowRecipeEvents = { 40634, 52634, 62634 };
    private static readonly int[] FortificationEvents = { 40415, 51505, 62415 };

    public static IReadOnlyList<UnlockGrant> All { get; } = new[]
    {
        new UnlockGrant(new Grant.Tag("FixDoorAdv"), new[] { UnlockGroup.Doors }, DoorRepairEvents, Array.Empty<string>()),
        new UnlockGrant(new Grant.Tag("UpgradeDoorAdv"), new[] { UnlockGroup.Doors }, DoorReinforceEvents, Array.Empty<string>()),
        new UnlockGrant(new Grant.Recipe(100), new[] { UnlockGroup.Doors }, Join(DoorRepairEvents, ManualEvents), new[] { WorkDeskBook }),

        // The All-Purpose Strengthener is what both the door's and the window's Advanced Reinforce use.
        new UnlockGrant(new Grant.Recipe(110), new[] { UnlockGroup.Doors, UnlockGroup.Windows }, Join(DoorReinforceEvents, ManualEvents), new[] { WorkDeskBook }),
        new UnlockGrant(new Grant.Tag("FixWindowAdv"), new[] { UnlockGroup.Windows }, WindowRepairEvents, Array.Empty<string>()),
        new UnlockGrant(new Grant.Recipe(101), new[] { UnlockGroup.Windows }, Join(WindowRecipeEvents, ManualEvents), new[] { WorkDeskBook }),
        new UnlockGrant(new Grant.Tag("UpgradeDoorAdv2"), new[] { UnlockGroup.Fortifications }, FortificationEvents, Array.Empty<string>()),
        new UnlockGrant(new Grant.CraftUnlock(313), new[] { UnlockGroup.Fortifications }, FortificationEvents, Array.Empty<string>()),
        new UnlockGrant(new Grant.Recipe(313), new[] { UnlockGroup.Fortifications }, FortificationEvents, Array.Empty<string>()),
    };

    /// <summary>
    /// One pass over a loaded story run: gives every grant an enabled setting wants and the run lacks, marking each,
    /// then takes back each marked grant no setting wants any more, unless the game gave it too. Returns a log line per
    /// grant given or taken. Craft unlocks go first, since the game also records the recipe for one.
    /// </summary>
    public static IReadOnlyList<string> Run(IUnlockGame game, UnlockSettings settings, IReadOnlyList<UnlockGrant>? grants = null)
    {
        grants ??= All;

        var lines = new List<string>();
        var missing = new List<UnlockGrant>();

        foreach (var grant in grants)
        {
            if (IsWanted(grant, settings) && !game.Has(grant.Grant)) missing.Add(grant);
        }

        if (missing.Count > 0) Give(game, missing, lines);

        foreach (var grant in grants)
        {
            if (IsWanted(grant, settings) || !game.Has(grant.Grant.Marker)) continue;

            if (GameGave(game, grant))
            {
                lines.Add($"kept {grant.Grant.Key}: the game gave it too");
            }
            else
            {
                game.Take(grant.Grant);
                lines.Add($"took back {grant.Grant.Key}");
            }

            game.Take(grant.Grant.Marker);
        }

        return lines;
    }

    private static void Give(IUnlockGame game, List<UnlockGrant> missing, List<string> lines)
    {
        var crafts = new List<Grant>();
        var others = new List<Grant>();
        var markers = new List<Grant>();

        foreach (var grant in missing)
        {
            if (grant.Grant is Grant.CraftUnlock) crafts.Add(grant.Grant);
        }

        if (crafts.Count > 0) game.Give(crafts);

        foreach (var grant in missing)
        {
            if (grant.Grant is not Grant.CraftUnlock && !game.Has(grant.Grant)) others.Add(grant.Grant);
        }

        if (others.Count > 0) game.Give(others);

        foreach (var grant in missing)
        {
            if (!game.Has(grant.Grant))
            {
                lines.Add($"could not give {grant.Grant.Key}");

                continue;
            }

            markers.Add(grant.Grant.Marker);
            lines.Add($"gave {grant.Grant.Key}");
        }

        if (markers.Count > 0) game.Give(markers);
    }

    private static bool IsWanted(UnlockGrant grant, UnlockSettings settings)
    {
        foreach (var group in grant.Groups)
        {
            if (settings.IsOn(group)) return true;
        }

        return false;
    }

    private static bool GameGave(IUnlockGame game, UnlockGrant grant)
    {
        foreach (var id in grant.SourceEvents)
        {
            if (game.HasEventFired(id)) return true;
        }

        foreach (var tag in grant.SourceTags)
        {
            if (game.Has(new Grant.Tag(tag))) return true;
        }

        return false;
    }

    private static int[] Join(int[] first, int[] second)
    {
        var joined = new int[first.Length + second.Length];

        first.CopyTo(joined, 0);
        second.CopyTo(joined, first.Length);

        return joined;
    }
}

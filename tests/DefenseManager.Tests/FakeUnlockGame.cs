using DefenseManager.Core;

namespace DefenseManager.Tests;

/// <summary>
/// A run's tags and craft table as the unlocks see them. Like the game, a craft unlock also records its recipe, a
/// recipe counts as known when it is recorded or craft-unlocked, and an event counts as fired once started.
/// </summary>
internal sealed class FakeUnlockGame : IUnlockGame
{
    public HashSet<string> Tags { get; } = new();

    public HashSet<int> Recipes { get; } = new();

    public HashSet<int> CraftUnlocks { get; } = new();

    public HashSet<int> FiredEvents { get; } = new();

    /// <summary>Ids the craft table refuses, as the game does for a recipe it cannot find.</summary>
    public HashSet<int> Refused { get; } = new();

    public bool Has(Grant grant) => grant switch
    {
        Grant.Tag tag => Tags.Contains(tag.Name),
        Grant.Recipe recipe => Recipes.Contains(recipe.Id) || CraftUnlocks.Contains(recipe.Id),
        Grant.CraftUnlock craft => CraftUnlocks.Contains(craft.Id),
        _ => false,
    };

    public void Give(IReadOnlyList<Grant> grants)
    {
        foreach (var grant in grants)
        {
            switch (grant)
            {
                case Grant.Tag tag:
                    Tags.Add(tag.Name);
                    break;
                case Grant.Recipe recipe when !Refused.Contains(recipe.Id):
                    Recipes.Add(recipe.Id);
                    break;
                case Grant.CraftUnlock craft when !Refused.Contains(craft.Id):
                    CraftUnlocks.Add(craft.Id);
                    Recipes.Add(craft.Id);
                    break;
            }
        }
    }

    public void Take(Grant grant)
    {
        switch (grant)
        {
            case Grant.Tag tag:
                Tags.Remove(tag.Name);
                break;
            case Grant.Recipe recipe:
                Recipes.Remove(recipe.Id);
                break;
            case Grant.CraftUnlock craft:
                CraftUnlocks.Remove(craft.Id);
                break;
        }
    }

    public bool HasEventFired(int eventId) => FiredEvents.Contains(eventId);

    /// <summary>The game's own tags and recipes, markers left out.</summary>
    public IEnumerable<string> GameTags => Tags.Where(tag => !tag.StartsWith(Grant.MarkerPrefix, StringComparison.Ordinal));

    public IEnumerable<string> Markers => Tags.Where(tag => tag.StartsWith(Grant.MarkerPrefix, StringComparison.Ordinal));
}

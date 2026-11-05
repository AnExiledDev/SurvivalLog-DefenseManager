namespace DefenseManager.Adapters;

/// <summary>
/// With Exiled's Toolkit 0.17.0 or newer, a setting with this tag can be set per world (Customizable Difficulty picks it
/// for a new world, and the Mods screen changes it for the world being played). Every tagged setting must be watched,
/// or the Toolkit leaves it out.
/// </summary>
internal static class WorldRule
{
    public const string Tag = "ModManager.WorldRule";
}

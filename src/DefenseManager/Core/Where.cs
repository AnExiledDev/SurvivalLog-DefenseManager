using System;

namespace DefenseManager.Core;

/// <summary>Where the player is, as far as the defenses go. They stand on the home map and nowhere else.</summary>
public enum Where
{
    /// <summary>The title screen, a save still loading, or the prologue.</summary>
    NoGame,

    /// <summary>A loaded game on another map than home: an expedition.</summary>
    Away,

    /// <summary>A loaded game on the home map, with the defenses.</summary>
    Home,
}

/// <summary>
/// Where the mod shows itself. Everything it does is to the home's defenses, so the HUD button, the panel and the
/// Layout bar are there only at home; on an expedition they would have nothing to offer but a refusal.
/// </summary>
public static class Presence
{
    public static bool ShowsButton(Where where) => IsAtDefenses(where);

    /// <summary>The panel's view, or null when the panel must stay shut or close: anywhere but home. Away, nothing is read.</summary>
    public static string? PanelView(Where where, Func<DefenseRead> read) => IsAtDefenses(where) ? DefenseView.Of(read()) : null;

    private static bool IsAtDefenses(Where where) => where switch
    {
        Where.Home => true,
        Where.Away or Where.NoGame => false,
        _ => throw new ArgumentOutOfRangeException(nameof(where), where, "unknown place"),
    };
}

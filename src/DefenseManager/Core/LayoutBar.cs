using System;

namespace DefenseManager.Core;

/// <summary>
/// The Repair all / Clear all broken bar on the game's Layout screen. It draws from the same view as the panel, so its
/// counts and its locked reason always match the panel's top buttons.
/// </summary>
public static class LayoutBar
{
    /// <summary>The view the bar draws from, or null when it should not show: switched off, not in Layout, or no game.</summary>
    public static string? View(bool isEnabled, bool isInLayout, Func<DefenseRead> read)
    {
        if (!isEnabled || !isInLayout) return null;

        return DefenseView.Of(read());
    }
}

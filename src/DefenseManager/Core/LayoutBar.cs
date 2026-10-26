using System;

namespace DefenseManager.Core;

/// <summary>
/// The Repair all / Clear all broken bar on the game's Layout screen. Its counts and its locked reason are worked out
/// the same way as the panel's top buttons, so they always match, and it shows only where the panel may open. It gets
/// only those, not the panel's whole view.
/// </summary>
public static class LayoutBar
{
    /// <summary>The view the bar draws from, or null when it should not show: switched off, not in Layout, away from home, or no game.</summary>
    public static string? View(bool isEnabled, bool isInLayout, Where where, Func<DefenseRead> read, bool isChinese = false)
    {
        if (!isEnabled || !isInLayout) return null;

        return Presence.BarView(where, read, isChinese);
    }
}

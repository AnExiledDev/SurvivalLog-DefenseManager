using System;

namespace DefenseManager.Core;

/// <summary>
/// The Repair all / Clear all broken bar on the game's Layout screen. It draws from the same view as the panel, so its
/// counts and its locked reason always match the panel's top buttons, and it shows only where the panel may open.
/// </summary>
public static class LayoutBar
{
    /// <summary>The view the bar draws from, or null when it should not show: switched off, not in Layout, away from home, or no game.</summary>
    public static string? View(bool isEnabled, bool isInLayout, Where where, Func<DefenseRead> read, bool isChinese = false)
    {
        if (!isEnabled || !isInLayout) return null;

        return Presence.PanelView(where, read, isChinese: isChinese);
    }
}

using System;
using System.Globalization;

namespace DefenseManager.Core;

/// <summary>
/// What the panel and HUD button scripts ask for. They send "slmod|defensemanager|" followed by a verb and its
/// arguments (a defense id, a row, a power mode, a setting and its value); anything else parses to Unknown so it can
/// be logged rather than acted on (the script's "diag|..." notes land there on purpose).
/// </summary>
public abstract record PageCommand
{
    public const string Prefix = "slmod|defensemanager|";

    /// <summary>What the page sends for "no setting of its own" in place of a power mode's name.</summary>
    public const string NoMode = "none";

    private PageCommand()
    {
    }

    /// <summary>The HUD button was clicked: open the panel, or close it when it is open.</summary>
    public sealed record Toggle : PageCommand;

    public sealed record Close : PageCommand;

    public sealed record RepairAll : PageCommand;

    public sealed record ClearBroken : PageCommand;

    /// <summary>Repair every damaged defense in one row of the yard grid, counted from 1 at the house.</summary>
    public sealed record RepairRow(int Row) : PageCommand;

    public sealed record Repair(long DefenseId) : PageCommand;

    public sealed record Remove(long DefenseId) : PageCommand;

    /// <summary>A power mode picked for that one defense; null lets it follow its type.</summary>
    public sealed record Power(long DefenseId, PowerMode? Mode) : PageCommand;

    /// <summary>A power mode picked for every defense of that defense's type; null lets the type follow the global default.</summary>
    public sealed record PowerType(long DefenseId, PowerMode? Mode) : PageCommand;

    /// <summary>A change on the panel's Settings tab.</summary>
    public sealed record Setting(SettingChange Change) : PageCommand;

    /// <summary>The page script just installed itself.</summary>
    public sealed record Ready : PageCommand;

    /// <summary>The page has no script, usually because the web view reloaded.</summary>
    public sealed record NoScript : PageCommand;

    public sealed record Unknown(string Text) : PageCommand;

    public static bool IsOurs(string? message) => message is not null && message.StartsWith(Prefix, StringComparison.Ordinal);

    /// <summary>Parses a whole message, prefix included.</summary>
    public static PageCommand Parse(string message)
    {
        if (!IsOurs(message)) return new Unknown(message);

        var parts = message.Substring(Prefix.Length).Split('|');

        return (parts[0], parts.Length) switch
        {
            ("toggle", 1) => new Toggle(),
            ("close", 1) => new Close(),
            ("repairall", 1) => new RepairAll(),
            ("clearbroken", 1) => new ClearBroken(),
            ("repairrow", 2) when TryRow(parts[1], out var row) => new RepairRow(row),
            ("repair", 2) when TryId(parts[1], out var id) => new Repair(id),
            ("remove", 2) when TryId(parts[1], out var id) => new Remove(id),
            ("power", 3) when TryId(parts[1], out var id) && TryMode(parts[2], out var mode) => new Power(id, mode),
            ("powertype", 3) when TryId(parts[1], out var id) && TryMode(parts[2], out var mode) => new PowerType(id, mode),
            ("set", 3) when SettingChange.Parse(parts[1], parts[2]) is { } change => new Setting(change),
            ("ready", 1) => new Ready(),
            ("noscript", 1) => new NoScript(),
            _ => new Unknown(message),
        };
    }

    private static bool TryId(string text, out long id) => long.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out id);

    private static bool TryRow(string text, out int row) => int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out row) && row > 0;

    private static bool TryMode(string text, out PowerMode? mode)
    {
        mode = text == NoMode ? null : PowerModes.Parse(text);

        return text == NoMode || mode != null;
    }
}

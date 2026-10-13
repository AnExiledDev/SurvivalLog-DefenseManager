using System;
using System.Globalization;

namespace DefenseManager.Core;

/// <summary>
/// What the panel and HUD button scripts ask for. They send "slmod|defensemanager|" followed by a verb and, for a row
/// button, the defense id; anything else parses to Unknown so it can be logged rather than acted on (the script's
/// "diag|..." notes land there on purpose).
/// </summary>
public abstract record PageCommand
{
    public const string Prefix = "slmod|defensemanager|";

    private PageCommand()
    {
    }

    /// <summary>The HUD button was clicked: open the panel, or close it when it is open.</summary>
    public sealed record Toggle : PageCommand;

    public sealed record Close : PageCommand;

    public sealed record RepairAll : PageCommand;

    public sealed record ClearBroken : PageCommand;

    public sealed record Repair(long DefenseId) : PageCommand;

    public sealed record Remove(long DefenseId) : PageCommand;

    /// <summary>A row's own power button: the next power setting for that one defense.</summary>
    public sealed record Power(long DefenseId) : PageCommand;

    /// <summary>A row's type power button: the next power setting for every defense of that defense's type.</summary>
    public sealed record PowerType(long DefenseId) : PageCommand;

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
            ("repair", 2) when TryId(parts[1], out var id) => new Repair(id),
            ("remove", 2) when TryId(parts[1], out var id) => new Remove(id),
            ("power", 2) when TryId(parts[1], out var id) => new Power(id),
            ("powertype", 2) when TryId(parts[1], out var id) => new PowerType(id),
            ("ready", 1) => new Ready(),
            ("noscript", 1) => new NoScript(),
            _ => new Unknown(message),
        };
    }

    private static bool TryId(string text, out long id) => long.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out id);
}

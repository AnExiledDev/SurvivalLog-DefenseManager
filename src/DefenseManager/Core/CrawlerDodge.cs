using System.Globalization;
using System.Linq;

namespace DefenseManager.Core;

/// <summary>
/// The player's dodge chance for each kind of crawler, from 0 (every hit lands) to 1 (never hit). 1 is also the game's
/// own chance, so a chance of 1 keeps that crawler's rules as the game has them, the older crawler's 0.7 too.
/// </summary>
public sealed record CrawlerDodge(float Crawler, float ArmoredCrawler)
{
    /// <summary>The game's own chance, and the default: installing the mod changes no balance until a chance is set.</summary>
    public const float GameChance = 1f;

    // Zombie type ids from the game's TowerZombieType table. 9008 is the older crawler (same model as the armored
    // one) that the game's own rules still list.
    private const int OldCrawlerId = 9008;
    private const int CrawlerId = 9109;
    private const int ArmoredCrawlerId = 9110;

    /// <summary>
    /// The game's dodge rules ("zombie:device:chance" entries split by ';') with every crawler's chance replaced by
    /// the player's, except where the player's is the game's own (<see cref="GameChance"/>). Other zombies' rules and
    /// entries that do not parse are kept as they are, so the game reads them the same way it did before.
    /// </summary>
    public string Rewrite(string rules) => string.Join(";", rules.Split(';').Select(RewriteEntry));

    private string RewriteEntry(string entry)
    {
        var parts = entry.Trim().Split(':');

        if (parts.Length != 3 || !int.TryParse(parts[0], out var zombie)) return entry;

        var chance = ChanceFor(zombie);

        return chance is { } c ? $"{parts[0]}:{parts[1]}:{c.ToString("0.###", CultureInfo.InvariantCulture)}" : entry;
    }

    private float? ChanceFor(int zombie) => zombie switch
    {
        OldCrawlerId or CrawlerId => Changed(Crawler),
        ArmoredCrawlerId => Changed(ArmoredCrawler),
        _ => null,
    };

    private static float? Changed(float chance) => chance >= GameChance ? null : chance;
}

/// <summary>
/// Which dodge rules text to write. It is always rewritten from the game's own text, kept from the last look that found
/// text this mod did not write, so a chance set back to <see cref="CrawlerDodge.GameChance"/> puts the game's rules
/// back instead of leaving the mod's last ones in place.
/// </summary>
public sealed class DodgeRulesKeeper
{
    private string? _gameRules;
    private string? _ours;

    /// <returns>The text to write over <paramref name="current"/>, or null when it already holds the player's chances.</returns>
    public string? Next(string current, CrawlerDodge dodge)
    {
        if (current != _ours || _gameRules is null) _gameRules = current;

        var wanted = dodge.Rewrite(_gameRules);

        if (wanted == current) return null;

        _ours = wanted;

        return wanted;
    }
}

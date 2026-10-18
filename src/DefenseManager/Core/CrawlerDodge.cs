using System.Globalization;
using System.Linq;

namespace DefenseManager.Core;

/// <summary>
/// The player's dodge chance for each kind of crawler, from 0 (every hit lands) to 1 (never hit).
/// </summary>
public sealed record CrawlerDodge(float Crawler, float ArmoredCrawler)
{
    // Zombie type ids from the game's TowerZombieType table. 9008 is the older crawler (same model as the armored
    // one) that the game's own rules still list.
    private const int OldCrawlerId = 9008;
    private const int CrawlerId = 9109;
    private const int ArmoredCrawlerId = 9110;

    /// <summary>
    /// The game's dodge rules ("zombie:device:chance" entries split by ';') with every crawler's chance replaced by
    /// the player's. Other zombies' rules and entries that do not parse are kept as they are, so the game reads them
    /// the same way it did before.
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
        OldCrawlerId or CrawlerId => Crawler,
        ArmoredCrawlerId => ArmoredCrawler,
        _ => null,
    };
}

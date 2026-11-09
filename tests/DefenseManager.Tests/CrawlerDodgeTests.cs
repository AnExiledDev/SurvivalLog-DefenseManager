using DefenseManager.Core;
using Xunit;

namespace DefenseManager.Tests;

public class CrawlerDodgeTests
{
    // The game's rules as shipped: crawlers never get hit by the chainsaw (36004), the older crawler 70% of the time.
    private const string GameRules = "9008:36004:0.7;9109:36004:1.0;9110:36004:1.0";

    [Fact]
    public void Crawlers_take_the_players_dodge_chance_instead_of_the_games()
    {
        var dodge = new CrawlerDodge(Crawler: 0.25f, ArmoredCrawler: 0.4f);

        Assert.Equal("9008:36004:0.25;9109:36004:0.25;9110:36004:0.4", dodge.Rewrite(GameRules));
    }

    [Fact]
    public void A_chance_of_zero_means_every_hit_lands()
    {
        var dodge = new CrawlerDodge(0f, 0f);

        Assert.Equal("9008:36004:0;9109:36004:0;9110:36004:0", dodge.Rewrite(GameRules));
    }

    [Fact]
    public void Rules_for_other_zombies_and_entries_that_do_not_parse_are_left_alone()
    {
        var dodge = new CrawlerDodge(0.25f, 0.25f);

        Assert.Equal("9104:36001:0.5;broken;9109:36004:0.25", dodge.Rewrite("9104:36001:0.5;broken;9109:36004:1.0"));
    }

    [Fact]
    public void Rewriting_twice_gives_the_same_rules()
    {
        var dodge = new CrawlerDodge(0.25f, 0.4f);
        var once = dodge.Rewrite(GameRules);

        Assert.Equal(once, dodge.Rewrite(once));
    }

    [Fact]
    public void The_default_chances_keep_the_games_rules_the_older_crawlers_too()
    {
        // Installing the mod must change no balance until the player picks a chance.
        var dodge = new CrawlerDodge(CrawlerDodge.GameChance, CrawlerDodge.GameChance);

        Assert.Equal(GameRules, dodge.Rewrite(GameRules));
        Assert.Null(new DodgeRulesKeeper().Next(GameRules, dodge));
    }

    [Fact]
    public void A_chance_set_back_to_the_games_puts_the_games_rules_back()
    {
        var keeper = new DodgeRulesKeeper();
        var lowered = keeper.Next(GameRules, new CrawlerDodge(0.25f, 0.25f));

        Assert.NotNull(lowered);

        var back = keeper.Next(lowered, new CrawlerDodge(CrawlerDodge.GameChance, CrawlerDodge.GameChance));

        Assert.Equal("9008:36004:0.25;9109:36004:0.25;9110:36004:0.25", lowered);
        Assert.Equal(GameRules, back);
    }

    [Fact]
    public void Rules_the_game_loads_again_are_rewritten_and_rules_already_written_are_left_alone()
    {
        var keeper = new DodgeRulesKeeper();
        var dodge = new CrawlerDodge(0.25f, 0.4f);
        var written = keeper.Next(GameRules, dodge);

        Assert.NotNull(written);
        Assert.Null(keeper.Next(written, dodge));
        Assert.Equal(written, keeper.Next(GameRules, dodge));
    }
}

using DefenseManager.Core;
using Xunit;

namespace DefenseManager.Tests;

public class DayOneUnlocksTests
{
    private static readonly UnlockSettings DoorsOn = new(true, false, false);
    private static readonly UnlockSettings WindowsOn = new(false, true, false);
    private static readonly UnlockSettings FortsOn = new(false, false, true);
    private static readonly UnlockSettings DoorsAndWindows = new(true, true, false);

    [Fact]
    public void With_every_setting_off_a_run_is_left_as_the_game_made_it()
    {
        var game = new FakeUnlockGame();

        game.Tags.Add("FixDoorAdv");

        var lines = DayOneUnlocks.Run(game, UnlockSettings.None);

        Assert.Empty(lines);
        Assert.Equal(new[] { "FixDoorAdv" }, game.Tags);
        Assert.Empty(game.Recipes);
    }

    [Fact]
    public void Door_upgrades_show_both_door_options_and_their_two_recipes_and_nothing_for_windows()
    {
        var game = new FakeUnlockGame();

        DayOneUnlocks.Run(game, DoorsOn);

        Assert.Equal(new[] { "FixDoorAdv", "UpgradeDoorAdv" }, game.GameTags.OrderBy(t => t));
        Assert.Equal(new[] { 100, 110 }, game.Recipes.OrderBy(r => r));
        Assert.Empty(game.CraftUnlocks);
    }

    [Fact]
    public void Window_upgrades_include_the_strengthener_that_windows_advanced_reinforce_uses()
    {
        var game = new FakeUnlockGame();

        DayOneUnlocks.Run(game, WindowsOn);

        Assert.Equal(new[] { "FixWindowAdv" }, game.GameTags);
        Assert.Equal(new[] { 101, 110 }, game.Recipes.OrderBy(r => r));
    }

    /// <summary>Recipe 313 has no craft level, so knowing it is not enough: the craft table must also be allowed to make it.</summary>
    [Fact]
    public void Fortifications_make_the_reinforcement_components_craftable_not_only_known()
    {
        var game = new FakeUnlockGame();

        DayOneUnlocks.Run(game, FortsOn);

        Assert.Equal(new[] { "UpgradeDoorAdv2" }, game.GameTags);
        Assert.Contains(313, game.CraftUnlocks);
        Assert.Contains(313, game.Recipes);
    }

    [Fact]
    public void Turning_a_setting_off_takes_back_everything_it_gave()
    {
        var game = new FakeUnlockGame();

        DayOneUnlocks.Run(game, FortsOn);
        DayOneUnlocks.Run(game, UnlockSettings.None);

        Assert.Empty(game.Tags);
        Assert.Empty(game.Recipes);
        Assert.Empty(game.CraftUnlocks);
    }

    [Fact]
    public void What_the_run_already_had_before_the_setting_stays_when_it_is_turned_off()
    {
        var game = new FakeUnlockGame();

        game.Tags.Add("FixDoorAdv");
        game.Recipes.Add(100);

        DayOneUnlocks.Run(game, DoorsOn);
        DayOneUnlocks.Run(game, UnlockSettings.None);

        Assert.Equal(new[] { "FixDoorAdv" }, game.Tags);
        Assert.Equal(new[] { 100 }, game.Recipes);
    }

    [Fact]
    public void The_shared_strengthener_stays_while_the_other_setting_still_wants_it()
    {
        var game = new FakeUnlockGame();

        DayOneUnlocks.Run(game, DoorsAndWindows);
        DayOneUnlocks.Run(game, WindowsOn);

        Assert.Equal(new[] { 101, 110 }, game.Recipes.OrderBy(r => r));
        Assert.Equal(new[] { "FixWindowAdv" }, game.GameTags);

        DayOneUnlocks.Run(game, UnlockSettings.None);

        Assert.Empty(game.Recipes);
        Assert.Empty(game.Tags);
    }

    /// <summary>Day 5's event gives door reinforce and the strengthener; once it has fired, they are the game's to keep.</summary>
    [Fact]
    public void A_grant_the_games_own_event_gave_too_stays_when_the_setting_is_turned_off()
    {
        var game = new FakeUnlockGame();

        DayOneUnlocks.Run(game, DoorsOn);
        game.FiredEvents.Add(40412);

        DayOneUnlocks.Run(game, UnlockSettings.None);

        Assert.Equal(new[] { "UpgradeDoorAdv" }, game.Tags);
        Assert.Equal(new[] { 110 }, game.Recipes);
    }

    [Fact]
    public void Repair_recipes_the_work_desk_manual_shows_stay_when_the_setting_is_turned_off()
    {
        var game = new FakeUnlockGame();

        DayOneUnlocks.Run(game, DoorsAndWindows);
        game.Tags.Add("WorkDeskBook");

        DayOneUnlocks.Run(game, UnlockSettings.None);

        Assert.Equal(new[] { 100, 101, 110 }, game.Recipes.OrderBy(r => r));
        Assert.Equal(new[] { "WorkDeskBook" }, game.Tags);
    }

    [Fact]
    public void A_setting_turned_back_on_after_the_game_kept_a_grant_gives_only_what_is_missing()
    {
        var game = new FakeUnlockGame();

        DayOneUnlocks.Run(game, DoorsOn);
        game.FiredEvents.Add(40412);
        DayOneUnlocks.Run(game, UnlockSettings.None);
        game.FiredEvents.Clear();

        var lines = DayOneUnlocks.Run(game, DoorsOn);

        Assert.Equal(new[] { "gave Tag_FixDoorAdv", "gave Recipe_100" }, lines);

        // The kept grants carry no marker, so even with no event on record they stay the game's.
        DayOneUnlocks.Run(game, UnlockSettings.None);

        Assert.Equal(new[] { "UpgradeDoorAdv" }, game.Tags);
        Assert.Equal(new[] { 110 }, game.Recipes);
    }

    [Fact]
    public void A_second_pass_with_nothing_changed_does_nothing()
    {
        var game = new FakeUnlockGame();

        DayOneUnlocks.Run(game, DoorsAndWindows);

        Assert.Empty(DayOneUnlocks.Run(game, DoorsAndWindows));
    }

    [Fact]
    public void A_grant_the_game_refuses_is_reported_and_never_marked_as_given()
    {
        var game = new FakeUnlockGame();

        game.Refused.Add(313);

        var lines = DayOneUnlocks.Run(game, FortsOn);

        Assert.Contains("could not give Craft_313", lines);
        Assert.Equal(new[] { Grant.MarkerPrefix + "Tag_UpgradeDoorAdv2" }, game.Markers);
    }
}

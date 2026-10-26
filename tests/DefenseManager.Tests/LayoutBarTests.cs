using System.Text.Json;
using DefenseManager.Core;
using Xunit;
using static DefenseManager.Tests.FakeGame;

namespace DefenseManager.Tests;

public class LayoutBarTests
{
    private static FakeGame Home() => new() { Defenses = { Def(1), Def(2, cur: 0, broken: true), Def(3, cur: 100) } };

    private static JsonElement Parse(string? view)
    {
        Assert.NotNull(view);

        return JsonDocument.Parse(view ?? "").RootElement;
    }

    [Fact]
    public void In_Layout_the_bar_counts_what_Repair_all_and_Clear_all_broken_would_take()
    {
        var bar = Parse(LayoutBar.View(isEnabled: true, isInLayout: true, Where.Home, Home().Read));

        Assert.Equal(2, bar.GetProperty("repairAll").GetInt32());
        Assert.Equal(1, bar.GetProperty("clearAll").GetInt32());
        Assert.Equal(JsonValueKind.Null, bar.GetProperty("locked").ValueKind);
    }

    /// <summary>The bar gets its own small view, so its counts, note and locked reason must still be the panel's.</summary>
    [Fact]
    public void The_bar_and_the_panel_never_disagree_on_counts_note_or_locked_reason()
    {
        var wave = Home();
        wave.IsWaveLocked = true;
        var away = Home();
        away.IsHome = false;
        var notReady = Home();
        notReady.NotReadyReason = "no character";
        var queued = new FakeGame { Defenses = { Def(1, cur: 10), Def(2, cur: 0, broken: true), Def(3, cur: 20) } };
        queued.Running.Add(new Act(Job.Repair, 1));

        foreach (var game in new[] { Home(), wave, away, notReady, queued })
        {
            var panel = Parse(Presence.PanelView(Where.Home, game.Read));
            var bar = Parse(LayoutBar.View(isEnabled: true, isInLayout: true, Where.Home, game.Read));

            foreach (var name in new[] { "note", "locked", "repairAll", "clearAll" })
            {
                Assert.Equal(panel.GetProperty(name).GetRawText(), bar.GetProperty(name).GetRawText());
            }
        }
    }

    /// <summary>The bar is sent again whenever its view changes, so a defense losing condition without changing a count must not change it.</summary>
    [Fact]
    public void A_defense_losing_condition_without_changing_a_count_leaves_the_bar_view_as_it_was()
    {
        var before = new FakeGame { Defenses = { Def(1, cur: 50), Def(2, cur: 0, broken: true) } };
        var after = new FakeGame { Defenses = { Def(1, cur: 40), Def(2, cur: 0, broken: true) } };

        Assert.NotEqual(Presence.PanelView(Where.Home, before.Read), Presence.PanelView(Where.Home, after.Read));
        Assert.Equal(LayoutBar.View(true, true, Where.Home, before.Read), LayoutBar.View(true, true, Where.Home, after.Read));
    }

    [Fact]
    public void The_bar_greys_its_buttons_for_the_same_reasons_the_panel_does()
    {
        var waveGame = Home();
        waveGame.IsWaveLocked = true;
        // The map can change between the home check and the read, so the read's own away still greys the buttons.
        var awayGame = Home();
        awayGame.IsHome = false;

        Assert.Equal(Texts.WaveLocked.En, Parse(LayoutBar.View(true, true, Where.Home, waveGame.Read)).GetProperty("locked").GetString());
        Assert.Equal(Texts.AwayFromHome.En, Parse(LayoutBar.View(true, true, Where.Home, awayGame.Read)).GetProperty("locked").GetString());
    }

    [Fact]
    public void A_game_that_cannot_be_read_shows_an_empty_bar_that_says_why()
    {
        var game = Home();
        game.NotReadyReason = "no character";

        var bar = Parse(LayoutBar.View(true, true, Where.Home, game.Read));

        Assert.Equal("Can't manage defenses right now: no character.", bar.GetProperty("note").GetString());
        Assert.Equal(0, bar.GetProperty("repairAll").GetInt32());
    }

    [Fact]
    public void Switched_off_in_the_config_the_bar_never_shows()
    {
        Assert.Null(LayoutBar.View(isEnabled: false, isInLayout: true, Where.Home, Home().Read));
    }

    [Fact]
    public void Outside_Layout_the_bar_is_taken_away()
    {
        Assert.Null(LayoutBar.View(isEnabled: true, isInLayout: false, Where.Home, Home().Read));
    }

    [Fact]
    public void Outside_a_loaded_game_the_bar_is_taken_away()
    {
        var game = Home();
        game.IsNoGame = true;

        Assert.Null(LayoutBar.View(true, true, Where.Home, game.Read));
        Assert.Null(LayoutBar.View(true, true, Where.NoGame, Home().Read));
    }

    [Fact]
    public void On_an_expedition_the_Layout_bar_stays_off()
    {
        Assert.Null(LayoutBar.View(isEnabled: true, isInLayout: true, Where.Away, Home().Read));
    }
}

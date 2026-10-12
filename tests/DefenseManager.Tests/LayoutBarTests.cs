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
        var bar = Parse(LayoutBar.View(isEnabled: true, isInLayout: true, Home().Read));

        Assert.Equal(2, bar.GetProperty("repairAll").GetInt32());
        Assert.Equal(1, bar.GetProperty("clearAll").GetInt32());
        Assert.Equal(JsonValueKind.Null, bar.GetProperty("locked").ValueKind);
    }

    [Fact]
    public void The_bar_and_the_panel_draw_from_the_same_view_so_their_counts_never_disagree()
    {
        var game = Home();

        Assert.Equal(DefenseView.Of(game.Read()), LayoutBar.View(isEnabled: true, isInLayout: true, game.Read));
    }

    [Fact]
    public void The_bar_greys_its_buttons_for_the_same_reasons_the_panel_does()
    {
        var waveGame = Home();
        waveGame.IsWaveLocked = true;
        var awayGame = Home();
        awayGame.IsHome = false;

        Assert.Equal(OutcomeMessage.WaveLockedText, Parse(LayoutBar.View(true, true, waveGame.Read)).GetProperty("locked").GetString());
        Assert.Equal(OutcomeMessage.AwayFromHomeText, Parse(LayoutBar.View(true, true, awayGame.Read)).GetProperty("locked").GetString());
    }

    [Fact]
    public void A_game_that_cannot_be_read_shows_an_empty_bar_that_says_why()
    {
        var game = Home();
        game.NotReadyReason = "no character";

        var bar = Parse(LayoutBar.View(true, true, game.Read));

        Assert.Equal("Can't manage defenses right now: no character.", bar.GetProperty("note").GetString());
        Assert.Equal(0, bar.GetProperty("repairAll").GetInt32());
    }

    [Fact]
    public void Switched_off_in_the_config_the_bar_never_shows()
    {
        Assert.Null(LayoutBar.View(isEnabled: false, isInLayout: true, Home().Read));
    }

    [Fact]
    public void Outside_Layout_the_bar_is_taken_away()
    {
        Assert.Null(LayoutBar.View(isEnabled: true, isInLayout: false, Home().Read));
    }

    [Fact]
    public void Outside_a_loaded_game_the_bar_is_taken_away()
    {
        var game = Home();
        game.IsNoGame = true;

        Assert.Null(LayoutBar.View(true, true, game.Read));
    }
}

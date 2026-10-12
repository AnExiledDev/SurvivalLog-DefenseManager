using DefenseManager.Core;
using Xunit;
using static DefenseManager.Tests.FakeGame;

namespace DefenseManager.Tests;

/// <summary>The defenses are on the home map, so the mod shows itself only there.</summary>
public class PresenceTests
{
    private static FakeGame Home() => new() { Defenses = { Def(1), Def(2, cur: 0, broken: true) } };

    [Fact]
    public void At_home_the_defenses_button_shows()
    {
        Assert.True(Presence.ShowsButton(Where.Home));
    }

    [Fact]
    public void On_an_expedition_the_defenses_button_is_hidden()
    {
        Assert.False(Presence.ShowsButton(Where.Away));
    }

    [Fact]
    public void Outside_a_loaded_game_the_defenses_button_is_hidden()
    {
        Assert.False(Presence.ShowsButton(Where.NoGame));
    }

    [Fact]
    public void At_home_the_panel_shows_the_defenses()
    {
        var game = Home();

        var view = Presence.PanelView(Where.Home, game.Read);

        Assert.NotNull(view);
        Assert.Equal(DefenseView.Of(game.Read()), view);
    }

    /// <summary>The session keeps the panel shut on a null view, and closes an open one at its next refresh.</summary>
    [Fact]
    public void On_an_expedition_there_is_no_panel_view()
    {
        Assert.Null(Presence.PanelView(Where.Away, Home().Read));
    }

    /// <summary>A read that throws would shut the panel with a failure message; away, there is nothing to fail at.</summary>
    [Fact]
    public void On_an_expedition_the_panel_key_does_nothing_even_when_the_defenses_cannot_be_read()
    {
        Assert.Null(Presence.PanelView(Where.Away, () => throw new InvalidOperationException("read on an expedition")));
    }

    [Fact]
    public void Outside_a_loaded_game_the_panel_stays_shut()
    {
        Assert.Null(Presence.PanelView(Where.NoGame, Home().Read));
    }
}

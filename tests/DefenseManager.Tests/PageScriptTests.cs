using DefenseManager.Core;
using Xunit;
using static DefenseManager.Tests.FakeGame;

namespace DefenseManager.Tests;

/// <summary>The bar page.js puts on the game's Layout screen, fed the real core's view the way the plugin feeds it.</summary>
public class PageScriptTests
{
    private static FakeGame Home() => new() { Defenses = { Def(1), Def(2, cur: 0, broken: true), Def(3, cur: 100) } };

    private static string BarView(FakeGame game) => LayoutBar.View(isEnabled: true, isInLayout: true, Where.Home, game.Read) ?? throw new InvalidOperationException("no bar view");

    private static string PanelView(FakeGame game) => DefenseView.Of(game.Read()) ?? throw new InvalidOperationException("no panel view");

    /// <summary>A loaded game with the panel on F7, its Layout screen open and the bar's view sent.</summary>
    private static PageScript InLayout(FakeGame game)
    {
        var page = new PageScript();

        page.Live(true, "F7");
        page.OpenLayout();
        page.Layout(BarView(game));

        return page;
    }

    private static FakeGame InWave()
    {
        var game = Home();
        game.IsWaveLocked = true;

        return game;
    }

    private static string Label(PageScript page, string action) => page.Text($"barButton('{action}').textContent");

    // ===== What the bar shows =====

    [Fact]
    public void In_Layout_the_bar_shows_the_panels_counts_and_a_way_to_the_list()
    {
        var page = InLayout(Home());

        Assert.Equal("Repair all (2)", Label(page, "repairall"));
        Assert.Equal("Clear all broken (1)", Label(page, "clearbroken"));
        Assert.Equal("Defense list (F7)", Label(page, "toggle"));
    }

    [Fact]
    public void A_Layout_page_the_game_draws_after_the_view_arrived_gets_the_bar_on_the_next_check()
    {
        var page = new PageScript();

        page.Live(true, "F7");
        page.Layout(BarView(Home()));
        page.OpenLayout();

        Assert.False(page.Is("bar()"));

        page.Poll();

        Assert.Equal("Repair all (2)", Label(page, "repairall"));
    }

    [Fact]
    public void Leaving_Layout_takes_the_bar_away()
    {
        var page = InLayout(Home());

        Assert.True(page.Is("bar()"));

        page.Layout(null);

        Assert.False(page.Is("bar()"));
    }

    [Fact]
    public void Leaving_the_game_takes_the_bar_away()
    {
        var page = InLayout(Home());

        Assert.True(page.Is("bar()"));

        page.Live(false, "F7");

        Assert.False(page.Is("bar()"));
    }

    /// <summary>Drawing the bar again would reset the game's hover and press state on it every 400 ms.</summary>
    [Fact]
    public void The_page_check_leaves_a_drawn_bar_alone()
    {
        var page = InLayout(Home());

        page.Run("var drawn = bar()");
        page.Poll();
        page.Poll();

        Assert.True(page.Is("bar() === drawn"));
    }

    [Fact]
    public void New_counts_redraw_the_bar()
    {
        var game = Home();
        var page = InLayout(game);

        game.Defenses.Add(Def(4, cur: 0, broken: true));
        page.Layout(BarView(game));

        Assert.Equal("Repair all (3)", Label(page, "repairall"));
        Assert.Equal("Clear all broken (2)", Label(page, "clearbroken"));
    }

    // ===== Pressing it =====

    [Fact]
    public void The_bars_buttons_ask_for_the_same_commands_as_the_panels()
    {
        var page = InLayout(Home());

        page.Click("barButton('repairall')");
        page.Status("Repairing 2 defenses.");
        page.Click("barButton('clearbroken')");
        page.Status("Clearing 1 broken defense.");
        page.Click("barButton('toggle')");

        Assert.Equal(new PageCommand[] { new PageCommand.RepairAll(), new PageCommand.ClearBroken(), new PageCommand.Toggle() }, page.Sent);
    }

    /// <summary>The game's click-through dispatches on whatever is under the pointer, which can be a corner mark.</summary>
    [Fact]
    public void A_click_on_a_buttons_corner_mark_presses_the_button()
    {
        var page = InLayout(Home());

        page.Click("barButton('repairall').firstChild");

        Assert.Equal(new PageCommand[] { new PageCommand.RepairAll() }, page.Sent);
    }

    /// <summary>
    /// The game skips disabled elements when it notes where a page is clickable, so a greyed button needs something
    /// around it the game does note, or a click on it (or between the buttons) lands on the world behind Layout.
    /// </summary>
    [Fact]
    public void Greyed_buttons_and_the_gaps_between_them_still_catch_clicks_for_the_game()
    {
        var page = InLayout(InWave());

        Assert.True(page.Is("barButton('repairall').disabled"));
        Assert.True(page.Is("isCoveredForClicks(barButton('repairall'))"));
        Assert.True(page.Is("isCoveredForClicks(barButton('clearbroken'))"));
        Assert.True(page.Is("isCoveredForClicks(bar().firstChild)"));
    }

    [Fact]
    public void During_a_wave_a_click_on_a_greyed_button_sends_nothing_and_the_bar_says_why()
    {
        var page = InLayout(InWave());

        page.Click("barButton('repairall')");
        page.Click("barButton('clearbroken')");
        page.Click("barButton('toggle')");

        Assert.Equal(new PageCommand[] { new PageCommand.Toggle() }, page.Sent);
        Assert.Equal(OutcomeMessage.WaveLockedText, page.Text("bar().querySelector('.dm-lb-note').textContent"));
    }

    /// <summary>A press holds the buttons until its answer; opening or shutting the panel gets no answer, so it must not.</summary>
    [Fact]
    public void The_list_button_still_opens_the_panel_while_a_press_waits_for_its_answer()
    {
        var page = InLayout(Home());

        page.Click("barButton('repairall')");
        page.Click("barButton('clearbroken')");
        page.Click("barButton('toggle')");

        Assert.Equal(new PageCommand[] { new PageCommand.RepairAll(), new PageCommand.Toggle() }, page.Sent);
    }

    [Fact]
    public void The_panels_close_button_still_works_while_a_press_from_the_bar_waits_for_its_answer()
    {
        var game = Home();
        var page = InLayout(game);

        page.Open(PanelView(game));
        page.Click("barButton('repairall')");
        page.Click("panelButton('close')");

        Assert.Equal(new PageCommand[] { new PageCommand.RepairAll(), new PageCommand.Close() }, page.Sent);
    }
}

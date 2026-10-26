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
    public void In_a_chinese_game_the_bar_takes_the_words_the_plugin_sends()
    {
        var page = new PageScript();

        page.Run(PageWords.Script(isChinese: true));
        page.Live(true, "F7");
        page.OpenLayout();
        page.Layout(BarView(Home()));

        Assert.Equal("全部修理（2）", Label(page, "repairall"));
        Assert.Equal("清除全部损毁（1）", Label(page, "clearbroken"));
        Assert.Equal("防御列表（F7）", Label(page, "toggle"));
    }

    [Fact]
    public void The_hud_button_goes_on_top_of_the_tool_column_and_carries_its_place_for_the_toolkits_layout()
    {
        var page = new PageScript();

        page.Run("openHud()");
        page.Live(true, "F7");
        page.Poll();

        Assert.True(page.Is("hudButton().parentNode.firstChild === hudButton()"));
        Assert.Equal("40", page.Text("hudButton().getAttribute('data-etk-dock')"));
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
        Assert.Equal(Texts.WaveLocked.En, page.Text("bar().querySelector('.dm-lb-note').textContent"));
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

    // ===== The panel =====

    /// <summary>Row 1 along the house at z 0, row 2 behind it at z -0.7, three squares each; the last square of row 2 is not the player's yet.</summary>
    private static Yard TwoRows() => new(
        new[]
        {
            new YardSquare("a", 1, -1.4, 0, true, 0), new YardSquare("b", 1, -0.7, 0, true, 0), new YardSquare("c", 1, 0, 0, true, 0),
            new YardSquare("d", 2, -1.4, -0.7, true, 0), new YardSquare("e", 2, -0.7, -0.7, true, 0), new YardSquare("f", 2, 0, -0.7, false, 50),
        },
        new[] { new WallFeature(WallFeatureKind.Door, -0.35) });

    private static Defense Tesla(long id, double x, double z) => Def(id, x: x, z: z, name: "Tesla coil") with { Power = new DefensePower(IsOn: true, IsOperational: true) };

    /// <summary>Two damaged defenses in row 1 (one a powered Tesla coil) and an intact one in row 2.</summary>
    private static FakeGame Yard(bool isChinese = false) => new()
    {
        IsChinese = isChinese,
        Yard = TwoRows(),
        Defenses = { Tesla(1, -1.4, 0), Def(2, x: 0, z: 0), Def(3, x: -0.7, z: -0.7, cur: 100) },
    };

    private static readonly PanelSettings Settings = new("F7", LayoutControls: true, PowerMode.WaveOnly, ProximityRange: 15, CrawlerDodge: 0.3f, ArmoredCrawlerDodge: 1f, AttackRate: 1f);

    private static string YardView(FakeGame game) =>
        DefenseView.Of(game.Read(), new PowerPolicy(PowerSettings.None, PowerMode.WaveOnly), game.IsChinese, Settings) ?? throw new InvalidOperationException("no panel view");

    private static PageScript OpenYard(bool isChinese = false)
    {
        var page = new PageScript();

        if (isChinese) page.Run(PageWords.Script(isChinese: true));

        page.Open(YardView(Yard(isChinese)));

        return page;
    }

    private static string Count(PageScript page, string selector) => page.Text($"document.querySelectorAll('{selector}').length");

    [Fact]
    public void The_yard_is_drawn_as_its_squares_with_a_number_on_each_row_saying_how_many_wait_for_repair()
    {
        var page = OpenYard();

        Assert.Equal("6", Count(page, ".dm-cell"));
        Assert.Equal("3", Count(page, ".dm-def"));
        Assert.Equal("1", Count(page, ".dm-cell.dm-buy"));
        Assert.Equal("12", page.Text("document.querySelectorAll('.dm-rlab')[0].textContent"));
        Assert.Equal("2", page.Text("document.querySelectorAll('.dm-rlab')[1].textContent"));
        Assert.Equal("Door", page.Text("document.querySelector('.dm-door').textContent"));
    }

    [Fact]
    public void A_door_or_window_spans_the_three_squares_centred_on_it_the_way_its_sandbags_cover_it()
    {
        var squares = Enumerable.Range(0, 7).Select(i => new YardSquare("s" + i, 1, i * 0.7, 0, true, 0)).ToArray();
        var game = new FakeGame { Yard = new Yard(squares, new[] { new WallFeature(WallFeatureKind.Window, 0.7), new WallFeature(WallFeatureKind.Door, 2.8) }) };
        var page = new PageScript();

        page.Open(YardView(game));

        // Square n is grid column n + 1, after the row number column.
        Assert.Equal("2 / 5", page.Text("document.querySelector('.dm-window').style.gridColumn"));
        Assert.Equal("5 / 8", page.Text("document.querySelector('.dm-door').style.gridColumn"));
    }

    [Fact]
    public void Picking_a_defense_shows_it_and_its_power_choices_set_it_alone_or_send_it_back_to_its_type()
    {
        var page = OpenYard();

        page.Click("panelButtonWith('pick', '1')");

        Assert.Equal("Row 1, square 1", page.Text("document.querySelector('.dm-ihd').querySelector('small').textContent"));
        Assert.Equal("Same as every Tesla coil (Wave only)", page.Text("panelButtonWith('power', '1|none').textContent"));
        Assert.True(page.Is("panelButtonWith('power', '1|none').classList.contains('dm-on')"));

        page.Click("panelButtonWith('power', '1|Proximity')");
        page.Status("Tesla coil set to Proximity.");
        page.Click("panelButtonWith('power', '1|none')");

        Assert.Equal(new PageCommand[] { new PageCommand.Power(1, PowerMode.Proximity), new PageCommand.Power(1, null) }, page.Sent);
    }

    /// <summary>Picking, tabs and going back only change what the panel shows, so they send nothing and are never held by a press.</summary>
    [Fact]
    public void Picking_and_switching_tabs_send_nothing_and_still_work_while_a_press_waits_for_its_answer()
    {
        var page = OpenYard();

        page.Click("panelButton('repairall')");
        page.Click("panelButtonWith('pick', '2')");

        Assert.True(page.Is("panelButtonWith('remove', '2')"));

        page.Click("panelButton('back')");
        page.Click("panelButtonWith('tab', 'settings')");

        Assert.True(page.Is("document.querySelector('.dm-sgrid')"));
        Assert.Equal(new PageCommand[] { new PageCommand.RepairAll() }, page.Sent);
    }

    [Fact]
    public void A_row_number_opens_the_row_and_its_button_repairs_just_that_row()
    {
        var page = OpenYard();

        page.Click("panelButtonWith('row', '1')");

        Assert.Equal("Repair row 1 (2)", page.Text("panelButton('repairrow').textContent"));

        page.Click("panelButton('repairrow')");

        Assert.Equal(new PageCommand[] { new PageCommand.RepairRow(1) }, page.Sent);
    }

    [Fact]
    public void A_square_not_yet_yours_says_its_price()
    {
        var page = OpenYard();

        page.Click("panelButtonWith('cell', '2:3')");

        Assert.Equal("Unlock it from the Layout screen for 50 Survival Points.", page.Text("document.querySelector('.dm-desc').textContent"));
        Assert.Empty(page.Sent);
    }

    [Fact]
    public void The_power_tab_sets_a_whole_type_or_sends_it_back_to_the_global_mode()
    {
        var page = OpenYard();

        page.Click("panelButtonWith('tab', 'power')");

        Assert.Equal("Global (Wave only)", page.Text("panelButtonWith('powertype', '1|none').textContent"));

        page.Click("panelButtonWith('powertype', '1|AlwaysOn')");
        page.Status("Every Tesla coil set to Always on.");
        page.Click("panelButtonWith('powertype', '1|none')");

        Assert.Equal(new PageCommand[] { new PageCommand.PowerType(1, PowerMode.AlwaysOn), new PageCommand.PowerType(1, null) }, page.Sent);
    }

    [Fact]
    public void The_settings_tab_steps_a_value_flips_a_toggle_and_greys_a_step_past_the_end()
    {
        var page = OpenYard();

        page.Click("panelButtonWith('tab', 'settings')");

        Assert.Equal("15 m", page.Text("document.querySelectorAll('.dm-stepper')[0].querySelector('b').textContent"));
        Assert.Equal("30%", page.Text("document.querySelectorAll('.dm-stepper')[1].querySelector('b').textContent"));
        Assert.True(page.Is("panelButtonWith('set', 'armoredcrawlerdodge|1.05').disabled"));

        page.Click("panelButtonWith('set', 'proximityrange|16')");
        page.Status("Setting saved.");
        page.Click("panelButtonWith('set', 'crawlerdodge|0.25')");
        page.Status("Setting saved.");
        page.Click("panelButtonWith('set', 'layoutcontrols|0')");
        page.Status("Setting saved.");
        page.Click("panelButtonWith('set', 'defaultmode|Proximity')");

        Assert.Equal(
            new PageCommand[]
            {
                new PageCommand.Setting(new SettingChange.ProximityRange(16)),
                new PageCommand.Setting(new SettingChange.CrawlerDodge(0.25f)),
                new PageCommand.Setting(new SettingChange.LayoutControls(false)),
                new PageCommand.Setting(new SettingChange.DefaultMode(PowerMode.Proximity)),
            },
            page.Sent);
    }

    [Fact]
    public void In_a_chinese_game_the_panel_is_labelled_in_chinese()
    {
        var page = OpenYard(isChinese: true);

        Assert.Equal("空地2", page.Text("panelButtonWith('tab', 'yard').textContent"));

        page.Click("panelButtonWith('pick', '1')");

        Assert.Equal("第 1 行，第 1 格", page.Text("document.querySelector('.dm-ihd').querySelector('small').textContent"));
        Assert.Equal("与所有Tesla coil相同（仅尸潮）", page.Text("panelButtonWith('power', '1|none').textContent"));
        Assert.Equal("僵尸靠近", page.Text("panelButtonWith('power', '1|Proximity').textContent"));
    }

    [Fact]
    public void Without_a_grid_every_defense_is_still_listed_and_can_be_picked()
    {
        var game = Home();
        var page = new PageScript();

        page.Open(PanelView(game));

        Assert.Equal("3", Count(page, ".dm-cell"));

        page.Click("panelButtonWith('pick', '2')");

        Assert.Equal("Not on the yard grid", page.Text("document.querySelector('.dm-ihd').querySelector('small').textContent"));
    }

    // ===== Refreshing the open panel =====

    /// <summary>A view as the plugin sends it: trimmed to the rows the page does not hold yet, and then held by it.</summary>
    private static string Through(ViewDelta delta, string view)
    {
        var next = delta.Next(view);
        delta.Sent();

        return next;
    }

    /// <summary>Drawing every square and list row again each second was most of what a refresh cost the page.</summary>
    [Fact]
    public void A_refresh_keeps_the_squares_that_did_not_change_and_draws_the_changed_defense_again()
    {
        var game = Yard();
        var delta = new ViewDelta();
        var page = new PageScript();

        page.Open(Through(delta, YardView(game)));
        page.Run("var intact = panelButtonWith('pick', '3'); var hit = panelButtonWith('pick', '2'); var forSale = panelButtonWith('cell', '2:3')");

        game.Defenses[1] = Def(2, x: 0, z: 0, cur: 10);
        page.Show(Through(delta, YardView(game)));

        Assert.True(page.Is("panelButtonWith('pick', '3') === intact"));
        Assert.True(page.Is("panelButtonWith('cell', '2:3') === forSale"));
        Assert.False(page.Is("panelButtonWith('pick', '2') === hit"));
        Assert.Empty(page.Sent);
    }

    /// <summary>Rows sent as their ids and nodes kept from the last drawing must come out exactly as a whole view drawn fresh.</summary>
    [Fact]
    public void A_refresh_sent_as_changed_rows_draws_the_panel_exactly_as_the_whole_view_would()
    {
        var game = Yard();
        var delta = new ViewDelta();
        var trimmed = new PageScript();

        trimmed.Open(Through(delta, YardView(game)));
        trimmed.Click("panelButtonWith('row', '1')");
        game.Defenses[1] = Def(2, x: 0, z: 0, cur: 10);
        trimmed.Show(Through(delta, YardView(game)));

        var whole = new PageScript();

        whole.Open(YardView(game));
        whole.Click("panelButtonWith('row', '1')");

        Assert.Equal(whole.Text("drawingOf(document.querySelector('#dm-root'))"), trimmed.Text("drawingOf(document.querySelector('#dm-root'))"));
    }

    /// <summary>A page that missed a view cannot fill in rows sent as ids, so it asks for everything again rather than show them wrong.</summary>
    [Fact]
    public void Rows_sent_as_ids_the_page_never_got_ask_for_the_whole_view_again()
    {
        var game = Yard();
        var delta = new ViewDelta();
        var page = new PageScript();

        Through(delta, YardView(game));
        page.Open(YardView(new FakeGame { Yard = TwoRows() }));
        page.Show(Through(delta, YardView(game)));

        Assert.Equal(new PageCommand[] { new PageCommand.Resend() }, page.Sent);
        Assert.Equal("0", Count(page, ".dm-def"));
    }

    /// <summary>The head draws only from the view, the tab and the words, so a pick must not build it again; anything it shows must.</summary>
    [Fact]
    public void A_pick_leaves_the_head_as_drawn_and_a_new_view_or_tab_draws_it_again()
    {
        var game = Yard();
        var page = new PageScript();

        page.Open(YardView(game));
        page.Run("var logo = document.querySelector('.dm-logo')");
        page.Click("panelButtonWith('pick', '1')");

        Assert.True(page.Is("document.querySelector('.dm-logo') === logo"));

        page.Click("panelButtonWith('tab', 'power')");

        Assert.False(page.Is("document.querySelector('.dm-logo') === logo"));
        Assert.True(page.Is("panelButtonWith('tab', 'power').classList.contains('dm-on')"));

        page.Run("logo = document.querySelector('.dm-logo')");
        game.Defenses.RemoveAt(2);
        page.Show(YardView(game));

        Assert.False(page.Is("document.querySelector('.dm-logo') === logo"));
        Assert.Equal("2 placed, 5 of 6 squares yours", page.Text("document.querySelector('.dm-ttl').querySelector('small').textContent"));
    }

    /// <summary>Opening the panel again sends only the rows that changed while it was shut, and must draw it as a whole view would.</summary>
    [Fact]
    public void A_panel_opened_again_takes_the_rows_it_holds_as_ids_and_draws_as_the_whole_view_would()
    {
        var game = Yard();
        var delta = new ViewDelta();
        var page = new PageScript();

        page.Open(Through(delta, YardView(game)));
        page.Run("var intact = panelButtonWith('pick', '3'); window.__sldefense.close()");
        game.Defenses[1] = Def(2, x: 0, z: 0, cur: 10);

        var again = Through(delta, YardView(game));

        page.Open(again);

        Assert.DoesNotContain("{\"id\":\"3\"", again);
        Assert.True(page.Is("panelButtonWith('pick', '3') === intact"));
        Assert.Empty(page.Sent);

        var whole = new PageScript();

        whole.Open(YardView(game));

        Assert.Equal(whole.Text("drawingOf(document.querySelector('#dm-root'))"), page.Text("drawingOf(document.querySelector('#dm-root'))"));
    }

    // ===== The page check =====

    /// <summary>The check runs every 400 ms for the whole session; with the button and the bar up it must not search the pages.</summary>
    [Fact]
    public void Once_the_button_and_the_bar_are_up_the_page_check_searches_nothing()
    {
        var page = new PageScript();

        page.OpenLayout();
        page.Run("openHud()");
        page.Live(true, "F7");
        page.Layout(BarView(Home()));
        page.Run("searches = 0");
        page.Poll();
        page.Poll();

        Assert.Equal("0", page.Text("searches"));
        Assert.True(page.Is("hudButton()"));
        Assert.True(page.Is("bar()"));
    }

    [Fact]
    public void Outside_a_game_the_page_check_searches_nothing()
    {
        var page = new PageScript();

        page.Run("openHud(); searches = 0");
        page.Poll();

        Assert.Equal("0", page.Text("searches"));
        Assert.False(page.Is("hudButton()"));
    }

    [Fact]
    public void A_HUD_the_game_draws_in_a_new_iframe_gets_the_button_on_the_next_check()
    {
        var page = new PageScript();

        page.Run("var old = openHud()");
        page.Live(true, "F7");
        page.Run("old.remove(); openHud()");
        page.Poll();

        Assert.True(page.Is("hudButton()"));
    }

    /// <summary>The old button still sits in the old document's column, so it looks placed unless the iframe's document is checked too.</summary>
    [Fact]
    public void A_HUD_iframe_that_loads_a_new_document_gets_the_button_on_the_next_check()
    {
        var page = new PageScript();

        page.Run("var frame = openHud()");
        page.Live(true, "F7");
        page.Run("var fresh = openHud(); frame.contentDocument = fresh.contentDocument; fresh.remove()");
        page.Poll();

        Assert.True(page.Is("frame.contentDocument.querySelector('.dm-hud-btn')"));
    }

    /// <summary>With the HUD button still in place, only the bar's own check can notice its page was drawn again.</summary>
    [Fact]
    public void A_Layout_page_the_game_draws_again_gets_the_bar_on_the_next_check()
    {
        var page = InLayout(Home());

        page.Run("openHud()");
        page.Poll();
        page.Run("document.querySelector('iframe').contentDocument = new FakeDocument()");
        page.Poll();

        Assert.Equal("Repair all (2)", Label(page, "repairall"));
    }

    // ===== UI size (Exiled's Toolkit) =====

    /// <summary>A stand-in for the Toolkit's ExiledUi that records what each mod hands it.</summary>
    private const string FakeUi = "var handed = []; window.ExiledUi = { size: function (el, options) { handed.push({ el: el, options: options }); } }";

    /// <summary>
    /// The mask fills the screen at any zoom, so a window measured by the mask would never be shrunk to fit; the window
    /// itself is what must fit. The notice is never shrunk. Each is handed once, however often the panel opens.
    /// </summary>
    [Fact]
    public void The_panels_window_is_what_must_fit_the_screen_and_the_notice_is_never_shrunk()
    {
        var game = Yard();
        var page = new PageScript();

        page.Run(FakeUi);
        page.Open(YardView(game));
        page.Show(YardView(game));
        page.Run("window.__sldefense.close()");
        page.Open(YardView(game));

        Assert.Equal("2", page.Text("handed.length"));
        Assert.Equal("dm-root", page.Text("handed[0].el.id"));
        Assert.True(page.Is("handed[0].options.fit === document.querySelector('.dm-plate')"));
        Assert.Equal("dm-toast", page.Text("handed[1].el.id"));
        Assert.True(page.Is("handed[1].options.fit === false"));
    }

    /// <summary>The Toolkit drops a panel taken out of its page, so the panel drawn again in its place must be handed again.</summary>
    [Fact]
    public void A_panel_drawn_again_after_the_page_lost_it_is_handed_again()
    {
        var game = Yard();
        var page = new PageScript();

        page.Run(FakeUi);
        page.Open(YardView(game));
        page.Run("document.getElementById('dm-root').remove(); document.getElementById('dm-toast').remove()");
        page.Open(YardView(game));

        Assert.Equal("4", page.Text("handed.length"));
        Assert.True(page.Is("handed[2].el !== handed[0].el && handed[2].el === document.getElementById('dm-root')"));
        Assert.True(page.Is("handed[2].options.fit === document.querySelector('.dm-plate')"));
    }

    /// <summary>The Toolkit's script can load after this one; the panel waits in the list it reads when it starts.</summary>
    [Fact]
    public void Before_the_toolkit_has_loaded_the_panel_waits_for_it()
    {
        var page = new PageScript();

        page.Open(YardView(Yard()));

        Assert.Equal("2", page.Text("window.__exiledUiWait.length"));
        Assert.Equal("dm-root", page.Text("window.__exiledUiWait[0][0].id"));
        Assert.True(page.Is("window.__exiledUiWait[0][1].fit === document.querySelector('.dm-plate')"));
    }

    /// <summary>The page draws in English until the plugin's words arrive, so its own copy must say the same as Texts.</summary>
    [Fact]
    public void The_pages_own_english_words_are_the_same_as_the_plugins()
    {
        var source = PageScript.Source;
        var block = source[source.IndexOf("var ENGLISH = {", StringComparison.Ordinal)..];
        block = block[..block.IndexOf("  };", StringComparison.Ordinal)];
        var english = System.Text.RegularExpressions.Regex.Matches(block, @"(\w+): '((?:[^'\\]|\\.)*)'")
            .ToDictionary(match => match.Groups[1].Value, match => match.Groups[2].Value.Replace("\\'", "'"));

        Assert.Equal(Texts.Page.ToDictionary(word => word.Key, word => word.Phrase.En), english);
    }
}

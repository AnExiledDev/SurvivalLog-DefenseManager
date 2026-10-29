using System.Text.Json;
using DefenseManager.Core;
using Xunit;
using static DefenseManager.Tests.FakeGame;

namespace DefenseManager.Tests;

public class DefenseViewTests
{
    private static Survey SurveyOf(IEnumerable<Defense> defenses, IReadOnlyDictionary<int, int>? stock = null, bool isWaveLocked = false, bool isHome = true)
    {
        var materials = (stock ?? new Dictionary<int, int>()).ToDictionary(entry => entry.Key, entry => new Material(NameOf(entry.Key), $"Res/Item/{NameOf(entry.Key)}.png", entry.Value));

        return new Survey(defenses.ToList(), new Spot(0, 0, 0), materials, QueueOnStart.Appends, isWaveLocked, isHome);
    }

    private static JsonElement View(Survey survey) => JsonDocument.Parse(DefenseView.Build(survey)).RootElement;

    private static JsonElement Row(JsonElement view, long id) => view.GetProperty("defenses").EnumerateArray().Single(row => row.GetProperty("id").GetString() == id.ToString());

    [Fact]
    public void Rows_are_in_name_order_so_they_do_not_jump_under_the_cursor()
    {
        var view = View(SurveyOf(new[] { Def(3, name: "Spike barrier"), Def(9, name: "Sandbags"), Def(2, name: "Sandbags") }));

        var ids = view.GetProperty("defenses").EnumerateArray().Select(row => row.GetProperty("id").GetString());

        Assert.Equal(new[] { "2", "9", "3" }, ids);
    }

    [Fact]
    public void Ids_go_to_the_page_as_text_so_no_long_loses_digits()
    {
        var view = View(SurveyOf(new[] { Def(9007199254740993) }));

        Assert.Equal("9007199254740993", Row(view, 9007199254740993).GetProperty("id").GetString());
    }

    [Fact]
    public void A_row_shows_the_defenses_condition()
    {
        var view = View(SurveyOf(new[] { Def(1, cur: 0, broken: true), Def(2, cur: 30, max: 120), Def(3, cur: 100, max: 100) }));

        Assert.Equal("broken", Row(view, 1).GetProperty("state").GetString());
        Assert.Equal("damaged", Row(view, 2).GetProperty("state").GetString());
        Assert.Equal(30, Row(view, 2).GetProperty("cur").GetInt32());
        Assert.Equal(120, Row(view, 2).GetProperty("max").GetInt32());
        Assert.Equal("intact", Row(view, 3).GetProperty("state").GetString());
    }

    [Fact]
    public void A_row_says_what_is_queued_for_it_and_a_removal_outranks_a_repair()
    {
        var view = View(SurveyOf(new[] { Def(1) with { IsRepairQueued = true }, Def(2) with { IsRemovalQueued = true }, Def(3) with { IsRepairQueued = true, IsRemovalQueued = true }, Def(4) }));

        Assert.Equal("repair", Row(view, 1).GetProperty("queued").GetString());
        Assert.Equal("remove", Row(view, 2).GetProperty("queued").GetString());
        Assert.Equal("remove", Row(view, 3).GetProperty("queued").GetString());
        Assert.Equal(JsonValueKind.Null, Row(view, 4).GetProperty("queued").ValueKind);
    }

    [Fact]
    public void A_row_shows_its_cost_and_what_the_materials_left_after_queued_repairs_lack()
    {
        var queued = Def(1, cost: Cost((Wood, 6))) with { IsRepairQueued = true };
        var waiting = Def(2, cost: Cost((Nail, 1), (Wood, 6)));

        var cost = Row(View(SurveyOf(new[] { queued, waiting }, new Dictionary<int, int> { [Wood] = 10, [Nail] = 5 })), 2).GetProperty("cost").EnumerateArray().ToList();

        Assert.Equal(new[] { "Wood", "Nail" }, cost.Select(entry => entry.GetProperty("name").GetString()));
        Assert.Equal(new[] { 6, 1 }, cost.Select(entry => entry.GetProperty("count").GetInt32()));
        Assert.Equal(new[] { 2, 0 }, cost.Select(entry => entry.GetProperty("short").GetInt32()));
        Assert.Equal("Res/Item/Wood.png", cost[0].GetProperty("icon").GetString());
    }

    [Fact]
    public void A_row_repair_button_is_on_only_when_the_repair_would_be_taken_and_says_why_not()
    {
        var view = View(SurveyOf(new[] { Def(1, cost: Cost((Wood, 2))), Def(2, cost: Cost((Wood, 9))), Def(3, blocked: "Needs a workbench."), Def(4, cur: 100, max: 100) }, new Dictionary<int, int> { [Wood] = 5 }));

        Assert.True(Row(view, 1).GetProperty("canRepair").GetBoolean());
        Assert.False(Row(view, 2).GetProperty("canRepair").GetBoolean());
        Assert.Equal("Needs 4 more Wood", Row(view, 2).GetProperty("why").GetString());
        Assert.False(Row(view, 3).GetProperty("canRepair").GetBoolean());
        Assert.Equal("Needs a workbench", Row(view, 3).GetProperty("why").GetString());
        Assert.False(Row(view, 4).GetProperty("canRepair").GetBoolean());
        Assert.Equal(JsonValueKind.Null, Row(view, 4).GetProperty("why").ValueKind);
    }

    [Fact]
    public void A_row_remove_button_is_off_while_a_repair_of_it_is_queued()
    {
        var view = View(SurveyOf(new[] { Def(1) with { IsRepairQueued = true }, Def(2), Def(3, cur: 100, max: 100) }));

        Assert.False(Row(view, 1).GetProperty("canRemove").GetBoolean());
        Assert.Equal("Repair queued: let it finish or cancel it first", Row(view, 1).GetProperty("removeWhy").GetString());
        Assert.True(Row(view, 2).GetProperty("canRemove").GetBoolean());
        Assert.True(Row(view, 3).GetProperty("canRemove").GetBoolean());
    }

    [Fact]
    public void During_a_zombie_wave_every_button_is_off_and_the_panel_says_why()
    {
        var view = View(SurveyOf(new[] { Def(1), Def(2, cur: 0, broken: true) }, isWaveLocked: true));

        Assert.Equal(Texts.WaveLocked.En, view.GetProperty("locked").GetString());
        Assert.All(view.GetProperty("defenses").EnumerateArray(), row => Assert.False(row.GetProperty("canRepair").GetBoolean() || row.GetProperty("canRemove").GetBoolean()));
    }

    [Fact]
    public void Away_from_home_every_button_is_off_and_the_panel_says_why()
    {
        var view = View(SurveyOf(new[] { Def(1), Def(2, cur: 0, broken: true) }, isHome: false));

        Assert.Equal(Texts.AwayFromHome.En, view.GetProperty("locked").GetString());
        Assert.All(view.GetProperty("defenses").EnumerateArray(), row => Assert.False(row.GetProperty("canRepair").GetBoolean() || row.GetProperty("canRemove").GetBoolean()));
    }

    [Fact]
    public void An_unlocked_panel_has_no_lock_text()
    {
        Assert.Equal(JsonValueKind.Null, View(SurveyOf(new[] { Def(1) })).GetProperty("locked").ValueKind);
    }

    [Fact]
    public void The_top_buttons_count_what_a_press_would_still_take_on()
    {
        var defenses = new[]
        {
            Def(1),
            Def(2) with { IsRepairQueued = true },
            Def(3) with { IsRemovalQueued = true },
            Def(4, cur: 0, broken: true),
            Def(5, cur: 0, broken: true) with { IsRepairQueued = true },
            Def(6, cur: 100, max: 100),
        };

        var view = View(SurveyOf(defenses));

        Assert.Equal(2, view.GetProperty("repairAll").GetInt32());
        Assert.Equal(1, view.GetProperty("clearAll").GetInt32());
    }

    [Fact]
    public void Materials_show_what_is_free_after_queued_repairs_and_what_the_rest_need()
    {
        var defenses = new[] { Def(1, cost: Cost((Wood, 4))) with { IsRepairQueued = true }, Def(2, cost: Cost((Wood, 3), (Nail, 2))), Def(3, cost: Cost((Wood, 5))) };

        var materials = View(SurveyOf(defenses, new Dictionary<int, int> { [Nail] = 1, [Wood] = 10 })).GetProperty("materials").EnumerateArray().ToList();

        Assert.Equal(new[] { "Wood", "Nail" }, materials.Select(material => material.GetProperty("name").GetString()));
        Assert.Equal(new[] { 6, 1 }, materials.Select(material => material.GetProperty("free").GetInt32()));
        Assert.Equal(new[] { 8, 2 }, materials.Select(material => material.GetProperty("need").GetInt32()));
    }

    [Fact]
    public void Names_reach_the_page_as_the_same_text_whatever_they_hold()
    {
        const string name = "\"Sandbags\" </script>\u6c99\u5305";

        var view = View(SurveyOf(new[] { Def(1, name: name) }));

        Assert.Equal(name, Row(view, 1).GetProperty("name").GetString());
        Assert.DoesNotContain("</script>", DefenseView.Build(SurveyOf(new[] { Def(1, name: name) })));
    }

    [Fact]
    public void An_empty_panel_carries_its_note_and_nothing_to_act_on()
    {
        var view = JsonDocument.Parse(DefenseView.Empty("Load a game first.")).RootElement;

        Assert.Equal("Load a game first.", view.GetProperty("note").GetString());
        Assert.Equal(0, view.GetProperty("repairAll").GetInt32());
        Assert.Equal(0, view.GetProperty("clearAll").GetInt32());
        Assert.Empty(view.GetProperty("materials").EnumerateArray());
        Assert.Empty(view.GetProperty("defenses").EnumerateArray());
    }

    // ===== Power =====

    private const int DefType = 36001;

    private static Defense Electrical(Defense defense, bool isOn = true, bool isOperational = true) => defense with { Power = new DefensePower(isOn, isOperational) };

    private static JsonElement Power(JsonElement view, long id) => Row(view, id).GetProperty("power");

    private static JsonElement View(Survey survey, PowerPolicy policy) => JsonDocument.Parse(DefenseView.Build(survey, policy)).RootElement;

    [Fact]
    public void Only_a_defense_that_runs_on_electricity_gets_power_controls()
    {
        var view = View(SurveyOf(new[] { Electrical(Def(1)), Def(2) }), new PowerPolicy(PowerSettings.None, PowerMode.WaveOnly));

        Assert.Equal(JsonValueKind.Object, Power(view, 1).ValueKind);
        Assert.Equal(JsonValueKind.Null, Power(view, 2).ValueKind);
    }

    [Fact]
    public void An_electrical_row_shows_its_switch_its_mode_and_its_own_and_its_types_settings()
    {
        var own = Electrical(Def(1, x: 5), isOn: true);
        var following = Electrical(Def(2, x: 30), isOn: false);
        var settings = PowerSettings.None.WithType(DefType, PowerMode.Proximity).WithDefense(DefenseKey.Of(DefType, own.Position), PowerMode.AlwaysOn);

        var view = View(SurveyOf(new[] { own, following }), new PowerPolicy(settings, PowerMode.WaveOnly));

        Assert.True(Power(view, 1).GetProperty("on").GetBoolean());
        Assert.Equal("Always on", Power(view, 1).GetProperty("mode").GetString());
        Assert.Equal("AlwaysOn", Power(view, 1).GetProperty("own").GetString());
        Assert.Equal("Proximity", Power(view, 1).GetProperty("typeOwn").GetString());
        Assert.False(Power(view, 2).GetProperty("on").GetBoolean());
        Assert.Equal("Proximity", Power(view, 2).GetProperty("mode").GetString());
        Assert.Equal(JsonValueKind.Null, Power(view, 2).GetProperty("own").ValueKind);
        Assert.Equal("Proximity", Power(view, 2).GetProperty("typeMode").GetProperty("name").GetString());
    }

    [Fact]
    public void A_type_with_no_setting_shows_the_global_default_it_follows()
    {
        var view = View(SurveyOf(new[] { Electrical(Def(1)) }), new PowerPolicy(PowerSettings.None, PowerMode.GameDefault));

        Assert.Equal("Default", Power(view, 1).GetProperty("mode").GetString());
        Assert.Equal(JsonValueKind.Null, Power(view, 1).GetProperty("typeOwn").ValueKind);
        Assert.Equal("Default", Power(view, 1).GetProperty("typeMode").GetProperty("label").GetString());
        Assert.Equal("Default", view.GetProperty("global").GetProperty("name").GetString());
    }

    [Fact]
    public void A_broken_or_not_working_defense_shows_the_mod_is_not_switching_it()
    {
        var view = View(SurveyOf(new[] { Electrical(Def(1)), Electrical(Def(2, cur: 0, broken: true)), Electrical(Def(3), isOperational: false) }), new PowerPolicy(PowerSettings.None, PowerMode.WaveOnly));

        Assert.True(Power(view, 1).GetProperty("managed").GetBoolean());
        Assert.False(Power(view, 2).GetProperty("managed").GetBoolean());
        Assert.False(Power(view, 3).GetProperty("managed").GetBoolean());
    }

    /// <summary>Without settings (none loaded, or a file that cannot be read) a button would show a mode the pass is not using.</summary>
    [Fact]
    public void Without_power_settings_no_row_offers_power_controls()
    {
        var view = View(SurveyOf(new[] { Electrical(Def(1)) }));

        Assert.Equal(JsonValueKind.Null, Power(view, 1).ValueKind);
    }

    // ===== The yard grid =====

    /// <summary>Two rows of three squares, 0.7 apart, row 1 against the house; the middle of row 2 is not owned yet.</summary>
    private static Yard SmallYard() => new(
        new[]
        {
            new YardSquare("A", 1, -1.4, 0, true, 0), new YardSquare("B", 1, -0.7, 0, true, 0), new YardSquare("C", 1, 0, 0, true, 0),
            new YardSquare("D", 2, -1.4, -0.7, true, 0), new YardSquare("E", 2, -0.7, -0.7, false, 5), new YardSquare("F", 2, 0, -0.7, true, 0),
        },
        new[] { new WallFeature(WallFeatureKind.Door, -0.35) });

    private static Survey WithYard(IEnumerable<Defense> defenses) =>
        new(defenses.ToList(), new Spot(0, 0, 0), new Dictionary<int, Material>(), QueueOnStart.Appends, false, true, Yard: SmallYard());

    [Fact]
    public void Each_defense_row_says_which_square_of_the_grid_it_stands_on_and_one_off_the_grid_says_none()
    {
        var view = View(WithYard(new[] { Def(1, x: -0.68, z: -0.72), Def(2, x: 0.02, z: 0.01), Def(3, x: 9, z: 9) }));

        Assert.Equal(2, Row(view, 1).GetProperty("row").GetInt32());
        Assert.Equal(2, Row(view, 1).GetProperty("col").GetInt32());
        Assert.Equal(1, Row(view, 2).GetProperty("row").GetInt32());
        Assert.Equal(3, Row(view, 2).GetProperty("col").GetInt32());
        Assert.Equal(JsonValueKind.Null, Row(view, 3).GetProperty("row").ValueKind);
        Assert.Equal(JsonValueKind.Null, Row(view, 3).GetProperty("col").ValueKind);
    }

    [Fact]
    public void The_view_carries_the_grid_with_the_squares_still_for_sale_and_the_door_between_columns()
    {
        var yard = View(WithYard(Array.Empty<Defense>())).GetProperty("yard");
        var forSale = yard.GetProperty("squares").EnumerateArray().Where(square => !square.GetProperty("own").GetBoolean()).ToList();
        var door = yard.GetProperty("wall").EnumerateArray().Single();

        Assert.Equal(3, yard.GetProperty("cols").GetInt32());
        Assert.Equal(2, yard.GetProperty("rows").GetInt32());
        Assert.Single(forSale);
        Assert.Equal(2, forSale[0].GetProperty("r").GetInt32());
        Assert.Equal(2, forSale[0].GetProperty("c").GetInt32());
        Assert.Equal(5, forSale[0].GetProperty("price").GetInt32());
        Assert.Equal("door", door.GetProperty("kind").GetString());
        Assert.Equal(2.5, door.GetProperty("at").GetDouble(), 2);
    }

    [Fact]
    public void Each_square_carries_the_games_name_for_it_so_an_install_can_say_where()
    {
        var names = View(WithYard(Array.Empty<Defense>())).GetProperty("yard").GetProperty("squares").EnumerateArray().Select(square => square.GetProperty("n").GetString());

        Assert.Equal(new[] { "A", "B", "C", "D", "E", "F" }, names.OrderBy(name => name));
    }

    [Fact]
    public void The_install_list_holds_only_defenses_with_a_package_somewhere_in_name_order()
    {
        var packages = new[]
        {
            new DefensePackage(30, "Spikes", "s.png", new long[] { 7, 8 }, 1),
            new DefensePackage(20, "Barbed wire", null, Array.Empty<long>(), 2),
            new DefensePackage(40, "Tesla coil", null, Array.Empty<long>(), 0),
        };
        var view = View(WithYard(Array.Empty<Defense>()) with { Packages = packages }).GetProperty("packages").EnumerateArray().ToList();

        Assert.Equal(new[] { "Barbed wire", "Spikes" }, view.Select(package => package.GetProperty("name").GetString()));
        Assert.Equal(30, view[1].GetProperty("type").GetInt32());
        Assert.Equal(2, view[1].GetProperty("yard").GetInt32());
        Assert.Equal(1, view[1].GetProperty("bank").GetInt32());
        Assert.Equal(0, view[0].GetProperty("yard").GetInt32());
    }

    [Fact]
    public void An_empty_panel_offers_nothing_to_install()
    {
        Assert.Empty(JsonDocument.Parse(DefenseView.Empty("Load a game first.")).RootElement.GetProperty("packages").EnumerateArray());
    }

    [Fact]
    public void Without_a_grid_the_view_says_so()
    {
        Assert.Equal(JsonValueKind.Null, View(SurveyOf(new[] { Def(1) })).GetProperty("yard").ValueKind);
    }

    [Fact]
    public void The_settings_tab_gets_the_saved_values_even_when_the_game_could_not_be_read()
    {
        var settings = new PanelSettings("F7", false, PowerMode.Proximity, 15, 0.25f, 0.3f, 1.5f, AllowDuringWaves: true);
        var view = JsonDocument.Parse(DefenseView.Empty("Load a game first.", settings)).RootElement.GetProperty("settings");

        Assert.Equal("F7", view.GetProperty("key").GetString());
        Assert.False(view.GetProperty("layout").GetBoolean());
        Assert.Equal("Proximity", view.GetProperty("mode").GetString());
        Assert.Equal(15, view.GetProperty("range").GetDouble());
        Assert.Equal(0.3, view.GetProperty("armored").GetDouble(), 3);
        Assert.Equal(1.5, view.GetProperty("rate").GetDouble());
        Assert.True(view.GetProperty("waves").GetBoolean());
    }
}

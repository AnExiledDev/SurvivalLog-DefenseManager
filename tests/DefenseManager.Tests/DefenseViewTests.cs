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

        Assert.Equal(OutcomeMessage.WaveLockedText, view.GetProperty("locked").GetString());
        Assert.All(view.GetProperty("defenses").EnumerateArray(), row => Assert.False(row.GetProperty("canRepair").GetBoolean() || row.GetProperty("canRemove").GetBoolean()));
    }

    [Fact]
    public void Away_from_home_every_button_is_off_and_the_panel_says_why()
    {
        var view = View(SurveyOf(new[] { Def(1), Def(2, cur: 0, broken: true) }, isHome: false));

        Assert.Equal(OutcomeMessage.AwayFromHomeText, view.GetProperty("locked").GetString());
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
}

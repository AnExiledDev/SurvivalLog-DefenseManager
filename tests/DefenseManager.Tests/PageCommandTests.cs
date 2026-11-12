using DefenseManager.Core;
using Xunit;

namespace DefenseManager.Tests;

public class PageCommandTests
{
    [Fact]
    public void The_panel_and_hud_button_verbs_parse_to_their_commands()
    {
        Assert.Equal(new PageCommand.Toggle(), PageCommand.Parse("slmod|defensemanager|toggle"));
        Assert.Equal(new PageCommand.Close(), PageCommand.Parse("slmod|defensemanager|close"));
        Assert.Equal(new PageCommand.RepairAll(), PageCommand.Parse("slmod|defensemanager|repairall"));
        Assert.Equal(new PageCommand.ClearBroken(), PageCommand.Parse("slmod|defensemanager|clearbroken"));
        Assert.Equal(new PageCommand.Ready(), PageCommand.Parse("slmod|defensemanager|ready"));
        Assert.Equal(new PageCommand.NoScript(), PageCommand.Parse("slmod|defensemanager|noscript"));
        Assert.Equal(new PageCommand.Resend(), PageCommand.Parse("slmod|defensemanager|resend"));
    }

    [Fact]
    public void A_row_button_carries_the_whole_defense_id()
    {
        Assert.Equal(new PageCommand.Repair(9007199254740993), PageCommand.Parse("slmod|defensemanager|repair|9007199254740993"));
        Assert.Equal(new PageCommand.Remove(-42), PageCommand.Parse("slmod|defensemanager|remove|-42"));
    }

    [Fact]
    public void A_power_pick_carries_the_whole_defense_id_the_mode_picked_and_whether_it_is_for_the_type()
    {
        Assert.Equal(new PageCommand.Power(9007199254740993, PowerMode.AlwaysOn), PageCommand.Parse("slmod|defensemanager|power|9007199254740993|AlwaysOn"));
        Assert.Equal(new PageCommand.PowerType(-42, PowerMode.GameDefault), PageCommand.Parse("slmod|defensemanager|powertype|-42|Default"));
    }

    [Fact]
    public void Picking_none_lets_go_of_the_setting()
    {
        Assert.Equal(new PageCommand.Power(7, null), PageCommand.Parse("slmod|defensemanager|power|7|none"));
        Assert.Equal(new PageCommand.PowerType(7, null), PageCommand.Parse("slmod|defensemanager|powertype|7|none"));
    }

    [Fact]
    public void Repair_row_carries_the_row_number()
    {
        Assert.Equal(new PageCommand.RepairRow(3), PageCommand.Parse("slmod|defensemanager|repairrow|3"));
    }

    [Fact]
    public void An_install_carries_the_square_and_the_defense_furniture_id()
    {
        Assert.Equal(new PageCommand.Install("TowerSlot_2_3", 36001), PageCommand.Parse("slmod|defensemanager|install|TowerSlot_2_3|36001"));
    }

    [Fact]
    public void A_buy_carries_the_square_a_move_the_defense_and_the_square_and_a_cancel_the_plan()
    {
        Assert.Equal(new PageCommand.Buy("TowerSlot_2_3"), PageCommand.Parse("slmod|defensemanager|buy|TowerSlot_2_3"));
        Assert.Equal(new PageCommand.Move(41, "TowerSlot_1_2"), PageCommand.Parse("slmod|defensemanager|move|41|TowerSlot_1_2"));
        Assert.Equal(new PageCommand.Cancel(PlanKind.Install, 900), PageCommand.Parse("slmod|defensemanager|cancel|install|900"));
        Assert.Equal(new PageCommand.Cancel(PlanKind.Move, 41), PageCommand.Parse("slmod|defensemanager|cancel|move|41"));
    }

    [Fact]
    public void A_settings_change_carries_the_setting_and_its_value()
    {
        Assert.Equal(new PageCommand.Setting(new SettingChange.LayoutControls(false)), PageCommand.Parse("slmod|defensemanager|set|layoutcontrols|0"));
        Assert.Equal(new PageCommand.Setting(new SettingChange.ProximityRange(20)), PageCommand.Parse("slmod|defensemanager|set|proximityrange|20"));
    }

    [Theory]
    [InlineData("slmod|defensemanager|")]
    [InlineData("slmod|defensemanager|Toggle")]
    [InlineData("slmod|defensemanager|toggle|now")]
    [InlineData("slmod|defensemanager|repair")]
    [InlineData("slmod|defensemanager|repair|")]
    [InlineData("slmod|defensemanager|repair|12x")]
    [InlineData("slmod|defensemanager|remove|1.5")]
    [InlineData("slmod|defensemanager|remove|1|2")]
    [InlineData("slmod|defensemanager|power")]
    [InlineData("slmod|defensemanager|power|1")]
    [InlineData("slmod|defensemanager|power|x|AlwaysOn")]
    [InlineData("slmod|defensemanager|power|1|Sometimes")]
    [InlineData("slmod|defensemanager|Power|1|AlwaysOn")]
    [InlineData("slmod|defensemanager|powertype")]
    [InlineData("slmod|defensemanager|powertype|1|AlwaysOn|2")]
    [InlineData("slmod|defensemanager|repairrow")]
    [InlineData("slmod|defensemanager|repairrow|0")]
    [InlineData("slmod|defensemanager|repairrow|two")]
    [InlineData("slmod|defensemanager|set|layoutcontrols")]
    [InlineData("slmod|defensemanager|set|layoutcontrols|yes")]
    [InlineData("slmod|defensemanager|set|panelkey|F8")]
    [InlineData("slmod|defensemanager|install")]
    [InlineData("slmod|defensemanager|install|s1")]
    [InlineData("slmod|defensemanager|install||36001")]
    [InlineData("slmod|defensemanager|install|s1|0")]
    [InlineData("slmod|defensemanager|install|s1|-5")]
    [InlineData("slmod|defensemanager|install|s1|tesla")]
    [InlineData("slmod|defensemanager|install|s1|36001|2")]
    [InlineData("slmod|defensemanager|buy")]
    [InlineData("slmod|defensemanager|buy|")]
    [InlineData("slmod|defensemanager|buy|s1|s2")]
    [InlineData("slmod|defensemanager|move|x|s1")]
    [InlineData("slmod|defensemanager|move|7|")]
    [InlineData("slmod|defensemanager|move|7")]
    [InlineData("slmod|defensemanager|cancel|other|1")]
    [InlineData("slmod|defensemanager|cancel|move|x")]
    [InlineData("slmod|defensemanager|cancel|move")]
    [InlineData("slmod|defensemanager|set|layoutactions|on")]
    [InlineData("slmod|defensemanager|diag|hud button placed")]
    [InlineData("slmod|unpackall|toggle")]
    public void Anything_else_is_unknown_and_kept_whole_for_the_log(string message)
    {
        Assert.Equal(new PageCommand.Unknown(message), PageCommand.Parse(message));
    }

    [Fact]
    public void Only_messages_with_the_mods_prefix_are_ours()
    {
        Assert.True(PageCommand.IsOurs("slmod|defensemanager|toggle"));
        Assert.False(PageCommand.IsOurs("slmod|storagehub|toggle"));
        Assert.False(PageCommand.IsOurs(""));
        Assert.False(PageCommand.IsOurs(null));
    }
}

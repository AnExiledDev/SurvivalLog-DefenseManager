using System.Text.Json;
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
    }

    [Fact]
    public void A_row_button_carries_the_whole_defense_id()
    {
        Assert.Equal(new PageCommand.Repair(9007199254740993), PageCommand.Parse("slmod|defensemanager|repair|9007199254740993"));
        Assert.Equal(new PageCommand.Remove(-42), PageCommand.Parse("slmod|defensemanager|remove|-42"));
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

    [Theory]
    [InlineData("")]
    [InlineData("plain")]
    [InlineData("say \"hi\" \\ back")]
    [InlineData("line\nbreak\r\tand \u2028 \u2029")]
    [InlineData("</script><script>alert(1)</script>")]
    [InlineData("\u5f53\u524d\u65e0\u6cd5\u4f7f\u7528\u3002")]
    public void Text_pasted_into_a_script_reads_back_as_the_same_text(string text)
    {
        var literal = JsText.Literal(text);

        Assert.Equal(text, JsonSerializer.Deserialize<string>(literal));
        Assert.DoesNotContain("<", literal);
        Assert.DoesNotContain("\u2028", literal);
    }

    [Fact]
    public void Missing_text_becomes_an_empty_string()
    {
        Assert.Equal("\"\"", JsText.Literal(null));
    }
}

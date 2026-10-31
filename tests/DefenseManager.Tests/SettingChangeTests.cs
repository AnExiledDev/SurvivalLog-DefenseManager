using DefenseManager.Core;
using Xunit;

namespace DefenseManager.Tests;

public class SettingChangeTests
{
    [Fact]
    public void Each_setting_on_the_tab_parses_to_its_change()
    {
        Assert.Equal(new SettingChange.LayoutControls(true), SettingChange.Parse("layoutcontrols", "1"));
        Assert.Equal(new SettingChange.DefaultMode(PowerMode.Proximity), SettingChange.Parse("defaultmode", "Proximity"));
        Assert.Equal(new SettingChange.ProximityRange(16), SettingChange.Parse("proximityrange", "16"));
        Assert.Equal(new SettingChange.CrawlerDodge(0.3f), SettingChange.Parse("crawlerdodge", "0.3"));
        Assert.Equal(new SettingChange.ArmoredCrawlerDodge(0f), SettingChange.Parse("armoredcrawlerdodge", "0"));
        Assert.Equal(new SettingChange.AttackRate(2.5f), SettingChange.Parse("attackrate", "2.5"));
        Assert.Equal(new SettingChange.AllowDuringWaves(true), SettingChange.Parse("wavework", "1"));
        Assert.Equal(new SettingChange.AllowDuringWaves(false), SettingChange.Parse("wavework", "0"));
        Assert.Equal(new SettingChange.Unlock(UnlockGroup.Doors, true), SettingChange.Parse("doorunlock", "1"));
        Assert.Equal(new SettingChange.Unlock(UnlockGroup.Windows, false), SettingChange.Parse("windowunlock", "0"));
        Assert.Equal(new SettingChange.Unlock(UnlockGroup.Fortifications, true), SettingChange.Parse("fortunlock", "1"));
        Assert.Null(SettingChange.Parse("fortunlock", "yes"));
    }

    /// <summary>A stepper pressed past the end of a range stays at the end instead of saving a value the config refuses.</summary>
    [Theory]
    [InlineData("proximityrange", "0", 1f)]
    [InlineData("proximityrange", "101", 100f)]
    [InlineData("crawlerdodge", "1.05", 1f)]
    [InlineData("crawlerdodge", "-0.05", 0f)]
    [InlineData("attackrate", "0", 0.25f)]
    [InlineData("attackrate", "10.25", 10f)]
    public void A_value_past_the_end_of_its_range_stays_at_the_end(string name, string value, float expected)
    {
        var kept = SettingChange.Parse(name, value) switch
        {
            SettingChange.ProximityRange range => range.Meters,
            SettingChange.CrawlerDodge dodge => dodge.Chance,
            SettingChange.AttackRate rate => rate.Rate,
            _ => float.NaN,
        };

        Assert.Equal(expected, kept, 3);
    }

    [Fact]
    public void A_value_between_steps_is_rounded_to_the_nearest_step()
    {
        Assert.Equal(new SettingChange.CrawlerDodge(0.15f), SettingChange.Parse("crawlerdodge", "0.1500001"));
        Assert.Equal(new SettingChange.AttackRate(1.25f), SettingChange.Parse("attackrate", "1.3"));
        Assert.Equal(new SettingChange.ProximityRange(15f), SettingChange.Parse("proximityrange", "15.4"));
    }

    [Theory]
    [InlineData("layoutcontrols", "true")]
    [InlineData("defaultmode", "Sometimes")]
    [InlineData("proximityrange", "far")]
    [InlineData("crawlerdodge", "NaN")]
    [InlineData("attackrate", "Infinity")]
    [InlineData("wavework", "true")]
    [InlineData("panelkey", "F8")]
    public void A_name_or_value_that_does_not_parse_changes_nothing(string name, string value)
    {
        Assert.Null(SettingChange.Parse(name, value));
    }
}

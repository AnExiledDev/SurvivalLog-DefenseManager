using System.Collections.Generic;
using System.Linq;
using DefenseManager.Core;
using Xunit;

namespace DefenseManager.Tests;

/// <summary>Every word the mod shows has an English and a Chinese version, and the game's language picks between them.</summary>
public class TextsTests
{
    public static TheoryData<int> Entries()
    {
        var data = new TheoryData<int>();

        foreach (var index in Enumerable.Range(0, Texts.All.Count)) data.Add(index);

        return data;
    }

    [Theory]
    [MemberData(nameof(Entries))]
    public void Every_text_has_plain_english_and_a_real_chinese_translation(int index)
    {
        var phrase = Texts.All[index];

        Assert.False(string.IsNullOrWhiteSpace(phrase.En));
        Assert.True(phrase.En.All(ch => ch is >= ' ' and <= '~'), phrase.En);
        Assert.Contains(phrase.Zh, ch => ch is >= '\u4e00' and <= '\u9fff');
        Assert.NotEqual(phrase.En, phrase.Zh);
    }

    [Theory]
    [MemberData(nameof(Entries))]
    public void Both_languages_fill_in_the_same_blanks(int index)
    {
        var phrase = Texts.All[index];
        var isSingular = Texts.Counts.Any(counted => counted.One == phrase);

        // English spells out the 1 in "1 repair", so only its count blank may go unused.
        if (!isSingular) Assert.Equal(phrase.En.Contains("{0}"), phrase.Zh.Contains("{0}"));

        Assert.Equal(phrase.En.Contains("{1}"), phrase.Zh.Contains("{1}"));
    }

    [Theory]
    [InlineData(1, "Repairing 1 defense.", "正在修理 1 个防御设施。")]
    [InlineData(3, "Repairing 3 defenses.", "正在修理 3 个防御设施。")]
    public void A_count_is_said_in_the_singular_or_plural_in_english_and_one_way_in_chinese(int count, string english, string chinese)
    {
        Assert.Equal(english, Texts.Repairing.Say(isChinese: false, count));
        Assert.Equal(chinese, Texts.Repairing.Say(isChinese: true, count));
    }

    [Fact]
    public void A_chinese_game_gets_its_press_results_and_reasons_in_chinese()
    {
        Assert.Equal("正在修理沙袋。", OutcomeMessage.Describe(new DefenseOutcome.Queued(Job.Repair, "沙袋"), isChinese: true));
        Assert.Equal("正在修理 2 个防御设施。离开「规划模式」后开始工作。", OutcomeMessage.Describe(new DefenseOutcome.Repaired(new RepairTally(2, 0, new List<Shortfall>(), 0, null, 0, 0)), isInLayout: true, isChinese: true));
        Assert.Equal("已排队修理：请等它完成或先取消", OutcomeMessage.WhyNotRemove(new RemoveVerdict.QueuedForRepair(), isChinese: true));
    }

    [Fact]
    public void A_chinese_game_gets_the_power_settings_named_in_chinese()
    {
        Assert.Equal("仅尸潮", PowerModes.Label(PowerMode.WaveOnly, isChinese: true));
        Assert.Equal("Wave only", PowerModes.Label(PowerMode.WaveOnly));
    }

    [Theory]
    [MemberData(nameof(Entries))]
    public void The_games_language_picks_the_text(int index)
    {
        var phrase = Texts.All[index];

        Assert.Equal(phrase.Zh, phrase.Pick(isChinese: true));
        Assert.Equal(phrase.En, phrase.Pick(isChinese: false));
    }

    [Fact]
    public void No_text_is_listed_twice()
    {
        Assert.Equal(Texts.All.Count, Texts.All.Distinct().Count());
    }
}

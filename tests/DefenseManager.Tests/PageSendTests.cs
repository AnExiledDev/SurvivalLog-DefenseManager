using System.Text.Json;
using DefenseManager.Core;
using Xunit;
using static DefenseManager.Tests.FakeGame;

namespace DefenseManager.Tests;

/// <summary>What goes to the page with each call: the words only when it lacks them, and only the rows that changed.</summary>
public class PageSendTests
{
    private static FakeGame Home() => new() { Defenses = { Def(1), Def(2, cur: 0, broken: true), Def(3, cur: 100) } };

    private static string View(FakeGame game) => DefenseView.Of(game.Read()) ?? throw new InvalidOperationException("no panel view");

    private static JsonElement[] Rows(string view) => JsonDocument.Parse(view).RootElement.GetProperty("defenses").EnumerateArray().ToArray();

    private static ViewDelta Holding(string view)
    {
        var delta = new ViewDelta();

        delta.Next(view);
        delta.Sent();

        return delta;
    }

    // ===== Rows =====

    /// <summary>The panel refreshes every second; rows the page already has must not be sent again.</summary>
    [Fact]
    public void Rows_the_page_already_holds_go_as_their_ids_and_a_changed_row_goes_whole()
    {
        var game = Home();
        var delta = Holding(View(game));

        game.Defenses[0] = Def(1, cur: 20);

        var rows = Rows(delta.Next(View(game)));

        Assert.Equal(JsonValueKind.Object, rows[0].ValueKind);
        Assert.Equal("1", rows[0].GetProperty("id").GetString());
        Assert.Equal("2", rows[1].GetString());
        Assert.Equal("3", rows[2].GetString());
    }

    /// <summary>A trimmed view keeps everything around the rows, so the page still gets the counts, the yard and the rest.</summary>
    [Fact]
    public void Only_the_rows_are_trimmed_and_the_rest_of_the_view_goes_as_it_is()
    {
        var view = View(Home());
        var trimmed = Holding(view).Next(view);
        var whole = JsonDocument.Parse(view).RootElement;
        var cut = JsonDocument.Parse(trimmed).RootElement;

        foreach (var name in new[] { "note", "locked", "repairAll", "clearAll", "materials", "yard" })
        {
            Assert.Equal(whole.GetProperty(name).GetRawText(), cut.GetProperty(name).GetRawText());
        }

        Assert.All(Rows(trimmed), row => Assert.Equal(JsonValueKind.String, row.ValueKind));
    }

    /// <summary>A call that did not run in the page left it without the rows, so they must go whole next time.</summary>
    [Fact]
    public void A_view_that_never_ran_in_the_page_leaves_its_rows_to_go_whole_again()
    {
        var view = View(Home());
        var delta = new ViewDelta();

        delta.Next(view);

        Assert.Same(view, delta.Next(view));
    }

    /// <summary>A reloaded page, or one that asked for everything again, holds no rows.</summary>
    [Fact]
    public void After_the_page_is_forgotten_every_row_goes_whole()
    {
        var view = View(Home());
        var delta = Holding(view);

        delta.Forget();

        Assert.Same(view, delta.Next(view));
    }

    [Fact]
    public void A_view_with_no_rows_goes_as_it_is()
    {
        var view = View(new FakeGame());
        var delta = Holding(view);

        Assert.Same(view, delta.Next(view));
        Assert.Same("{}", delta.Next("{}"));
    }

    /// <summary>With two rows under one id the page could not tell which one an id means.</summary>
    [Fact]
    public void Two_rows_with_one_id_send_the_view_whole()
    {
        const string view = "{\"defenses\":[{\"id\":\"1\",\"cur\":1},{\"id\":\"1\",\"cur\":2}]}";
        var delta = Holding(view);

        Assert.Same(view, delta.Next(view));
    }

    /// <summary>Braces and brackets inside a name must not end the row early.</summary>
    [Fact]
    public void A_row_ends_where_its_object_ends_whatever_its_strings_hold()
    {
        const string view = "{\"defenses\":[{\"id\":\"1\",\"name\":\"a \\\"}\\\" b\"},{\"id\":\"2\",\"name\":\"]\"}],\"tail\":1}";
        var delta = Holding(view);

        Assert.Equal("{\"defenses\":[\"1\",\"2\"],\"tail\":1}", delta.Next(view));
    }

    // ===== Words =====

    [Fact]
    public void The_words_go_with_the_first_call_and_not_again_while_the_language_stays()
    {
        var words = new WordsOnce();

        Assert.Equal(PageWords.Script(isChinese: false), words.Prefix(isChinese: false));

        words.Sent(isChinese: false);

        Assert.Equal("", words.Prefix(isChinese: false));
    }

    [Fact]
    public void Switching_the_games_language_sends_the_other_words()
    {
        var words = new WordsOnce();

        words.Sent(isChinese: false);

        Assert.Equal(PageWords.Script(isChinese: true), words.Prefix(isChinese: true));
    }

    /// <summary>A call that failed never put the words in the page.</summary>
    [Fact]
    public void Words_on_a_call_that_did_not_run_go_again()
    {
        var words = new WordsOnce();

        words.Prefix(isChinese: false);

        Assert.Equal(PageWords.Script(isChinese: false), words.Prefix(isChinese: false));
    }

    /// <summary>A reloaded page has lost the words with everything else.</summary>
    [Fact]
    public void A_reloaded_page_gets_the_words_again()
    {
        var words = new WordsOnce();

        words.Sent(isChinese: true);
        words.Forget();

        Assert.Equal(PageWords.Script(isChinese: true), words.Prefix(isChinese: true));
    }
}

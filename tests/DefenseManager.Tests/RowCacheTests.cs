using DefenseManager.Core;
using Xunit;

namespace DefenseManager.Tests;

public class RowCacheTests
{
    private sealed record Row(string Name);

    private static readonly IconIndex Icons = IconIndex.Build(new[] { "Res/Item/UI_Item_Icon_Wood.png" });

    private static readonly TableKey English = new(Config: 100, IsChinese: false, Icons);

    private static RowCache<TableKey, Row> Filled(TableKey key, Row row)
    {
        var cache = new RowCache<TableKey, Row>();

        cache.Follow(key);
        cache.Add(7, row);

        return cache;
    }

    /// <summary>The panel reads every second; a row read once must be the one handed back, not read again.</summary>
    [Fact]
    public void A_row_read_under_the_same_key_is_kept_and_handed_back()
    {
        var row = new Row("Spike trap");
        var cache = Filled(English, row);

        Assert.False(cache.Follow(English with { Icons = Icons }));
        Assert.True(cache.TryGet(7, out var kept));
        Assert.Same(row, kept);
    }

    /// <summary>A name read in English must not show once the game is switched to Chinese, nor rows of tables that were reloaded.</summary>
    [Theory]
    [InlineData(100, true)]
    [InlineData(200, false)]
    public void A_new_language_or_new_tables_drop_every_row(long config, bool isChinese)
    {
        var cache = Filled(English, new Row("Spike trap"));

        Assert.True(cache.Follow(new TableKey(config, isChinese, Icons)));
        Assert.False(cache.TryGet(7, out _));
    }

    /// <summary>The icons arrive after the first reads, which were made with none, so those rows have to be read again.</summary>
    [Fact]
    public void Icons_that_arrive_later_drop_the_rows_read_without_them()
    {
        var cache = Filled(English with { Icons = IconIndex.Empty }, new Row("Spike trap"));

        Assert.True(cache.Follow(English));
        Assert.False(cache.TryGet(7, out _));
    }

    /// <summary>An id with nothing to show (a row that is not a defense) is kept as nothing, so it is not looked up every read.</summary>
    [Fact]
    public void An_id_with_no_row_is_kept_as_none()
    {
        var cache = new RowCache<TableKey, Row>();

        cache.Follow(English);
        cache.Add(9, null);

        Assert.True(cache.TryGet(9, out var kept));
        Assert.Null(kept);
    }
}

public class IconLoadTests
{
    private static readonly IconIndex Icons = IconIndex.Build(new[] { "Res/Item/UI_Item_Icon_Wood.png" });

    /// <summary>The folder walk runs off the main thread; until it is done the panel draws letters rather than wait for it.</summary>
    [Fact]
    public void Until_the_walk_is_done_there_are_no_icons_and_after_it_the_same_ones_every_time()
    {
        var log = new List<string>();
        var walk = new TaskCompletionSource<(IconIndex, string)>();
        var load = new IconLoad(log.Add);

        load.Start(() => walk.Task);

        Assert.Same(IconIndex.Empty, load.Current);

        walk.SetResult((Icons, "1 web icons found"));

        Assert.Same(Icons, load.Current);
        Assert.Same(Icons, load.Current);
        Assert.Equal(new[] { "1 web icons found" }, log);
    }

    /// <summary>Over a thousand files: walking them again on every read is the stall the walk was moved off the main thread for.</summary>
    [Fact]
    public void The_walk_is_started_once_however_often_it_is_asked_for()
    {
        var walks = 0;
        var load = new IconLoad(_ => { });
        Func<Task<(IconIndex, string)>> walk = () =>
        {
            walks++;

            return Task.FromResult((Icons, "found"));
        };

        load.Start(walk);
        _ = load.Current;
        load.Start(walk);
        load.Start(walk);

        Assert.Equal(1, walks);
        Assert.Same(Icons, load.Current);
    }

    [Fact]
    public void A_walk_that_fails_leaves_letters_and_says_why_once()
    {
        var log = new List<string>();
        var walk = new TaskCompletionSource<(IconIndex, string)>();
        var load = new IconLoad(log.Add);

        load.Start(() => walk.Task);
        walk.SetException(new IOException("no such folder"));

        Assert.Same(IconIndex.Empty, load.Current);
        Assert.Same(IconIndex.Empty, load.Current);
        Assert.Equal(new[] { "no web icons, defenses and materials will show letters instead: no such folder" }, log);
    }
}

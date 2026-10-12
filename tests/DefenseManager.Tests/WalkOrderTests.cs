using DefenseManager.Core;
using Xunit;
using static DefenseManager.Tests.FakeGame;

namespace DefenseManager.Tests;

public class WalkOrderTests
{
    private static readonly Spot Start = new(0, 0, 0);

    private static long[] Ids(IReadOnlyList<Defense> round) => round.Select(defense => defense.Id).ToArray();

    [Fact]
    public void Each_stop_is_the_nearest_to_the_last_one_visited_not_to_the_start()
    {
        // Nearest to the start would be A, B, C (2, 3, 6 away). Walking on from A, C is 4 away and B is 5.
        var round = WalkOrder.Visit(Start, new[] { Def(2, x: -3), Def(3, x: 6), Def(1, x: 2) }, _ => true);

        Assert.Equal(new long[] { 1, 3, 2 }, Ids(round));
    }

    [Fact]
    public void Distance_counts_every_axis()
    {
        var round = WalkOrder.Visit(Start, new[] { Def(1, x: 3, z: 4), Def(2, x: 4.5) }, _ => true);

        Assert.Equal(new long[] { 2, 1 }, Ids(round));
    }

    [Fact]
    public void Equal_distances_go_to_the_lower_id_whatever_order_the_game_lists_them_in()
    {
        var forward = WalkOrder.Visit(Start, new[] { Def(7, x: 5), Def(4, x: -5) }, _ => true);
        var backward = WalkOrder.Visit(Start, new[] { Def(4, x: -5), Def(7, x: 5) }, _ => true);

        Assert.Equal(new long[] { 4, 7 }, Ids(forward));
        Assert.Equal(new long[] { 4, 7 }, Ids(backward));
    }

    [Fact]
    public void A_stop_turned_down_is_left_out_of_the_round()
    {
        var round = WalkOrder.Visit(Start, new[] { Def(1, x: 2), Def(2, x: -1.4), Def(3, x: 3.9) }, stop => stop.Id != 2);

        Assert.Equal(new long[] { 1, 3 }, Ids(round));
    }

    [Fact]
    public void Every_stop_is_offered_once_even_when_all_are_turned_down()
    {
        var offered = new List<long>();

        var round = WalkOrder.Visit(Start, new[] { Def(1, x: 2), Def(2, x: -1), Def(3, x: 5) }, stop =>
        {
            offered.Add(stop.Id);

            return false;
        });

        Assert.Empty(round);
        Assert.Equal(new long[] { 2, 1, 3 }, offered);
    }

    [Fact]
    public void After_a_stop_is_turned_down_the_next_is_nearest_to_where_the_walker_still_stands()
    {
        // A (x=1) is turned down. Standing at the start, B (x=-2) is nearer than C (x=3); standing at A it would be C.
        var round = WalkOrder.Visit(Start, new[] { Def(1, x: 1), Def(2, x: -2), Def(3, x: 3) }, stop => stop.Id != 1);

        Assert.Equal(new long[] { 2, 3 }, Ids(round));
    }

    [Fact]
    public void No_stops_make_an_empty_round()
    {
        Assert.Empty(WalkOrder.Visit(Start, Array.Empty<Defense>(), _ => true));
    }
}

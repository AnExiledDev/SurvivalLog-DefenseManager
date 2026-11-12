using DefenseManager.Core;
using Xunit;

namespace DefenseManager.Tests;

public class YardTests
{
    /// <summary>The game's square names do not run along the yard, so the columns must come from where squares stand.</summary>
    private static Yard Shuffled() => new(
        new[]
        {
            new YardSquare("HomeBuildingTowerPos09", 1, -18.325, 0, true, 0),
            new YardSquare("HomeBuildingTowerPos01", 1, -19.725, 0, true, 0),
            new YardSquare("HomeBuildingTowerPos40", 1, -19.025, 0, true, 0),
            new YardSquare("HomeBuildingTowerPos02", 2, -19.725, -0.7, false, 5),
        },
        new[] { new WallFeature(WallFeatureKind.Window, -19.375) });

    [Fact]
    public void Columns_run_left_to_right_by_position_not_by_the_number_in_the_name()
    {
        var yard = Shuffled();

        Assert.Equal(3, yard.Columns);
        Assert.Equal(2, yard.Rows);
        Assert.Equal(new GridPlace(1, 1), yard.PlaceOf(yard.Squares[1]));
        Assert.Equal(new GridPlace(1, 2), yard.PlaceOf(yard.Squares[2]));
        Assert.Equal(new GridPlace(1, 3), yard.PlaceOf(yard.Squares[0]));
        Assert.Equal(new GridPlace(2, 1), yard.PlaceOf(yard.Squares[3]));
    }

    [Fact]
    public void A_wall_feature_between_two_squares_sits_between_their_columns()
    {
        var yard = Shuffled();

        Assert.Equal(1.5, yard.ColumnAt(yard.Wall[0].X), 3);
    }

    /// <summary>
    /// A small copy of the Warehouse Manager's yard: it wraps round a corner of the house (top left, where x is low and
    /// z high), and the game's row numbers are bands that turn the corner, so one game row spans two lines of squares.
    /// </summary>
    internal static Yard Corner(IReadOnlyList<WallFeature>? wall = null) => new(CornerSquares(), wall ?? Array.Empty<WallFeature>());

    internal static YardSquare[] CornerSquares() => new[]
    {
        new YardSquare("WarehouseTowerPos01", 1, 1.4, 0, true, 0), new YardSquare("WarehouseTowerPos02", 2, 2.1, 0, false, 5),
        new YardSquare("WarehouseTowerPos03", 1, 0, -0.7, true, 0), new YardSquare("WarehouseTowerPos04", 1, 0.7, -0.7, true, 0),
        new YardSquare("WarehouseTowerPos05", 2, 1.4, -0.7, false, 5), new YardSquare("WarehouseTowerPos06", 2, 2.1, -0.7, false, 5),
        new YardSquare("WarehouseTowerPos07", 3, 0, -1.4, false, 9), new YardSquare("WarehouseTowerPos08", 3, 0.7, -1.4, false, 9),
        new YardSquare("WarehouseTowerPos09", 3, 1.4, -1.4, false, 9), new YardSquare("WarehouseTowerPos10", 3, 2.1, -1.4, false, 9),
    };

    [Fact]
    public void A_yard_round_a_corner_gets_a_row_for_each_line_of_squares_so_no_two_squares_share_a_place()
    {
        var yard = Corner();
        var places = yard.Squares.Select(yard.PlaceOf).ToList();

        Assert.Equal(3, yard.Rows);
        Assert.Equal(4, yard.Columns);
        Assert.Equal(places.Count, places.Distinct().Count());
        Assert.Equal(new GridPlace(1, 3), places[0]);
        Assert.Equal(new GridPlace(1, 4), places[1]);
        Assert.Equal(new GridPlace(2, 1), places[2]);
        Assert.False(yard.HasStraightRows);
    }

    [Fact]
    public void A_straight_yard_keeps_the_game_rows()
    {
        Assert.True(Shuffled().HasStraightRows);
    }

    [Fact]
    public void Row_1_is_the_side_by_the_house_whichever_way_the_yard_runs()
    {
        // The College Student's yard runs the same way as the Wage Slave's; this one runs the other way round.
        var yard = new Yard(new[] { new YardSquare("near", 1, 0, -3, true, 0), new YardSquare("far", 2, 0, -2.3, true, 0) }, Array.Empty<WallFeature>());

        Assert.Equal(new GridPlace(1, 1), yard.PlaceOf(yard.Squares[0]));
        Assert.Equal(new GridPlace(2, 1), yard.PlaceOf(yard.Squares[1]));
        Assert.True(yard.HasStraightRows);
    }

    [Fact]
    public void Only_the_doors_and_windows_in_the_wall_along_the_yard_are_kept()
    {
        // The Wage Slave's home: two rows of the yard, its door and window by the yard, and two windows at the far
        // side of the house.
        var squares = new[] { -30.925, -24.625, -18.325 }.SelectMany(x => new[] { new YardSquare("r1", 1, x, 4.58, true, 0), new YardSquare("r2", 2, x, 3.88, true, 0) }).ToList();
        var openings = new[]
        {
            new WallOpening(WallFeatureKind.Door, -21.15, 5.16),
            new WallOpening(WallFeatureKind.Window, -26.754, 5.16),
            new WallOpening(WallFeatureKind.Window, -44.48, 11.57),
            new WallOpening(WallFeatureKind.Window, -41.78, 19.842),
        };

        var wall = Yard.WallAlongTheYard(squares, openings);

        Assert.Equal(new[] { new WallFeature(WallFeatureKind.Window, -26.754), new WallFeature(WallFeatureKind.Door, -21.15) }, wall);
    }

    [Fact]
    public void Round_a_corner_only_the_window_on_the_house_side_of_row_1_is_in_the_wall()
    {
        // The others: a door by a lower row, a window past the end of row 1, one too far from it, and one out in the yard.
        var openings = new[]
        {
            new WallOpening(WallFeatureKind.Door, 0.35, -0.2),
            new WallOpening(WallFeatureKind.Window, 1.75, 0.5),
            new WallOpening(WallFeatureKind.Window, -3, 0.5),
            new WallOpening(WallFeatureKind.Window, 1.75, 3),
            new WallOpening(WallFeatureKind.Window, 2.0, -0.35),
        };

        var wall = Yard.WallAlongTheYard(CornerSquares(), openings);

        Assert.Equal(new[] { new WallFeature(WallFeatureKind.Window, 1.75) }, wall);
    }

    [Fact]
    public void A_defense_is_on_the_nearest_square_and_one_far_from_every_square_is_on_none()
    {
        var yard = Shuffled();

        Assert.Equal(new GridPlace(1, 2), yard.PlaceAt(new Spot(-19.1, 0.4, 0.1)));
        Assert.Equal(new GridPlace(2, 1), yard.PlaceAt(new Spot(-19.7, 0, -0.6)));
        Assert.Null(yard.PlaceAt(new Spot(-19.025, 0, 0.6)));
        Assert.Null(yard.PlaceAt(new Spot(-25, 0, 0)));
    }
}

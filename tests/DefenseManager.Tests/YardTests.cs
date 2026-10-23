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

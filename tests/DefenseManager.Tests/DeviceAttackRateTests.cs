using DefenseManager.Core;
using Xunit;

namespace DefenseManager.Tests;

public class DeviceAttackRateTests
{
    [Fact]
    public void A_higher_rate_strikes_more_often()
    {
        Assert.Equal(1.5f, DeviceAttackRate.From(2f).Interval(3f));
        Assert.Equal(6f, DeviceAttackRate.From(0.5f).Interval(3f));
    }

    [Fact]
    public void At_the_games_rate_the_interval_is_unchanged()
    {
        Assert.Equal(3f, DeviceAttackRate.From(1f).Interval(3f));
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(-1f)]
    public void A_device_with_no_interval_is_left_alone(float gameSeconds)
    {
        Assert.Equal(gameSeconds, DeviceAttackRate.From(4f).Interval(gameSeconds));
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(-3f)]
    [InlineData(float.NaN)]
    public void A_rate_that_is_not_above_zero_keeps_the_games_rate(float requested)
    {
        Assert.True(DeviceAttackRate.From(requested).IsGameRate);
    }

    [Theory]
    [InlineData(1f, 2f)]
    [InlineData(2f, 0.5f)]
    [InlineData(4f, 1f)]
    [InlineData(0.25f, 10f)]
    public void A_device_built_at_one_rate_strikes_as_if_built_at_the_new_one(float before, float after)
    {
        var from = DeviceAttackRate.From(before);
        var to = DeviceAttackRate.From(after);

        Assert.Equal(to.Interval(3f), to.Rescale(from.Interval(3f), from), 4);
    }

    [Fact]
    public void Back_at_the_games_rate_a_device_strikes_at_the_games_time_again()
    {
        var doubled = DeviceAttackRate.From(2f);

        Assert.Equal(3f, DeviceAttackRate.From(1f).Rescale(doubled.Interval(3f), doubled), 4);
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(-1f)]
    public void A_device_with_no_interval_still_has_none_after_a_rate_change(float seconds)
    {
        Assert.Equal(seconds, DeviceAttackRate.From(4f).Rescale(seconds, DeviceAttackRate.From(0.5f)));
    }

    [Theory]
    [InlineData(0.1f, 0.25f)]
    [InlineData(50f, 10f)]
    public void A_rate_out_of_range_is_pulled_to_the_nearest_end(float requested, float expected)
    {
        Assert.Equal(expected, DeviceAttackRate.From(requested).Value);
    }
}

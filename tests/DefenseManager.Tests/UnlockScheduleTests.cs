using DefenseManager.Core;
using Xunit;

namespace DefenseManager.Tests;

/// <summary>The Unlocks pass runs when something could have changed what the run should have, and not on a timer.</summary>
public class UnlockScheduleTests
{
    [Fact]
    public void A_pass_runs_when_the_mod_starts()
    {
        Assert.True(new UnlockSchedule(isHooked: true).IsDue(0));
    }

    [Fact]
    public void Once_a_loaded_run_has_been_checked_no_more_passes_run()
    {
        var schedule = new UnlockSchedule(isHooked: true);

        schedule.Ran(0, isLoading: false);

        Assert.False(schedule.IsDue(1000));
    }

    [Fact]
    public void A_setting_change_asks_for_a_pass_at_once()
    {
        var schedule = new UnlockSchedule(isHooked: true);

        schedule.Ran(0, isLoading: false);
        schedule.Ask();

        Assert.True(schedule.IsDue(0.1));
    }

    [Fact]
    public void A_run_still_loading_is_tried_again_a_second_later()
    {
        var schedule = new UnlockSchedule(isHooked: true);

        schedule.Ran(0, isLoading: true);

        Assert.False(schedule.IsDue(0.5));
        Assert.True(schedule.IsDue(1));
    }

    [Fact]
    public void After_a_start_the_last_run_looking_loaded_does_not_end_the_passes()
    {
        var schedule = new UnlockSchedule(isHooked: true);

        schedule.Started(10);
        schedule.Ran(10, isLoading: false);

        Assert.True(schedule.IsDue(11));
    }

    [Fact]
    public void After_a_start_the_passes_end_once_the_new_run_has_loaded()
    {
        var schedule = new UnlockSchedule(isHooked: true);

        schedule.Started(10);
        schedule.Ran(10, isLoading: true);
        schedule.Ran(11, isLoading: false);

        Assert.False(schedule.IsDue(1000));
    }

    [Fact]
    public void After_a_start_with_no_loading_seen_the_passes_end_after_a_while()
    {
        var schedule = new UnlockSchedule(isHooked: true);

        schedule.Started(10);
        schedule.Ran(10 + UnlockSchedule.StartSeconds, isLoading: false);

        Assert.False(schedule.IsDue(1000));
    }

    [Fact]
    public void Without_the_start_hook_a_pass_runs_every_5_seconds()
    {
        var schedule = new UnlockSchedule(isHooked: false);

        schedule.Ran(0, isLoading: false);

        Assert.False(schedule.IsDue(4.9));
        Assert.True(schedule.IsDue(5));
    }
}

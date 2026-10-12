using DefenseManager.Core;
using Xunit;
using static DefenseManager.Tests.FakeGame;

namespace DefenseManager.Tests;

public class OutcomeMessageTests
{
    private static readonly Need ThreeWood = new(Wood, "Wood", 3);
    private static readonly Need TwoNails = new(Nail, "Nail", 2);
    private static readonly Need OnePart = new(20302, "Part", 1);

    private static RepairTally Tally(int queued = 0, int alreadyQueued = 0, IReadOnlyList<Shortfall>? shortfalls = null, int blocked = 0, string? reason = null, int beingRemoved = 0, int refused = 0) =>
        new(queued, alreadyQueued, shortfalls ?? Array.Empty<Shortfall>(), blocked, reason, beingRemoved, refused);

    private static string? Repaired(RepairTally tally) => OutcomeMessage.Describe(new DefenseOutcome.Repaired(tally));

    private static string? Cleared(int queued = 0, int alreadyQueued = 0, int beingRepaired = 0, int refused = 0) =>
        OutcomeMessage.Describe(new DefenseOutcome.Cleared(new ClearTally(queued, alreadyQueued, beingRepaired, refused)));

    [Fact]
    public void A_press_outside_a_game_passes_without_a_word()
    {
        Assert.Null(OutcomeMessage.Describe(new DefenseOutcome.NoGame()));
    }

    [Fact]
    public void Repair_all_says_how_many_started_and_how_many_were_already_going()
    {
        Assert.Equal("Repairing 1 defense.", Repaired(Tally(queued: 1)));
        Assert.Equal("Repairing 4 defenses.", Repaired(Tally(queued: 4)));
        Assert.Equal("Repairing 2 defenses, 3 more already queued.", Repaired(Tally(queued: 2, alreadyQueued: 3)));
    }

    [Fact]
    public void A_second_repair_all_press_reports_the_repairs_still_queued()
    {
        Assert.Equal("Already repairing: 1 repair still queued.", Repaired(Tally(alreadyQueued: 1)));
        Assert.Equal("Already repairing: 3 repairs still queued.", Repaired(Tally(alreadyQueued: 3)));
    }

    [Fact]
    public void Every_defense_left_unrepaired_is_accounted_for()
    {
        var tally = Tally(queued: 2, shortfalls: new[] { new Shortfall("Spike barrier", new[] { ThreeWood }) }, blocked: 1, reason: "Needs a workbench", beingRemoved: 1, refused: 1);

        Assert.Equal(
            "Repairing 2 defenses. 1 left for lack of materials: Spike barrier needs 3 more Wood. 1 can't be repaired right now: Needs a workbench. 1 queued for removal, so not repaired. 1 refused by the game's queue.",
            Repaired(tally));
    }

    [Fact]
    public void A_shortfall_lists_every_missing_material()
    {
        var tally = Tally(shortfalls: new[] { new Shortfall("Sandbags", new[] { ThreeWood }), new Shortfall("Electric fence", new[] { ThreeWood, TwoNails, OnePart }), new Shortfall("Chainsaw", new[] { TwoNails, OnePart }) });

        Assert.Equal(
            "Nothing repaired. 3 left for lack of materials: Sandbags needs 3 more Wood; Electric fence needs 3 more Wood, 2 more Nail and 1 more Part; Chainsaw needs 2 more Nail and 1 more Part.",
            Repaired(tally));
    }

    [Fact]
    public void Past_three_shortfalls_the_rest_are_counted_not_named()
    {
        var shortfalls = Enumerable.Range(1, 5).Select(number => new Shortfall($"Trap {number}", new[] { OnePart })).ToList();

        Assert.Equal(
            "Nothing repaired. 5 left for lack of materials: Trap 1 needs 1 more Part; Trap 2 needs 1 more Part; Trap 3 needs 1 more Part; and 2 more.",
            Repaired(Tally(shortfalls: shortfalls)));
    }

    [Fact]
    public void Blocked_repairs_without_a_reason_are_still_counted()
    {
        Assert.Equal("Nothing repaired. 2 can't be repaired right now.", Repaired(Tally(blocked: 2)));
        Assert.Equal("Nothing repaired. 2 can't be repaired right now.", Repaired(Tally(blocked: 2, reason: "  ")));
    }

    [Fact]
    public void The_games_reason_loses_its_colour_tags_and_its_own_full_stop()
    {
        Assert.Equal("Nothing repaired. 1 can't be repaired right now: Needs a workbench.", Repaired(Tally(blocked: 1, reason: "<color=#ff5555>Needs a workbench.</color>")));
        Assert.Equal("Nothing repaired. 1 can't be repaired right now: \u5f53\u524d\u65e0\u6cd5\u4f7f\u7528.", Repaired(Tally(blocked: 1, reason: "<#ff0000>\u5f53\u524d\u65e0\u6cd5\u4f7f\u7528\u3002")));
    }

    [Fact]
    public void Clear_all_says_how_many_started_and_what_it_kept()
    {
        Assert.Equal("Removing 1 broken defense.", Cleared(queued: 1));
        Assert.Equal("Removing 2 broken defenses, 1 more already queued.", Cleared(queued: 2, alreadyQueued: 1));
        Assert.Equal("Already clearing: 2 removals still queued.", Cleared(alreadyQueued: 2));
        Assert.Equal("Nothing cleared. 1 queued for repair, so kept. 1 refused by the game's queue.", Cleared(beingRepaired: 1, refused: 1));
    }

    [Fact]
    public void A_refused_row_repair_says_why_in_the_defenses_own_name()
    {
        string? Refused(RepairVerdict verdict) => OutcomeMessage.Describe(new DefenseOutcome.RepairRefused("Sandbags", verdict));

        Assert.Equal("Sandbags is already queued for repair.", Refused(new RepairVerdict.AlreadyQueued()));
        Assert.Equal("Sandbags is queued for removal.", Refused(new RepairVerdict.QueuedForRemoval()));
        Assert.Equal("Sandbags needs no repair.", Refused(new RepairVerdict.Undamaged()));
        Assert.Equal("Can't repair Sandbags: Needs a workbench.", Refused(new RepairVerdict.Blocked("Needs a workbench.")));
        Assert.Equal("Can't repair Sandbags right now.", Refused(new RepairVerdict.Blocked("")));
        Assert.Equal("Can't repair Sandbags: needs 3 more Wood and 2 more Nail.", Refused(new RepairVerdict.Short(new[] { ThreeWood, TwoNails })));
    }

    [Fact]
    public void A_refused_row_remove_says_why()
    {
        Assert.Equal("Sandbags is already queued for removal.", OutcomeMessage.Describe(new DefenseOutcome.RemoveRefused("Sandbags", new RemoveVerdict.AlreadyQueued())));
        Assert.Equal("Sandbags is queued for repair: let it finish or cancel it first.", OutcomeMessage.Describe(new DefenseOutcome.RemoveRefused("Sandbags", new RemoveVerdict.QueuedForRepair())));
    }

    [Fact]
    public void Row_jobs_and_failures_read_as_sentences()
    {
        Assert.Equal("Repairing Sandbags.", OutcomeMessage.Describe(new DefenseOutcome.Queued(Job.Repair, "Sandbags")));
        Assert.Equal("Removing Sandbags.", OutcomeMessage.Describe(new DefenseOutcome.Queued(Job.Remove, "Sandbags")));
        Assert.Equal("The game would not queue the removal of Sandbags.", OutcomeMessage.Describe(new DefenseOutcome.NotQueued(Job.Remove, "Sandbags")));
        Assert.Equal("Could not start the repair, see BepInEx/defense-manager.log.", OutcomeMessage.Describe(new DefenseOutcome.StartFailed(Job.Repair, 1)));
        Assert.Equal("Could not start the removals, see BepInEx/defense-manager.log.", OutcomeMessage.Describe(new DefenseOutcome.StartFailed(Job.Remove, 3)));
    }

    [Fact]
    public void Presses_that_touch_nothing_say_why()
    {
        Assert.Equal(OutcomeMessage.WaveLockedText, OutcomeMessage.Describe(new DefenseOutcome.WaveLocked()));
        Assert.Equal(OutcomeMessage.AwayFromHomeText, OutcomeMessage.Describe(new DefenseOutcome.AwayFromHome()));
        Assert.Equal("Finish or cancel your queued actions first.", OutcomeMessage.Describe(new DefenseOutcome.QueueBusy()));
        Assert.Equal("Every defense is in full repair.", OutcomeMessage.Describe(new DefenseOutcome.NothingToDo(Job.Repair)));
        Assert.Equal("No broken defenses.", OutcomeMessage.Describe(new DefenseOutcome.NothingToDo(Job.Remove)));
        Assert.Equal("That defense is gone.", OutcomeMessage.Describe(new DefenseOutcome.Gone()));
        Assert.Equal("Can't manage defenses right now: no character.", OutcomeMessage.Describe(new DefenseOutcome.NotReady("no character.")));
    }

    [Fact]
    public void A_rows_repair_button_carries_a_reason_only_when_the_row_does_not_already_show_one()
    {
        Assert.Null(OutcomeMessage.WhyNot(new RepairVerdict.Allowed()));
        Assert.Null(OutcomeMessage.WhyNot(new RepairVerdict.Undamaged()));
        Assert.Null(OutcomeMessage.WhyNot(new RepairVerdict.AlreadyQueued()));
        Assert.Null(OutcomeMessage.WhyNot(new RepairVerdict.QueuedForRemoval()));
        Assert.Equal("Needs a workbench", OutcomeMessage.WhyNot(new RepairVerdict.Blocked("<b>Needs a workbench!</b>")));
        Assert.Equal("The game would not allow it", OutcomeMessage.WhyNot(new RepairVerdict.Blocked("")));
        Assert.Equal("Needs 3 more Wood", OutcomeMessage.WhyNot(new RepairVerdict.Short(new[] { ThreeWood })));
    }

    [Fact]
    public void A_rows_remove_button_carries_a_reason_only_while_a_repair_holds_it()
    {
        Assert.Null(OutcomeMessage.WhyNotRemove(new RemoveVerdict.Allowed()));
        Assert.Null(OutcomeMessage.WhyNotRemove(new RemoveVerdict.AlreadyQueued()));
        Assert.Equal("Repair queued: let it finish or cancel it first", OutcomeMessage.WhyNotRemove(new RemoveVerdict.QueuedForRepair()));
    }

    [Fact]
    public void A_press_in_Layout_that_lines_up_work_says_the_work_waits_for_Layout_to_close()
    {
        string? InLayout(DefenseOutcome outcome) => OutcomeMessage.Describe(outcome, isInLayout: true);

        Assert.Equal("Repairing Sandbags. " + OutcomeMessage.InLayoutText, InLayout(new DefenseOutcome.Queued(Job.Repair, "Sandbags")));
        Assert.Equal("Removing Sandbags. " + OutcomeMessage.InLayoutText, InLayout(new DefenseOutcome.Queued(Job.Remove, "Sandbags")));
        Assert.Equal("Repairing 2 defenses. " + OutcomeMessage.InLayoutText, InLayout(new DefenseOutcome.Repaired(Tally(queued: 2))));
        Assert.Equal("Removing 3 broken defenses. " + OutcomeMessage.InLayoutText, InLayout(new DefenseOutcome.Cleared(new ClearTally(3, 0, 0, 0))));
    }

    [Fact]
    public void A_second_press_in_Layout_also_says_the_queued_work_is_waiting()
    {
        Assert.Equal("Already repairing: 2 repairs still queued. " + OutcomeMessage.InLayoutText, OutcomeMessage.Describe(new DefenseOutcome.Repaired(Tally(alreadyQueued: 2)), isInLayout: true));
        Assert.Equal("Already clearing: 1 removal still queued. " + OutcomeMessage.InLayoutText, OutcomeMessage.Describe(new DefenseOutcome.Cleared(new ClearTally(0, 1, 0, 0)), isInLayout: true));
    }

    [Fact]
    public void Outside_Layout_a_press_reads_exactly_as_before()
    {
        Assert.Equal("Repairing Sandbags.", OutcomeMessage.Describe(new DefenseOutcome.Queued(Job.Repair, "Sandbags"), isInLayout: false));
        Assert.Equal("Repairing 2 defenses.", OutcomeMessage.Describe(new DefenseOutcome.Repaired(Tally(queued: 2)), isInLayout: false));
        Assert.Equal("Removing 3 broken defenses.", OutcomeMessage.Describe(new DefenseOutcome.Cleared(new ClearTally(3, 0, 0, 0)), isInLayout: false));
    }

    [Fact]
    public void A_press_in_Layout_that_lined_up_nothing_does_not_promise_any_work()
    {
        string? InLayout(DefenseOutcome outcome) => OutcomeMessage.Describe(outcome, isInLayout: true);

        Assert.Null(InLayout(new DefenseOutcome.NoGame()));
        Assert.Equal(OutcomeMessage.WaveLockedText, InLayout(new DefenseOutcome.WaveLocked()));
        Assert.Equal(OutcomeMessage.AwayFromHomeText, InLayout(new DefenseOutcome.AwayFromHome()));
        Assert.Equal("Finish or cancel your queued actions first.", InLayout(new DefenseOutcome.QueueBusy()));
        Assert.Equal("Every defense is in full repair.", InLayout(new DefenseOutcome.NothingToDo(Job.Repair)));
        Assert.Equal("That defense is gone.", InLayout(new DefenseOutcome.Gone()));
        Assert.Equal("Sandbags needs no repair.", InLayout(new DefenseOutcome.RepairRefused("Sandbags", new RepairVerdict.Undamaged())));
        Assert.Equal("The game would not queue the removal of Sandbags.", InLayout(new DefenseOutcome.NotQueued(Job.Remove, "Sandbags")));
        Assert.Equal("Could not start the repair, see BepInEx/defense-manager.log.", InLayout(new DefenseOutcome.StartFailed(Job.Repair, 1)));
        Assert.Equal("Nothing repaired. 2 can't be repaired right now.", InLayout(new DefenseOutcome.Repaired(Tally(blocked: 2))));
        Assert.Equal("Nothing cleared. 1 queued for repair, so kept.", InLayout(new DefenseOutcome.Cleared(new ClearTally(0, 0, 1, 0))));
    }
}

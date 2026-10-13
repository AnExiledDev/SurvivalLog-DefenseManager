using DefenseManager.Core;
using Xunit;
using static DefenseManager.Tests.FakeGame;

namespace DefenseManager.Tests;

public class DefenseRunTests
{
    private static readonly Func<IDefenseGame, DefenseOutcome>[] EveryPress =
    {
        DefenseRun.RepairAll,
        DefenseRun.ClearBroken,
        game => DefenseRun.Repair(game, 1),
        game => DefenseRun.Remove(game, 1),
    };

    private static RepairTally Repaired(DefenseOutcome outcome) => Assert.IsType<DefenseOutcome.Repaired>(outcome).Tally;

    private static ClearTally Cleared(DefenseOutcome outcome) => Assert.IsType<DefenseOutcome.Cleared>(outcome).Tally;

    private static Defense Broken(long id, double x = 0) => Def(id, x: x, cur: 0, broken: true);

    private static Defense Intact(long id, double x = 0) => Def(id, x: x, cur: 100, max: 100);

    // Repair all

    [Fact]
    public void Repair_all_walks_the_damaged_defenses_nearest_next_from_the_character()
    {
        var game = new FakeGame { Defenses = { Def(2, x: -3), Def(3, x: 6), Def(1, x: 2) } };

        var tally = Repaired(DefenseRun.RepairAll(game));

        Assert.Equal(new long[] { 1, 3, 2 }, game.Repairing);
        Assert.Equal(3, tally.Queued);
    }

    [Fact]
    public void Repair_all_starts_the_walk_where_the_character_stands()
    {
        var game = new FakeGame { Character = new Spot(10, 0, 0), Defenses = { Def(1, x: 2), Def(2, x: 9) } };

        DefenseRun.RepairAll(game);

        Assert.Equal(new long[] { 2, 1 }, game.Repairing);
    }

    [Fact]
    public void Repair_all_never_queues_a_defense_in_full_repair()
    {
        var game = new FakeGame { Defenses = { Intact(1), Def(2) } };

        DefenseRun.RepairAll(game);

        Assert.Equal(new long[] { 2 }, game.Repairing);
    }

    [Fact]
    public void Repair_all_queues_while_materials_last_and_names_what_the_rest_need()
    {
        var game = new FakeGame
        {
            Stock = { [Wood] = 10 },
            Defenses = { Def(1, x: 1, cost: Cost((Wood, 6))), Def(2, x: 2, cost: Cost((Wood, 6)), name: "Spike barrier") },
        };

        var tally = Repaired(DefenseRun.RepairAll(game));

        Assert.Equal(new long[] { 1 }, game.Repairing);
        Assert.Empty(game.FailingRepairs);

        var shortfall = Assert.Single(tally.Short);

        Assert.Equal("Spike barrier", shortfall.Name);
        Assert.Equal(new[] { new Need(Wood, "Wood", 2) }, shortfall.Missing);
    }

    [Fact]
    public void A_costly_repair_does_not_hold_back_a_cheaper_one_behind_it()
    {
        var game = new FakeGame
        {
            Stock = { [Wood] = 5 },
            Defenses = { Def(1, x: 1, cost: Cost((Wood, 8))), Def(2, x: 2, cost: Cost((Wood, 3))), Def(3, x: 3, cost: Cost((Wood, 3))) },
        };

        var tally = Repaired(DefenseRun.RepairAll(game));

        Assert.Equal(new long[] { 2 }, game.Repairing);
        Assert.Empty(game.FailingRepairs);
        Assert.Equal(new[] { "Defense 1", "Defense 3" }, tally.Short.Select(shortfall => shortfall.Name));
    }

    [Fact]
    public void A_repair_needing_two_materials_is_queued_only_when_both_cover_it()
    {
        var game = new FakeGame
        {
            Stock = { [Wood] = 10, [Nail] = 1 },
            Defenses = { Def(1, cost: Cost((Wood, 2), (Nail, 3))) },
        };

        var tally = Repaired(DefenseRun.RepairAll(game));

        Assert.Empty(game.Repairing);
        Assert.Equal(new[] { new Need(Nail, "Nail", 2) }, Assert.Single(tally.Short).Missing);
    }

    [Fact]
    public void Repairs_already_queued_keep_their_materials_from_a_later_press()
    {
        var game = new FakeGame { Stock = { [Wood] = 10 }, Defenses = { Def(1, x: 1, cost: Cost((Wood, 6))) } };

        DefenseRun.RepairAll(game);
        game.Defenses.Add(Def(2, x: 2, cost: Cost((Wood, 6))));

        var tally = Repaired(DefenseRun.RepairAll(game));

        Assert.Equal(new long[] { 1 }, game.Repairing);
        Assert.Empty(game.FailingRepairs);
        Assert.Equal(1, tally.AlreadyQueued);
        Assert.Equal(new[] { new Need(Wood, "Wood", 2) }, Assert.Single(tally.Short).Missing);
    }

    [Fact]
    public void Repairs_the_game_would_refuse_are_left_and_counted_with_its_reason()
    {
        var game = new FakeGame { Defenses = { Def(1, x: 1, blocked: "Needs a workbench"), Def(2, x: 2, blocked: "Something else"), Def(3, x: 3) } };

        var tally = Repaired(DefenseRun.RepairAll(game));

        Assert.Equal(new long[] { 3 }, game.Repairing);
        Assert.Empty(game.FailingRepairs);
        Assert.Equal(2, tally.Blocked);
        Assert.Equal("Needs a workbench", tally.BlockedReason);
    }

    [Fact]
    public void Repair_all_lines_up_only_the_repairs_the_stamina_covers()
    {
        var game = new FakeGame { Stamina = new RepairStamina(25, 10), Defenses = { Def(1, x: 1), Def(2, x: 2), Def(3, x: 3), Def(4, x: 4) } };

        var tally = Repaired(DefenseRun.RepairAll(game));

        Assert.Equal(new long[] { 1, 2 }, game.Repairing);
        Assert.Empty(game.FailingRepairs);
        Assert.Equal(2, tally.Blocked);
        Assert.Equal(RepairCheck.OutStaminaReason(10), tally.BlockedReason);
    }

    [Fact]
    public void Repair_all_leaves_the_stamina_of_repairs_already_queued_to_them()
    {
        var game = new FakeGame { Stamina = new RepairStamina(30, 10), Defenses = { Def(1, x: 1), Def(2, x: 2), Def(3, x: 3), Def(4, x: 4) } };

        DefenseRun.Repair(game, 4);
        DefenseRun.Repair(game, 3);
        DefenseRun.RepairAll(game);

        Assert.Equal(new long[] { 4, 3, 1 }, game.Repairing);
        Assert.Empty(game.FailingRepairs);
    }

    [Fact]
    public void Repair_all_is_not_capped_when_the_stamina_could_not_be_read()
    {
        var game = new FakeGame { Stamina = null, Defenses = { Def(1, x: 1), Def(2, x: 2), Def(3, x: 3) } };

        DefenseRun.RepairAll(game);

        Assert.Equal(new long[] { 1, 2, 3 }, game.Repairing);
    }

    [Fact]
    public void A_second_press_reports_progress_and_queues_nothing_twice()
    {
        var game = new FakeGame { Defenses = { Def(1, x: 1), Def(2, x: 2), Def(3, x: 3) } };

        DefenseRun.RepairAll(game);

        var tally = Repaired(DefenseRun.RepairAll(game));

        Assert.Equal(new long[] { 1, 2, 3 }, game.Repairing);
        Assert.Equal(0, tally.Queued);
        Assert.Equal(3, tally.AlreadyQueued);
    }

    [Fact]
    public void Repair_all_with_nothing_damaged_has_nothing_to_do()
    {
        var game = new FakeGame { Defenses = { Intact(1), Intact(2) } };

        Assert.Equal(new DefenseOutcome.NothingToDo(Job.Repair), DefenseRun.RepairAll(game));
        Assert.Equal(new[] { Walking }, game.Running);
    }

    [Fact]
    public void Repair_all_never_trades_actions_the_player_queued_for_repairs()
    {
        var game = new FakeGame { StartClearsQueue = true, Running = { Eating }, Defenses = { Def(1) } };

        Assert.Equal(new DefenseOutcome.QueueBusy(), DefenseRun.RepairAll(game));
        Assert.Empty(game.LinedUp);
        Assert.Equal(new[] { Walking, Eating }, game.Running);
    }

    [Fact]
    public void Repair_all_may_replace_what_the_character_chose_to_do_itself()
    {
        var game = new FakeGame { StartClearsQueue = true, Defenses = { Def(1, x: 1), Def(2, x: 2) } };

        DefenseRun.RepairAll(game);

        Assert.Equal(new[] { new Act(Job.Repair, 1), new Act(Job.Repair, 2) }, game.Running);
    }

    [Fact]
    public void Repairs_join_the_end_of_the_queue_and_keep_the_players_actions()
    {
        var game = new FakeGame { Running = { Eating }, Defenses = { Def(1) } };

        DefenseRun.RepairAll(game);

        Assert.Equal(new[] { Walking, Eating, new Act(Job.Repair, 1) }, game.Running);
    }

    [Fact]
    public void A_press_with_nothing_affordable_names_the_shortfalls_and_starts_nothing()
    {
        // Starting an empty batch would cancel the walk, and the player's queue is not in the way of a press that queues nothing.
        var game = new FakeGame { StartClearsQueue = true, Running = { Eating }, Defenses = { Def(1, cost: Cost((Wood, 2))) } };

        var tally = Repaired(DefenseRun.RepairAll(game));

        Assert.Equal(0, tally.Queued);
        Assert.Single(tally.Short);
        Assert.Equal(new[] { Walking, Eating }, game.Running);
    }

    [Fact]
    public void Repair_all_reports_a_batch_the_game_would_not_start()
    {
        var game = new FakeGame { FailsToStart = true, Defenses = { Def(1), Def(2) } };

        Assert.Equal(new DefenseOutcome.StartFailed(Job.Repair, 2), DefenseRun.RepairAll(game));
    }

    [Fact]
    public void Repairs_the_game_queue_turns_down_are_counted_and_the_rest_still_start()
    {
        var game = new FakeGame { Refuses = { 2 }, Defenses = { Def(1, x: 1), Def(2, x: 2), Def(3, x: 3) } };

        var tally = Repaired(DefenseRun.RepairAll(game));

        Assert.Equal(new long[] { 1, 3 }, game.Repairing);
        Assert.Equal(2, tally.Queued);
        Assert.Equal(1, tally.Refused);
    }

    [Fact]
    public void Repair_all_leaves_a_defense_queued_for_removal()
    {
        var game = new FakeGame { Defenses = { Def(1, x: 1), Def(2, x: 2) } };

        DefenseRun.Remove(game, 1);

        var tally = Repaired(DefenseRun.RepairAll(game));

        Assert.Equal(new long[] { 2 }, game.Repairing);
        Assert.Equal(1, tally.BeingRemoved);
    }

    // Clear all broken

    [Fact]
    public void Clear_all_takes_down_only_broken_defenses_nearest_next()
    {
        var game = new FakeGame { Defenses = { Broken(1, x: 5), Def(2, x: 1), Broken(3, x: -2), Intact(4, x: 0.5) } };

        var tally = Cleared(DefenseRun.ClearBroken(game));

        Assert.Equal(new long[] { 3, 1 }, game.Removing);
        Assert.Empty(game.Repairing);
        Assert.Equal(2, tally.Queued);
    }

    [Fact]
    public void Clear_all_keeps_a_broken_defense_the_player_queued_for_repair()
    {
        var game = new FakeGame { Defenses = { Broken(1, x: 1), Broken(2, x: 2) } };

        DefenseRun.Repair(game, 1);

        var tally = Cleared(DefenseRun.ClearBroken(game));

        Assert.Equal(new long[] { 2 }, game.Removing);
        Assert.Equal(1, tally.BeingRepaired);
    }

    [Fact]
    public void Clear_all_with_nothing_broken_has_nothing_to_do()
    {
        var game = new FakeGame { Defenses = { Def(1), Intact(2) } };

        Assert.Equal(new DefenseOutcome.NothingToDo(Job.Remove), DefenseRun.ClearBroken(game));
        Assert.Equal(new[] { Walking }, game.Running);
    }

    [Fact]
    public void A_second_clear_queues_no_removal_twice_and_starts_nothing()
    {
        var game = new FakeGame { Defenses = { Broken(1), Broken(2, x: 1) } };

        DefenseRun.ClearBroken(game);

        var runningAfterFirst = game.Running.ToList();

        var tally = Cleared(DefenseRun.ClearBroken(game));

        Assert.Equal(new ClearTally(0, 2, 0, 0), tally);
        Assert.Equal(runningAfterFirst, game.Running);
    }

    [Fact]
    public void Clear_all_never_trades_actions_the_player_queued_for_removals()
    {
        var game = new FakeGame { StartClearsQueue = true, Running = { Eating }, Defenses = { Broken(1) } };

        Assert.Equal(new DefenseOutcome.QueueBusy(), DefenseRun.ClearBroken(game));
        Assert.Empty(game.LinedUp);
        Assert.Equal(new[] { Walking, Eating }, game.Running);
    }

    [Fact]
    public void Clear_all_reports_a_batch_the_game_would_not_start()
    {
        var game = new FakeGame { FailsToStart = true, Defenses = { Broken(1) } };

        Assert.Equal(new DefenseOutcome.StartFailed(Job.Remove, 1), DefenseRun.ClearBroken(game));
    }

    [Fact]
    public void Removals_the_game_queue_turns_down_are_counted()
    {
        var game = new FakeGame { Refuses = { 1 }, Defenses = { Broken(1), Broken(2, x: 1) } };

        Assert.Equal(new ClearTally(1, 0, 0, 1), Cleared(DefenseRun.ClearBroken(game)));
        Assert.Equal(new long[] { 2 }, game.Removing);
    }

    // One defense's Repair button

    [Fact]
    public void A_row_repair_queues_and_starts_that_one_repair()
    {
        var game = new FakeGame { Defenses = { Def(1, name: "Sandbags"), Def(2) } };

        Assert.Equal(new DefenseOutcome.Queued(Job.Repair, "Sandbags"), DefenseRun.Repair(game, 1));
        Assert.Equal(new long[] { 1 }, game.Repairing);
    }

    [Fact]
    public void A_row_repair_the_materials_do_not_cover_is_refused_with_what_it_needs()
    {
        var game = new FakeGame { Stock = { [Wood] = 1 }, Defenses = { Def(1, cost: Cost((Wood, 4))) } };

        var refused = Assert.IsType<DefenseOutcome.RepairRefused>(DefenseRun.Repair(game, 1));

        Assert.Equal(new[] { new Need(Wood, "Wood", 3) }, Assert.IsType<RepairVerdict.Short>(refused.Verdict).Missing);
        Assert.Empty(game.LinedUp);
        Assert.Empty(game.Repairing);
    }

    [Fact]
    public void A_row_repair_counts_the_materials_of_repairs_already_queued()
    {
        var game = new FakeGame { Stock = { [Wood] = 10 }, Defenses = { Def(1, cost: Cost((Wood, 6))), Def(2, cost: Cost((Wood, 6))) } };

        DefenseRun.Repair(game, 1);

        var refused = Assert.IsType<DefenseOutcome.RepairRefused>(DefenseRun.Repair(game, 2));

        Assert.IsType<RepairVerdict.Short>(refused.Verdict);
        Assert.Empty(game.FailingRepairs);
    }

    [Fact]
    public void A_row_repair_is_refused_when_repairs_already_queued_use_the_stamina()
    {
        var game = new FakeGame { Stamina = new RepairStamina(19, 10), Defenses = { Def(1), Def(2) } };

        DefenseRun.Repair(game, 1);

        var refused = Assert.IsType<DefenseOutcome.RepairRefused>(DefenseRun.Repair(game, 2));

        Assert.Equal(new RepairVerdict.Blocked(RepairCheck.OutStaminaReason(10)), refused.Verdict);
        Assert.Empty(game.FailingRepairs);
    }

    [Fact]
    public void A_row_repair_the_game_would_refuse_is_refused_with_its_reason()
    {
        var game = new FakeGame { Defenses = { Def(1, blocked: "Needs a workbench") } };

        var refused = Assert.IsType<DefenseOutcome.RepairRefused>(DefenseRun.Repair(game, 1));

        Assert.Equal(new RepairVerdict.Blocked("Needs a workbench"), refused.Verdict);
        Assert.Empty(game.Repairing);
    }

    [Fact]
    public void A_row_repair_of_a_defense_in_full_repair_is_refused()
    {
        var game = new FakeGame { Defenses = { Intact(1) } };

        Assert.Equal(new DefenseOutcome.RepairRefused("Defense 1", new RepairVerdict.Undamaged()), DefenseRun.Repair(game, 1));
        Assert.Empty(game.Repairing);
    }

    [Fact]
    public void A_row_repair_of_a_defense_queued_for_removal_is_refused()
    {
        var game = new FakeGame { Defenses = { Def(1) } };

        DefenseRun.Remove(game, 1);

        Assert.Equal(new DefenseOutcome.RepairRefused("Defense 1", new RepairVerdict.QueuedForRemoval()), DefenseRun.Repair(game, 1));
        Assert.Empty(game.Repairing);
    }

    [Fact]
    public void A_row_repair_pressed_twice_queues_one_repair()
    {
        var game = new FakeGame { Defenses = { Def(1) } };

        DefenseRun.Repair(game, 1);

        Assert.Equal(new DefenseOutcome.RepairRefused("Defense 1", new RepairVerdict.AlreadyQueued()), DefenseRun.Repair(game, 1));
        Assert.Equal(new long[] { 1 }, game.Repairing);
    }

    // One defense's Remove button

    [Fact]
    public void A_row_remove_queues_the_removal_at_once_without_asking()
    {
        var game = new FakeGame { Defenses = { Intact(1) with { Name = "Electric fence" } } };

        Assert.Equal(new DefenseOutcome.Queued(Job.Remove, "Electric fence"), DefenseRun.Remove(game, 1));
        Assert.Equal(new long[] { 1 }, game.Removing);
    }

    [Fact]
    public void A_row_remove_is_refused_while_a_repair_of_it_is_queued()
    {
        var game = new FakeGame { Defenses = { Def(1) } };

        DefenseRun.Repair(game, 1);

        Assert.Equal(new DefenseOutcome.RemoveRefused("Defense 1", new RemoveVerdict.QueuedForRepair()), DefenseRun.Remove(game, 1));
        Assert.Empty(game.Removing);
    }

    [Fact]
    public void A_row_remove_pressed_twice_queues_one_removal()
    {
        var game = new FakeGame { Defenses = { Def(1) } };

        DefenseRun.Remove(game, 1);

        Assert.Equal(new DefenseOutcome.RemoveRefused("Defense 1", new RemoveVerdict.AlreadyQueued()), DefenseRun.Remove(game, 1));
        Assert.Equal(new long[] { 1 }, game.Removing);
    }

    // Both row buttons

    [Fact]
    public void A_row_button_for_a_defense_that_is_gone_says_so()
    {
        var game = new FakeGame { Defenses = { Def(1) } };

        Assert.Equal(new DefenseOutcome.Gone(), DefenseRun.Repair(game, 99));
        Assert.Equal(new DefenseOutcome.Gone(), DefenseRun.Remove(game, 99));
        Assert.Empty(game.LinedUp);
    }

    [Fact]
    public void A_row_button_never_trades_actions_the_player_queued()
    {
        var game = new FakeGame { StartClearsQueue = true, Running = { Eating }, Defenses = { Def(1) } };

        Assert.Equal(new DefenseOutcome.QueueBusy(), DefenseRun.Repair(game, 1));
        Assert.Equal(new DefenseOutcome.QueueBusy(), DefenseRun.Remove(game, 1));
        Assert.Empty(game.LinedUp);
        Assert.Equal(new[] { Walking, Eating }, game.Running);
    }

    [Fact]
    public void A_row_job_the_game_will_not_queue_is_reported_and_nothing_starts()
    {
        var game = new FakeGame { RefusesToQueue = true, Defenses = { Def(1, name: "Sandbags") } };

        Assert.Equal(new DefenseOutcome.NotQueued(Job.Repair, "Sandbags"), DefenseRun.Repair(game, 1));
        Assert.Equal(new DefenseOutcome.NotQueued(Job.Remove, "Sandbags"), DefenseRun.Remove(game, 1));
        Assert.Equal(new[] { Walking }, game.Running);
    }

    [Fact]
    public void A_row_job_the_game_would_not_start_is_reported()
    {
        var game = new FakeGame { FailsToStart = true, Defenses = { Def(1) } };

        Assert.Equal(new DefenseOutcome.StartFailed(Job.Repair, 1), DefenseRun.Repair(game, 1));
    }

    // Every button

    [Fact]
    public void No_button_touches_the_defenses_during_a_zombie_wave()
    {
        foreach (var press in EveryPress)
        {
            var game = new FakeGame { IsWaveLocked = true, Defenses = { Broken(1) } };

            Assert.Equal(new DefenseOutcome.WaveLocked(), press(game));
            Assert.Empty(game.LinedUp);
            Assert.Equal(new[] { Walking }, game.Running);
        }
    }

    [Fact]
    public void No_button_touches_the_defenses_away_from_home()
    {
        foreach (var press in EveryPress)
        {
            var game = new FakeGame { IsHome = false, Defenses = { Broken(1) } };

            Assert.Equal(new DefenseOutcome.AwayFromHome(), press(game));
            Assert.Empty(game.LinedUp);
            Assert.Equal(new[] { Walking }, game.Running);
        }
    }

    [Fact]
    public void No_button_does_anything_outside_a_loaded_game()
    {
        foreach (var press in EveryPress)
        {
            var game = new FakeGame { IsNoGame = true, Defenses = { Broken(1) } };

            Assert.Equal(new DefenseOutcome.NoGame(), press(game));
            Assert.Empty(game.LinedUp);
            Assert.Equal(new[] { Walking }, game.Running);
        }
    }

    [Fact]
    public void No_button_does_anything_when_the_game_cannot_be_read()
    {
        foreach (var press in EveryPress)
        {
            var game = new FakeGame { NotReadyReason = "no character", Defenses = { Broken(1) } };

            Assert.Equal(new DefenseOutcome.NotReady("no character"), press(game));
            Assert.Empty(game.LinedUp);
        }
    }
}

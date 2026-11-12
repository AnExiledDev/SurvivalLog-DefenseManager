using DefenseManager.Core;
using Xunit;
using static DefenseManager.Tests.FakeGame;

namespace DefenseManager.Tests;

/// <summary>Buying yard squares, moving defenses and cancelling planned installs and moves from the panel.</summary>
public class LayoutActionsTests
{
    /// <summary>One row: two owned squares, then one for sale at 50 that needs Defense level 2, then one at level 3.</summary>
    private static Yard Row() => new(
        new[]
        {
            new YardSquare("s1", 1, -1.4, 0, true, 0),
            new YardSquare("s2", 1, -0.7, 0, true, 0),
            new YardSquare("s3", 1, 0, 0, false, 50, RequiredLevel: 2),
            new YardSquare("s4", 1, 0.7, 0, false, 80, RequiredLevel: 3),
        },
        Array.Empty<WallFeature>());

    private static FakeGame Game(int points = 100, int level = 2) => new() { Yard = Row(), SurvivalPoints = points, DefenseLevel = level };

    private static YardSquare Square(FakeGame game, string name) => game.Yard!.Squares.Single(square => square.Name == name);

    private static Survey SurveyOf(FakeGame game) => Assert.IsType<DefenseRead.Ready>(game.Read()).Survey;

    private static BuyVerdict Judge(FakeGame game, string square) => BuyCheck.Judge(SurveyOf(game), Square(game, square));

    // Buying

    [Fact]
    public void A_square_at_the_players_level_they_can_pay_for_may_be_bought()
    {
        Assert.Equal(new BuyVerdict.Allowed(), Judge(Game(points: 50, level: 2), "s3"));
    }

    [Fact]
    public void A_square_one_level_up_is_shown_with_the_level_it_unlocks_at()
    {
        Assert.Equal(new BuyVerdict.NeedsLevel(2), Judge(Game(level: 1), "s3"));
    }

    [Fact]
    public void A_square_two_levels_up_is_one_the_game_still_hides()
    {
        Assert.Equal(new BuyVerdict.Hidden(3), Judge(Game(level: 1), "s4"));
    }

    [Fact]
    public void A_square_the_player_cannot_pay_for_says_how_many_points_they_have()
    {
        Assert.Equal(new BuyVerdict.Short(50, 49), Judge(Game(points: 49), "s3"));
    }

    [Fact]
    public void The_level_is_checked_before_the_points_as_the_game_does()
    {
        Assert.Equal(new BuyVerdict.NeedsLevel(3), Judge(Game(points: 0, level: 2), "s4"));
    }

    [Fact]
    public void Points_or_a_level_that_could_not_be_read_promise_nothing()
    {
        var noPoints = Game();
        noPoints.SurvivalPoints = null;
        var noLevel = Game();
        noLevel.DefenseLevel = null;

        Assert.Equal(new BuyVerdict.Unreadable(), Judge(noPoints, "s3"));
        Assert.Equal(new BuyVerdict.Unreadable(), Judge(noLevel, "s3"));
    }

    [Fact]
    public void A_wave_locks_buying_and_an_owned_square_is_not_for_sale()
    {
        var wave = Game();
        wave.IsWaveLocked = true;

        Assert.Equal(new BuyVerdict.WaveLocked(), Judge(wave, "s3"));
        Assert.Equal(new BuyVerdict.Owned(), Judge(Game(), "s1"));
    }

    [Fact]
    public void Buying_hands_the_square_to_the_games_purchase_and_says_what_it_cost()
    {
        var game = Game();

        Assert.Equal(new DefenseOutcome.Bought(50), DefenseRun.Buy(game, "s3"));
        Assert.Equal(new[] { "s3" }, game.Bought);
    }

    [Fact]
    public void A_square_that_cannot_be_bought_never_reaches_the_games_purchase()
    {
        var game = Game(points: 10);

        Assert.Equal(new DefenseOutcome.BuyRefused(new BuyVerdict.Short(50, 10)), DefenseRun.Buy(game, "s3"));
        Assert.Equal(new DefenseOutcome.BuyRefused(new BuyVerdict.Owned()), DefenseRun.Buy(game, "s1"));
        Assert.Equal(new DefenseOutcome.NotBought(), DefenseRun.Buy(game, "nowhere"));
        Assert.Empty(game.Bought);
    }

    [Fact]
    public void A_purchase_the_game_turns_down_is_not_called_bought()
    {
        var game = Game();
        game.RefusesBuy = true;

        Assert.Equal(new DefenseOutcome.NotBought(), DefenseRun.Buy(game, "s3"));
    }

    [Fact]
    public void Nothing_is_bought_during_a_wave_or_away_from_home()
    {
        var wave = Game();
        wave.IsWaveLocked = true;
        var away = Game();
        away.IsHome = false;

        Assert.Equal(new DefenseOutcome.WaveLocked(), DefenseRun.Buy(wave, "s3"));
        Assert.Equal(new DefenseOutcome.AwayFromHome(), DefenseRun.Buy(away, "s3"));
        Assert.Empty(wave.Bought.Concat(away.Bought));
    }

    [Fact]
    public void Every_reason_a_square_cannot_be_bought_is_said_and_a_buyable_one_has_none()
    {
        Assert.Null(OutcomeMessage.WhyNotBuy(new BuyVerdict.Allowed()));
        Assert.Null(OutcomeMessage.WhyNotBuy(new BuyVerdict.Owned()));
        Assert.Equal("Unlocks at Defense level 2", OutcomeMessage.WhyNotBuy(new BuyVerdict.NeedsLevel(2)));
        Assert.Equal("The game does not show it yet. It unlocks at Defense level 3", OutcomeMessage.WhyNotBuy(new BuyVerdict.Hidden(3)));
        Assert.Equal("Not enough Survival Points, you have 10", OutcomeMessage.WhyNotBuy(new BuyVerdict.Short(50, 10)));
        Assert.Equal("Squares can't be bought during a zombie wave", OutcomeMessage.WhyNotBuy(new BuyVerdict.WaveLocked()));
        Assert.Equal("Can't buy that square. Unlocks at Defense level 2.", OutcomeMessage.Describe(new DefenseOutcome.BuyRefused(new BuyVerdict.NeedsLevel(2))));
        Assert.Equal("Square bought for 50 Survival Points.", OutcomeMessage.Describe(new DefenseOutcome.Bought(50)));
    }

    // Moving

    private static FakeGame WithDefenseOnS1()
    {
        var game = Game();
        game.Defenses.Add(Def(7, x: -1.4, name: "Spike barrier"));

        return game;
    }

    [Fact]
    public void A_defense_is_moved_to_another_owned_square_through_the_games_move()
    {
        var game = WithDefenseOnS1();

        Assert.Equal(new DefenseOutcome.Moving("Spike barrier"), DefenseRun.Move(game, 7, "s2"));
        Assert.Equal(new[] { (7L, "s2") }, game.Moved);
    }

    [Fact]
    public void A_defense_on_the_target_square_is_left_to_the_game()
    {
        var game = WithDefenseOnS1();
        game.Defenses.Add(Def(8, x: -0.7));

        Assert.Equal(new DefenseOutcome.Moving("Spike barrier"), DefenseRun.Move(game, 7, "s2"));
    }

    [Theory]
    [InlineData("s3")]
    [InlineData("nowhere")]
    public void A_defense_is_never_moved_to_a_square_the_player_does_not_own(string square)
    {
        var game = WithDefenseOnS1();

        Assert.Equal(new DefenseOutcome.NotOwned(), DefenseRun.Move(game, 7, square));
        Assert.Empty(game.Moved);
    }

    [Fact]
    public void Moving_a_defense_onto_its_own_square_does_nothing()
    {
        var game = WithDefenseOnS1();

        Assert.Equal(new DefenseOutcome.SameSquare("Spike barrier"), DefenseRun.Move(game, 7, "s1"));
        Assert.Empty(game.Moved);
    }

    [Fact]
    public void A_defense_with_a_move_planned_is_not_given_a_second_walk()
    {
        var game = WithDefenseOnS1();
        game.Plans.Add(new QueuedPlan(PlanKind.Move, 7, "s2", "Spike barrier", null));

        Assert.Equal(new DefenseOutcome.MoveQueued("Spike barrier"), DefenseRun.Move(game, 7, "s2"));
        Assert.Empty(game.Moved);
    }

    [Fact]
    public void A_planned_install_on_the_defense_does_not_count_as_a_planned_move()
    {
        var game = WithDefenseOnS1();
        game.Plans.Add(new QueuedPlan(PlanKind.Install, 7, "s2", "Spike barrier", null));

        Assert.IsType<DefenseOutcome.Moving>(DefenseRun.Move(game, 7, "s2"));
    }

    [Fact]
    public void A_move_the_game_refuses_says_so_and_one_for_a_gone_defense_is_not_tried()
    {
        var game = WithDefenseOnS1();
        game.RefusesMove = true;

        Assert.Equal(new DefenseOutcome.NotMoved("Spike barrier"), DefenseRun.Move(game, 7, "s2"));
        Assert.Equal(new DefenseOutcome.Gone(), DefenseRun.Move(game, 99, "s2"));
    }

    [Fact]
    public void Nothing_is_moved_during_a_wave()
    {
        var game = WithDefenseOnS1();
        game.IsWaveLocked = true;

        Assert.Equal(new DefenseOutcome.WaveLocked(), DefenseRun.Move(game, 7, "s2"));
        Assert.Empty(game.Moved);
    }

    [Fact]
    public void A_move_made_in_layout_says_it_starts_when_the_player_leaves()
    {
        Assert.Equal("Moving Spike barrier. Work starts when you leave Layout.", OutcomeMessage.Describe(new DefenseOutcome.Moving("Spike barrier"), isInLayout: true));
    }

    // Cancelling

    [Fact]
    public void A_planned_install_is_cancelled_by_its_package_and_named()
    {
        var game = Game();
        game.Plans.Add(new QueuedPlan(PlanKind.Install, 71, "s2", "Tesla coil", null));

        Assert.Equal(new DefenseOutcome.Cancelled("Tesla coil"), DefenseRun.Cancel(game, PlanKind.Install, 71));
        Assert.Equal(new[] { (PlanKind.Install, 71L) }, game.Cancelled);
    }

    [Fact]
    public void A_plan_no_longer_queued_or_of_the_other_kind_is_not_cancelled()
    {
        var game = Game();
        game.Plans.Add(new QueuedPlan(PlanKind.Move, 7, "s2", "Spike barrier", null));

        Assert.Equal(new DefenseOutcome.PlanGone(), DefenseRun.Cancel(game, PlanKind.Install, 7));
        Assert.Equal(new DefenseOutcome.PlanGone(), DefenseRun.Cancel(game, PlanKind.Move, 8));
        Assert.Empty(game.Cancelled);
    }

    [Fact]
    public void A_plan_can_be_cancelled_during_a_wave_since_it_only_stops_work()
    {
        var game = Game();
        game.IsWaveLocked = true;
        game.Plans.Add(new QueuedPlan(PlanKind.Move, 7, "s2", "Spike barrier", null));

        Assert.Equal(new DefenseOutcome.Cancelled("Spike barrier"), DefenseRun.Cancel(game, PlanKind.Move, 7));
    }

    [Fact]
    public void A_cancel_the_game_refuses_says_so()
    {
        var game = Game();
        game.RefusesCancel = true;
        game.Plans.Add(new QueuedPlan(PlanKind.Move, 7, "s2", "Spike barrier", null));

        Assert.Equal(new DefenseOutcome.NotCancelled("Spike barrier"), DefenseRun.Cancel(game, PlanKind.Move, 7));
    }

    [Fact]
    public void Cancelling_outside_a_game_says_there_is_none()
    {
        var game = Game();
        game.IsNoGame = true;

        Assert.Equal(new DefenseOutcome.NoGame(), DefenseRun.Cancel(game, PlanKind.Move, 7));
    }
}

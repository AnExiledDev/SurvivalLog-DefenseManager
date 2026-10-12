using DefenseManager.Core;
using Xunit;
using static DefenseManager.Tests.FakeGame;

namespace DefenseManager.Tests;

public class MaterialBudgetTests
{
    private static Survey SurveyOf(IEnumerable<Defense> defenses, params (int ItemId, int Stock)[] stock) =>
        new(defenses.ToList(), new Spot(0, 0, 0), stock.ToDictionary(item => item.ItemId, item => new Material(NameOf(item.ItemId), null, item.Stock)), QueueOnStart.Appends, IsWaveLocked: false, IsHome: true);

    [Fact]
    public void Repairs_already_queued_have_their_materials_set_aside()
    {
        var queued = Def(1, cost: Cost((Wood, 4))) with { IsRepairQueued = true };
        var waiting = Def(2, cost: Cost((Wood, 9)));

        var budget = MaterialBudget.Of(SurveyOf(new[] { queued, waiting }, (Wood, 10)));

        Assert.Equal(6, budget.Left(Wood));
    }

    [Fact]
    public void A_removal_queued_sets_nothing_aside()
    {
        var removing = Def(1, cost: Cost((Wood, 4))) with { IsRemovalQueued = true };

        var budget = MaterialBudget.Of(SurveyOf(new[] { removing }, (Wood, 10)));

        Assert.Equal(10, budget.Left(Wood));
    }

    [Fact]
    public void Queued_repairs_the_stock_cannot_cover_leave_none_rather_than_a_debt()
    {
        var queued = Def(1, cost: Cost((Wood, 15))) with { IsRepairQueued = true };

        var budget = MaterialBudget.Of(SurveyOf(new[] { queued }, (Wood, 10)));

        Assert.Equal(0, budget.Left(Wood));
        Assert.Equal(new Dictionary<int, int> { [Wood] = 1 }, budget.Missing(Cost((Wood, 1))));
    }

    [Fact]
    public void Missing_names_only_the_items_the_budget_is_short_of_and_by_how_many()
    {
        var budget = new MaterialBudget(new Dictionary<int, int> { [Wood] = 5, [Nail] = 2 });

        var missing = budget.Missing(Cost((Wood, 3), (Nail, 6), (99, 1)));

        Assert.Equal(new Dictionary<int, int> { [Nail] = 4, [99] = 1 }, missing);
    }

    [Fact]
    public void A_cost_the_budget_covers_exactly_is_missing_nothing()
    {
        var budget = new MaterialBudget(new Dictionary<int, int> { [Wood] = 5 });

        Assert.Empty(budget.Missing(Cost((Wood, 5))));
    }

    [Fact]
    public void Reserving_takes_from_what_later_costs_see()
    {
        var budget = new MaterialBudget(new Dictionary<int, int> { [Wood] = 5 });

        budget.Reserve(Cost((Wood, 3)));

        Assert.Equal(2, budget.Left(Wood));
        Assert.Equal(new Dictionary<int, int> { [Wood] = 1 }, budget.Missing(Cost((Wood, 3))));
    }

    [Fact]
    public void Negative_stock_counts_as_none()
    {
        var budget = new MaterialBudget(new Dictionary<int, int> { [Wood] = -4 });

        Assert.Equal(0, budget.Left(Wood));
    }
}

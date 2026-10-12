using System;
using System.Collections.Generic;
using System.Linq;

namespace DefenseManager.Core;

/// <summary>
/// The repair materials one press may still promise. The game takes a repair's materials when the repair runs, not
/// when it is queued, so repairs already queued have their cost set aside first: a new repair is only promised what
/// they leave.
/// </summary>
public sealed class MaterialBudget
{
    private readonly Dictionary<int, int> _left;

    public MaterialBudget(IReadOnlyDictionary<int, int> stock)
    {
        _left = stock.ToDictionary(entry => entry.Key, entry => Math.Max(0, entry.Value));
    }

    public static MaterialBudget Of(Survey survey)
    {
        var budget = new MaterialBudget(survey.Materials.ToDictionary(entry => entry.Key, entry => entry.Value.Stock));

        foreach (var queued in survey.Defenses.Where(defense => defense.IsRepairQueued)) budget.Reserve(queued.Cost);

        return budget;
    }

    public int Left(int itemId) => _left.GetValueOrDefault(itemId);

    /// <summary>How many more of each item the cost needs than is left, by item id; empty when it is affordable.</summary>
    public IReadOnlyDictionary<int, int> Missing(IReadOnlyDictionary<int, int> cost)
    {
        var missing = new SortedDictionary<int, int>();

        foreach (var (itemId, count) in cost)
        {
            var shortBy = count - Left(itemId);

            if (shortBy > 0) missing[itemId] = shortBy;
        }

        return missing;
    }

    /// <summary>Sets the cost aside. Never goes below none left, so queued repairs the stock cannot cover leave nothing rather than a debt.</summary>
    public void Reserve(IReadOnlyDictionary<int, int> cost)
    {
        foreach (var (itemId, count) in cost) _left[itemId] = Math.Max(0, Left(itemId) - Math.Max(0, count));
    }
}

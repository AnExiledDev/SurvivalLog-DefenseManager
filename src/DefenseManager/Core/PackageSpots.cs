using System;
using System.Collections.Generic;
using System.Linq;

namespace DefenseManager.Core;

/// <summary>The yard's package spots, where delivered and bought packages wait to be installed.</summary>
public static class PackageSpots
{
    private const string Prefix = "HomePackage";

    /// <summary>
    /// Only the numbered spots (HomePackage1, HomePackage2, ...) are the yard's package spots. The game's other package
    /// points (on the car, a bag of plastic) hold packages that are not the player's to install from here.
    /// </summary>
    public static bool IsYardSpot(string? node) =>
        node is not null
        && node.Length > Prefix.Length
        && node.StartsWith(Prefix, StringComparison.Ordinal)
        && node.Skip(Prefix.Length).All(character => character is >= '0' and <= '9');

    /// <summary>
    /// The install list: one entry per kind of defense, from the yard's packages (furniture id and package id, in yard
    /// order) and the furniture id of each banked package. Names and pictures come from <paramref name="describe"/>,
    /// which answers null for furniture that is not a defense; those are left out.
    /// </summary>
    public static List<DefensePackage> Gather(IEnumerable<(int ConfigId, long BagId)> yard, IEnumerable<int> banked, Func<int, (string Name, string? Icon)?> describe)
    {
        var bags = new Dictionary<int, List<long>>();
        var bankCounts = new Dictionary<int, int>();

        foreach (var (configId, bagId) in yard)
        {
            if (!bags.TryGetValue(configId, out var list)) bags[configId] = list = new List<long>();

            list.Add(bagId);
        }

        foreach (var configId in banked) bankCounts[configId] = bankCounts.GetValueOrDefault(configId) + 1;

        var packages = new List<DefensePackage>();

        foreach (var configId in bags.Keys.Concat(bankCounts.Keys).Distinct().OrderBy(id => id))
        {
            if (describe(configId) is not { } facts) continue;

            packages.Add(new DefensePackage(configId, facts.Name, facts.Icon, bags.TryGetValue(configId, out var list) ? list : new List<long>(), bankCounts.GetValueOrDefault(configId)));
        }

        return packages;
    }
}

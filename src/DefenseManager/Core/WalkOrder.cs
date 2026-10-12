using System;
using System.Collections.Generic;

namespace DefenseManager.Core;

/// <summary>The order the character walks a round of defenses in, so a batch does not zigzag across the yard.</summary>
public static class WalkOrder
{
    /// <summary>
    /// Goes from the start to the nearest stop, then from there to the nearest stop left, and so on. A stop that
    /// tryVisit turns down is crossed off and the walker stays where it was, so the next stop is the nearest to the
    /// last one actually visited. Equal distances go to the lower id, so the same home always gives the same order.
    /// </summary>
    public static IReadOnlyList<Defense> Visit(Spot start, IEnumerable<Defense> stops, Func<Defense, bool> tryVisit)
    {
        var left = new List<Defense>(stops);
        var visited = new List<Defense>();
        var at = start;

        while (left.Count > 0)
        {
            var nearest = NearestIndex(at, left);
            var stop = left[nearest];
            left.RemoveAt(nearest);

            if (!tryVisit(stop)) continue;

            visited.Add(stop);
            at = stop.Position;
        }

        return visited;
    }

    private static int NearestIndex(Spot from, IReadOnlyList<Defense> stops)
    {
        var best = 0;

        for (var i = 1; i < stops.Count; i++)
        {
            if (IsCloser(from, stops[i], stops[best])) best = i;
        }

        return best;
    }

    private static bool IsCloser(Spot from, Defense candidate, Defense best)
    {
        var candidateDistance = from.DistanceSquaredTo(candidate.Position);
        var bestDistance = from.DistanceSquaredTo(best.Position);

        return candidateDistance < bestDistance || (candidateDistance == bestDistance && candidate.Id < best.Id);
    }
}

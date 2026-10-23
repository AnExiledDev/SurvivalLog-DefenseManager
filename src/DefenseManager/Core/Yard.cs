using System;
using System.Collections.Generic;
using System.Linq;

namespace DefenseManager.Core;

/// <summary>
/// One square of the home's defense grid, from the game's map slots: its slot name, its row (1 against the house, the
/// highest at the road side), its centre on the ground, whether the player owns it, and the Survival Points it costs
/// when they do not.
/// </summary>
public sealed record YardSquare(string Name, int Row, double X, double Z, bool IsOwned, int Price);

public enum WallFeatureKind
{
    Door,
    Window,
}

/// <summary>A door or window in the house wall along row 1, by where it stands along the wall.</summary>
public sealed record WallFeature(WallFeatureKind Kind, double X);

/// <summary>A square by its grid row and column, both counted from 1.</summary>
public readonly record struct GridPlace(int Row, int Column);

/// <summary>
/// The home yard's grid of defense squares. The game names its squares but the numbers in the names do not run along
/// the yard, so columns come from where the squares stand: every distinct x, left to right. A defense is put on the
/// square whose centre is nearest it; one nowhere near a square (at another place the game lets defenses stand) is on
/// no square.
/// </summary>
public sealed class Yard
{
    /// <summary>How far from a square's centre a defense may stand and still count as on it. Squares are 0.7 apart.</summary>
    public const double MatchDistance = 0.5;

    private readonly double[] _columns;

    public Yard(IReadOnlyList<YardSquare> squares, IReadOnlyList<WallFeature> wall)
    {
        Squares = squares;
        Wall = wall;
        // Squares in one column can differ in the last digits; each column is where its squares stand on average.
        _columns = squares.GroupBy(square => Math.Round(square.X, 2)).Select(column => column.Average(square => square.X)).OrderBy(x => x).ToArray();
        Rows = squares.Count == 0 ? 0 : squares.Max(square => square.Row);
    }

    public IReadOnlyList<YardSquare> Squares { get; }

    public IReadOnlyList<WallFeature> Wall { get; }

    public int Columns => _columns.Length;

    public int Rows { get; }

    /// <summary>The column whose line is nearest, from 1.</summary>
    public int ColumnOf(double x)
    {
        var best = 0;

        for (var i = 1; i < _columns.Length; i++)
        {
            if (Math.Abs(_columns[i] - x) < Math.Abs(_columns[best] - x)) best = i;
        }

        return best + 1;
    }

    /// <summary>Where along the columns a point stands, as a column number with a fraction: 1 is the first column's centre.</summary>
    public double ColumnAt(double x)
    {
        if (_columns.Length < 2) return 1;

        var step = (_columns[^1] - _columns[0]) / (_columns.Length - 1);

        return 1 + (x - _columns[0]) / step;
    }

    public GridPlace PlaceOf(YardSquare square) => new(square.Row, ColumnOf(square.X));

    /// <summary>The square a defense stands on, or null when it stands on none.</summary>
    public YardSquare? SquareAt(Spot spot)
    {
        YardSquare? best = null;
        var bestDistance = MatchDistance * MatchDistance;

        foreach (var square in Squares)
        {
            var dx = square.X - spot.X;
            var dz = square.Z - spot.Z;
            var distance = dx * dx + dz * dz;

            if (distance > bestDistance) continue;

            best = square;
            bestDistance = distance;
        }

        return best;
    }

    /// <summary>The row and column a defense stands on, or null when it stands on no square.</summary>
    public GridPlace? PlaceAt(Spot spot) => SquareAt(spot) is { } square ? PlaceOf(square) : null;
}

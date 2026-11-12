using System;
using System.Collections.Generic;
using System.Linq;

namespace DefenseManager.Core;

/// <summary>
/// One square of the home's defense grid, from the game's map slots: its slot name, the game's row number for it (1 next
/// to the house, higher further out), its centre on the ground, whether the player owns it, and the Survival Points it
/// costs when they do not, and the Defense level the game asks before it lets the player buy it (0 when it asks none).
/// </summary>
public sealed record YardSquare(string Name, int Row, double X, double Z, bool IsOwned, int Price, int RequiredLevel = 0);

public enum WallFeatureKind
{
    Door,
    Window,
}

/// <summary>A door or window in the house wall along row 1, by where it stands along the wall.</summary>
public sealed record WallFeature(WallFeatureKind Kind, double X);

/// <summary>A door or window of the house, by where it stands on the ground.</summary>
public sealed record WallOpening(WallFeatureKind Kind, double X, double Z);

/// <summary>A square by its grid row and column, both counted from 1.</summary>
public readonly record struct GridPlace(int Row, int Column);

/// <summary>
/// The home yard's grid of defense squares. The game names its squares but the numbers in the names do not run along
/// the yard, so the grid comes from where the squares stand: a column for every distinct x, left to right, and a row
/// for every distinct z, starting at the house. Most homes have a straight yard whose rows are the game's rows. The
/// Warehouse Manager's yard wraps round a corner of the warehouse, and the game numbers it in bands that turn that
/// corner, so its rows are only where the squares stand. A defense is put on the square whose centre is nearest it;
/// one nowhere near a square (at another place the game lets defenses stand) is on no square.
/// </summary>
public sealed class Yard
{
    /// <summary>How far from a square's centre a defense may stand and still count as on it. Squares are 0.7 apart.</summary>
    public const double MatchDistance = 0.5;

    /// <summary>How far past the row by the house a door or window may stand and still be in the wall along it.</summary>
    public const double WallDistance = 1.4;

    private readonly double[] _columns;
    private readonly double[] _rows;

    public Yard(IReadOnlyList<YardSquare> squares, IReadOnlyList<WallFeature> wall)
    {
        Squares = squares;
        Wall = wall;
        _columns = Lines(squares.Select(square => square.X)).OrderBy(x => x).ToArray();
        _rows = RowsFromHouse(squares);
        HasStraightRows = squares.All(square => RowOf(square.Z) == square.Row);
    }

    public IReadOnlyList<YardSquare> Squares { get; }

    public IReadOnlyList<WallFeature> Wall { get; }

    public int Columns => _columns.Length;

    public int Rows => _rows.Length;

    /// <summary>
    /// Whether each row is one straight line of squares, row 1 against the house, so a row's number says how far out
    /// it is.
    /// </summary>
    public bool HasStraightRows { get; }

    /// <summary>
    /// The doors and windows in the house wall along row 1: those standing just past it on the house side, and within
    /// its length. The others are elsewhere in the house, away from the yard.
    /// </summary>
    public static IReadOnlyList<WallFeature> WallAlongTheYard(IReadOnlyList<YardSquare> squares, IEnumerable<WallOpening> openings)
    {
        var rows = RowsFromHouse(squares);

        if (rows.Length == 0) return Array.Empty<WallFeature>();

        var firstRow = squares.Where(square => Nearest(rows, square.Z) == 0).ToList();
        var left = firstRow.Min(square => square.X) - MatchDistance;
        var right = firstRow.Max(square => square.X) + MatchDistance;
        var towardHouse = rows.Length > 1 ? Math.Sign(rows[0] - rows[1]) : 0;

        return openings
            .Where(opening => opening.X >= left && opening.X <= right && IsJustPast(opening.Z - rows[0], towardHouse))
            .OrderBy(opening => opening.X)
            .Select(opening => new WallFeature(opening.Kind, opening.X))
            .ToList();
    }

    /// <summary>The row whose line is nearest, from 1.</summary>
    public int RowOf(double z) => Nearest(_rows, z) + 1;

    /// <summary>The column whose line is nearest, from 1.</summary>
    public int ColumnOf(double x) => Nearest(_columns, x) + 1;

    /// <summary>Where along the columns a point stands, as a column number with a fraction: 1 is the first column's centre.</summary>
    public double ColumnAt(double x)
    {
        if (_columns.Length < 2) return 1;

        var step = (_columns[^1] - _columns[0]) / (_columns.Length - 1);

        return 1 + (x - _columns[0]) / step;
    }

    public GridPlace PlaceOf(YardSquare square) => new(RowOf(square.Z), ColumnOf(square.X));

    /// <summary>The square a defense stands on, or null when it stands on none.</summary>
    public YardSquare? SquareAt(Spot spot)
    {
        YardSquare? best = null;
        var bestDistance = MatchDistance * MatchDistance;

        for (var i = 0; i < Squares.Count; i++)
        {
            var square = Squares[i];
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

    /// <summary>Every distinct z, from the house outward. The house is on the side of the game's first row.</summary>
    private static double[] RowsFromHouse(IReadOnlyList<YardSquare> squares)
    {
        if (squares.Count == 0) return Array.Empty<double>();

        var lines = Lines(squares.Select(square => square.Z)).OrderBy(z => z).ToArray();
        var firstRow = squares.Min(square => square.Row);
        var isHouseAtHighZ = squares.Where(square => square.Row == firstRow).Average(square => square.Z) >= squares.Average(square => square.Z);

        if (isHouseAtHighZ) Array.Reverse(lines);

        return lines;
    }

    /// <summary>Squares in one line can differ in the last digits; each line is where its squares stand on average.</summary>
    private static IEnumerable<double> Lines(IEnumerable<double> positions) =>
        positions.GroupBy(position => Math.Round(position, 2)).Select(line => line.Average());

    /// <summary>With one row there is no telling which side the house is on, so either side counts.</summary>
    private static bool IsJustPast(double distance, int towardHouse) =>
        towardHouse == 0 ? Math.Abs(distance) <= WallDistance : distance * towardHouse > 0 && distance * towardHouse <= WallDistance;

    private static int Nearest(double[] lines, double position)
    {
        var best = 0;

        for (var i = 1; i < lines.Length; i++)
        {
            if (Math.Abs(lines[i] - position) < Math.Abs(lines[best] - position)) best = i;
        }

        return best;
    }
}

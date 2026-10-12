namespace DefenseManager.Core;

/// <summary>A point in the home, in the game's world units.</summary>
public readonly record struct Spot(double X, double Y, double Z)
{
    public double DistanceSquaredTo(Spot other)
    {
        var dx = X - other.X;
        var dy = Y - other.Y;
        var dz = Z - other.Z;

        return dx * dx + dy * dy + dz * dz;
    }
}

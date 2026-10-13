using System;
using System.Globalization;

namespace DefenseManager.Core;

/// <summary>
/// One defense across game sessions: its furniture type and where it stands, to a tenth of a metre. A defense taken
/// down and built again in the same spot is the same defense to the player, so it keeps its setting.
/// </summary>
public readonly record struct DefenseKey(int ConfigId, long X, long Y, long Z)
{
    private const char Separator = ':';

    public static DefenseKey Of(int configId, Spot position) => new(configId, Tenths(position.X), Tenths(position.Y), Tenths(position.Z));

    public static bool TryParse(string text, out DefenseKey key)
    {
        var fields = text.Split(Separator);
        key = default;

        if (fields.Length != 4 || !int.TryParse(fields[0], NumberStyles.None, CultureInfo.InvariantCulture, out var configId)) return false;

        if (!TryLong(fields[1], out var x) || !TryLong(fields[2], out var y) || !TryLong(fields[3], out var z)) return false;

        key = new DefenseKey(configId, x, y, z);

        return true;
    }

    public override string ToString() => string.Join(Separator, ConfigId.ToString(CultureInfo.InvariantCulture), Text(X), Text(Y), Text(Z));

    private static long Tenths(double metres) => (long)Math.Round(metres * 10, MidpointRounding.AwayFromZero);

    private static string Text(long tenths) => tenths.ToString(CultureInfo.InvariantCulture);

    private static bool TryLong(string text, out long value) => long.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value);
}

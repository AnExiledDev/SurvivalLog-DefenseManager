using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace DefenseManager.Core;

/// <summary>
/// The power settings file: one tab-separated line per setting, for every save at once. A defense's line is
/// "save, defense, key, mode" and a type's is "save, type, furniture id, mode". The save is URI-escaped so a tab or a
/// line break in its name cannot split a line. Writing one save's settings keeps every other line exactly as it was.
/// </summary>
public static class PowerFile
{
    public const string Header = "# Defense Manager power settings. One line per setting: save, defense or type, defense key or furniture id, mode.";

    private const char Separator = '\t';
    private const string DefenseKind = "defense";
    private const string TypeKind = "type";

    public static PowerSettings Read(string? text, string save)
    {
        var settings = PowerSettings.None;

        foreach (var fields in Lines(text).Select(Fields).Where(fields => IsSave(fields, save)))
        {
            if (PowerModes.Parse(fields[3]) is not { } mode) continue;

            if (fields[1] == DefenseKind && DefenseKey.TryParse(fields[2], out var key)) settings = settings.WithDefense(key, mode);
            else if (fields[1] == TypeKind && TryConfigId(fields[2], out var configId)) settings = settings.WithType(configId, mode);
        }

        return settings;
    }

    public static string Write(string? text, string save, PowerSettings settings)
    {
        var file = new StringBuilder().Append(Header).Append('\n');
        var escaped = Uri.EscapeDataString(save);

        foreach (var line in Lines(text).Where(line => !IsSave(Fields(line), save))) file.Append(line).Append('\n');

        foreach (var (key, mode) in settings.Defenses.OrderBy(entry => entry.Key.ToString(), StringComparer.Ordinal)) Append(file, escaped, DefenseKind, key.ToString(), mode);

        foreach (var (configId, mode) in settings.Types.OrderBy(entry => entry.Key)) Append(file, escaped, TypeKind, configId.ToString(CultureInfo.InvariantCulture), mode);

        return file.ToString();
    }

    private static void Append(StringBuilder file, string save, string kind, string target, PowerMode mode) =>
        file.Append(string.Join(Separator, save, kind, target, PowerModes.ConfigName(mode))).Append('\n');

    private static IEnumerable<string> Lines(string? text) =>
        (text ?? "").Split('\n').Select(line => line.TrimEnd('\r')).Where(line => line.Length > 0 && line != Header);

    private static string[] Fields(string line) => line.StartsWith('#') ? Array.Empty<string>() : line.Split(Separator);

    /// <summary>A line of this save, readable or not, so writing the save replaces every line it had.</summary>
    private static bool IsSave(string[] fields, string save) => fields.Length == 4 && Unescape(fields[0]) == save;

    private static string? Unescape(string text)
    {
        try
        {
            return Uri.UnescapeDataString(text);
        }
        catch (UriFormatException)
        {
            return null;
        }
    }

    private static bool TryConfigId(string text, out int configId) => int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out configId);
}

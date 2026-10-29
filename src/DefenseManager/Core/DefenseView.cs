using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace DefenseManager.Core;

/// <summary>
/// The JSON the panel draws from: what the two top buttons would act on, the repair materials, and one row per
/// defense with its condition, its repair cost and whether each of its buttons would be taken. A button that would be
/// refused carries the reason as visible text, since the web view draws no hover tips. Ids are sent as strings because
/// a JavaScript number cannot hold every long. An electrical defense's row also carries its power: the switch as it
/// stands, its own setting and its type's, when there are power settings to show. Each row also says which square of the
/// yard grid it stands on, and the view carries the grid itself (each square by its game name too, which an Install
/// press names), the Settings tab's values and the defense packages the player could install.
/// </summary>
public static class DefenseView
{
    /// <summary>The view for a read: the defenses, an empty view saying why they could not be read, or null outside a game.</summary>
    public static string? Of(DefenseRead read, PowerPolicy? power = null, bool isChinese = false, PanelSettings? settings = null) => read switch
    {
        DefenseRead.Ready ready => Build(ready.Survey, power, settings),
        DefenseRead.NotReady notReady => Empty(NotReadyNote(notReady, isChinese), settings),
        DefenseRead.NoGame => null,
        _ => throw new InvalidOperationException("unknown defense read " + read),
    };

    public static string Build(Survey survey, PowerPolicy? power = null, PanelSettings? settings = null)
    {
        var budget = MaterialBudget.Of(survey);
        var locked = LockedText(survey);
        var toRepair = survey.Defenses.Where(IsForRepairAll).ToList();
        var toClear = survey.Defenses.Count(IsForClearAll);
        var materials = survey.Materials.OrderBy(entry => entry.Key).Select(entry => MaterialJson(entry.Value, budget.Left(entry.Key), NeededBy(toRepair, entry.Key)));
        var rows = survey.Defenses.OrderBy(defense => defense.Name, StringComparer.Ordinal).ThenBy(defense => defense.Id).Select(defense => RowJson(defense, survey, budget, locked is null, power));

        var packages = (survey.Packages ?? Array.Empty<DefensePackage>()).Where(package => package.Count > 0).OrderBy(package => package.Name, StringComparer.Ordinal).Select(PackageJson);

        return Json(note: null, locked, toRepair.Count, toClear, materials, rows, YardJson(survey.Yard), PowerModeJson(power?.Global, survey.IsChinese), SettingsJson(settings), packages);
    }

    /// <summary>The panel with no defenses and a note saying why, for when the game could not be read.</summary>
    public static string Empty(string note, PanelSettings? settings = null) =>
        Json(note, locked: null, repairAll: 0, clearAll: 0, Enumerable.Empty<string>(), Enumerable.Empty<string>(), "null", "null", SettingsJson(settings), Enumerable.Empty<string>());

    /// <summary>
    /// What the Layout bar draws from: the panel view's note, locked reason and two top-button counts, worked out the
    /// same way, so the bar and the panel never disagree. Only these go, so the bar is sent again only when one of them
    /// changes, not whenever a defense's condition does. Null outside a game.
    /// </summary>
    public static string? Bar(DefenseRead read, bool isChinese = false)
    {
        switch (read)
        {
            case DefenseRead.Ready ready:
                var defenses = ready.Survey.Defenses;
                var toRepair = 0;
                var toClear = 0;

                for (var i = 0; i < defenses.Count; i++)
                {
                    if (IsForRepairAll(defenses[i])) toRepair++;
                    if (IsForClearAll(defenses[i])) toClear++;
                }

                return BarJson(note: null, LockedText(ready.Survey), toRepair, toClear);
            case DefenseRead.NotReady notReady:
                return BarJson(NotReadyNote(notReady, isChinese), locked: null, repairAll: 0, clearAll: 0);
            case DefenseRead.NoGame:
                return null;
            default:
                throw new InvalidOperationException("unknown defense read " + read);
        }
    }

    private static string BarJson(string? note, string? locked, int repairAll, int clearAll) =>
        $"{{\"note\":{Str(note)},\"locked\":{Str(locked)},\"repairAll\":{Num(repairAll)},\"clearAll\":{Num(clearAll)}}}";

    private static string NotReadyNote(DefenseRead.NotReady notReady, bool isChinese) =>
        OutcomeMessage.Describe(new DefenseOutcome.NotReady(notReady.Reason), isChinese) ?? notReady.Reason;

    /// <summary>Damaged and not queued for anything: what Repair all takes on.</summary>
    private static bool IsForRepairAll(Defense defense) => defense.IsDamaged && !defense.IsRepairQueued && !defense.IsRemovalQueued;

    /// <summary>Broken and allowed to come down: what Clear all broken takes on.</summary>
    private static bool IsForClearAll(Defense defense) => defense.IsBroken && RemoveCheck.Judge(defense) is RemoveVerdict.Allowed;

    private static string? LockedText(Survey survey)
    {
        if (survey.IsWaveLocked) return Texts.WaveLocked.Pick(survey.IsChinese);
        if (!survey.IsHome) return Texts.AwayFromHome.Pick(survey.IsChinese);

        return null;
    }

    private static string Json(string? note, string? locked, int repairAll, int clearAll, IEnumerable<string> materials, IEnumerable<string> rows, string yard, string global, string settings, IEnumerable<string> packages)
    {
        var json = new StringBuilder(4096);

        json.Append("{\"note\":").Append(Str(note))
            .Append(",\"locked\":").Append(Str(locked))
            .Append(",\"repairAll\":").Append(Num(repairAll))
            .Append(",\"clearAll\":").Append(Num(clearAll))
            .Append(",\"materials\":[").AppendJoin(',', materials)
            .Append("],\"defenses\":[").AppendJoin(',', rows)
            .Append("],\"yard\":").Append(yard)
            .Append(",\"global\":").Append(global)
            .Append(",\"settings\":").Append(settings)
            .Append(",\"packages\":[").AppendJoin(',', packages)
            .Append("]}");

        return json.ToString();
    }

    /// <summary>A packed defense the Install list offers: how many lie in the yard and how many wait in Package Bank.</summary>
    private static string PackageJson(DefensePackage package) =>
        $"{{\"type\":{Num(package.ConfigId)},\"name\":{Str(package.Name)},\"icon\":{Str(package.Icon)},\"yard\":{Num(package.YardBags.Count)},\"bank\":{Num(package.Banked)}}}";

    private static string MaterialJson(Material material, int free, int need) =>
        $"{{\"name\":{Str(material.Name)},\"icon\":{Str(material.Icon)},\"free\":{Num(free)},\"need\":{Num(need)}}}";

    /// <summary>
    /// One defense, judged on its own against the character's stamina and the materials left after repairs already queued: the row says whether
    /// this one repair would be taken now, not whether it fits in a Repair all round.
    /// </summary>
    private static string RowJson(Defense defense, Survey survey, MaterialBudget budget, bool isUnlocked, PowerPolicy? power)
    {
        var repair = RepairCheck.Judge(defense, budget, survey);
        var remove = RemoveCheck.Judge(defense);
        var missing = budget.Missing(defense.Cost);
        var cost = defense.Cost.OrderBy(entry => entry.Key).Select(entry => CostJson(survey, entry.Key, entry.Value, missing.GetValueOrDefault(entry.Key)));

        var place = survey.Yard?.PlaceAt(defense.Position);

        return $"{{\"id\":{Str(defense.Id.ToString(CultureInfo.InvariantCulture))},\"type\":{Num(defense.ConfigId)},\"name\":{Str(defense.Name)},\"icon\":{Str(defense.Icon)}"
            + $",\"row\":{(place is { } at ? Num(at.Row) : "null")},\"col\":{(place is { } on ? Num(on.Column) : "null")}"
            + $",\"cur\":{Num(defense.Current)},\"max\":{Num(defense.Max)},\"state\":{Str(State(defense))},\"queued\":{Str(Queued(defense))}"
            + $",\"cost\":[{string.Join(",", cost)}]"
            + $",\"canRepair\":{Bool(isUnlocked && repair is RepairVerdict.Allowed)},\"why\":{Str(OutcomeMessage.WhyNot(repair, survey.IsChinese))}"
            + $",\"canRemove\":{Bool(isUnlocked && remove is RemoveVerdict.Allowed)},\"removeWhy\":{Str(OutcomeMessage.WhyNotRemove(remove, survey.IsChinese))}"
            + $",\"power\":{PowerJson(defense, power, survey.IsChinese)}}}";
    }

    /// <summary>
    /// Null for a defense with no power switch, or with no power settings to show (while they cannot be read). "mode" is
    /// the mode it runs in; "own" and "typeOwn" are the defense's and its type's own settings (a config name, or null
    /// for none) and "typeMode" the mode its type runs in. "managed" is false while the mod leaves the defense alone
    /// because it is broken or not working.
    /// </summary>
    private static string PowerJson(Defense defense, PowerPolicy? power, bool isChinese)
    {
        if (defense.Power is not { } state || power is null) return "null";

        var key = DefenseKey.Of(defense.ConfigId, defense.Position);

        return $"{{\"on\":{Bool(state.IsOn)},\"managed\":{Bool(defense.IsPowerManaged)},\"mode\":{Str(PowerModes.Label(power.ModeOf(key), isChinese))}"
            + $",\"own\":{ModeName(power.Settings.DefenseSetting(key))},\"typeOwn\":{ModeName(power.Settings.TypeSetting(defense.ConfigId))}"
            + $",\"typeMode\":{PowerModeJson(power.TypeMode(defense.ConfigId), isChinese)}}}";
    }

    private static string ModeName(PowerMode? mode) => mode is { } set ? Str(PowerModes.ConfigName(set)) : "null";

    /// <summary>A mode as its config name and its label, or null.</summary>
    private static string PowerModeJson(PowerMode? mode, bool isChinese) =>
        mode is { } set ? $"{{\"name\":{Str(PowerModes.ConfigName(set))},\"label\":{Str(PowerModes.Label(set, isChinese))}}}" : "null";

    /// <summary>
    /// The grid's size, every square by row and column with whether it is the player's and its price, and the door and
    /// window along the house wall by column (with a fraction, as they stand between square centres). Null without a grid.
    /// The game side hands back the same grid until a square changes hands, so the text for the last grid is kept.
    /// </summary>
    private static string YardJson(Yard? yard)
    {
        if (yard is null || yard.Squares.Count == 0) return "null";

        // Read and replaced as one reference, so views built on two threads at once (as tests are) never mix grids.
        if (_yardText is { } kept && ReferenceEquals(kept.Yard, yard)) return kept.Json;

        var json = BuildYardJson(yard);

        _yardText = new YardText(yard, json);

        return json;
    }

    private sealed record YardText(Yard Yard, string Json);

    private static YardText? _yardText;

    private static string BuildYardJson(Yard yard)
    {
        var squares = yard.Squares.Select(square =>
        {
            var place = yard.PlaceOf(square);

            return $"{{\"n\":{Str(square.Name)},\"r\":{Num(place.Row)},\"c\":{Num(place.Column)},\"own\":{Bool(square.IsOwned)},\"price\":{Num(square.Price)}}}";
        });
        var wall = yard.Wall.Select(feature => $"{{\"kind\":{Str(feature.Kind == WallFeatureKind.Door ? "door" : "window")},\"at\":{Dec(yard.ColumnAt(feature.X))}}}");

        return $"{{\"cols\":{Num(yard.Columns)},\"rows\":{Num(yard.Rows)},\"squares\":[{string.Join(",", squares)}],\"wall\":[{string.Join(",", wall)}]}}";
    }

    private static string SettingsJson(PanelSettings? settings) => settings is null
        ? "null"
        : $"{{\"key\":{Str(settings.Key)},\"layout\":{Bool(settings.LayoutControls)},\"mode\":{Str(PowerModes.ConfigName(settings.DefaultMode))}"
            + $",\"range\":{Dec(settings.ProximityRange)},\"crawler\":{Dec(settings.CrawlerDodge)},\"armored\":{Dec(settings.ArmoredCrawlerDodge)},\"rate\":{Dec(settings.AttackRate)},\"waves\":{Bool(settings.AllowDuringWaves)}}}";

    private static string Dec(double value) => Math.Round(value, 2).ToString("0.##", CultureInfo.InvariantCulture);

    private static string CostJson(Survey survey, int itemId, int count, int shortBy)
    {
        var icon = survey.Materials.TryGetValue(itemId, out var material) ? material.Icon : null;

        return $"{{\"name\":{Str(survey.MaterialName(itemId))},\"icon\":{Str(icon)},\"count\":{Num(count)},\"short\":{Num(shortBy)}}}";
    }

    private static int NeededBy(IEnumerable<Defense> defenses, int itemId) => defenses.Sum(defense => defense.Cost.GetValueOrDefault(itemId));

    private static string State(Defense defense)
    {
        if (defense.IsBroken) return "broken";

        return defense.IsDamaged ? "damaged" : "intact";
    }

    /// <summary>A removal outranks a repair: the defense is coming down either way.</summary>
    private static string? Queued(Defense defense)
    {
        if (defense.IsRemovalQueued) return "remove";

        return defense.IsRepairQueued ? "repair" : null;
    }

    private static string Str(string? text) => text is null ? "null" : JsText.Literal(text);

    private static string Num(int value) => value.ToString(CultureInfo.InvariantCulture);

    private static string Bool(bool value) => value ? "true" : "false";
}

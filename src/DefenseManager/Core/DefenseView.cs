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
/// stands and the labels of its two power buttons, when there are power settings to show.
/// </summary>
public static class DefenseView
{
    /// <summary>The view for a read: the defenses, an empty view saying why they could not be read, or null outside a game.</summary>
    public static string? Of(DefenseRead read, PowerPolicy? power = null, bool isChinese = false) => read switch
    {
        DefenseRead.Ready ready => Build(ready.Survey, power),
        DefenseRead.NotReady notReady => Empty(OutcomeMessage.Describe(new DefenseOutcome.NotReady(notReady.Reason), isChinese) ?? notReady.Reason),
        DefenseRead.NoGame => null,
        _ => throw new InvalidOperationException("unknown defense read " + read),
    };

    public static string Build(Survey survey, PowerPolicy? power = null)
    {
        var budget = MaterialBudget.Of(survey);
        var locked = LockedText(survey);
        var toRepair = survey.Defenses.Where(defense => defense.IsDamaged && !defense.IsRepairQueued && !defense.IsRemovalQueued).ToList();
        var toClear = survey.Defenses.Count(defense => defense.IsBroken && RemoveCheck.Judge(defense) is RemoveVerdict.Allowed);
        var materials = survey.Materials.OrderBy(entry => entry.Key).Select(entry => MaterialJson(entry.Value, budget.Left(entry.Key), NeededBy(toRepair, entry.Key)));
        var rows = survey.Defenses.OrderBy(defense => defense.Name, StringComparer.Ordinal).ThenBy(defense => defense.Id).Select(defense => RowJson(defense, survey, budget, locked is null, power));

        return Json(note: null, locked, toRepair.Count, toClear, materials, rows);
    }

    /// <summary>The panel with no defenses and a note saying why, for when the game could not be read.</summary>
    public static string Empty(string note) => Json(note, locked: null, repairAll: 0, clearAll: 0, Enumerable.Empty<string>(), Enumerable.Empty<string>());

    private static string? LockedText(Survey survey)
    {
        if (survey.IsWaveLocked) return Texts.WaveLocked.Pick(survey.IsChinese);
        if (!survey.IsHome) return Texts.AwayFromHome.Pick(survey.IsChinese);

        return null;
    }

    private static string Json(string? note, string? locked, int repairAll, int clearAll, IEnumerable<string> materials, IEnumerable<string> rows)
    {
        var json = new StringBuilder(4096);

        json.Append("{\"note\":").Append(Str(note))
            .Append(",\"locked\":").Append(Str(locked))
            .Append(",\"repairAll\":").Append(Num(repairAll))
            .Append(",\"clearAll\":").Append(Num(clearAll))
            .Append(",\"materials\":[").AppendJoin(',', materials)
            .Append("],\"defenses\":[").AppendJoin(',', rows)
            .Append("]}");

        return json.ToString();
    }

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

        return $"{{\"id\":{Str(defense.Id.ToString(CultureInfo.InvariantCulture))},\"name\":{Str(defense.Name)},\"icon\":{Str(defense.Icon)}"
            + $",\"cur\":{Num(defense.Current)},\"max\":{Num(defense.Max)},\"state\":{Str(State(defense))},\"queued\":{Str(Queued(defense))}"
            + $",\"cost\":[{string.Join(",", cost)}]"
            + $",\"canRepair\":{Bool(isUnlocked && repair is RepairVerdict.Allowed)},\"why\":{Str(OutcomeMessage.WhyNot(repair, survey.IsChinese))}"
            + $",\"canRemove\":{Bool(isUnlocked && remove is RemoveVerdict.Allowed)},\"removeWhy\":{Str(OutcomeMessage.WhyNotRemove(remove, survey.IsChinese))}"
            + $",\"power\":{PowerJson(defense, power, survey.IsChinese)}}}";
    }

    /// <summary>
    /// Null for a defense with no power switch, or with no power settings to show (while they cannot be read). "label"
    /// is what the row's own button shows (the defense's setting) and "typeLabel" what its type's button shows;
    /// "managed" is false while the mod leaves the defense alone because it is broken or not working.
    /// </summary>
    private static string PowerJson(Defense defense, PowerPolicy? power, bool isChinese)
    {
        if (defense.Power is not { } state || power is null) return "null";

        var key = DefenseKey.Of(defense.ConfigId, defense.Position);

        return $"{{\"on\":{Bool(state.IsOn)},\"managed\":{Bool(defense.IsPowerManaged)},\"mode\":{Str(PowerModes.Label(power.ModeOf(key), isChinese))}"
            + $",\"label\":{Str(power.DefenseLabel(key, isChinese))},\"typeLabel\":{Str(power.TypeLabel(defense.ConfigId, isChinese))}}}";
    }

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

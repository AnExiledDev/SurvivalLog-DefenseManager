using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace DefenseManager.Core;

/// <summary>
/// The words a press ends with, and the short reasons the panel shows beside a disabled button: what started, then
/// each reason a defense was left and how many it left, so a defense that was not repaired is never a mystery.
/// </summary>
public static class OutcomeMessage
{
    public const string WaveLockedText = "A zombie wave is on: defenses can't be repaired or removed until it ends.";
    public const string AwayFromHomeText = "Go home first: your defenses are there.";

    private const int NamedShortfalls = 3;

    private static readonly char[] ClosingPunctuation = { '.', '!', '?', '\u3002', '\uff01', '\uff1f', ' ', '\t', '\r', '\n' };

    /// <summary>Unity rich-text tags such as &lt;color=#f55&gt;, &lt;/b&gt; or &lt;#ff0000&gt;, which the game's own UI renders but the panel would print.</summary>
    private static readonly Regex RichTextTag = new("</?[A-Za-z][^<>]*>|<#[0-9A-Fa-f]{3,8}>", RegexOptions.CultureInvariant);

    /// <summary>The status line for a press, or null when the press should pass without a word.</summary>
    public static string? Describe(DefenseOutcome outcome) => outcome switch
    {
        DefenseOutcome.NoGame => null,
        DefenseOutcome.NotReady notReady => $"Can't manage defenses right now: {Reason(notReady.Reason)}.",
        DefenseOutcome.WaveLocked => WaveLockedText,
        DefenseOutcome.AwayFromHome => AwayFromHomeText,
        DefenseOutcome.QueueBusy => "Finish or cancel your queued actions first.",
        DefenseOutcome.NothingToDo { Job: Job.Repair } => "Every defense is in full repair.",
        DefenseOutcome.NothingToDo { Job: Job.Remove } => "No broken defenses.",
        DefenseOutcome.Gone => "That defense is gone.",
        DefenseOutcome.RepairRefused refused => RepairRefusedMessage(refused.Name, refused.Verdict),
        DefenseOutcome.RemoveRefused refused => RemoveRefusedMessage(refused.Name, refused.Verdict),
        DefenseOutcome.NotQueued notQueued => $"The game would not queue the {JobNoun(notQueued.Job)} of {notQueued.Name}.",
        DefenseOutcome.StartFailed failed => $"Could not start the {Noun(failed.Count, JobNoun(failed.Job))}, see BepInEx/defense-manager.log.",
        DefenseOutcome.Queued { Job: Job.Repair } queued => $"Repairing {queued.Name}.",
        DefenseOutcome.Queued { Job: Job.Remove } queued => $"Removing {queued.Name}.",
        DefenseOutcome.Repaired repaired => Describe(repaired.Tally),
        DefenseOutcome.Cleared cleared => Describe(cleared.Tally),
        _ => throw new InvalidOperationException("unknown defense outcome " + outcome),
    };

    /// <summary>Why a row's Repair button is off, or null when the row already says it (allowed, intact, or queued).</summary>
    public static string? WhyNot(RepairVerdict verdict) => verdict switch
    {
        RepairVerdict.Allowed or RepairVerdict.Undamaged or RepairVerdict.AlreadyQueued or RepairVerdict.QueuedForRemoval => null,
        RepairVerdict.Blocked blocked => Reason(blocked.Reason) is { Length: > 0 } reason ? reason : "The game would not allow it",
        RepairVerdict.Short shortOf => "Needs " + Needs(shortOf.Missing),
        _ => throw new InvalidOperationException("unknown repair verdict " + verdict),
    };

    /// <summary>Why a row's Remove button is off, or null when the row already says it.</summary>
    public static string? WhyNotRemove(RemoveVerdict verdict) => verdict switch
    {
        RemoveVerdict.Allowed or RemoveVerdict.AlreadyQueued => null,
        RemoveVerdict.QueuedForRepair => "Repair queued: let it finish or cancel it first",
        _ => throw new InvalidOperationException("unknown remove verdict " + verdict),
    };

    private static string Describe(RepairTally tally)
    {
        var lines = new List<string> { RepairHeadline(tally.Queued, tally.AlreadyQueued) };

        if (tally.Short.Count > 0) lines.Add(ShortfallLine(tally.Short));
        if (tally.Blocked > 0) lines.Add(BlockedLine(tally.Blocked, tally.BlockedReason));
        if (tally.BeingRemoved > 0) lines.Add($"{tally.BeingRemoved} queued for removal, so not repaired.");
        if (tally.Refused > 0) lines.Add($"{tally.Refused} refused by the game's queue.");

        return string.Join(" ", lines);
    }

    private static string Describe(ClearTally tally)
    {
        var lines = new List<string> { ClearHeadline(tally.Queued, tally.AlreadyQueued) };

        if (tally.BeingRepaired > 0) lines.Add($"{tally.BeingRepaired} queued for repair, so kept.");
        if (tally.Refused > 0) lines.Add($"{tally.Refused} refused by the game's queue.");

        return string.Join(" ", lines);
    }

    private static string RepairHeadline(int queued, int alreadyQueued) => (queued, alreadyQueued) switch
    {
        (0, 0) => "Nothing repaired.",
        (0, _) => $"Already repairing: {Plural(alreadyQueued, "repair")} still queued.",
        (_, 0) => $"Repairing {Plural(queued, "defense")}.",
        _ => $"Repairing {Plural(queued, "defense")}, {alreadyQueued} more already queued.",
    };

    private static string ClearHeadline(int queued, int alreadyQueued) => (queued, alreadyQueued) switch
    {
        (0, 0) => "Nothing cleared.",
        (0, _) => $"Already clearing: {Plural(alreadyQueued, "removal")} still queued.",
        (_, 0) => $"Removing {Plural(queued, "broken defense")}.",
        _ => $"Removing {Plural(queued, "broken defense")}, {alreadyQueued} more already queued.",
    };

    /// <summary>Names the first few defenses left for lack of materials and what each is missing; the rest are counted.</summary>
    private static string ShortfallLine(IReadOnlyList<Shortfall> shortfalls)
    {
        var named = shortfalls.Take(NamedShortfalls).Select(shortfall => $"{shortfall.Name} needs {Needs(shortfall.Missing)}");
        var unnamed = shortfalls.Count - NamedShortfalls;
        var list = string.Join("; ", named) + (unnamed > 0 ? $"; and {unnamed} more" : "");

        return $"{shortfalls.Count} left for lack of materials: {list}.";
    }

    private static string BlockedLine(int count, string? reason)
    {
        var trimmed = reason is null ? "" : Reason(reason);

        return trimmed.Length == 0 ? $"{count} can't be repaired right now." : $"{count} can't be repaired right now: {trimmed}.";
    }

    private static string RepairRefusedMessage(string name, RepairVerdict verdict) => verdict switch
    {
        RepairVerdict.AlreadyQueued => $"{name} is already queued for repair.",
        RepairVerdict.QueuedForRemoval => $"{name} is queued for removal.",
        RepairVerdict.Undamaged => $"{name} needs no repair.",
        RepairVerdict.Blocked blocked => Reason(blocked.Reason) is { Length: > 0 } reason ? $"Can't repair {name}: {reason}." : $"Can't repair {name} right now.",
        RepairVerdict.Short shortOf => $"Can't repair {name}: needs {Needs(shortOf.Missing)}.",
        RepairVerdict.Allowed => throw new InvalidOperationException("an allowed repair was reported as refused"),
        _ => throw new InvalidOperationException("unknown repair verdict " + verdict),
    };

    private static string RemoveRefusedMessage(string name, RemoveVerdict verdict) => verdict switch
    {
        RemoveVerdict.AlreadyQueued => $"{name} is already queued for removal.",
        RemoveVerdict.QueuedForRepair => $"{name} is queued for repair: let it finish or cancel it first.",
        RemoveVerdict.Allowed => throw new InvalidOperationException("an allowed removal was reported as refused"),
        _ => throw new InvalidOperationException("unknown remove verdict " + verdict),
    };

    /// <summary>"3 more Wood", "3 more Wood and 1 more Part", "3 more Wood, 2 more Nail and 1 more Part".</summary>
    private static string Needs(IReadOnlyList<Need> missing)
    {
        var parts = missing.Select(need => $"{need.Missing} more {need.ItemName}").ToList();

        return parts.Count <= 1 ? string.Concat(parts) : string.Join(", ", parts.Take(parts.Count - 1)) + " and " + parts[^1];
    }

    /// <summary>
    /// The game's reasons are whole sentences, often in Chinese and sometimes coloured with rich-text tags; the tags
    /// would print as text, and their own closing mark would double ours.
    /// </summary>
    private static string Reason(string reason) => RichTextTag.Replace(reason, "").Trim().TrimEnd(ClosingPunctuation);

    private static string JobNoun(Job job) => job switch
    {
        Job.Repair => "repair",
        Job.Remove => "removal",
        _ => throw new ArgumentOutOfRangeException(nameof(job), job, "unknown job"),
    };

    private static string Plural(int count, string noun) => $"{count} {Noun(count, noun)}";

    private static string Noun(int count, string noun) => count == 1 ? noun : noun + "s";
}

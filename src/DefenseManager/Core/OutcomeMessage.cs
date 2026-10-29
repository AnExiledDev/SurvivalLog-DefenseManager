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
    private const int NamedShortfalls = 3;

    private static readonly char[] ClosingPunctuation = { '.', '!', '?', '。', '！', '？', ' ', '\t', '\r', '\n' };

    /// <summary>Unity rich-text tags such as &lt;color=#f55&gt;, &lt;/b&gt; or &lt;#ff0000&gt;, which the game's own UI renders but the panel would print.</summary>
    private static readonly Regex RichTextTag = new("</?[A-Za-z][^<>]*>|<#[0-9A-Fa-f]{3,8}>", RegexOptions.CultureInvariant);

    /// <summary>The status line for a press, or null when the press should pass without a word.</summary>
    public static string? Describe(DefenseOutcome outcome, bool isChinese = false) => outcome switch
    {
        DefenseOutcome.NoGame => null,
        DefenseOutcome.NotReady notReady => Texts.NotReady.Say(isChinese, Reason(notReady.Reason)),
        DefenseOutcome.WaveLocked => Texts.WaveLocked.Pick(isChinese),
        DefenseOutcome.AwayFromHome => Texts.AwayFromHome.Pick(isChinese),
        DefenseOutcome.QueueBusy => Texts.QueueBusy.Pick(isChinese),
        DefenseOutcome.NothingToDo { Job: Job.Repair } => Texts.AllRepaired.Pick(isChinese),
        DefenseOutcome.NothingToDo { Job: Job.Remove } => Texts.NoneBroken.Pick(isChinese),
        DefenseOutcome.Gone => Texts.Gone.Pick(isChinese),
        DefenseOutcome.RepairRefused refused => RepairRefusedMessage(refused.Name, refused.Verdict, isChinese),
        DefenseOutcome.RemoveRefused refused => RemoveRefusedMessage(refused.Name, refused.Verdict, isChinese),
        DefenseOutcome.NotQueued notQueued => Texts.NotQueued.Say(isChinese, JobNoun(notQueued.Job, 1, isChinese), notQueued.Name),
        DefenseOutcome.StartFailed failed => Texts.StartFailed.Say(isChinese, JobNoun(failed.Job, failed.Count, isChinese)),
        DefenseOutcome.Queued { Job: Job.Repair } queued => Texts.QueuedRepair.Say(isChinese, queued.Name),
        DefenseOutcome.Queued { Job: Job.Remove } queued => Texts.QueuedRemoval.Say(isChinese, queued.Name),
        DefenseOutcome.SquareTaken => Texts.SquareTaken.Pick(isChinese),
        DefenseOutcome.NoPackage => Texts.NoPackage.Pick(isChinese),
        DefenseOutcome.InstallBusy => Texts.InstallBusy.Pick(isChinese),
        DefenseOutcome.NoFreeSpot noSpot => Texts.NoFreeSpot.Say(isChinese, noSpot.Name),
        DefenseOutcome.BankFailed failed => Texts.BankFailed.Say(isChinese, failed.Name),
        DefenseOutcome.NotInstalled refused => FromBank(Texts.NotInstalled.Say(isChinese, refused.Name), refused.FromBank, isChinese),
        DefenseOutcome.Installing installing => FromBank(Texts.Installing.Say(isChinese, installing.Name), installing.FromBank, isChinese),
        DefenseOutcome.Repaired repaired => Describe(repaired.Tally, isChinese),
        DefenseOutcome.Cleared cleared => Describe(cleared.Tally, isChinese),
        _ => throw new InvalidOperationException("unknown defense outcome " + outcome),
    };

    /// <summary>
    /// The status line for a press made while the game's Layout screen is up. Layout pauses the game, so work it lines
    /// up waits there: the line says so whenever the press left anything in the queue, new or already there.
    /// </summary>
    public static string? Describe(DefenseOutcome outcome, bool isInLayout, bool isChinese = false)
    {
        var message = Describe(outcome, isChinese);

        return isInLayout && message != null && LeavesWorkQueued(outcome) ? Texts.Sentences(isChinese, new[] { message, Texts.InLayout.Pick(isChinese) }) : message;
    }

    /// <summary>Why a row's Repair button is off, or null when the row already says it (allowed, intact, or queued).</summary>
    public static string? WhyNot(RepairVerdict verdict, bool isChinese = false) => verdict switch
    {
        RepairVerdict.Allowed or RepairVerdict.Undamaged or RepairVerdict.AlreadyQueued or RepairVerdict.QueuedForRemoval => null,
        RepairVerdict.Blocked blocked => Reason(blocked.Reason) is { Length: > 0 } reason ? reason : Texts.NotAllowed.Pick(isChinese),
        RepairVerdict.Short shortOf => Texts.Needs.Say(isChinese, Needs(shortOf.Missing, isChinese)),
        _ => throw new InvalidOperationException("unknown repair verdict " + verdict),
    };

    /// <summary>Why a row's Remove button is off, or null when the row already says it.</summary>
    public static string? WhyNotRemove(RemoveVerdict verdict, bool isChinese = false) => verdict switch
    {
        RemoveVerdict.Allowed or RemoveVerdict.AlreadyQueued => null,
        RemoveVerdict.QueuedForRepair => Texts.RepairQueuedFirst.Pick(isChinese),
        _ => throw new InvalidOperationException("unknown remove verdict " + verdict),
    };

    private static bool LeavesWorkQueued(DefenseOutcome outcome) => outcome switch
    {
        DefenseOutcome.Queued or DefenseOutcome.Installing => true,
        DefenseOutcome.Repaired repaired => repaired.Tally.Queued + repaired.Tally.AlreadyQueued > 0,
        DefenseOutcome.Cleared cleared => cleared.Tally.Queued + cleared.Tally.AlreadyQueued > 0,
        DefenseOutcome.NoGame or DefenseOutcome.NotReady or DefenseOutcome.WaveLocked or DefenseOutcome.AwayFromHome or DefenseOutcome.QueueBusy
            or DefenseOutcome.NothingToDo or DefenseOutcome.Gone or DefenseOutcome.RepairRefused or DefenseOutcome.RemoveRefused
            or DefenseOutcome.NotQueued or DefenseOutcome.StartFailed or DefenseOutcome.SquareTaken or DefenseOutcome.NoPackage
            or DefenseOutcome.InstallBusy or DefenseOutcome.NoFreeSpot or DefenseOutcome.BankFailed or DefenseOutcome.NotInstalled => false,
        _ => throw new InvalidOperationException("unknown defense outcome " + outcome),
    };

    /// <summary>A package that came out of the bank stays in the yard whatever happens next, so the line says where it went.</summary>
    private static string FromBank(string message, bool isFromBank, bool isChinese) =>
        isFromBank ? Texts.Sentences(isChinese, new[] { message, Texts.TakenOutOfBank.Pick(isChinese) }) : message;

    private static string Describe(RepairTally tally, bool isChinese)
    {
        var lines = new List<string> { RepairHeadline(tally.Queued, tally.AlreadyQueued, isChinese) };

        if (tally.Short.Count > 0) lines.Add(ShortfallLine(tally.Short, isChinese));
        if (tally.Blocked > 0) lines.Add(BlockedLine(tally.Blocked, tally.BlockedReason, isChinese));
        if (tally.BeingRemoved > 0) lines.Add(Texts.BeingRemoved.Say(isChinese, tally.BeingRemoved));
        if (tally.Refused > 0) lines.Add(Texts.RefusedByQueue.Say(isChinese, tally.Refused));

        return Texts.Sentences(isChinese, lines);
    }

    private static string Describe(ClearTally tally, bool isChinese)
    {
        var lines = new List<string> { ClearHeadline(tally.Queued, tally.AlreadyQueued, isChinese) };

        if (tally.BeingRepaired > 0) lines.Add(Texts.BeingRepaired.Say(isChinese, tally.BeingRepaired));
        if (tally.Refused > 0) lines.Add(Texts.RefusedByQueue.Say(isChinese, tally.Refused));

        return Texts.Sentences(isChinese, lines);
    }

    private static string RepairHeadline(int queued, int alreadyQueued, bool isChinese) => (queued, alreadyQueued) switch
    {
        (0, 0) => Texts.NothingRepaired.Pick(isChinese),
        (0, _) => Texts.AlreadyRepairing.Say(isChinese, alreadyQueued),
        (_, 0) => Texts.Repairing.Say(isChinese, queued),
        _ => Texts.RepairingMore.Say(isChinese, queued, alreadyQueued),
    };

    private static string ClearHeadline(int queued, int alreadyQueued, bool isChinese) => (queued, alreadyQueued) switch
    {
        (0, 0) => Texts.NothingCleared.Pick(isChinese),
        (0, _) => Texts.AlreadyClearing.Say(isChinese, alreadyQueued),
        (_, 0) => Texts.Removing.Say(isChinese, queued),
        _ => Texts.RemovingMore.Say(isChinese, queued, alreadyQueued),
    };

    /// <summary>Names the first few defenses left for lack of materials and what each is missing; the rest are counted.</summary>
    private static string ShortfallLine(IReadOnlyList<Shortfall> shortfalls, bool isChinese)
    {
        var named = shortfalls.Take(NamedShortfalls).Select(shortfall => Texts.ShortfallNamed.Say(isChinese, shortfall.Name, Needs(shortfall.Missing, isChinese))).ToList();
        var unnamed = shortfalls.Count - NamedShortfalls;

        if (unnamed > 0) named.Add(Texts.AndMore.Say(isChinese, unnamed));

        return Texts.ShortOfMaterials.Say(isChinese, shortfalls.Count, string.Join(isChinese ? "；" : "; ", named));
    }

    private static string BlockedLine(int count, string? reason, bool isChinese)
    {
        var trimmed = reason is null ? "" : Reason(reason);

        return trimmed.Length == 0 ? Texts.CantRepairCount.Say(isChinese, count) : Texts.CantRepairCountWhy.Say(isChinese, count, trimmed);
    }

    private static string RepairRefusedMessage(string name, RepairVerdict verdict, bool isChinese) => verdict switch
    {
        RepairVerdict.AlreadyQueued => Texts.AlreadyQueuedRepair.Say(isChinese, name),
        RepairVerdict.QueuedForRemoval => Texts.QueuedForRemoval.Say(isChinese, name),
        RepairVerdict.Undamaged => Texts.NeedsNoRepair.Say(isChinese, name),
        RepairVerdict.Blocked blocked => Reason(blocked.Reason) is { Length: > 0 } reason ? Texts.CantRepairWhy.Say(isChinese, name, reason) : Texts.CantRepair.Say(isChinese, name),
        RepairVerdict.Short shortOf => Texts.CantRepairShort.Say(isChinese, name, Needs(shortOf.Missing, isChinese)),
        RepairVerdict.Allowed => throw new InvalidOperationException("an allowed repair was reported as refused"),
        _ => throw new InvalidOperationException("unknown repair verdict " + verdict),
    };

    private static string RemoveRefusedMessage(string name, RemoveVerdict verdict, bool isChinese) => verdict switch
    {
        RemoveVerdict.AlreadyQueued => Texts.AlreadyQueuedRemoval.Say(isChinese, name),
        RemoveVerdict.QueuedForRepair => Texts.QueuedForRepair.Say(isChinese, name),
        RemoveVerdict.Allowed => throw new InvalidOperationException("an allowed removal was reported as refused"),
        _ => throw new InvalidOperationException("unknown remove verdict " + verdict),
    };

    /// <summary>"3 more Wood", "3 more Wood and 1 more Part", "3 more Wood, 2 more Nail and 1 more Part".</summary>
    private static string Needs(IReadOnlyList<Need> missing, bool isChinese)
    {
        var parts = missing.Select(need => Texts.NeedItem.Say(isChinese, need.Missing, need.ItemName)).ToList();
        var comma = isChinese ? "、" : ", ";
        var and = isChinese ? "和" : " and ";

        return parts.Count <= 1 ? string.Concat(parts) : string.Join(comma, parts.Take(parts.Count - 1)) + and + parts[^1];
    }

    /// <summary>
    /// The game's reasons are whole sentences, often in Chinese and sometimes coloured with rich-text tags; the tags
    /// would print as text, and their own closing mark would double ours.
    /// </summary>
    private static string Reason(string reason) => RichTextTag.Replace(reason, "").Trim().TrimEnd(ClosingPunctuation);

    private static string JobNoun(Job job, int count, bool isChinese) => job switch
    {
        Job.Repair => (count == 1 ? Texts.RepairJob : Texts.RepairJobs).Pick(isChinese),
        Job.Remove => (count == 1 ? Texts.RemovalJob : Texts.RemovalJobs).Pick(isChinese),
        _ => throw new ArgumentOutOfRangeException(nameof(job), job, "unknown job"),
    };
}

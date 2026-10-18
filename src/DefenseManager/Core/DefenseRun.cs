using System;
using System.Collections.Generic;
using System.Linq;

namespace DefenseManager.Core;

/// <summary>
/// The four panel buttons. Each reads the game once, decides everything from that read, and only then lines up real
/// repairs or removals, the same ones the defense's own menu queues. Nothing is lined up during a zombie wave, away
/// from home, or when starting would throw away actions the player queued; a repair is only lined up when the game's
/// own check allows it, the character has the stamina to start one, and the materials left after earlier repairs
/// cover it.
/// </summary>
public static class DefenseRun
{
    public static DefenseOutcome RepairAll(IDefenseGame game) => WithSurvey(game, survey => RepairAll(game, survey));

    public static DefenseOutcome ClearBroken(IDefenseGame game) => WithSurvey(game, survey => ClearBroken(game, survey));

    public static DefenseOutcome Repair(IDefenseGame game, long defenseId) => WithSurvey(game, survey => Repair(game, survey, defenseId));

    public static DefenseOutcome Remove(IDefenseGame game, long defenseId) => WithSurvey(game, survey => Remove(game, survey, defenseId));

    private static DefenseOutcome WithSurvey(IDefenseGame game, Func<Survey, DefenseOutcome> press)
    {
        var read = game.Read();

        return read switch
        {
            DefenseRead.NoGame => new DefenseOutcome.NoGame(),
            DefenseRead.NotReady notReady => new DefenseOutcome.NotReady(notReady.Reason),
            DefenseRead.Ready { Survey.IsWaveLocked: true } => new DefenseOutcome.WaveLocked(),
            DefenseRead.Ready { Survey.IsHome: false } => new DefenseOutcome.AwayFromHome(),
            DefenseRead.Ready ready => press(ready.Survey),
            _ => throw new InvalidOperationException("unknown defense read " + read),
        };
    }

    /// <summary>
    /// Repairs every damaged defense the materials cover, walking nearest-next from the character so the round does
    /// not zigzag. A defense the materials left cannot cover is skipped and the walk goes on to the next nearest one
    /// they can, so one costly repair does not hold back the cheap ones behind it.
    /// </summary>
    private static DefenseOutcome RepairAll(IDefenseGame game, Survey survey)
    {
        var damaged = survey.Defenses.Where(defense => defense.IsDamaged).ToList();

        if (damaged.Count == 0) return new DefenseOutcome.NothingToDo(Job.Repair);

        var alreadyQueued = damaged.Count(defense => defense.IsRepairQueued);
        var beingRemoved = damaged.Count(defense => defense.IsRemovalQueued && !defense.IsRepairQueued);
        var candidates = damaged.Where(defense => !defense.IsRepairQueued && !defense.IsRemovalQueued);
        var plan = new RepairPlan(survey);
        var round = WalkOrder.Visit(survey.Character, candidates, plan.TryTake);

        if (round.Count == 0) return new DefenseOutcome.Repaired(plan.Tally(0, alreadyQueued, beingRemoved, 0));
        // Actions the player queued are never traded for repairs, so this is checked before anything is lined up.
        if (survey.QueueOnStart == QueueOnStart.ReplacesPlayerActions) return new DefenseOutcome.QueueBusy();

        var (queued, refused) = QueueAll(game, Job.Repair, round);
        var done = new DefenseOutcome.Repaired(plan.Tally(queued, alreadyQueued, beingRemoved, refused));

        return Start(game, Job.Repair, queued, done);
    }

    /// <summary>
    /// Takes down every broken defense, nearest-next from the character. One queued for repair is kept: the player
    /// chose to fix it, and taking it down would throw that repair's materials away.
    /// </summary>
    private static DefenseOutcome ClearBroken(IDefenseGame game, Survey survey)
    {
        var broken = survey.Defenses.Where(defense => defense.IsBroken).ToList();

        if (broken.Count == 0) return new DefenseOutcome.NothingToDo(Job.Remove);

        var verdicts = broken.Select(defense => (Defense: defense, Verdict: RemoveCheck.Judge(defense))).ToList();
        var alreadyQueued = verdicts.Count(judged => judged.Verdict is RemoveVerdict.AlreadyQueued);
        var beingRepaired = verdicts.Count(judged => judged.Verdict is RemoveVerdict.QueuedForRepair);
        var candidates = verdicts.Where(judged => judged.Verdict is RemoveVerdict.Allowed).Select(judged => judged.Defense).ToList();

        if (candidates.Count == 0) return new DefenseOutcome.Cleared(new ClearTally(0, alreadyQueued, beingRepaired, 0));
        if (survey.QueueOnStart == QueueOnStart.ReplacesPlayerActions) return new DefenseOutcome.QueueBusy();

        var round = WalkOrder.Visit(survey.Character, candidates, _ => true);
        var (queued, refused) = QueueAll(game, Job.Remove, round);
        var done = new DefenseOutcome.Cleared(new ClearTally(queued, alreadyQueued, beingRepaired, refused));

        return Start(game, Job.Remove, queued, done);
    }

    private static DefenseOutcome Repair(IDefenseGame game, Survey survey, long defenseId)
    {
        var defense = survey.Defenses.FirstOrDefault(candidate => candidate.Id == defenseId);

        if (defense is null) return new DefenseOutcome.Gone();

        var verdict = RepairCheck.Judge(defense, MaterialBudget.Of(survey), survey);

        if (verdict is not RepairVerdict.Allowed) return new DefenseOutcome.RepairRefused(defense.Name, verdict);
        if (survey.QueueOnStart == QueueOnStart.ReplacesPlayerActions) return new DefenseOutcome.QueueBusy();

        return QueueOne(game, Job.Repair, defense);
    }

    private static DefenseOutcome Remove(IDefenseGame game, Survey survey, long defenseId)
    {
        var defense = survey.Defenses.FirstOrDefault(candidate => candidate.Id == defenseId);

        if (defense is null) return new DefenseOutcome.Gone();

        var verdict = RemoveCheck.Judge(defense);

        if (verdict is not RemoveVerdict.Allowed) return new DefenseOutcome.RemoveRefused(defense.Name, verdict);
        if (survey.QueueOnStart == QueueOnStart.ReplacesPlayerActions) return new DefenseOutcome.QueueBusy();

        return QueueOne(game, Job.Remove, defense);
    }

    private static DefenseOutcome QueueOne(IDefenseGame game, Job job, Defense defense)
    {
        if (!game.TryQueue(job, defense.Id)) return new DefenseOutcome.NotQueued(job, defense.Name);

        return game.TryStart() ? new DefenseOutcome.Queued(job, defense.Name) : new DefenseOutcome.StartFailed(job, 1);
    }

    private static (int Queued, int Refused) QueueAll(IDefenseGame game, Job job, IEnumerable<Defense> round)
    {
        var queued = 0;
        var refused = 0;

        foreach (var defense in round)
        {
            if (game.TryQueue(job, defense.Id)) queued++;
            else refused++;
        }

        return (queued, refused);
    }

    /// <summary>
    /// Starting with nothing lined up does nothing useful: the game logs an error, and when starting replaces the
    /// queue it first cancels whatever the character is doing.
    /// </summary>
    private static DefenseOutcome Start(IDefenseGame game, Job job, int queued, DefenseOutcome done)
    {
        if (queued == 0) return done;

        return game.TryStart() ? done : new DefenseOutcome.StartFailed(job, queued);
    }

    /// <summary>The materials one Repair all may still hand out, and every damaged defense it had to leave and why.</summary>
    private sealed class RepairPlan
    {
        private readonly Survey _survey;
        private readonly MaterialBudget _budget;
        private readonly List<Shortfall> _short = new();
        private int _blocked;
        private string? _blockedReason;

        public RepairPlan(Survey survey)
        {
            _survey = survey;
            _budget = MaterialBudget.Of(survey);
        }

        /// <summary>Takes the defense into the round when the game allows its repair and the materials left cover it, setting them aside.</summary>
        public bool TryTake(Defense defense)
        {
            var verdict = RepairCheck.Judge(defense, _budget, _survey);

            switch (verdict)
            {
                case RepairVerdict.Allowed:
                    _budget.Reserve(defense.Cost);

                    return true;
                case RepairVerdict.Short shortOf:
                    _short.Add(new Shortfall(defense.Name, shortOf.Missing));

                    return false;
                case RepairVerdict.Blocked blocked:
                    _blocked++;
                    _blockedReason ??= blocked.Reason;

                    return false;
                case RepairVerdict.Undamaged or RepairVerdict.AlreadyQueued or RepairVerdict.QueuedForRemoval:
                    return false;
                default:
                    throw new InvalidOperationException("unknown repair verdict " + verdict);
            }
        }

        public RepairTally Tally(int queued, int alreadyQueued, int beingRemoved, int refused) =>
            new(queued, alreadyQueued, _short, _blocked, _blockedReason, beingRemoved, refused);
    }
}

using DefenseManager.Core;

namespace DefenseManager.Tests;

/// <summary>
/// The game as far as a defense press can tell: the home's defenses, the player's materials, the jobs the character
/// has lined up and its action queue. Like the game, lining up a repair is never refused for want of materials or by
/// the repair's own check (the game only finds out when the repair's turn comes), so <see cref="FailingRepairs"/> shows
/// a press that queued repairs bound to fail. Starting appends to the queue, as the game does after the prologue; with
/// <see cref="StartClearsQueue"/> it replaces the queue, as the game's default action mode does, so a test can see a
/// press that would have thrown away the character's actions.
/// </summary>
internal sealed class FakeGame : IDefenseGame
{
    public const int Wood = 1;
    public const int Nail = 2;

    /// <summary>What the character is doing before any press, by its own choice. Not a defense job.</summary>
    public static readonly Act Walking = new(null, -1);

    /// <summary>An action the player queued (eating). Not a defense job.</summary>
    public static readonly Act Eating = new(null, -2);

    /// <summary>The title screen, a loading save or the prologue: there is no game to manage.</summary>
    public bool IsNoGame { get; set; }

    public string? NotReadyReason { get; set; }

    public bool IsWaveLocked { get; set; }

    public bool IsHome { get; set; } = true;

    /// <summary>The game's language is Chinese.</summary>
    public bool IsChinese { get; set; }

    public bool StartClearsQueue { get; set; }

    /// <summary>The game's queue refuses everything, as it does for an action id it does not know.</summary>
    public bool RefusesToQueue { get; set; }

    /// <summary>Defenses the game's queue refuses one by one.</summary>
    public HashSet<long> Refuses { get; } = new();

    public bool FailsToStart { get; set; }

    /// <summary>The yard grid the read hands out, none by default.</summary>
    public Yard? Yard { get; set; }

    public Spot Character { get; set; }

    /// <summary>The defenses as they stand; their queued flags are ignored, the queue below says what is queued.</summary>
    public List<Defense> Defenses { get; } = new();

    /// <summary>The character's stamina and the least a repair may start with; null when the game could not report it.</summary>
    public RepairStamina? Stamina { get; set; }

    /// <summary>What one repair really spends, which the mod cannot read and which is often less than the starting minimum.</summary>
    public float SpentPerRepair { get; set; }

    /// <summary>Material stock by item id.</summary>
    public Dictionary<int, int> Stock { get; } = new();

    public List<Act> LinedUp { get; } = new();

    /// <summary>The character's action queue, the running action first.</summary>
    public List<Act> Running { get; private set; } = new() { Walking };

    public IEnumerable<long> Repairing => Running.Where(act => act.Job == Job.Repair).Select(act => act.Target);

    public IEnumerable<long> Removing => Running.Where(act => act.Job == Job.Remove).Select(act => act.Target);

    /// <summary>
    /// Repairs in the queue the game will refuse when their turn comes: blocked by its own check, or beyond the
    /// starting minimum of stamina or the materials once the repairs ahead of them have spent theirs.
    /// </summary>
    public IEnumerable<long> FailingRepairs
    {
        get
        {
            var left = new Dictionary<int, int>(Stock);
            var staminaLeft = Stamina?.Have ?? float.PositiveInfinity;
            var neededToStart = Stamina?.NeededToStart ?? 0;

            foreach (var act in LinedUp.Concat(Running).Where(act => act.Job == Job.Repair))
            {
                var defense = Defenses.Single(candidate => candidate.Id == act.Target);
                var isAffordable = defense.Cost.All(entry => left.GetValueOrDefault(entry.Key) >= entry.Value);

                if (defense.RepairBlockedReason is not null || !isAffordable || staminaLeft < neededToStart)
                {
                    yield return defense.Id;
                    continue;
                }

                foreach (var (itemId, count) in defense.Cost) left[itemId] = left.GetValueOrDefault(itemId) - count;

                staminaLeft -= SpentPerRepair;
            }
        }
    }

    /// <summary>A defense halfway repaired unless told otherwise, standing on the x axis.</summary>
    public static Defense Def(long id, double x = 0, double z = 0, int cur = 50, int max = 100, bool broken = false, string? blocked = null, IReadOnlyDictionary<int, int>? cost = null, string? name = null) =>
        new(id, 36001, name ?? $"Defense {id}", null, cur, max, broken, new Spot(x, 0, z), cost ?? Cost(), blocked, IsRepairQueued: false, IsRemovalQueued: false);

    public static IReadOnlyDictionary<int, int> Cost(params (int ItemId, int Count)[] items) => items.ToDictionary(item => item.ItemId, item => item.Count);

    public static string NameOf(int itemId) => itemId switch
    {
        Wood => "Wood",
        Nail => "Nail",
        _ => $"Item {itemId}",
    };

    public DefenseRead Read()
    {
        if (IsNoGame) return new DefenseRead.NoGame();
        if (NotReadyReason is { } reason) return new DefenseRead.NotReady(reason);

        var queue = LinedUp.Concat(Running).ToList();
        var defenses = Defenses.Select(defense => defense with { IsRepairQueued = queue.Contains(new Act(Job.Repair, defense.Id)), IsRemovalQueued = queue.Contains(new Act(Job.Remove, defense.Id)) }).ToList();
        var materials = Stock.ToDictionary(entry => entry.Key, entry => new Material(NameOf(entry.Key), null, entry.Value));

        return new DefenseRead.Ready(new Survey(defenses, Character, materials, WhatStartDoes(), IsWaveLocked, IsHome, Stamina, IsChinese, Yard));
    }

    /// <summary>Like the game's own queue, this takes any job it is handed, whatever the materials or the repair check say.</summary>
    public bool TryQueue(Job job, long defenseId)
    {
        if (RefusesToQueue || Refuses.Contains(defenseId)) return false;

        LinedUp.Add(new Act(job, defenseId));

        return true;
    }

    public bool TryStart()
    {
        if (FailsToStart) return false;

        if (StartClearsQueue) Running = LinedUp.ToList();
        else Running.AddRange(LinedUp);

        LinedUp.Clear();

        return true;
    }

    private QueueOnStart WhatStartDoes()
    {
        var hasPlayerActions = Running.Any(act => act != Walking);

        if (!StartClearsQueue) return hasPlayerActions ? QueueOnStart.AppendsBehindPlayerActions : QueueOnStart.Appends;

        return hasPlayerActions ? QueueOnStart.ReplacesPlayerActions : QueueOnStart.ReplacesOwnActions;
    }
}

/// <summary>One entry in the character's queue: a defense job and its target, or (Job null) anything else.</summary>
internal readonly record struct Act(Job? Job, long Target);

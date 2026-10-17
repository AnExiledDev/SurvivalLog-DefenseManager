using System.Collections.Generic;
using System.Globalization;

namespace DefenseManager.Core;

/// <summary>One piece of on-screen text in the two languages the game ships. {0}, {1} are filled in by Say.</summary>
public readonly record struct Phrase(string En, string Zh)
{
    public string Pick(bool isChinese) => isChinese ? Zh : En;

    public string Say(bool isChinese, params object[] args) => string.Format(CultureInfo.InvariantCulture, Pick(isChinese), args);
}

/// <summary>A phrase about a count: English says "1 repair" and "2 repairs", Chinese has one form for both. {0} is the count.</summary>
public readonly record struct Counted(Phrase One, Phrase Many)
{
    public string Say(bool isChinese, int count, params object[] more)
    {
        var args = new object[more.Length + 1];

        args[0] = count;
        more.CopyTo(args, 1);

        return (count == 1 ? One : Many).Say(isChinese, args);
    }
}

/// <summary>The words the mod puts on screen: press results, refusal reasons, power settings and the panel's own labels.</summary>
public static class Texts
{
    private const string Log = "BepInEx/defense-manager.log";

    // Press results and the reasons beside a disabled button.
    public static readonly Phrase NotReady = new("Can't manage defenses right now: {0}.", "现在无法管理防御设施：{0}。");
    public static readonly Phrase WaveLocked = new("A zombie wave is on: defenses can't be repaired or removed until it ends.", "尸潮正在进行：结束前无法修理或拆除防御设施。");
    public static readonly Phrase AwayFromHome = new("Go home first: your defenses are there.", "请先回家：你的防御设施在那里。");
    public static readonly Phrase InLayout = new("Work starts when you leave Layout.", "离开「规划模式」后开始工作。");
    public static readonly Phrase QueueBusy = new("Finish or cancel your queued actions first.", "请先完成或取消已排队的行动。");
    public static readonly Phrase AllRepaired = new("Every defense is in full repair.", "所有防御设施都不需要修理。");
    public static readonly Phrase NoneBroken = new("No broken defenses.", "没有损毁的防御设施。");
    public static readonly Phrase Gone = new("That defense is gone.", "这个防御设施已经不在了。");

    /// <summary>{0} is the job ("repair", "removal"), {1} the defense.</summary>
    public static readonly Phrase NotQueued = new("The game would not queue the {0} of {1}.", "游戏没有把{1}加入{0}队列。");

    /// <summary>{0} is the job, already in the plural when there were several.</summary>
    public static readonly Phrase StartFailed = new("Could not start the {0}, see " + Log + ".", "无法开始{0}，详见 " + Log + "。");

    public static readonly Phrase RepairJob = new("repair", "修理");
    public static readonly Phrase RepairJobs = new("repairs", "修理工作");
    public static readonly Phrase RemovalJob = new("removal", "拆除");
    public static readonly Phrase RemovalJobs = new("removals", "拆除工作");
    public static readonly Phrase QueuedRepair = new("Repairing {0}.", "正在修理{0}。");
    public static readonly Phrase QueuedRemoval = new("Removing {0}.", "正在拆除{0}。");
    public static readonly Phrase NothingRepaired = new("Nothing repaired.", "没有修理任何防御设施。");
    public static readonly Counted AlreadyRepairing = new(new("Already repairing: 1 repair still queued.", "已在修理：还有 {0} 项修理在排队。"), new("Already repairing: {0} repairs still queued.", "已在修理：还有 {0} 项修理在排队。"));
    public static readonly Counted Repairing = new(new("Repairing 1 defense.", "正在修理 {0} 个防御设施。"), new("Repairing {0} defenses.", "正在修理 {0} 个防御设施。"));

    /// <summary>{1} is how many were already queued.</summary>
    public static readonly Counted RepairingMore = new(new("Repairing 1 defense, {1} more already queued.", "正在修理 {0} 个防御设施，另有 {1} 个已在队列中。"), new("Repairing {0} defenses, {1} more already queued.", "正在修理 {0} 个防御设施，另有 {1} 个已在队列中。"));

    public static readonly Phrase NothingCleared = new("Nothing cleared.", "没有清除任何防御设施。");
    public static readonly Counted AlreadyClearing = new(new("Already clearing: 1 removal still queued.", "已在清除：还有 {0} 项拆除在排队。"), new("Already clearing: {0} removals still queued.", "已在清除：还有 {0} 项拆除在排队。"));
    public static readonly Counted Removing = new(new("Removing 1 broken defense.", "正在拆除 {0} 个损毁的防御设施。"), new("Removing {0} broken defenses.", "正在拆除 {0} 个损毁的防御设施。"));
    public static readonly Counted RemovingMore = new(new("Removing 1 broken defense, {1} more already queued.", "正在拆除 {0} 个损毁的防御设施，另有 {1} 个已在队列中。"), new("Removing {0} broken defenses, {1} more already queued.", "正在拆除 {0} 个损毁的防御设施，另有 {1} 个已在队列中。"));
    public static readonly Phrase BeingRemoved = new("{0} queued for removal, so not repaired.", "{0} 个已排队拆除，因此未修理。");
    public static readonly Phrase BeingRepaired = new("{0} queued for repair, so kept.", "{0} 个已排队修理，因此保留。");
    public static readonly Phrase RefusedByQueue = new("{0} refused by the game's queue.", "{0} 个被游戏的队列拒绝。");

    /// <summary>{0} is how many, {1} the named list.</summary>
    public static readonly Phrase ShortOfMaterials = new("{0} left for lack of materials: {1}.", "{0} 个因材料不足未修理：{1}。");

    public static readonly Phrase ShortfallNamed = new("{0} needs {1}", "{0}还差{1}");
    public static readonly Phrase AndMore = new("and {0} more", "另有 {0} 个");
    public static readonly Phrase CantRepairCount = new("{0} can't be repaired right now.", "{0} 个现在无法修理。");
    public static readonly Phrase CantRepairCountWhy = new("{0} can't be repaired right now: {1}.", "{0} 个现在无法修理：{1}。");
    public static readonly Phrase AlreadyQueuedRepair = new("{0} is already queued for repair.", "{0}已在修理队列中。");
    public static readonly Phrase QueuedForRemoval = new("{0} is queued for removal.", "{0}已排队等待拆除。");
    public static readonly Phrase NeedsNoRepair = new("{0} needs no repair.", "{0}不需要修理。");
    public static readonly Phrase CantRepairWhy = new("Can't repair {0}: {1}.", "无法修理{0}：{1}。");
    public static readonly Phrase CantRepair = new("Can't repair {0} right now.", "现在无法修理{0}。");
    public static readonly Phrase CantRepairShort = new("Can't repair {0}: needs {1}.", "无法修理{0}：还差{1}。");
    public static readonly Phrase AlreadyQueuedRemoval = new("{0} is already queued for removal.", "{0}已在拆除队列中。");
    public static readonly Phrase QueuedForRepair = new("{0} is queued for repair: let it finish or cancel it first.", "{0}已排队修理：请等它完成或先取消。");
    public static readonly Phrase NotAllowed = new("The game would not allow it", "游戏不允许");
    public static readonly Phrase Needs = new("Needs {0}", "还差{0}");
    public static readonly Phrase RepairQueuedFirst = new("Repair queued: let it finish or cancel it first", "已排队修理：请等它完成或先取消");

    /// <summary>One missing material: {0} is how many, {1} its name.</summary>
    public static readonly Phrase NeedItem = new("{0} more {1}", "{0} 个{1}");

    public static readonly Phrase OutOfStamina = new("Not enough stamina for another repair (each needs {0})", "精力不足，无法再修理（每次需要 {0}）");

    /// <summary>A material the game did not name; {0} is its id.</summary>
    public static readonly Phrase UnnamedItem = new("Item {0}", "物品 {0}");

    // Why the game could not be read, or why one defense cannot be repaired.
    public static readonly Phrase NoWorld = new("the game world is not loaded", "游戏世界尚未载入");
    public static readonly Phrase ReadFailed = new("the game could not be read, see " + Log, "无法读取游戏，详见 " + Log);
    public static readonly Phrase NoRepair = new("it has no repair", "它无法修理");
    public static readonly Phrase CostUnreadable = new("its repair cost could not be read", "无法读取它的修理费用");
    public static readonly Phrase CheckFailed = new("the game's repair check failed, see " + Log, "游戏的修理检查出错，详见 " + Log);

    public static readonly Phrase Failed = new("Defense Manager failed, see " + Log + ".", "Defense Manager 出错，详见 " + Log + "。");

    // Power settings.
    public static readonly Phrase NoSave = new("Power settings are kept per save, and this game has no save yet. Save the game first.", "供电设置按存档保存，而这局游戏还没有存档。请先保存游戏。");
    public static readonly Phrase Unreadable = new("The power settings file could not be read, so nothing was changed. See " + Log + ".", "无法读取供电设置文件，因此没有做任何更改。详见 " + Log + "。");
    public static readonly Phrase NoGame = new("Load a game first.", "请先载入游戏。");
    public static readonly Phrase NoSwitch = new("That defense has no power switch.", "这个防御设施没有电源开关。");
    public static readonly Phrase NotSaved = new("It could not be written to the settings file, so it lasts until the game is closed.", "设置无法写入设置文件，只在关闭游戏前有效。");

    /// <summary>{0} is the defense, {1} the mode.</summary>
    public static readonly Phrase DefenseSet = new("{0} set to {1}.", "{0}已设为{1}。");

    public static readonly Phrase DefenseFollowsType = new("{0} now follows its type setting ({1}).", "{0}现在跟随类型设置（{1}）。");
    public static readonly Phrase TypeSet = new("Every {0} set to {1}.", "所有{0}已设为{1}。");
    public static readonly Phrase TypeFollowsGlobal = new("Every {0} now follows the global setting ({1}).", "所有{0}现在跟随全局设置（{1}）。");
    public static readonly Phrase TypeSetting = new("Type setting ({0})", "类型设置（{0}）");
    public static readonly Phrase GlobalSetting = new("Global setting ({0})", "全局设置（{0}）");
    public static readonly Phrase AlwaysOn = new("Always on", "常开");
    public static readonly Phrase WaveOnly = new("Wave only", "仅尸潮");
    public static readonly Phrase Proximity = new("Proximity", "僵尸靠近");
    public static readonly Phrase GameDefault = new("Default", "默认");

    /// <summary>The words page.js draws the panel, the HUD button's tip and the Layout bar with, by the key it looks them up by.</summary>
    public static readonly IReadOnlyList<(string Key, Phrase Phrase)> Page = new (string, Phrase)[]
    {
        ("broken", new("Broken", "损毁")),
        ("damaged", new("Damaged", "受损")),
        ("intact", new("Intact", "完好")),
        ("repairQueued", new("Repair queued", "已排队修理")),
        ("removeQueued", new("Removal queued", "已排队拆除")),
        ("repairAll", new("Repair all ({0})", "全部修理（{0}）")),
        ("clearAll", new("Clear all broken ({0})", "清除全部损毁（{0}）")),
        ("materials", new("Materials free / needed:", "材料 可用 / 所需：")),
        ("empty", new("No traps or barricades outside your home yet.", "家外面还没有陷阱或路障。")),
        ("repairCost", new("Repair:", "修理：")),
        ("short", new("({0} short)", "（还差 {0}）")),
        ("powerOn", new("Power on", "通电")),
        ("powerOff", new("Power off", "断电")),
        ("mode", new("Mode: {0}", "模式：{0}")),
        ("unmanaged", new("Not switched while broken or not working", "损毁或不工作时不切换")),
        ("thisOne", new("This one: {0}", "这个：{0}")),
        ("every", new("Every {0}: {1}", "所有{0}：{1}")),
        ("repair", new("Repair", "修理")),
        ("remove", new("Remove", "拆除")),
        ("escClose", new("Esc to close", "按 Esc 关闭")),
        ("tip", new("Repair or remove your outdoor defenses", "修理或拆除你的室外防御设施")),
        ("defenseList", new("Defense list", "防御列表")),
        ("defenseListKey", new("Defense list ({0})", "防御列表（{0}）")),
    };

    public static IReadOnlyList<Counted> Counts { get; } = new[] { AlreadyRepairing, Repairing, RepairingMore, AlreadyClearing, Removing, RemovingMore };

    public static IReadOnlyList<Phrase> All { get; } = Collect();

    /// <summary>Sentences in a row: English puts a space between them, Chinese runs them together after the full stop.</summary>
    public static string Sentences(bool isChinese, IEnumerable<string> sentences) => string.Join(isChinese ? "" : " ", sentences);

    private static List<Phrase> Collect()
    {
        var all = new List<Phrase>
        {
            NotReady, WaveLocked, AwayFromHome, InLayout, QueueBusy, AllRepaired, NoneBroken, Gone, NotQueued, StartFailed,
            RepairJob, RepairJobs, RemovalJob, RemovalJobs, QueuedRepair, QueuedRemoval, NothingRepaired, NothingCleared,
            BeingRemoved, BeingRepaired, RefusedByQueue, ShortOfMaterials, ShortfallNamed, AndMore, CantRepairCount, CantRepairCountWhy,
            AlreadyQueuedRepair, QueuedForRemoval, NeedsNoRepair, CantRepairWhy, CantRepair, CantRepairShort, AlreadyQueuedRemoval,
            QueuedForRepair, NotAllowed, Needs, RepairQueuedFirst, NeedItem, OutOfStamina, UnnamedItem,
            NoWorld, ReadFailed, NoRepair, CostUnreadable, CheckFailed, Failed,
            NoSave, Unreadable, NoGame, NoSwitch, NotSaved, DefenseSet, DefenseFollowsType, TypeSet, TypeFollowsGlobal,
            TypeSetting, GlobalSetting, AlwaysOn, WaveOnly, Proximity, GameDefault,
        };

        foreach (var counted in Counts)
        {
            all.Add(counted.One);
            all.Add(counted.Many);
        }

        foreach (var (_, phrase) in Page) all.Add(phrase);

        return all;
    }
}

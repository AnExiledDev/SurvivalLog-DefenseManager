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
    public static readonly Phrase WaveLocked = new("A zombie wave is on: defenses can't be repaired or removed until it ends. Work during waves in Settings lifts this.", "尸潮正在进行：结束前无法修理或拆除防御设施。可在设置中开启「尸潮期间操作」。");
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
    public static readonly Phrase SquareTaken = new("That square is not free any more.", "这个格子已经不是空格了。");
    public static readonly Phrase NoPackage = new("No package of that defense in the yard or in Package Bank.", "院子里和 Package Bank 中都没有这个防御设施的包裹。");
    public static readonly Phrase InstallBusy = new("Already installing something: let it finish first.", "正在安装别的东西：请等它完成。");
    public static readonly Phrase NoFreeSpot = new("{0} is in Package Bank, but every yard package spot is full.", "{0}在 Package Bank 中，但院子里的包裹位都满了。");
    public static readonly Phrase BankFailed = new("Package Bank would not give {0} back, see " + Log + ".", "Package Bank 没能取出{0}，详见 " + Log + "。");
    public static readonly Phrase NotInstalled = new("The game would not install {0} there.", "游戏无法在这里安装{0}。");
    public static readonly Phrase Installing = new("Installing {0}.", "正在安装{0}。");
    public static readonly Phrase TakenOutOfBank = new("Took it out of Package Bank, it is in the yard now.", "已从 Package Bank 取出，现在在院子里。");
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

    public static readonly Phrase OutOfStamina = new("Not enough stamina to start a repair (needs {0})", "精力不足，无法开始修理（需要 {0}）");

    /// <summary>A material the game did not name; {0} is its id.</summary>
    public static readonly Phrase UnnamedItem = new("Item {0}", "物品 {0}");

    // Why the game could not be read, or why one defense cannot be repaired.
    public static readonly Phrase NoWorld = new("the game world is not loaded", "游戏世界尚未载入");
    public static readonly Phrase ReadFailed = new("the game could not be read, see " + Log, "无法读取游戏，详见 " + Log);
    public static readonly Phrase NoRepair = new("it has no repair", "它无法修理");
    public static readonly Phrase CostUnreadable = new("its repair cost could not be read", "无法读取它的修理费用");
    public static readonly Phrase CheckFailed = new("the game's repair check failed, see " + Log, "游戏的修理检查出错，详见 " + Log);

    /// <summary>Who a toast is from, on its first line.</summary>
    public static readonly Phrase Source = new("Defense Manager", "防御管理");

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
    public static readonly Phrase AlwaysOn = new("Always on", "常开");
    public static readonly Phrase WaveOnly = new("Wave only", "仅尸潮");
    public static readonly Phrase Proximity = new("Proximity", "僵尸靠近");
    public static readonly Phrase GameDefault = new("Default", "默认");

    // The Settings tab.
    public static readonly Phrase Saved = new("Setting saved.", "设置已保存。");
    public static readonly Phrase SavedAtRestart = new("Setting saved. It takes effect at the next game start.", "设置已保存，下次启动游戏时生效。");

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
        ("empty", new("No traps or barricades outside your home yet.", "家外面还没有陷阱或路障。")),
        ("unmanaged", new("Not switched while broken or not working", "损毁或不工作时不切换")),
        ("repair", new("Repair", "修理")),
        ("remove", new("Remove", "拆除")),
        ("escClose", new("Esc to close", "按 Esc 关闭")),
        ("tip", new("Repair or remove your outdoor defenses", "修理或拆除你的室外防御设施")),
        ("defenseList", new("Defense list", "防御列表")),
        ("defenseListKey", new("Defense list ({0})", "防御列表（{0}）")),
        ("title", new("DEFENSES", "防御设施")),
        ("placed", new("{0} placed, {1} of {2} squares yours", "已放置 {0} 个，{2} 格中 {1} 格属于你")),
        ("placedOnly", new("{0} placed", "已放置 {0} 个")),
        ("tabYard", new("Yard", "空地")),
        ("tabPower", new("Power", "供电")),
        ("tabSettings", new("Settings", "设置")),
        ("house", new("House", "房屋")),
        ("hint", new("Click a defense, a row number or a square", "点击防御设施、行号或格子")),
        ("window", new("Window", "窗户")),
        ("door", new("Door", "门")),
        ("road", new("Road side", "道路一侧")),
        ("offGridLabel", new("Not on the grid", "不在格子上")),
        ("legEmpty", new("Yours, empty", "你的空格")),
        ("legBuy", new("Not yours yet (Survival Points)", "尚未解锁（生存点）")),
        ("needsRepair", new("Needs repair", "需要修理")),
        ("shortOf", new("Not enough for every repair: {0}", "材料不够全部修理：{0}")),
        ("queuedCount", new("{0} queued", "{0} 个已排队")),
        ("allGood", new("All good", "一切正常")),
        ("nothingNeeds", new("Nothing needs repair.", "没有需要修理的防御设施。")),
        ("back", new("< Back to repairs", "< 返回修理列表")),
        ("where", new("Row {0}, square {1}", "第 {0} 行，第 {1} 格")),
        ("offGrid", new("Not on the yard grid", "不在空地格子上")),
        ("condition", new("Condition", "耐久")),
        ("nothingToRepair", new("Nothing to repair", "无需修理")),
        ("onNow", new("On now", "当前通电")),
        ("offNow", new("Off now", "当前断电")),
        ("sameAsType", new("Same as every {0} ({1})", "与所有{0}相同（{1}）")),
        ("row", new("Row {0}", "第 {0} 行")),
        ("againstHouse", new("Against the house", "紧贴房屋")),
        ("rowOut1", new("1 row out from the house", "离房屋 1 行")),
        ("rowsOut", new("{0} rows out from the house", "离房屋 {0} 行")),
        ("rowInfo", new("Defenses: {0}. Squares yours: {1} of {2}.", "防御设施：{0} 个。属于你的格子：{2} 格中 {1} 格。")),
        ("rowPrice", new("The other squares cost {0} Survival Points each.", "其余格子每格需要 {0} 生存点。")),
        ("repairRow", new("Repair row {0} ({1})", "修理第 {0} 行（{1}）")),
        ("emptySquare", new("Empty square", "空格")),
        ("notYours", new("Not yours yet", "尚未解锁")),
        ("buildHere", new("Build a defense here from the Layout screen.", "可在「规划模式」中在这里建造防御设施。")),
        ("installHere", new("Install a packed defense here:", "在这里安装已打包的防御设施：")),
        ("install", new("Install", "安装")),
        ("inYard", new("{0} in the yard", "院子里 {0} 个")),
        ("inBank", new("{0} in Package Bank", "Package Bank 中 {0} 个")),
        ("noPackages", new("No defense packages in the yard or in Package Bank.", "院子里和 Package Bank 中都没有防御设施包裹。")),
        ("unlockFor", new("Unlock it from the Layout screen for {0} Survival Points.", "可在「规划模式」中花费 {0} 生存点解锁。")),
        ("everyType", new("Every {0}", "所有{0}")),
        ("global", new("Global ({0})", "全局（{0}）")),
        ("ownList", new("Set on their own", "单独设置")),
        ("followType", new("Follow the type", "跟随类型")),
        ("noneOwn", new("None. Pick one in the Yard tab to set it alone.", "无。在「空地」标签中选中一个即可单独设置。")),
        ("noPower", new("No powered defenses yet.", "还没有用电的防御设施。")),
        ("powerNote", new("Wave only: on during waves and while zombies are at the base. Proximity: on while a zombie is in range. Always on: never turns off. Default: the game's own power switch decides.", "仅尸潮：尸潮期间以及基地有僵尸时开启。僵尸靠近：有僵尸在范围内时开启。常开：始终开启。默认：由游戏自身的电源开关决定。")),
        ("modeWaveOnly", WaveOnly),
        ("modeProximity", Proximity),
        ("modeAlwaysOn", AlwaysOn),
        ("modeDefault", GameDefault),
        ("setControls", new("Controls", "控制")),
        ("setCombat", new("Combat", "战斗")),
        ("setKey", new("Open the panel", "打开面板")),
        ("setKeyDesc", new("Key that opens and closes this window.", "打开和关闭此窗口的按键。")),
        ("keyNone", new("None", "无")),
        ("setLayout", new("Layout screen buttons", "「规划模式」按钮")),
        ("setLayoutDesc", new("Show Repair all, Clear all broken and Defense list on the Layout screen.", "在「规划模式」界面显示「全部修理」「清除全部损毁」「防御列表」按钮。")),
        ("setMode", new("Default power mode", "默认供电模式")),
        ("setModeDesc", new("Mode for powered defenses with no setting of their own.", "没有单独设置的用电防御设施所用的模式。")),
        ("setRange", new("Proximity range", "靠近范围")),
        ("setRangeDesc", new("How close a zombie must come to turn a Proximity defense on, 1 to 100 meters.", "僵尸进入多少米内时开启设为「僵尸靠近」的防御设施，1 到 100 米。")),
        ("meters", new("{0} m", "{0} 米")),
        ("setCrawler", new("Crawler dodge chance", "「爬行者丧尸」闪避概率")),
        ("setCrawlerDesc", new("Chance a Crawler Zombie dodges a Mechanical Chainsaw hit. The base game has 100%.", "「爬行者丧尸」闪避「机械电锯」攻击的概率。原版游戏为 100%。")),
        ("setArmored", new("Armored crawler dodge chance", "「重甲爬行者丧尸」闪避概率")),
        ("setArmoredDesc", new("Chance an Armored Crawler Zombie dodges a Mechanical Chainsaw hit. The base game has 100%.", "「重甲爬行者丧尸」闪避「机械电锯」攻击的概率。原版游戏为 100%。")),
        ("setRate", new("Device attack rate", "防御设施攻击频率")),
        ("setRateDesc", new("How often defense devices strike, 0.25 to 10. Takes effect at the next game start.", "防御设施攻击的频率，0.25 到 10。下次启动游戏时生效。")),
        ("setWaves", new("Work during waves", "尸潮期间操作")),
        ("setWavesDesc", new("Repair, remove, install and move defenses while a zombie wave is on. The base game locks them until it ends.", "尸潮进行时也可修理、拆除、安装和移动防御设施。原版游戏在尸潮结束前禁止这些操作。")),
        ("setUnlocks", new("Unlocks", "提前解锁")),
        ("setDoors", new("Door upgrades from day one", "第一天起解锁门的升级")),
        ("setDoorsDesc", new("Advanced Repair and Advanced Reinforce on doors, with their recipes, from the first day of a story run. The base game gives them on days 2 and 5.", "剧情模式第一天起即可对门使用「高级修理」「高级加固」，并获得对应配方。原版游戏在第 2 天和第 5 天提供。")),
        ("setWindows", new("Window upgrades from day one", "第一天起解锁窗户的升级")),
        ("setWindowsDesc", new("Advanced Repair, Advanced Reinforce and Reinforce Window on windows, with their recipes, from the first day of a story run. The base game gives them on days 13 and 14.", "剧情模式第一天起即可对窗户使用「高级修理」「高级加固」「加固窗户」，并获得对应配方。原版游戏在第 13 天和第 14 天提供。")),
        ("setForts", new("Reinforce Fortifications from day one", "第一天起解锁「强化加固」")),
        ("setFortsDesc", new("Reinforce Fortifications on doors and windows, with its recipe, from the first day of a story run. The base game gives it on day 54.", "剧情模式第一天起即可对门窗使用「强化加固」，并获得对应配方。原版游戏在第 54 天提供。")),
        ("keyClose", new("{0} or Esc to close", "按 {0} 或 Esc 关闭")),
        ("noSettings", new("Settings are not available right now.", "现在无法读取设置。")),
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
            SquareTaken, NoPackage, InstallBusy, NoFreeSpot, BankFailed, NotInstalled, Installing, TakenOutOfBank,
            BeingRemoved, BeingRepaired, RefusedByQueue, ShortOfMaterials, ShortfallNamed, AndMore, CantRepairCount, CantRepairCountWhy,
            AlreadyQueuedRepair, QueuedForRemoval, NeedsNoRepair, CantRepairWhy, CantRepair, CantRepairShort, AlreadyQueuedRemoval,
            QueuedForRepair, NotAllowed, Needs, RepairQueuedFirst, NeedItem, OutOfStamina, UnnamedItem,
            NoWorld, ReadFailed, NoRepair, CostUnreadable, CheckFailed, Failed,
            NoSave, Unreadable, NoGame, NoSwitch, NotSaved, DefenseSet, DefenseFollowsType, TypeSet, TypeFollowsGlobal,
            AlwaysOn, WaveOnly, Proximity, GameDefault, Saved, SavedAtRestart,
        };

        foreach (var counted in Counts)
        {
            all.Add(counted.One);
            all.Add(counted.Many);
        }

        // The page's mode words are the power phrases above, listed once.
        foreach (var (_, phrase) in Page)
        {
            if (!all.Contains(phrase)) all.Add(phrase);
        }

        return all;
    }
}

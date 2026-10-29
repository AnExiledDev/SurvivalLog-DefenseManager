using System.IO;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using DefenseManager.Adapters;
using DefenseManager.Core;
using HarmonyLib;
using UnityEngine;

namespace DefenseManager;

[BepInPlugin(Guid, "Defense Manager", Version)]
[BepInDependency(OptionalLive.ToolkitGuid, OptionalLive.ToolkitVersion)]
[BepInDependency(OptionalBank.Guid, BepInDependency.DependencyFlags.SoftDependency)]
public sealed class Plugin : BasePlugin
{
    private const string Guid = "com.dev.survivallog.defensemanager";
    private const string Version = "0.9.0";
    private const string PowerFileName = Guid + ".power.txt";

    public override void Load()
    {
        OptionalUiSize.Bind(Config);

        var panelKey = Config.Bind("Controls", "PanelKey", KeyCode.F7,
            "Key that opens and closes the Defense Manager panel, F7 by default. None turns the key off; the shield button in the bottom-left corner of the screen still opens the panel.\n" +
            "打开和关闭 Defense Manager 面板的按键，默认 F7。设为 None 则停用该按键，屏幕左下角的盾牌按钮仍可打开面板。");
        var layoutControls = Config.Bind("Controls", "LayoutControls", true,
            "Show Repair all, Clear all broken and Defense list buttons at the top middle of the game's Layout screen. Set to false to hide them; the panel key and the shield button still work.\n" +
            "在游戏「规划模式」界面的顶部中间显示「全部修理」「清除全部损毁」「防御列表」三个按钮。设为 false 则隐藏这些按钮，面板按键和盾牌按钮照常可用。");
        var defaultModeName = Config.Bind("Power", "DefaultMode", PowerModes.ConfigName(PowerMode.WaveOnly), new ConfigDescription(
            "How electrical defenses are powered when neither the defense nor its type has a setting of its own in the Defense Manager panel. WaveOnly: on during waves and while zombies are at the base, off otherwise. AlwaysOn: always on. Proximity: on while a living zombie is within ProximityRange, off once none has been in range for 10 game minutes. Default: the mod leaves the defense alone and the game's own power switch decides.\n" +
            "电力防御设施在该设施及其类型都没有单独设置（在 Defense Manager 面板中设置）时的供电方式。WaveOnly：尸潮期间以及基地有僵尸时开启，其余时间关闭。AlwaysOn：始终开启。Proximity：有活着的僵尸进入 ProximityRange 范围内时开启，范围内连续 10 分钟游戏时间没有僵尸后关闭。Default：本模组不干预，由游戏自身的电源开关决定。",
            new AcceptableValueList<string>(PowerModes.ConfigNames)));
        var proximityRange = Config.Bind("Power", "ProximityRange", 15f, new ConfigDescription(
            "How close, in meters, a living zombie must come to turn on a defense set to Proximity. Measured along the ground; height does not count. 15 by default, from 1 to 100.\n" +
            "设为 Proximity 的防御设施在活着的僵尸进入多少米范围内时开启。按地面水平距离计算，不计高度。默认 15，可设为 1 到 100。",
            new AcceptableValueRange<float>(1f, 100f)));
        var crawlerDodge = Config.Bind("Combat", "CrawlerDodgeChance", 0.25f, new ConfigDescription(
            "Chance that a Crawler Zombie dodges a hit from the Mechanical Chainsaw, from 0 (every hit lands) to 1 (never hit). In the base game it is 1, so the chainsaw blade never hits crawlers. 0.25 by default.\n" +
            "「爬行者丧尸」闪避「机械电锯」攻击的概率，0（每次都命中）到 1（永远打不到）。原版游戏中为 1，电锯刀片完全打不到爬行者。默认 0.25。",
            new AcceptableValueRange<float>(0f, 1f)));
        var armoredCrawlerDodge = Config.Bind("Combat", "ArmoredCrawlerDodgeChance", 0.25f, new ConfigDescription(
            "Chance that an Armored Crawler Zombie dodges a hit from the Mechanical Chainsaw, from 0 (every hit lands) to 1 (never hit). In the base game it is 1. 0.25 by default.\n" +
            "「重甲爬行者丧尸」闪避「机械电锯」攻击的概率，0（每次都命中）到 1（永远打不到）。原版游戏中为 1。默认 0.25。",
            new AcceptableValueRange<float>(0f, 1f)));
        var attackRateEntry = Config.Bind("Combat", "DeviceAttackRate", 1f, new ConfigDescription(
            "How often defense devices strike: 2 strikes twice as often, 0.5 half as often, 1 is the game's own rate. Each strike of a powered device uses power, so a higher rate also uses more power. 1 by default, from 0.25 to 10. Takes effect at the next game start.\n" +
            "防御设施攻击的频率：2 为两倍频率，0.5 为一半，1 为游戏原速。用电的防御设施每次攻击都耗电，所以频率越高耗电越多。默认 1，可设为 0.25 到 10。下次启动游戏时生效。",
            new AcceptableValueRange<float>(DeviceAttackRate.Slowest, DeviceAttackRate.Fastest), "ModManager.Restart"));
        var allowDuringWaves = Config.Bind("Combat", "AllowDuringWaves", false,
            "Let defenses be repaired, removed, installed and moved while a zombie wave is on. The base game locks all of these until the wave ends. Off by default. Robot repairs and expeditions still wait for the wave to end.\n" +
            "允许在尸潮进行时修理、拆除、安装和移动防御设施。原版游戏在尸潮结束前禁止这些操作。默认关闭。机器人修理和外出探索仍需等尸潮结束。");
        var attackRate = DeviceAttackRate.From(attackRateEntry.Value);
        var log = new DefenseLog(Log, Path.Combine(Paths.BepInExRootPath, "defense-manager.log"));
        var harmony = new Harmony(Guid);
        var powerSlot = new PowerSlot(new PowerFileOnDisk(Path.Combine(Paths.ConfigPath, PowerFileName), log.Write));
        var power = new PowerSession(new GamePower(log.Write), powerSlot, log.Write);
        var config = new PanelConfig(panelKey, layoutControls, defaultModeName, proximityRange, crawlerDodge, armoredCrawlerDodge, attackRateEntry, allowDuringWaves);
        var session = new DefenseSession(new GameDefenses(log.Write), power, new WebPage(log.Write), config, log.Write);

        log.Write($"===== Defense Manager {Version} started, device attack rate {attackRate.Value}");
        InputGate.Apply(harmony, log.Write);
        PageMessages.Apply(harmony, log.Write);
        DodgeRulesPatch.Apply(harmony, log.Write);
        AttackRatePatch.Apply(harmony, attackRate, log.Write);
        WaveLockPatch.Apply(harmony, log.Write);

        config.ApplyControls = () =>
        {
            session.UseControls(config.PanelKey, config.LayoutControls);
            log.Write($"key {config.PanelKey}, Layout controls {(config.LayoutControls ? "on" : "off")}");
        };
        config.ApplyPower = () =>
        {
            power.Use(config.DefaultMode, config.ProximityRange);
            log.Write($"power default {PowerModes.ConfigName(config.DefaultMode)}, proximity range {config.ProximityRange} m");
        };
        config.ApplyDodge = () => DodgeRulesPatch.Use(config.Dodge);
        config.ApplyWaveWork = () => WaveLockPatch.Use(config.AllowDuringWaves);

        OptionalLive.Watch(config.ApplyControls, panelKey, layoutControls);
        OptionalLive.Watch(config.ApplyPower, defaultModeName, proximityRange);
        OptionalLive.Watch(config.ApplyDodge, crawlerDodge, armoredCrawlerDodge);
        OptionalLive.Watch(config.ApplyWaveWork, allowDuringWaves);

        DefenseTicker.Session = session;
        DefenseTicker.Log = log.Write;
        AddComponent<DefenseTicker>();
    }
}

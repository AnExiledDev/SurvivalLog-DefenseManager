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
public sealed class Plugin : BasePlugin
{
    private const string Guid = "com.dev.survivallog.defensemanager";
    private const string Version = "0.3.3";
    private const string PowerFileName = Guid + ".power.txt";

    public override void Load()
    {
        var panelKey = Config.Bind("Controls", "PanelKey", KeyCode.F7,
            "Key that opens and closes the Defense Manager panel, F7 by default. None turns the key off; the shield button in the bottom-left corner of the screen still opens the panel.\n" +
            "打开和关闭 Defense Manager 面板的按键，默认 F7。设为 None 则停用该按键，屏幕左下角的盾牌按钮仍可打开面板。").Value;
        var isLayoutBarEnabled = Config.Bind("Controls", "LayoutControls", true,
            "Show Repair all, Clear all broken and Defense list buttons at the top middle of the game's Layout screen. Set to false to hide them; the panel key and the shield button still work.\n" +
            "在游戏「规划模式」界面的顶部中间显示「全部修理」「清除全部损毁」「防御列表」三个按钮。设为 false 则隐藏这些按钮，面板按键和盾牌按钮照常可用。").Value;
        var defaultModeName = Config.Bind("Power", "DefaultMode", PowerModes.ConfigName(PowerMode.WaveOnly), new ConfigDescription(
            "How electrical defenses are powered when neither the defense nor its type has a setting of its own in the Defense Manager panel. WaveOnly: on during waves and while zombies are at the base, off otherwise. AlwaysOn: always on. Proximity: on while a living zombie is within ProximityRange, off once none has been in range for 10 game minutes. Default: the mod leaves the defense alone and the game's own power switch decides.\n" +
            "电力防御设施在该设施及其类型都没有单独设置（在 Defense Manager 面板中设置）时的供电方式。WaveOnly：尸潮期间以及基地有僵尸时开启，其余时间关闭。AlwaysOn：始终开启。Proximity：有活着的僵尸进入 ProximityRange 范围内时开启，范围内连续 10 分钟游戏时间没有僵尸后关闭。Default：本模组不干预，由游戏自身的电源开关决定。",
            new AcceptableValueList<string>(PowerModes.ConfigNames))).Value;
        var proximityRange = Config.Bind("Power", "ProximityRange", 15f, new ConfigDescription(
            "How close, in meters, a living zombie must come to turn on a defense set to Proximity. Measured along the ground; height does not count. 15 by default, from 1 to 100.\n" +
            "设为 Proximity 的防御设施在活着的僵尸进入多少米范围内时开启。按地面水平距离计算，不计高度。默认 15，可设为 1 到 100。",
            new AcceptableValueRange<float>(1f, 100f))).Value;
        var defaultMode = PowerModes.Parse(defaultModeName) ?? PowerMode.WaveOnly;
        var log = new DefenseLog(Log, Path.Combine(Paths.BepInExRootPath, "defense-manager.log"));
        var harmony = new Harmony(Guid);
        var powerSlot = new PowerSlot(new PowerFileOnDisk(Path.Combine(Paths.ConfigPath, PowerFileName), log.Write));
        var power = new PowerSession(new GamePower(log.Write), powerSlot, defaultMode, proximityRange, log.Write);

        log.Write($"===== Defense Manager {Version} started, key {panelKey}, Layout controls {(isLayoutBarEnabled ? "on" : "off")}, power default {PowerModes.ConfigName(defaultMode)}, proximity range {proximityRange} m");
        InputGate.Apply(harmony, log.Write);
        PageMessages.Apply(harmony, log.Write);
        DefenseTicker.Session = new DefenseSession(new GameDefenses(log.Write), power, new WebPage(log.Write), panelKey, isLayoutBarEnabled, log.Write);
        DefenseTicker.Log = log.Write;
        AddComponent<DefenseTicker>();
    }
}

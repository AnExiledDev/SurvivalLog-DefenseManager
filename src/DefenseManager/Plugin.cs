using System.IO;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using DefenseManager.Adapters;
using HarmonyLib;
using UnityEngine;

namespace DefenseManager;

[BepInPlugin(Guid, "Defense Manager", Version)]
public sealed class Plugin : BasePlugin
{
    private const string Guid = "com.dev.survivallog.defensemanager";
    private const string Version = "0.2.2";

    public override void Load()
    {
        var panelKey = Config.Bind("Controls", "PanelKey", KeyCode.F7,
            "Key that opens and closes the Defense Manager panel, F7 by default. None turns the key off; the shield button in the bottom-left corner of the screen still opens the panel.\n" +
            "打开和关闭 Defense Manager 面板的按键，默认 F7。设为 None 则停用该按键，屏幕左下角的盾牌按钮仍可打开面板。").Value;
        var isLayoutBarEnabled = Config.Bind("Controls", "LayoutControls", true,
            "Show Repair all, Clear all broken and Defense list buttons at the top middle of the game's Layout screen. Set to false to hide them; the panel key and the shield button still work.\n" +
            "在游戏「规划模式」界面的顶部中间显示「Repair all」「Clear all broken」「Defense list」三个按钮。设为 false 则隐藏这些按钮，面板按键和盾牌按钮照常可用。").Value;
        var log = new DefenseLog(Log, Path.Combine(Paths.BepInExRootPath, "defense-manager.log"));
        var harmony = new Harmony(Guid);

        log.Write($"===== Defense Manager {Version} started, key {panelKey}, Layout controls {(isLayoutBarEnabled ? "on" : "off")}");
        InputGate.Apply(harmony, log.Write);
        PageMessages.Apply(harmony, log.Write);
        DefenseTicker.Session = new DefenseSession(new GameDefenses(log.Write), new WebPage(log.Write), panelKey, isLayoutBarEnabled, log.Write);
        DefenseTicker.Log = log.Write;
        AddComponent<DefenseTicker>();
    }
}

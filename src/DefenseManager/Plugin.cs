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
    private const string Version = "0.1.0";

    public override void Load()
    {
        var panelKey = Config.Bind("Controls", "PanelKey", KeyCode.F7, "Key that opens and closes the Defense Manager panel. None turns the key off; the HUD button still works.").Value;
        var log = new DefenseLog(Log, Path.Combine(Paths.BepInExRootPath, "defense-manager.log"));
        var harmony = new Harmony(Guid);

        log.Write($"===== Defense Manager {Version} started, key {panelKey}");
        InputGate.Apply(harmony, log.Write);
        PageMessages.Apply(harmony, log.Write);
        DefenseTicker.Session = new DefenseSession(new GameDefenses(log.Write), new WebPage(log.Write), panelKey, log.Write);
        DefenseTicker.Log = log.Write;
        AddComponent<DefenseTicker>();
    }
}

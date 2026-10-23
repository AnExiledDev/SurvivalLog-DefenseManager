using System;
using BepInEx.Configuration;
using DefenseManager.Core;
using UnityEngine;

namespace DefenseManager.Adapters;

/// <summary>
/// The config entries the panel's Settings tab shows and changes. A change is saved to the config file and applied
/// at once through the same calls the start-up uses, so it works with or without Exiled's Toolkit watching the file.
/// </summary>
internal sealed class PanelConfig
{
    private readonly ConfigEntry<KeyCode> _panelKey;
    private readonly ConfigEntry<bool> _layoutControls;
    private readonly ConfigEntry<string> _defaultMode;
    private readonly ConfigEntry<float> _proximityRange;
    private readonly ConfigEntry<float> _crawlerDodge;
    private readonly ConfigEntry<float> _armoredCrawlerDodge;
    private readonly ConfigEntry<float> _attackRate;

    public PanelConfig(
        ConfigEntry<KeyCode> panelKey,
        ConfigEntry<bool> layoutControls,
        ConfigEntry<string> defaultMode,
        ConfigEntry<float> proximityRange,
        ConfigEntry<float> crawlerDodge,
        ConfigEntry<float> armoredCrawlerDodge,
        ConfigEntry<float> attackRate)
    {
        _panelKey = panelKey;
        _layoutControls = layoutControls;
        _defaultMode = defaultMode;
        _proximityRange = proximityRange;
        _crawlerDodge = crawlerDodge;
        _armoredCrawlerDodge = armoredCrawlerDodge;
        _attackRate = attackRate;
    }

    /// <summary>Set by the plugin: puts the controls, power and dodge values in use, as at start-up.</summary>
    public Action ApplyControls { get; set; } = () => { };

    public Action ApplyPower { get; set; } = () => { };

    public Action ApplyDodge { get; set; } = () => { };

    public KeyCode PanelKey => _panelKey.Value;

    public bool LayoutControls => _layoutControls.Value;

    public PowerMode DefaultMode => PowerModes.Parse(_defaultMode.Value) ?? PowerMode.WaveOnly;

    public float ProximityRange => _proximityRange.Value;

    public CrawlerDodge Dodge => new(_crawlerDodge.Value, _armoredCrawlerDodge.Value);

    public PanelSettings Read(string keyName) => new(
        keyName,
        _layoutControls.Value,
        DefaultMode,
        _proximityRange.Value,
        _crawlerDodge.Value,
        _armoredCrawlerDodge.Value,
        _attackRate.Value);

    /// <summary>Saves the change, puts it in use and returns the status line for it.</summary>
    public string Apply(SettingChange change, bool isChinese)
    {
        switch (change)
        {
            case SettingChange.LayoutControls layout:
                _layoutControls.Value = layout.IsShown;
                ApplyControls();
                break;
            case SettingChange.DefaultMode mode:
                _defaultMode.Value = PowerModes.ConfigName(mode.Mode);
                ApplyPower();
                break;
            case SettingChange.ProximityRange range:
                _proximityRange.Value = range.Meters;
                ApplyPower();
                break;
            case SettingChange.CrawlerDodge dodge:
                _crawlerDodge.Value = dodge.Chance;
                ApplyDodge();
                break;
            case SettingChange.ArmoredCrawlerDodge dodge:
                _armoredCrawlerDodge.Value = dodge.Chance;
                ApplyDodge();
                break;
            case SettingChange.AttackRate rate:
                _attackRate.Value = rate.Rate;

                return Texts.SavedAtRestart.Pick(isChinese);
            default:
                throw new InvalidOperationException("unknown setting change " + change);
        }

        return Texts.Saved.Pick(isChinese);
    }
}

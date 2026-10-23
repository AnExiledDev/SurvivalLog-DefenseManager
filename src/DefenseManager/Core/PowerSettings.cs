using System.Collections.Generic;

namespace DefenseManager.Core;

/// <summary>
/// One save's power settings: the defenses with a setting of their own, and the furniture types with one. A defense
/// or type that is not listed has none and follows the level above it.
/// </summary>
public sealed record PowerSettings(IReadOnlyDictionary<DefenseKey, PowerMode> Defenses, IReadOnlyDictionary<int, PowerMode> Types)
{
    public static readonly PowerSettings None = new(new Dictionary<DefenseKey, PowerMode>(), new Dictionary<int, PowerMode>());

    public PowerMode? DefenseSetting(DefenseKey key) => Defenses.TryGetValue(key, out var mode) ? mode : null;

    public PowerMode? TypeSetting(int configId) => Types.TryGetValue(configId, out var mode) ? mode : null;

    /// <summary>The settings with this defense's own setting changed; null takes it away.</summary>
    public PowerSettings WithDefense(DefenseKey key, PowerMode? mode) => this with { Defenses = Changed(Defenses, key, mode) };

    /// <summary>The settings with this type's setting changed; null takes it away.</summary>
    public PowerSettings WithType(int configId, PowerMode? mode) => this with { Types = Changed(Types, configId, mode) };

    private static Dictionary<TKey, PowerMode> Changed<TKey>(IReadOnlyDictionary<TKey, PowerMode> settings, TKey key, PowerMode? mode)
        where TKey : notnull
    {
        var changed = new Dictionary<TKey, PowerMode>();

        foreach (var (settingKey, setting) in settings) changed[settingKey] = setting;

        if (mode is { } set) changed[key] = set;
        else changed.Remove(key);

        return changed;
    }
}

/// <summary>
/// Which mode a defense runs in. Its own setting wins; without one it follows its furniture type's setting; a type
/// without one follows the global default from the config.
/// </summary>
public sealed record PowerPolicy(PowerSettings Settings, PowerMode Global)
{
    public PowerMode ModeOf(DefenseKey key) => Settings.DefenseSetting(key) ?? TypeMode(key.ConfigId);

    public PowerMode TypeMode(int configId) => Settings.TypeSetting(configId) ?? Global;
}

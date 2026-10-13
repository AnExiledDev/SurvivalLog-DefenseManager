using System;

namespace DefenseManager.Core;

/// <summary>The power settings file on disk.</summary>
public interface IPowerFile
{
    /// <summary>The file's text, "" when there is no file yet, or null when it is there but cannot be read.</summary>
    string? Read();

    /// <summary>Puts this text in place of the file's; false when that failed.</summary>
    bool Replace(string text);
}

public enum SettingsLoad
{
    NoSave,
    AlreadyRead,
    Read,
    Unreadable,
}

public enum SettingsEdit
{
    Saved,

    /// <summary>The change holds for this session but could not be written to the file.</summary>
    NotSaved,

    NoSave,

    /// <summary>The file could not be read, so nothing was changed and nothing written.</summary>
    Unreadable,
}

/// <summary>
/// The power settings of the save the game has loaded, read once per save. The file holds every save's settings, so
/// one that cannot be read is never written over: a write then would wipe every other save's settings, and this
/// save's too but for the one just changed. It is tried again on the next load, and is read again before every write.
/// </summary>
public sealed class PowerSlot
{
    private readonly IPowerFile _file;
    private string? _save;
    private string? _lastSave;
    private bool _isRead;

    public PowerSlot(IPowerFile file) => _file = file;

    public PowerSettings Settings { get; private set; } = PowerSettings.None;

    public string? Save => _save;

    /// <summary>
    /// Follows the save the game has loaded; true when it moved from one save to another. The save name can read as
    /// missing for a moment, so a gap between two reads of the same save is not a move.
    /// </summary>
    public bool Follow(string? save)
    {
        if (save == _save) return false;

        var isOtherSave = save != null && _lastSave != null && save != _lastSave;
        _save = save;
        _isRead = false;
        Settings = PowerSettings.None;

        if (save != null) _lastSave = save;

        return isOtherSave;
    }

    /// <summary>Reads the save's settings unless they are already in.</summary>
    public SettingsLoad Load()
    {
        if (_save is not { } save) return SettingsLoad.NoSave;

        if (_isRead) return SettingsLoad.AlreadyRead;

        if (_file.Read() is not { } text) return SettingsLoad.Unreadable;

        Settings = PowerFile.Read(text, save);
        _isRead = true;

        return SettingsLoad.Read;
    }

    public SettingsEdit SetDefense(DefenseKey key, PowerMode? mode) => Edit(settings => settings.WithDefense(key, mode));

    public SettingsEdit SetType(int configId, PowerMode? mode) => Edit(settings => settings.WithType(configId, mode));

    private SettingsEdit Edit(Func<PowerSettings, PowerSettings> change)
    {
        if (_save is not { } save) return SettingsEdit.NoSave;

        if (!_isRead || _file.Read() is not { } text) return SettingsEdit.Unreadable;

        Settings = change(Settings);

        return _file.Replace(PowerFile.Write(text, save, Settings)) ? SettingsEdit.Saved : SettingsEdit.NotSaved;
    }
}

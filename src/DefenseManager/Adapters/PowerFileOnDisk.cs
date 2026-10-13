using System;
using System.IO;
using DefenseManager.Core;

namespace DefenseManager.Adapters;

/// <summary>
/// The power settings file beside the mod's config. The game's save has no room for a mod's data, so the settings
/// live here, keyed by save. A write goes to a temporary file first and is moved into place, so a crash mid-write
/// cannot leave half a file. A read failure is logged when it first happens or its message changes, not on every
/// retry.
/// </summary>
internal sealed class PowerFileOnDisk : IPowerFile
{
    private readonly string _path;
    private readonly Action<string> _log;
    private string? _loggedReadFailure;

    public PowerFileOnDisk(string path, Action<string> log)
    {
        _path = path;
        _log = log;
    }

    public string? Read()
    {
        try
        {
            var text = File.Exists(_path) ? File.ReadAllText(_path) : "";
            _loggedReadFailure = null;

            return text;
        }
        catch (Exception ex)
        {
            if (ex.Message != _loggedReadFailure) _log($"could not read the power settings from {_path}: {ex.Message}");

            _loggedReadFailure = ex.Message;

            return null;
        }
    }

    public bool Replace(string text)
    {
        var temp = _path + ".tmp";

        try
        {
            File.WriteAllText(temp, text);
            File.Move(temp, _path, overwrite: true);

            return true;
        }
        catch (Exception ex)
        {
            _log($"could not save the power settings to {_path}: {ex.Message}");

            return false;
        }
    }
}

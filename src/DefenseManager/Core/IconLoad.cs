using System;
using System.Threading.Tasks;

namespace DefenseManager.Core;

/// <summary>
/// The game's web icons, found by a folder walk of well over a thousand files. The walk runs once, off the main thread,
/// so it never holds up a frame; until it is done the panel draws letters, and the next refresh after it shows the
/// pictures. The walk's own line for the log is written from the main thread, when its result is first taken.
/// </summary>
public sealed class IconLoad
{
    private readonly Action<string> _log;
    private Task<(IconIndex Icons, string Note)>? _scan;
    private IconIndex? _icons;

    public IconLoad(Action<string> log)
    {
        _log = log;
    }

    /// <summary>Starts the walk the first time only; it is never started again, whatever it found.</summary>
    public void Start(Func<Task<(IconIndex Icons, string Note)>> scan)
    {
        if (_scan == null && _icons == null) _scan = scan();
    }

    /// <summary>The icons once the walk is done, and no icons before that or when the walk failed.</summary>
    public IconIndex Current
    {
        get
        {
            if (_icons != null) return _icons;
            if (_scan == null || !_scan.IsCompleted) return IconIndex.Empty;

            if (_scan.Status == TaskStatus.RanToCompletion)
            {
                _icons = _scan.Result.Icons;
                _log(_scan.Result.Note);
            }
            else
            {
                _icons = IconIndex.Empty;
                _log("no web icons, defenses and materials will show letters instead: " + (_scan.Exception?.GetBaseException().Message ?? "the search was cancelled"));
            }

            _scan = null;

            return _icons;
        }
    }
}

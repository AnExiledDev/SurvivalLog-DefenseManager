namespace DefenseManager.Core;

/// <summary>
/// When the Unlocks pass runs. A setting change asks for one pass. A start of play asks for passes until the new run
/// has loaded: right after the start the last run can still look loaded, so a pass counts only once loading has been
/// seen, or after StartSeconds. While a pass is wanted it runs once a second. Without the start hook it runs every 5
/// seconds, as it always did.
/// </summary>
public sealed class UnlockSchedule
{
    public const double RetrySeconds = 1;
    public const double FallbackSeconds = 5;
    public const double StartSeconds = 30;

    private readonly bool _isHooked;
    private bool _isAsked = true;
    private bool _hasSeenLoading = true;
    private double _startedAt = double.NegativeInfinity;
    private double _nextAt = double.NegativeInfinity;

    public UnlockSchedule(bool isHooked) => _isHooked = isHooked;

    /// <summary>A setting changed: one pass, at once.</summary>
    public void Ask()
    {
        _isAsked = true;
        _hasSeenLoading = true;
        _nextAt = double.NegativeInfinity;
    }

    /// <summary>Play started: passes until the new run is loaded.</summary>
    public void Started(double now)
    {
        _isAsked = true;
        _hasSeenLoading = false;
        _startedAt = now;
        _nextAt = double.NegativeInfinity;
    }

    public bool IsDue(double now) => now >= _nextAt && (_isAsked || !_isHooked);

    /// <summary>A pass ran; isLoading says the run was there but not ready.</summary>
    public void Ran(double now, bool isLoading)
    {
        if (!_isHooked)
        {
            _nextAt = now + FallbackSeconds;

            return;
        }

        if (isLoading) _hasSeenLoading = true;
        else if (_hasSeenLoading || now - _startedAt >= StartSeconds) _isAsked = false;

        _nextAt = now + RetrySeconds;
    }
}

using System;

namespace DefenseManager.Core;

/// <summary>
/// How often defense devices strike: 2 strikes twice as often, 0.5 half as often, 1 is the game's own rate. Only
/// <see cref="From"/> makes one, so the rate is always in range and dividing by it needs no guard.
/// </summary>
public sealed class DeviceAttackRate
{
    public const float Slowest = 0.25f;
    public const float Fastest = 10f;

    private DeviceAttackRate(float value) => Value = value;

    public float Value { get; }

    public bool IsGameRate => Value == 1f;

    /// <summary>Non-numbers and anything at or below 0 fall back to the game's rate; the rest is pulled into range.</summary>
    public static DeviceAttackRate From(float requested) =>
        new(!float.IsFinite(requested) || requested <= 0f ? 1f : Math.Clamp(requested, Slowest, Fastest));

    /// <summary>The time between strikes; a device the game gives no interval (0 or less) is left alone.</summary>
    public float Interval(float gameSeconds) =>
        IsGameRate || !float.IsFinite(gameSeconds) || gameSeconds <= 0f ? gameSeconds : gameSeconds / Value;
}

using System.Globalization;

namespace VectorToCursor.Domain;

/// <summary>Finds the length after which a set of animations repeats exactly: one loop of the animated cursor.</summary>
internal static class AnimationLoop
{
    public static readonly TimeSpan MaximumLength = TimeSpan.FromMinutes(10);

    /// <returns>
    /// The least common multiple of the repeating animations' iteration lengths, extended to cover every finite animation;
    /// <see langword="null"/> when nothing ever moves.
    /// </returns>
    /// <exception cref="CursorConversionException">The loop would be longer than <see cref="MaximumLength"/>.</exception>
    public static TimeSpan? Calculate(IReadOnlyList<AnimationTiming> timings)
    {
        ArgumentNullException.ThrowIfNull(timings);
        if (timings.Any(timing => timing.SimpleDuration <= TimeSpan.Zero))
            throw new ArgumentException("Every animation needs a positive iteration length.", nameof(timings));

        long? period = RepeatingPeriod(timings);
        long lastEnd = timings.Where(timing => timing.ActiveDuration is not null).Select(EndTicks).DefaultIfEmpty(0).Max();
        long loop = period is long repeating ? RoundUpToMultiple(Math.Max(lastEnd, repeating), repeating) : lastEnd;
        if (loop <= 0)
            return null;

        ThrowIfTooLong(loop);
        return TimeSpan.FromTicks(loop);
    }

    private static long? RepeatingPeriod(IReadOnlyList<AnimationTiming> timings)
    {
        long? period = null;
        foreach (AnimationTiming timing in timings.Where(timing => timing.ActiveDuration is null))
        {
            long ticks = timing.SimpleDuration.Ticks;
            period = period is long current ? LeastCommonMultiple(current, ticks) : ticks;
            // Stopping as soon as the limit is exceeded also keeps the next multiplication far from overflowing.
            ThrowIfTooLong(period.Value);
        }
        return period;
    }

    private static long EndTicks(AnimationTiming timing) => (timing.Begin + timing.ActiveDuration.GetValueOrDefault()).Ticks;

    private static long RoundUpToMultiple(long value, long multiple) => (value + multiple - 1) / multiple * multiple;

    private static long LeastCommonMultiple(long first, long second) => checked(first / GreatestCommonDivisor(first, second) * second);

    private static long GreatestCommonDivisor(long first, long second)
    {
        while (second != 0)
            (first, second) = (second, first % second);
        return first;
    }

    private static void ThrowIfTooLong(long ticks)
    {
        if (ticks > MaximumLength.Ticks)
            throw new CursorConversionException(string.Create(CultureInfo.InvariantCulture, $"The animation only repeats after {TimeSpan.FromTicks(ticks).TotalSeconds:0.###} s, but animated cursors are limited to loops of {MaximumLength.TotalMinutes} minutes."));
    }
}

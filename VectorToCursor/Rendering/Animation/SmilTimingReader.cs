using System.Globalization;
using Svg;
using VectorToCursor.Domain;

namespace VectorToCursor.Rendering.Animation;

/// <summary>Reads when each SMIL animation plays, and normalizes its timing for a cursor that loops forever.</summary>
internal static class SmilTimingReader
{
    /// <summary>
    /// Returns the timing of every animation Svg.Skia's engine will play. Their times are rewritten as exact whole
    /// milliseconds, and endless animations that start late are shifted into their first iteration.
    /// </summary>
    /// <exception cref="CursorConversionException">An animation's timing can't be played back as a looping cursor.</exception>
    public static List<AnimationTiming> ReadAndNormalize(SvgDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        return [.. document.Descendants().OfType<SvgAnimationElement>().Where(IsPlayed).Select(ReadAndNormalize)];
    }

    // Mirrors the animation engine, which plays only animations that have a target element and an attribute to animate.
    private static bool IsPlayed(SvgAnimationElement animation) => animation.TargetElement is not null && !string.IsNullOrWhiteSpace(AnimatedAttribute(animation));

    private static string? AnimatedAttribute(SvgAnimationElement animation) => animation switch
    {
        SvgAnimateMotion => "transform",
        SvgAnimateTransform transform => string.IsNullOrWhiteSpace(transform.AnimationAttributeName) ? "transform" : transform.AnimationAttributeName,
        SvgAnimationAttributeElement attribute => attribute.AnimationAttributeName,
        _ => null,
    };

    private static AnimationTiming ReadAndNormalize(SvgAnimationElement animation)
    {
        RejectUnsupportedTiming(animation);
        TimeSpan duration = ReadDuration(animation);
        TimeSpan begin = ReadBegin(animation);
        TimeSpan? activeDuration = ReadActiveDuration(animation, duration);

        // Only the phase of an endless animation matters; a late start would freeze the first frames of every loop.
        if (activeDuration is null && begin > TimeSpan.Zero)
            begin -= duration * Math.Ceiling(begin / duration);

        animation.Duration = ClockValue.Format(duration);
        animation.Begin = ClockValue.Format(begin);
        return new AnimationTiming(begin, duration, activeDuration);
    }

    private static void RejectUnsupportedTiming(SvgAnimationElement animation)
    {
        foreach ((string attribute, string? value) in new[] { ("end", animation.End), ("min", animation.Minimum), ("max", animation.Maximum) })
        {
            if (!string.IsNullOrWhiteSpace(value))
                throw new CursorConversionException($"{Describe(animation)} uses '{attribute}', which a looping cursor can't play.");
        }
    }

    private static TimeSpan ReadDuration(SvgAnimationElement animation) =>
        ClockValue.TryParse(animation.Duration, out TimeSpan duration) && duration > TimeSpan.Zero
            ? duration
            : throw new CursorConversionException($"{Describe(animation)} needs a positive 'dur', but has '{animation.Duration}'.");

    private static TimeSpan ReadBegin(SvgAnimationElement animation)
    {
        string? begin = animation.Begin;
        if (string.IsNullOrWhiteSpace(begin))
            return TimeSpan.Zero;
        if (begin.Contains(';', StringComparison.Ordinal))
            throw new CursorConversionException($"{Describe(animation)} has several begin times ('{begin}'), which a looping cursor can't play.");

        return ClockValue.TryParse(begin, out TimeSpan offset)
            ? offset
            : throw new CursorConversionException($"{Describe(animation)} starts with '{begin}', but a cursor never receives events, so only time offsets are supported.");
    }

    // repeatCount and repeatDur combine as in SMIL: whichever ends first, both missing means one iteration.
    private static TimeSpan? ReadActiveDuration(SvgAnimationElement animation, TimeSpan duration)
    {
        double? count = ReadRepeatCount(animation.RepeatCount, out bool countGiven);
        string repeatDuration = animation.RepeatDuration ?? string.Empty;
        if (string.IsNullOrWhiteSpace(repeatDuration))
            return count is double finite ? duration * finite : null;
        if (ClockValue.IsIndefinite(repeatDuration))
            return countGiven && count is double limited ? duration * limited : null;
        if (!ClockValue.TryParse(repeatDuration, out TimeSpan limit) || limit < TimeSpan.Zero)
            throw new CursorConversionException($"{Describe(animation)} has an unreadable repeatDur '{repeatDuration}'.");

        return countGiven && count is double repeats && duration * repeats < limit ? duration * repeats : limit;
    }

    // Like the engine: the first ';'-separated entry counts, "indefinite" repeats forever, anything unreadable means 1.
    private static double? ReadRepeatCount(string? text, out bool given)
    {
        string first = (text ?? string.Empty).Split(';')[0].Trim();
        given = first.Length > 0;
        if (ClockValue.IsIndefinite(first))
            return null;
        if (double.TryParse(first, NumberStyles.Float, CultureInfo.InvariantCulture, out double count) && double.IsFinite(count))
            return Math.Max(0, count);

        given = false;
        return 1;
    }

    private static string Describe(SvgAnimationElement animation)
    {
        string target = animation.TargetElement?.ID is { Length: > 0 } id ? $" on '#{id}'" : string.Empty;
        return $"The animation of '{AnimatedAttribute(animation)}'{target}";
    }
}

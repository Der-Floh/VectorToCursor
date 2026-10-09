using VectorToCursor.Rendering.Animation.Css;

namespace VectorToCursor.Rendering.Animation;

/// <summary>The keyframes of one property within one CSS animation, from 0 to 1.</summary>
internal sealed class KeyframeTrack
{
    public KeyframeTrack(string property, IReadOnlyList<double> offsets, IReadOnlyList<string> values, IReadOnlyList<CssTimingFunction> easings)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(property);
        ArgumentNullException.ThrowIfNull(offsets);
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(easings);
        if (offsets.Count < 2 || values.Count != offsets.Count || easings.Count != offsets.Count - 1)
            throw new ArgumentException("A track needs at least two keyframes, one value per keyframe and one easing per interval.", nameof(offsets));

        Property = property;
        Offsets = offsets;
        Values = values;
        Easings = easings;
    }

    public string Property { get; }

    public IReadOnlyList<double> Offsets { get; }

    public IReadOnlyList<string> Values { get; }

    /// <summary>The easing of each interval between neighbouring keyframes.</summary>
    public IReadOnlyList<CssTimingFunction> Easings { get; }

    /// <summary>The track played backwards: keyframes in reverse order, mirrored offsets, and each easing reversed.</summary>
    public KeyframeTrack Reversed() =>
        new(Property, [.. Offsets.Reverse().Select(offset => 1 - offset)], [.. Values.Reverse()], [.. Easings.Reverse().Select(easing => easing.Reversed())]);

    /// <summary>The track followed by itself backwards, both squeezed into one iteration of twice the length.</summary>
    public KeyframeTrack Alternated()
    {
        KeyframeTrack backwards = Reversed();
        return new(
            Property,
            [.. Offsets.Select(offset => offset / 2), .. backwards.Offsets.Skip(1).Select(offset => 0.5 + offset / 2)],
            [.. Values, .. backwards.Values.Skip(1)],
            [.. Easings, .. backwards.Easings]);
    }
}

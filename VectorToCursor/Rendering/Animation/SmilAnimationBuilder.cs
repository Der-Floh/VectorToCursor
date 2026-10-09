using System.Globalization;
using Svg;
using VectorToCursor.Domain;
using VectorToCursor.Rendering.Animation.Css;

namespace VectorToCursor.Rendering.Animation;

/// <summary>Adds the SMIL elements that play one CSS animation of an element, so Svg.Skia's SMIL engine can render it.</summary>
internal static class SmilAnimationBuilder
{
    private const string Indefinite = "indefinite";
    private const double First = 0;
    private const double Last = 1;

    private static readonly string[] TransformOriginAttributes = ["transform-origin", "transform-box"];

    /// <exception cref="CursorConversionException">The animation uses something that can't be reproduced exactly.</exception>
    public static void Add(SvgElement target, CssAnimation animation, CssKeyframes keyframes)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(animation);
        ArgumentNullException.ThrowIfNull(keyframes);

        if (!ShouldPlay(animation))
            return;

        foreach (KeyframeTrack track in BuildTracks(target, animation, keyframes))
        {
            KeyframeTrack directed = ApplyDirection(track, animation.Direction);
            foreach (SvgAnimationValueElement element in CreateElements(target, directed, animation.Name))
            {
                ApplyTiming(element, animation, directed);
                target.Children.Add(element);
            }
        }
    }

    // An animation of zero length or zero iterations never changes anything visible.
    private static bool ShouldPlay(CssAnimation animation)
    {
        bool fillsForwards = animation.FillMode is CssAnimationFillMode.Forwards or CssAnimationFillMode.Both;
        if (animation.IsPaused)
            throw new CursorConversionException($"The animation '{animation.Name}' is paused, which a cursor can't resume.");
        if (animation.Duration == TimeSpan.Zero && fillsForwards)
            throw new CursorConversionException($"The animation '{animation.Name}' has no duration but fills forwards, which is not supported.");
        if (animation.Delay > TimeSpan.Zero && animation.FillMode is CssAnimationFillMode.Backwards or CssAnimationFillMode.Both)
            throw new CursorConversionException($"The animation '{animation.Name}' fills backwards during its delay, which is not supported.");

        return animation.Duration > TimeSpan.Zero && animation.IterationCount is not 0;
    }

    private static List<KeyframeTrack> BuildTracks(SvgElement target, CssAnimation animation, CssKeyframes keyframes)
    {
        // property -> offset -> value and the easing a keyframe sets for the interval that starts at it
        Dictionary<string, SortedDictionary<double, (string Value, CssTimingFunction? Easing)>> properties = new(StringComparer.Ordinal);
        foreach (CssKeyframeBlock block in keyframes.Blocks)
        {
            List<double> offsets = ParseOffsets(block.Selector, keyframes.Name);
            CssTimingFunction? easing = block.Declarations.LastOrDefault(declaration => declaration.Property == CssAnimationProperty.TimingFunction) is { } timing
                ? CssTimingFunction.Parse(timing.Value)
                : null;
            // CSS ignores !important and animation properties inside keyframes.
            foreach (CssDeclaration declaration in block.Declarations.Where(declaration => !declaration.Important && !CssAnimationProperty.IsAnimationProperty(declaration.Property)))
            {
                if (!CssAnimatedProperty.IsSupported(declaration.Property))
                    throw new CursorConversionException($"The animation '{keyframes.Name}' animates '{declaration.Property}', which is not supported.");

                if (!properties.TryGetValue(declaration.Property, out SortedDictionary<double, (string Value, CssTimingFunction? Easing)>? track))
                {
                    track = new SortedDictionary<double, (string Value, CssTimingFunction? Easing)>();
                    properties[declaration.Property] = track;
                }
                foreach (double offset in offsets)
                    track[offset] = (declaration.Value, easing);
            }
        }
        return [.. properties.Select(property => CompleteTrack(target, animation, property.Key, property.Value))];
    }

    // CSS builds a missing 0% or 100% keyframe from the element's own value of the property.
    private static KeyframeTrack CompleteTrack(SvgElement target, CssAnimation animation, string property, SortedDictionary<double, (string Value, CssTimingFunction? Easing)> keyframes)
    {
        foreach (double offset in new[] { First, Last })
        {
            if (!keyframes.ContainsKey(offset))
                keyframes[offset] = (BaseValue(target, property, animation.Name), null);
        }

        List<double> offsets = [.. keyframes.Keys];
        List<CssTimingFunction> easings = [.. keyframes.Values.SkipLast(1).Select(keyframe => keyframe.Easing ?? animation.TimingFunction)];
        return new KeyframeTrack(property, offsets, [.. keyframes.Values.Select(keyframe => keyframe.Value)], easings);
    }

    private static string BaseValue(SvgElement target, string property, string animationName)
    {
        if (property == CssAnimatedProperty.Transform)
        {
            // CSS would interpolate between the element's own transform and the keyframes as matrices, which SMIL can't.
            return target.Transforms is { Count: > 0 }
                ? throw new CursorConversionException($"The animation '{animationName}' leaves out its first or last transform keyframe on an element with its own transform; add explicit from and to keyframes.")
                : CssAnimatedProperty.InitialValue(property);
        }

        string? value = Convert.ToString(target.GetAnimationValue(property), CultureInfo.InvariantCulture)?.Trim();
        return string.IsNullOrEmpty(value) ? CssAnimatedProperty.InitialValue(property) : value;
    }

    private static List<double> ParseOffsets(string selector, string animationName)
    {
        List<double> offsets = [];
        foreach (string part in CssText.SplitTopLevel(selector, ','))
        {
            double? offset = part.ToLowerInvariant() switch
            {
                "from" => First,
                "to" => Last,
                string percentage when percentage.EndsWith('%') && double.TryParse(percentage[..^1], NumberStyles.Float, CultureInfo.InvariantCulture, out double value) && value is >= 0 and <= 100 => value / 100,
                _ => null,
            };
            offsets.Add(offset ?? throw new CursorConversionException($"The animation '{animationName}' has an invalid keyframe '{part}'."));
        }
        return offsets;
    }

    private static KeyframeTrack ApplyDirection(KeyframeTrack track, CssAnimationDirection direction) => direction switch
    {
        CssAnimationDirection.Reverse => track.Reversed(),
        CssAnimationDirection.Alternate => track.Alternated(),
        CssAnimationDirection.AlternateReverse => track.Reversed().Alternated(),
        _ => track,
    };

    private static IEnumerable<SvgAnimationValueElement> CreateElements(SvgElement target, KeyframeTrack track, string animationName)
    {
        IReadOnlyList<string> values = DiscreteValuesOrSame(track, animationName);
        if (track.Property == CssAnimatedProperty.Transform)
            return CreateTransformElements(target, values, animationName);
        if (track.Property == CssAnimatedProperty.StrokeDashArray)
            return [new SvgAnimate { AnimationAttributeName = track.Property, Values = string.Join(';', CssDashArray.ToSmilValues(values)) }];

        RejectNonInterpolableColors(track, values, animationName);
        return [new SvgAnimate { AnimationAttributeName = track.Property, Values = string.Join(';', values.Select(value => CssAnimatedProperty.ToSmilValue(track.Property, value))) }];
    }

    // SMIL holds each value from its key time on, which is step-end; a step-start interval shows the next value instead.
    private static IReadOnlyList<string> DiscreteValuesOrSame(KeyframeTrack track, string animationName)
    {
        if (!IsDiscrete(track, animationName))
            return track.Values;

        return [.. track.Values.Select((value, index) => index < track.Easings.Count && track.Easings[index] is CssTimingFunction.Step { JumpsAtStart: true } ? track.Values[index + 1] : value)];
    }

    private static bool IsDiscrete(KeyframeTrack track, string animationName)
    {
        int steps = track.Easings.Count(easing => easing is CssTimingFunction.Step);
        if (steps > 0 && steps < track.Easings.Count)
            throw new CursorConversionException($"The animation '{animationName}' mixes steps with smooth easing for '{track.Property}', which is not supported.");
        return steps > 0;
    }

    private static void RejectNonInterpolableColors(KeyframeTrack track, IReadOnlyList<string> values, string animationName)
    {
        if (CssAnimatedProperty.IsColor(track.Property) && !IsDiscrete(track, animationName) && values.Any(value => !IsPlainColor(value)))
            throw new CursorConversionException($"The animation '{animationName}' blends '{track.Property}' between a color and '{values.First(value => !IsPlainColor(value))}', which can't be interpolated; use step-end.");
    }

    private static bool IsPlainColor(string value) =>
        !string.Equals(value.Trim(), "none", StringComparison.OrdinalIgnoreCase) && !value.TrimStart().StartsWith("url(", StringComparison.OrdinalIgnoreCase);

    // One animateTransform per CSS function: the first replaces the element's transform, the rest are added to it.
    private static List<SvgAnimationValueElement> CreateTransformElements(SvgElement target, IReadOnlyList<string> values, string animationName)
    {
        if (TransformOriginAttributes.Any(attribute => target.TryGetAttribute(attribute, out _)))
            throw new CursorConversionException($"The animation '{animationName}' rotates or scales an element with transform-origin or transform-box, which is not supported.");

        List<List<CssTransformFunction>> keyframes = [.. values.Select(CssTransformFunction.ParseList)];
        List<SvgAnimateTransformType> types = keyframes.FirstOrDefault(functions => functions.Count > 0)?.Select(function => function.Type).ToList() ?? [];
        List<List<CssTransformFunction>> aligned = [.. keyframes.Select(functions => functions.Count == 0 ? [.. types.Select(CssTransformFunction.Identity)] : functions)];
        if (aligned.Any(functions => !functions.Select(function => function.Type).SequenceEqual(types)))
            throw new CursorConversionException($"The animation '{animationName}' uses different transform functions in its keyframes, which is not supported.");

        return [.. types.Select((type, index) => (SvgAnimationValueElement)new SvgAnimateTransform
        {
            AnimationAttributeName = CssAnimatedProperty.Transform,
            TransformType = type,
            Values = string.Join(';', aligned.Select(functions => functions[index].ToSmilValue())),
            Additive = index == 0 ? SvgAnimationAdditive.Replace : SvgAnimationAdditive.Sum,
        })];
    }

    private static void ApplyTiming(SvgAnimationValueElement element, CssAnimation animation, KeyframeTrack track)
    {
        bool alternates = animation.Direction is CssAnimationDirection.Alternate or CssAnimationDirection.AlternateReverse;
        bool discrete = track.Easings.All(easing => easing is CssTimingFunction.Step);

        element.KeyTimes = [.. track.Offsets.Select(offset => (float)offset)];
        element.CalcMode = discrete ? SvgAnimationCalcMode.Discrete : SvgAnimationCalcMode.Spline;
        if (!discrete)
            element.KeySplines = string.Join(';', track.Easings.Cast<CssTimingFunction.CubicBezier>().Select(easing => easing.ToKeySpline()));

        element.Duration = ClockValue.Format(alternates ? animation.Duration * 2 : animation.Duration);
        element.Begin = ClockValue.Format(animation.Delay);
        element.RepeatCount = animation.IterationCount is double count ? (alternates ? count / 2 : count).ToString(CultureInfo.InvariantCulture) : Indefinite;
        element.AnimationFill = animation.FillMode is CssAnimationFillMode.Forwards or CssAnimationFillMode.Both ? SvgAnimationFill.Freeze : SvgAnimationFill.Remove;
    }
}

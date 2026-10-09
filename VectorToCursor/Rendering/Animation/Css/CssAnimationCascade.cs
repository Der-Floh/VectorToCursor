using System.Globalization;
using Svg;
using VectorToCursor.Domain;

namespace VectorToCursor.Rendering.Animation.Css;

/// <summary>
/// Resolves the CSS animations of an element like the CSS cascade: important declarations win, then inline style, then
/// selector specificity, then source order. Svg.Custom drops animation declarations, so this has to be done here.
/// </summary>
internal static class CssAnimationCascade
{
    private const string Infinite = "infinite";
    private const string None = "none";

    public static List<CssAnimation> Resolve(SvgElement element, IReadOnlyList<CssAnimationRule> rules)
    {
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(rules);

        Dictionary<string, (Precedence Precedence, string Value)> longhands = new(StringComparer.Ordinal);
        foreach (CssAnimationRule rule in rules)
        {
            if (rule.MatchSpecificity(element) is not int specificity)
                continue;
            for (int position = 0; position < rule.Declarations.Count; position++)
            {
                CssDeclaration declaration = rule.Declarations[position];
                Declare(longhands, declaration.Property, declaration.Value, new Precedence(declaration.Important, Inline: false, specificity, rule.Order, position));
            }
        }
        DeclareInline(element, longhands);
        return longhands.Count == 0 ? [] : Build(longhands.ToDictionary(entry => entry.Key, entry => entry.Value.Value, StringComparer.Ordinal));
    }

    // Inline style reaches us as separate attributes whose order is lost; longhands are assumed to refine the shorthand.
    private static void DeclareInline(SvgElement element, Dictionary<string, (Precedence Precedence, string Value)> longhands)
    {
        if (element.TryGetAttribute(CssAnimationProperty.Shorthand, out string shorthand))
            Declare(longhands, CssAnimationProperty.Shorthand, shorthand, new Precedence(Important: false, Inline: true, 0, 0, 0));
        foreach (string longhand in CssAnimationProperty.InitialValues.Keys)
        {
            if (element.TryGetAttribute(longhand, out string value))
                Declare(longhands, longhand, value, new Precedence(Important: false, Inline: true, 0, 0, 1));
        }
    }

    private static void Declare(Dictionary<string, (Precedence Precedence, string Value)> longhands, string property, string value, Precedence precedence)
    {
        if (property == CssAnimationProperty.Shorthand)
        {
            foreach ((string longhand, string longhandValue) in CssAnimationShorthand.Expand(value))
                Consider(longhands, longhand, longhandValue, precedence);
        }
        else if (CssAnimationProperty.InitialValues.ContainsKey(property))
        {
            Consider(longhands, property, value, precedence);
        }
    }

    private static void Consider(Dictionary<string, (Precedence Precedence, string Value)> longhands, string longhand, string value, Precedence precedence)
    {
        if (!longhands.TryGetValue(longhand, out (Precedence Precedence, string Value) current) || precedence.CompareTo(current.Precedence) >= 0)
            longhands[longhand] = (precedence, value);
    }

    // Each longhand is a comma-separated list; the names decide how many animations there are and the other lists repeat.
    private static List<CssAnimation> Build(Dictionary<string, string> longhands)
    {
        Dictionary<string, List<string>> lists = CssAnimationProperty.InitialValues.ToDictionary(
            initial => initial.Key,
            initial => CssText.SplitTopLevel(longhands.GetValueOrDefault(initial.Key, initial.Value), ',') is { Count: > 0 } values ? values : [initial.Value],
            StringComparer.Ordinal);

        List<CssAnimation> animations = [];
        List<string> names = lists[CssAnimationProperty.Name];
        for (int index = 0; index < names.Count; index++)
        {
            string name = CssText.Unquote(names[index]);
            if (string.Equals(name, None, StringComparison.OrdinalIgnoreCase))
                continue;

            string Pick(string longhand) => lists[longhand][index % lists[longhand].Count];
            animations.Add(new CssAnimation(
                name,
                ParseDuration(Pick(CssAnimationProperty.Duration), name),
                CssTimingFunction.Parse(Pick(CssAnimationProperty.TimingFunction)),
                ParseTime(Pick(CssAnimationProperty.Delay), name, "delay"),
                ParseIterationCount(Pick(CssAnimationProperty.IterationCount), name),
                ParseDirection(Pick(CssAnimationProperty.Direction), name),
                ParseFillMode(Pick(CssAnimationProperty.FillMode), name),
                ParseIsPaused(Pick(CssAnimationProperty.PlayState), name)));
        }
        return animations;
    }

    private static TimeSpan ParseDuration(string text, string name)
    {
        TimeSpan duration = ParseTime(text, name, "duration");
        return duration >= TimeSpan.Zero ? duration : throw new CursorConversionException($"The animation '{name}' has a negative duration '{text}'.");
    }

    private static TimeSpan ParseTime(string text, string name, string what) =>
        CssTime.TryParse(text, out TimeSpan time) ? time : throw new CursorConversionException($"The animation '{name}' has an invalid {what} '{text}'; use a time such as 1.5s or 300ms.");

    private static double? ParseIterationCount(string text, string name)
    {
        if (string.Equals(text, Infinite, StringComparison.OrdinalIgnoreCase))
            return null;
        if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double count) && double.IsFinite(count) && count >= 0)
            return count;
        throw new CursorConversionException($"The animation '{name}' has an invalid iteration count '{text}'.");
    }

    private static CssAnimationDirection ParseDirection(string text, string name) => text.ToLowerInvariant() switch
    {
        "normal" => CssAnimationDirection.Normal,
        "reverse" => CssAnimationDirection.Reverse,
        "alternate" => CssAnimationDirection.Alternate,
        "alternate-reverse" => CssAnimationDirection.AlternateReverse,
        _ => throw new CursorConversionException($"The animation '{name}' has an invalid direction '{text}'."),
    };

    private static CssAnimationFillMode ParseFillMode(string text, string name) => text.ToLowerInvariant() switch
    {
        "none" => CssAnimationFillMode.None,
        "forwards" => CssAnimationFillMode.Forwards,
        "backwards" => CssAnimationFillMode.Backwards,
        "both" => CssAnimationFillMode.Both,
        _ => throw new CursorConversionException($"The animation '{name}' has an invalid fill mode '{text}'."),
    };

    private static bool ParseIsPaused(string text, string name) => text.ToLowerInvariant() switch
    {
        "running" => false,
        "paused" => true,
        _ => throw new CursorConversionException($"The animation '{name}' has an invalid play state '{text}'."),
    };

    private readonly record struct Precedence(bool Important, bool Inline, int Specificity, int Order, int Position) : IComparable<Precedence>
    {
        public int CompareTo(Precedence other) =>
            (Important, Inline, Specificity, Order, Position).CompareTo((other.Important, other.Inline, other.Specificity, other.Order, other.Position));
    }
}

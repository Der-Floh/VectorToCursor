using System.Globalization;
using VectorToCursor.Domain;

namespace VectorToCursor.Rendering.Animation.Css;

/// <summary>Splits an <c>animation</c> shorthand into the comma-separated values of its longhands.</summary>
internal static class CssAnimationShorthand
{
    private const string Infinite = "infinite";

    private static readonly HashSet<string> Directions = ["normal", "reverse", "alternate", "alternate-reverse"];
    private static readonly HashSet<string> FillModes = ["none", "forwards", "backwards", "both"];
    private static readonly HashSet<string> PlayStates = ["running", "paused"];

    /// <exception cref="CursorConversionException">An animation in the list can't be read.</exception>
    public static Dictionary<string, string> Expand(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        List<Dictionary<string, string>> animations = [.. CssText.SplitTopLevel(value, ',').Select(ParseAnimation)];
        return CssAnimationProperty.InitialValues.ToDictionary(
            longhand => longhand.Key,
            longhand => string.Join(", ", animations.Select(animation => animation.GetValueOrDefault(longhand.Key, longhand.Value))),
            StringComparer.Ordinal);
    }

    // Tokens are assigned in the order CSS defines: the first time is the duration, the second the delay, and a token that
    // fits no other longhand is the name.
    private static Dictionary<string, string> ParseAnimation(string animation)
    {
        Dictionary<string, string> longhands = new(StringComparer.Ordinal);
        foreach (string token in CssText.SplitOnWhitespace(animation))
        {
            string longhand = Classify(token, longhands) ?? throw new CursorConversionException($"Could not read the animation '{animation}'.");
            longhands[longhand] = token;
        }
        return longhands;
    }

    private static string? Classify(string token, Dictionary<string, string> assigned)
    {
        string lower = token.ToLowerInvariant();
        if (CssTime.TryParse(token, out _))
            return !assigned.ContainsKey(CssAnimationProperty.Duration) ? CssAnimationProperty.Duration : FreeOrNull(CssAnimationProperty.Delay, assigned);
        if (CssTimingFunction.IsTimingFunction(lower))
            return FreeOrNull(CssAnimationProperty.TimingFunction, assigned);
        if (lower == Infinite || IsNonNegativeNumber(token))
            return FreeOrNull(CssAnimationProperty.IterationCount, assigned);
        if (Directions.Contains(lower) && !assigned.ContainsKey(CssAnimationProperty.Direction))
            return CssAnimationProperty.Direction;
        if (FillModes.Contains(lower) && !assigned.ContainsKey(CssAnimationProperty.FillMode))
            return CssAnimationProperty.FillMode;
        if (PlayStates.Contains(lower) && !assigned.ContainsKey(CssAnimationProperty.PlayState))
            return CssAnimationProperty.PlayState;
        return FreeOrNull(CssAnimationProperty.Name, assigned);
    }

    private static string? FreeOrNull(string longhand, Dictionary<string, string> assigned) => assigned.ContainsKey(longhand) ? null : longhand;

    private static bool IsNonNegativeNumber(string token) =>
        double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out double number) && double.IsFinite(number) && number >= 0;
}

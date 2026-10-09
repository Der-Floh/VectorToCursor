using System.Globalization;
using VectorToCursor.Domain;

namespace VectorToCursor.Rendering.Animation.Css;

/// <summary>The CSS properties that can be animated, their initial values, and their conversion to SMIL values.</summary>
internal static class CssAnimatedProperty
{
    public const string Transform = "transform";
    public const string StrokeDashArray = "stroke-dasharray";

    private static readonly HashSet<string> Numbers = ["opacity", "fill-opacity", "stroke-opacity"];
    private static readonly HashSet<string> Lengths = ["stroke-width", "stroke-dashoffset"];
    private static readonly HashSet<string> Colors = ["fill", "stroke"];

    private static readonly Dictionary<string, string> InitialValues = new(StringComparer.Ordinal)
    {
        ["opacity"] = "1",
        ["fill-opacity"] = "1",
        ["stroke-opacity"] = "1",
        ["stroke-width"] = "1",
        ["stroke-dashoffset"] = "0",
        ["fill"] = "black",
        ["stroke"] = "none",
        [StrokeDashArray] = "none",
        [Transform] = "none",
    };

    public static bool IsSupported(string property) => InitialValues.ContainsKey(property);

    public static bool IsColor(string property) => Colors.Contains(property);

    /// <summary>The value an element has when neither it nor its ancestors set the property.</summary>
    public static string InitialValue(string property) => InitialValues[property];

    /// <summary>Converts a single keyframe value of a number, length or color property.</summary>
    /// <exception cref="CursorConversionException">The value isn't a plain number or px length where one is needed.</exception>
    public static string ToSmilValue(string property, string value)
    {
        string trimmed = value.Trim();
        if (Numbers.Contains(property))
            return ParseNumber(trimmed, property).ToString(CultureInfo.InvariantCulture);
        if (Lengths.Contains(property))
            return ParseNumber(trimmed.EndsWith("px", StringComparison.OrdinalIgnoreCase) ? trimmed[..^2] : trimmed, property).ToString(CultureInfo.InvariantCulture);
        return trimmed;
    }

    private static double ParseNumber(string text, string property) =>
        double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double number) && double.IsFinite(number)
            ? number
            : throw new CursorConversionException($"The {property} value '{text}' can't be animated; use a plain number{(Lengths.Contains(property) ? " or px length" : string.Empty)}.");
}

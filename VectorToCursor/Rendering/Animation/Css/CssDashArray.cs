using System.Globalization;
using VectorToCursor.Domain;

namespace VectorToCursor.Rendering.Animation.Css;

/// <summary><c>stroke-dasharray</c> keyframe values, brought to a common length so the animation engine can interpolate them.</summary>
internal static class CssDashArray
{
    /// <summary>
    /// Repeats every list to the least common multiple of their lengths, as CSS does; with unequal lengths the engine would
    /// jump between the lists instead of interpolating.
    /// </summary>
    /// <exception cref="CursorConversionException">A keyframe is <c>none</c> or not a list of lengths.</exception>
    public static List<string> ToSmilValues(IReadOnlyList<string> keyframeValues)
    {
        ArgumentNullException.ThrowIfNull(keyframeValues);

        List<double[]> lists = [.. keyframeValues.Select(Parse)];
        int length = lists.Select(list => list.Length).Aggregate(LeastCommonMultiple);
        return [.. lists.Select(list => string.Join(' ', Enumerable.Range(0, length).Select(index => list[index % list.Length].ToString(CultureInfo.InvariantCulture))))];
    }

    private static double[] Parse(string value)
    {
        string[] parts = value.Split([',', ' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0 || parts.Any(part => !TryParseLength(part, out _)))
            throw new CursorConversionException($"The stroke-dasharray '{value}' can't be animated; every keyframe needs a list of lengths.");

        return [.. parts.Select(part => TryParseLength(part, out double length) ? length : 0)];
    }

    private static bool TryParseLength(string text, out double length)
    {
        string number = text.EndsWith("px", StringComparison.OrdinalIgnoreCase) ? text[..^2] : text;
        return double.TryParse(number, NumberStyles.Float, CultureInfo.InvariantCulture, out length) && double.IsFinite(length) && length >= 0;
    }

    private static int LeastCommonMultiple(int first, int second) => first / GreatestCommonDivisor(first, second) * second;

    private static int GreatestCommonDivisor(int first, int second)
    {
        while (second != 0)
            (first, second) = (second, first % second);
        return first;
    }
}

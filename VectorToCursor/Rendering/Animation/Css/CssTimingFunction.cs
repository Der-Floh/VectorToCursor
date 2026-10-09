using System.Globalization;
using VectorToCursor.Domain;

namespace VectorToCursor.Rendering.Animation.Css;

/// <summary>A CSS easing function, limited to what SMIL key splines and discrete steps reproduce exactly.</summary>
internal abstract record CssTimingFunction
{
    private static readonly HashSet<string> Keywords = ["linear", "ease", "ease-in", "ease-out", "ease-in-out", "step-start", "step-end"];

    public static CssTimingFunction Linear { get; } = new CubicBezier(0, 0, 1, 1);

    public static CssTimingFunction Ease { get; } = new CubicBezier(0.25, 0.1, 0.25, 1);

    public static bool IsTimingFunction(string token)
    {
        string lower = token.Trim().ToLowerInvariant();
        return Keywords.Contains(lower) || lower.StartsWith("cubic-bezier(", StringComparison.Ordinal) || lower.StartsWith("steps(", StringComparison.Ordinal);
    }

    /// <exception cref="CursorConversionException">The function is unknown or can't be reproduced exactly.</exception>
    public static CssTimingFunction Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        string value = text.Trim();
        return value.ToLowerInvariant() switch
        {
            "linear" => Linear,
            "ease" => Ease,
            "ease-in" => new CubicBezier(0.42, 0, 1, 1),
            "ease-out" => new CubicBezier(0, 0, 0.58, 1),
            "ease-in-out" => new CubicBezier(0.42, 0, 0.58, 1),
            "step-start" => new Step(JumpsAtStart: true),
            "step-end" => new Step(JumpsAtStart: false),
            string lower when lower.StartsWith("cubic-bezier(", StringComparison.Ordinal) => ParseCubicBezier(value),
            string lower when lower.StartsWith("steps(", StringComparison.Ordinal) => ParseSteps(value),
            _ => throw new CursorConversionException($"The timing function '{value}' is not supported."),
        };
    }

    /// <summary>The same easing played backwards, for reversed animation directions.</summary>
    public abstract CssTimingFunction Reversed();

    private static CubicBezier ParseCubicBezier(string text)
    {
        double[] points = ParseArguments(text).Select(argument => ParseNumber(argument, text)).ToArray();
        if (points.Length != 4 || points[0] is < 0 or > 1 || points[2] is < 0 or > 1)
            throw new CursorConversionException($"The timing function '{text}' is not a valid cubic-bezier().");
        // The animation engine clamps eased progress to 0..1, so a curve that overshoots would play differently.
        if (points[1] is < 0 or > 1 || points[3] is < 0 or > 1)
            throw new CursorConversionException($"The timing function '{text}' overshoots (y outside 0 to 1), which is not supported.");

        return new CubicBezier(points[0], points[1], points[2], points[3]);
    }

    private static Step ParseSteps(string text)
    {
        List<string> arguments = ParseArguments(text);
        string position = arguments.Count > 1 ? arguments[1].ToLowerInvariant() : "end";
        if (arguments.Count is < 1 or > 2 || arguments[0] != "1" || position is not ("start" or "jump-start" or "end" or "jump-end"))
            throw new CursorConversionException($"The timing function '{text}' is not supported; only single steps (step-start, step-end, steps(1, start|end)) are.");

        return new Step(JumpsAtStart: position is "start" or "jump-start");
    }

    private static List<string> ParseArguments(string text)
    {
        int open = text.IndexOf('(', StringComparison.Ordinal);
        int close = text.LastIndexOf(')');
        return open < 0 || close < open ? [] : CssText.SplitTopLevel(text[(open + 1)..close], ',');
    }

    private static double ParseNumber(string text, string function) =>
        double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double number) && double.IsFinite(number)
            ? number
            : throw new CursorConversionException($"The timing function '{function}' has an invalid number '{text}'.");

    internal sealed record CubicBezier(double X1, double Y1, double X2, double Y2) : CssTimingFunction
    {
        // Fixed-point with 15 decimals: mirrored points such as 1 - 0.7 print as 0.3, and never in exponent notation.
        private const string KeySplineNumberFormat = "0.###############";

        public override CssTimingFunction Reversed() => new CubicBezier(1 - X2, 1 - Y2, 1 - X1, 1 - Y1);

        public string ToKeySpline() => string.Join(' ', new[] { X1, Y1, X2, Y2 }.Select(point => point.ToString(KeySplineNumberFormat, CultureInfo.InvariantCulture)));
    }

    /// <summary>A single jump at the start or the end of an interval.</summary>
    internal sealed record Step(bool JumpsAtStart) : CssTimingFunction
    {
        public override CssTimingFunction Reversed() => new Step(!JumpsAtStart);
    }
}

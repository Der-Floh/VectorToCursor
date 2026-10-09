using System.Globalization;
using Svg;
using VectorToCursor.Domain;

namespace VectorToCursor.Rendering.Animation.Css;

/// <summary>One CSS transform function, converted to the SVG transform type and arguments that SMIL animates.</summary>
internal sealed record CssTransformFunction(SvgAnimateTransformType Type, IReadOnlyList<double> Arguments)
{
    private const string None = "none";
    private const double DegreesPerRadian = 180 / Math.PI;
    private const double DegreesPerGradian = 0.9;
    private const double DegreesPerTurn = 360;

    /// <summary>Parses a transform list such as <c>translate(4px, 0) rotate(45deg)</c>; <c>none</c> is an empty list.</summary>
    /// <exception cref="CursorConversionException">A function or unit isn't supported.</exception>
    public static List<CssTransformFunction> ParseList(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        string text = value.Trim();
        if (string.Equals(text, None, StringComparison.OrdinalIgnoreCase))
            return [];

        List<CssTransformFunction> functions = [];
        foreach (string function in CssText.SplitOnWhitespace(text))
            functions.Add(Parse(function));
        return functions;
    }

    public static CssTransformFunction Identity(SvgAnimateTransformType type) => type switch
    {
        SvgAnimateTransformType.Translate => new(type, [0, 0]),
        SvgAnimateTransformType.Scale => new(type, [1, 1]),
        _ => new(type, [0]),
    };

    public string ToSmilValue() => string.Join(' ', Arguments.Select(argument => argument.ToString(CultureInfo.InvariantCulture)));

    private static CssTransformFunction Parse(string function)
    {
        int open = function.IndexOf('(', StringComparison.Ordinal);
        if (open <= 0 || !function.EndsWith(')'))
            throw new CursorConversionException($"The transform '{function}' can't be read.");

        string name = function[..open].Trim().ToLowerInvariant();
        List<string> arguments = CssText.SplitTopLevel(function[(open + 1)..^1], ',');
        return (name, arguments.Count) switch
        {
            ("rotate", 1) => new(SvgAnimateTransformType.Rotate, [Angle(arguments[0], function)]),
            ("translate", 1) => new(SvgAnimateTransformType.Translate, [Length(arguments[0], function), 0]),
            ("translate", 2) => new(SvgAnimateTransformType.Translate, [Length(arguments[0], function), Length(arguments[1], function)]),
            ("translatex", 1) => new(SvgAnimateTransformType.Translate, [Length(arguments[0], function), 0]),
            ("translatey", 1) => new(SvgAnimateTransformType.Translate, [0, Length(arguments[0], function)]),
            ("scale", 1) => new(SvgAnimateTransformType.Scale, [Number(arguments[0], function), Number(arguments[0], function)]),
            ("scale", 2) => new(SvgAnimateTransformType.Scale, [Number(arguments[0], function), Number(arguments[1], function)]),
            ("scalex", 1) => new(SvgAnimateTransformType.Scale, [Number(arguments[0], function), 1]),
            ("scaley", 1) => new(SvgAnimateTransformType.Scale, [1, Number(arguments[0], function)]),
            ("skewx", 1) => new(SvgAnimateTransformType.SkewX, [Angle(arguments[0], function)]),
            ("skewy", 1) => new(SvgAnimateTransformType.SkewY, [Angle(arguments[0], function)]),
            _ => throw new CursorConversionException($"The transform '{function}' is not supported; use rotate, translate, scale, skewX or skewY."),
        };
    }

    private static double Angle(string text, string function)
    {
        foreach ((string unit, double degrees) in new[] { ("deg", 1.0), ("grad", DegreesPerGradian), ("rad", DegreesPerRadian), ("turn", DegreesPerTurn) })
        {
            if (text.EndsWith(unit, StringComparison.OrdinalIgnoreCase))
                return Number(text[..^unit.Length], function) * degrees;
        }
        // CSS only allows a unitless angle when it is zero.
        return Number(text, function) == 0 ? 0 : throw new CursorConversionException($"The angle '{text}' in '{function}' needs a unit such as deg.");
    }

    private static double Length(string text, string function) =>
        Number(text.EndsWith("px", StringComparison.OrdinalIgnoreCase) ? text[..^2] : text, function);

    private static double Number(string text, string function) =>
        double.TryParse(text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double number) && double.IsFinite(number)
            ? number
            : throw new CursorConversionException($"The value '{text}' in '{function}' is not supported; use plain numbers, px lengths and deg, rad, grad or turn angles.");
}

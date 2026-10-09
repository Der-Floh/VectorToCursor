using Svg;

namespace VectorToCursor.Rendering.Animation.Css;

/// <summary>
/// Tag names for type selectors. An explicit map instead of reading <see cref="SvgElementAttribute"/> by reflection,
/// which NativeAOT trimming may not preserve.
/// </summary>
internal static class SvgElementNames
{
    public static string? NameOf(SvgElement element) => element switch
    {
        SvgPolyline => "polyline", // before SvgPolygon, which it derives from
        SvgPolygon => "polygon",
        SvgCircle => "circle",
        SvgEllipse => "ellipse",
        SvgRectangle => "rect",
        SvgLine => "line",
        SvgPath => "path",
        SvgGroup => "g",
        SvgUse => "use",
        SvgText => "text",
        SvgImage => "image",
        _ => null,
    };
}

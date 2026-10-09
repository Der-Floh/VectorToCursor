using Svg;
using Svg.Model.Services;

namespace VectorToCursor.Tests.Rendering.Animation;

/// <summary>Creates documents like the loader does: through SvgService, whose documents the animation engine can play.</summary>
internal static class TestSvg
{
    /// <summary>The arc of the busy cursor, which all four animations of <see cref="BusyCss"/> animate.</summary>
    public const string Arc = """<circle id="arc" class="arc" r="14.75" pathLength="360" fill="none" stroke="#30aa51" stroke-width="3.25" stroke-dasharray="270 90" />""";

    public const string BusyCss = """
        .arc { animation: spin 5.5s linear infinite, grow 1.1s cubic-bezier(.8, 0, .35, .8) infinite, move 1.1s cubic-bezier(.8, .2, .35, .7) infinite, color 5.5s step-end infinite; }
        @keyframes spin { to { transform: rotate(360deg); } }
        @keyframes grow { 0%, 100% { stroke-dasharray: 15 345; } 40% { stroke-dasharray: 270 90; } }
        @keyframes move { 0% { stroke-dashoffset: 0; animation-timing-function: linear; } 40% { stroke-dashoffset: -40; } 100% { stroke-dashoffset: -360; } }
        @keyframes color { 0% { stroke: #30aa51; } 20% { stroke: #ef4d38; } 40% { stroke: #458aff; } 60% { stroke: #ffbe00; } 80% { stroke: #ef4d38; } }
        """;

    public static SvgDocument Create(string css, string content)
    {
        string svg = $$"""<svg xmlns="http://www.w3.org/2000/svg" viewBox="-24 -24 48 48"><style>{{css}}</style>{{content}}</svg>""";
        return SvgService.FromSvg(svg, captureCompatibilityStyleState: false) ?? throw new InvalidOperationException("The test SVG could not be parsed.");
    }
}

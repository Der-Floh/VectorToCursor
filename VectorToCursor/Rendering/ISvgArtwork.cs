using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using VectorToCursor.Domain;

namespace VectorToCursor.Rendering;

/// <summary>A loaded SVG that can be rendered at any cursor size and, when animated, at any point of its animation.</summary>
internal interface ISvgArtwork : IDisposable
{
    /// <summary>The SVG's coordinate system: its viewBox, or its pixel size when it has none.</summary>
    ArtworkBounds Bounds { get; }

    /// <summary>The length of one loop of the SVG's animations, or <see langword="null"/> when it is static.</summary>
    TimeSpan? LoopDuration { get; }

    /// <summary>
    /// Renders the artwork as it looks at <paramref name="time"/> into a new square image of <c>fit.Size</c> pixels with
    /// straight (unpremultiplied) alpha. Static artwork ignores the time.
    /// </summary>
    Image<Rgba32> Render(SquareFit fit, TimeSpan time);
}

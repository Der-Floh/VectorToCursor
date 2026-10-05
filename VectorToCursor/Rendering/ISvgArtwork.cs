using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using VectorToCursor.Domain;

namespace VectorToCursor.Rendering;

/// <summary>A loaded SVG that can be rendered at any cursor size.</summary>
internal interface ISvgArtwork : IDisposable
{
    /// <summary>The SVG's coordinate system: its viewBox, or its pixel size when it has none.</summary>
    ArtworkBounds Bounds { get; }

    /// <summary>Renders the artwork into a new square image of <c>fit.Size</c> pixels with straight (unpremultiplied) alpha.</summary>
    Image<Rgba32> Render(SquareFit fit);
}

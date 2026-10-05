using VectorToCursor.Domain;

namespace VectorToCursor.Rendering;

/// <summary>Loads SVG files for rasterization.</summary>
internal interface ISvgLoader
{
    /// <exception cref="CursorConversionException">The file is not a usable SVG.</exception>
    ISvgArtwork Load(string path);
}

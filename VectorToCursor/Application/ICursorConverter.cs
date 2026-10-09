using VectorToCursor.Domain;

namespace VectorToCursor.Application;

/// <summary>Converts an SVG file into a multi-size Windows cursor, or an animated cursor when the SVG is animated.</summary>
internal interface ICursorConverter
{
    /// <exception cref="CursorConversionException">The SVG, the hotspot or the output path cannot be used.</exception>
    /// <exception cref="IOException">The cursor file could not be written.</exception>
    /// <exception cref="UnauthorizedAccessException">The cursor file could not be written.</exception>
    ConversionResult Convert(ConversionRequest request);
}

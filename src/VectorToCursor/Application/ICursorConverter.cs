using VectorToCursor.Domain;

namespace VectorToCursor.Application;

/// <summary>Converts an SVG file into a multi-size Windows cursor.</summary>
internal interface ICursorConverter
{
    /// <exception cref="CursorConversionException">The SVG or the hotspot cannot be used.</exception>
    /// <exception cref="IOException">The cursor file could not be written.</exception>
    /// <exception cref="UnauthorizedAccessException">The cursor file could not be written.</exception>
    ConversionResult Convert(ConversionRequest request);
}

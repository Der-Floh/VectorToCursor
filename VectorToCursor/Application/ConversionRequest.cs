using VectorToCursor.Domain;

namespace VectorToCursor.Application;

/// <param name="InputPath">The SVG file to convert.</param>
/// <param name="OutputPath">The cursor file to create or overwrite.</param>
/// <param name="Hotspot">The hotspot in SVG (viewBox) coordinates.</param>
internal sealed record ConversionRequest(string InputPath, string OutputPath, SvgPoint Hotspot);

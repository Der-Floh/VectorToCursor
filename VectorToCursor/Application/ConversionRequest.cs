using VectorToCursor.Domain;

namespace VectorToCursor.Application;

/// <param name="InputPath">The SVG file to convert.</param>
/// <param name="OutputPath">The cursor file to create or overwrite; <see langword="null"/> for the input path with .cur or .ani.</param>
/// <param name="Hotspot">The hotspot in SVG (viewBox) coordinates, shared by every animation frame.</param>
/// <param name="Bleed">How far around the artwork transparent pixels take the nearest edge color.</param>
/// <param name="FrameRate">How fast an animated SVG is sampled; ignored for static ones.</param>
/// <param name="Sizes">The sizes of the images in the cursor file.</param>
/// <param name="ImageFormat">
/// How every image in the cursor file is stored; <see langword="null"/> for <see cref="ImageFormats.StaticDefault"/> in a static
/// cursor, and in an animated one for <see cref="ImageFormats.AnimatedPreferred"/> when its frames fit, otherwise
/// <see cref="ImageFormats.AnimatedFallback"/>.
/// </param>
internal sealed record ConversionRequest(string InputPath, string? OutputPath, SvgPoint Hotspot, BleedPercentage Bleed, FrameRate FrameRate, CursorSizes Sizes, CursorImageFormat? ImageFormat);

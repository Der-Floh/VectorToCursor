using VectorToCursor.Domain;

namespace VectorToCursor.Application;

/// <param name="OutputPath">The full path of the written cursor file.</param>
/// <param name="Frames">The images in the cursor, smallest first, with the hotspot every animation frame shares.</param>
/// <param name="Animation">The animation that was written, or <see langword="null"/> for a static cursor.</param>
/// <param name="ImageFormat">How every image in the written file is stored.</param>
internal sealed record ConversionResult(string OutputPath, IReadOnlyList<FrameSummary> Frames, AnimationSummary? Animation, CursorImageFormat ImageFormat);

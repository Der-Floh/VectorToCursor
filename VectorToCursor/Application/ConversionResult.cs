namespace VectorToCursor.Application;

/// <param name="OutputPath">The full path of the written cursor file.</param>
/// <param name="Frames">The images in the cursor, smallest first.</param>
internal sealed record ConversionResult(string OutputPath, IReadOnlyList<FrameSummary> Frames);

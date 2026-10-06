using VectorToCursor.Cursors;
using VectorToCursor.Domain;
using VectorToCursor.Rendering;

namespace VectorToCursor.Application;

internal sealed class CursorConverter : ICursorConverter
{
    private readonly ISvgLoader _svgLoader;
    private readonly ICursorEncoder _cursorEncoder;

    public CursorConverter(ISvgLoader svgLoader, ICursorEncoder cursorEncoder)
    {
        ArgumentNullException.ThrowIfNull(svgLoader);
        ArgumentNullException.ThrowIfNull(cursorEncoder);

        _svgLoader = svgLoader;
        _cursorEncoder = cursorEncoder;
    }

    public ConversionResult Convert(ConversionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        string outputPath = Path.GetFullPath(request.OutputPath);
        using ISvgArtwork artwork = _svgLoader.Load(request.InputPath);
        List<SquareFit> fits = [.. CursorSizes.All.Select(size => SquareFit.Create(artwork.Bounds, size))];
        List<FrameSummary> summaries = [.. fits.Select(fit => new FrameSummary(fit.Size, fit.MapHotspot(request.Hotspot)))];

        List<CursorFrame> frames = [];
        try
        {
            for (int index = 0; index < fits.Count; index++)
            {
                CursorFrame frame = new(artwork.Render(fits[index]), summaries[index].Hotspot);
                frames.Add(frame);
                ColorBleed.Apply(frame.Image, request.Bleed.BandWidthFor(frame.Size));
            }

            WriteAtomically(outputPath, stream => _cursorEncoder.Encode(frames, stream));
        }
        finally
        {
            frames.ForEach(frame => frame.Dispose());
        }

        return new ConversionResult(outputPath, summaries);
    }

    // Writes next to the destination first, so a failed run never leaves a truncated cursor behind.
    private static void WriteAtomically(string outputPath, Action<Stream> write)
    {
        string directory = Path.GetDirectoryName(outputPath) ?? throw new CursorConversionException($"'{outputPath}' is not a valid file path.");
        Directory.CreateDirectory(directory);
        string temporaryPath = Path.Combine(directory, $".{Path.GetFileName(outputPath)}.{Guid.NewGuid():N}.tmp");
        try
        {
            using (FileStream stream = new(temporaryPath, FileMode.CreateNew, FileAccess.ReadWrite))
                write(stream);

            File.Move(temporaryPath, outputPath, overwrite: true);
        }
        catch
        {
            DeleteIfPossible(temporaryPath);
            throw;
        }
    }

    private static void DeleteIfPossible(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // The original failure matters more than a leftover temporary file.
        }
    }
}

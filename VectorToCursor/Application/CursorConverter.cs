using VectorToCursor.Cursors;
using VectorToCursor.Domain;
using VectorToCursor.Rendering;

namespace VectorToCursor.Application;

internal sealed class CursorConverter : ICursorConverter
{
    private readonly ISvgLoader _svgLoader;
    private readonly ICursorEncoder _cursorEncoder;
    private readonly IAnimatedCursorEncoder _animatedCursorEncoder;

    public CursorConverter(ISvgLoader svgLoader, ICursorEncoder cursorEncoder, IAnimatedCursorEncoder animatedCursorEncoder)
    {
        ArgumentNullException.ThrowIfNull(svgLoader);
        ArgumentNullException.ThrowIfNull(cursorEncoder);
        ArgumentNullException.ThrowIfNull(animatedCursorEncoder);

        _svgLoader = svgLoader;
        _cursorEncoder = cursorEncoder;
        _animatedCursorEncoder = animatedCursorEncoder;
    }

    public ConversionResult Convert(ConversionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        using ISvgArtwork artwork = _svgLoader.Load(request.InputPath);
        string outputPath = ResolveOutputPath(request, isAnimated: artwork.LoopDuration is not null);
        List<SquareFit> fits = [.. request.Sizes.Values.Select(size => SquareFit.Create(artwork.Bounds, size))];
        List<FrameSummary> summaries = [.. fits.Select(fit => new FrameSummary(fit.Size, fit.MapHotspot(request.Hotspot)))];

        if (artwork.LoopDuration is not TimeSpan loopDuration)
        {
            CursorImageFormat staticFormat = request.ImageFormat ?? ImageFormats.StaticDefault;
            WriteStaticCursor(artwork, fits, summaries, request, staticFormat, outputPath);
            return new ConversionResult(outputPath, summaries, Animation: null, staticFormat);
        }

        CursorImageFormat animatedFormat = request.ImageFormat ?? ChooseAnimatedFormat(artwork, fits, summaries, request.Bleed);
        AnimationSummary animation = WriteAnimatedCursor(artwork, fits, summaries, request, animatedFormat, loopDuration, outputPath);
        return new ConversionResult(outputPath, summaries, animation, animatedFormat);
    }

    private static string ResolveOutputPath(ConversionRequest request, bool isAnimated)
    {
        string extension = isAnimated ? CursorFileExtensions.Animated : CursorFileExtensions.Static;
        if (request.OutputPath is null)
            return Path.GetFullPath(Path.ChangeExtension(request.InputPath, extension));

        string outputPath = Path.GetFullPath(request.OutputPath);
        if (!string.Equals(Path.GetExtension(outputPath), extension, StringComparison.OrdinalIgnoreCase))
            throw new CursorConversionException($"'{request.InputPath}' is {(isAnimated ? "animated" : "not animated")}, so the output file must end in {extension}, but it is '{Path.GetFileName(outputPath)}'.");
        return outputPath;
    }

    private void WriteStaticCursor(ISvgArtwork artwork, IReadOnlyList<SquareFit> fits, IReadOnlyList<FrameSummary> summaries, ConversionRequest request, CursorImageFormat format, string outputPath)
    {
        byte[] cursor = EncodeCursor(artwork, fits, summaries, request.Bleed, TimeSpan.Zero, format);
        WriteAtomically(outputPath, stream => stream.Write(cursor));
    }

    // A BMP image has the same length in every frame, so when the first frame fits, all of them do.
    private CursorImageFormat ChooseAnimatedFormat(ISvgArtwork artwork, IReadOnlyList<SquareFit> fits, IReadOnlyList<FrameSummary> summaries, BleedPercentage bleed)
    {
        byte[] firstFrame = EncodeCursor(artwork, fits, summaries, bleed, TimeSpan.Zero, ImageFormats.AnimatedPreferred);
        return AniFrameLimit.FindImageBeyondLimit(firstFrame) is null ? ImageFormats.AnimatedPreferred : ImageFormats.AnimatedFallback;
    }

    // Each frame is checked as soon as it is encoded, so a frame Windows would refuse fails before the rest are rendered.
    private AnimationSummary WriteAnimatedCursor(ISvgArtwork artwork, IReadOnlyList<SquareFit> fits, IReadOnlyList<FrameSummary> summaries, ConversionRequest request, CursorImageFormat format, TimeSpan loopDuration, string outputPath)
    {
        AnimationTimeline timeline = AnimationTimeline.Create(loopDuration, request.FrameRate);
        List<byte[]> frames = new(timeline.FrameCount);
        for (int index = 0; index < timeline.FrameCount; index++)
        {
            byte[] frame = EncodeCursor(artwork, fits, summaries, request.Bleed, timeline.TimeOf(index), format);
            AniFrameLimit.EnsureFits(frame, index, format);
            frames.Add(frame);
        }

        WriteAtomically(outputPath, stream => _animatedCursorEncoder.Encode(frames, request.FrameRate, stream));
        return new AnimationSummary(timeline.FrameCount, request.FrameRate, loopDuration, timeline.EffectiveDuration);
    }

    // One complete .cur file: every size rendered at the same moment, with bled transparent pixels.
    private byte[] EncodeCursor(ISvgArtwork artwork, IReadOnlyList<SquareFit> fits, IReadOnlyList<FrameSummary> summaries, BleedPercentage bleed, TimeSpan time, CursorImageFormat format)
    {
        List<CursorFrame> frames = [];
        try
        {
            for (int index = 0; index < fits.Count; index++)
            {
                CursorFrame frame = new(artwork.Render(fits[index], time), summaries[index].Hotspot);
                frames.Add(frame);
                ColorBleed.Apply(frame.Image, bleed.BandWidthFor(frame.Size));
            }

            using MemoryStream stream = new();
            _cursorEncoder.Encode(frames, stream, format);
            return stream.ToArray();
        }
        finally
        {
            frames.ForEach(frame => frame.Dispose());
        }
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

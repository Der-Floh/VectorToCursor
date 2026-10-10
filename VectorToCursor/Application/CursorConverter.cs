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
        using ArtworkSnapshot snapshot = ArtworkSnapshot.Take(artwork, fits, TimeSpan.Zero);
        byte[] cursor = EncodeCursor(snapshot, summaries, request.Bleed, format);
        WriteAtomically(outputPath, stream => stream.Write(cursor));
    }

    // A BMP image has the same length in every frame, so when the first frame fits, all of them do.
    private CursorImageFormat ChooseAnimatedFormat(ISvgArtwork artwork, IReadOnlyList<SquareFit> fits, IReadOnlyList<FrameSummary> summaries, BleedPercentage bleed)
    {
        using ArtworkSnapshot snapshot = ArtworkSnapshot.Take(artwork, fits, TimeSpan.Zero);
        byte[] firstFrame = EncodeCursor(snapshot, summaries, bleed, ImageFormats.AnimatedPreferred);
        return AniFrameLimit.FindImageBeyondLimit(firstFrame) is null ? ImageFormats.AnimatedPreferred : ImageFormats.AnimatedFallback;
    }

    private AnimationSummary WriteAnimatedCursor(ISvgArtwork artwork, IReadOnlyList<SquareFit> fits, IReadOnlyList<FrameSummary> summaries, ConversionRequest request, CursorImageFormat format, TimeSpan loopDuration, string outputPath)
    {
        AnimationTimeline timeline = AnimationTimeline.Create(loopDuration, request.FrameRate);
        AnimationSequence sequence = SampleAnimation(artwork, fits, summaries, request.Bleed, format, timeline);

        WriteAtomically(outputPath, stream => _animatedCursorEncoder.Encode(sequence, stream));
        return new AnimationSummary(timeline.FrameCount, sequence.Frames.Count, request.FrameRate, loopDuration, timeline.EffectiveDuration);
    }

    // A frame that looks exactly like the previous one reuses its bytes and skips the costly bleeding and encoding. Every new
    // frame is checked as soon as it is encoded, so a frame Windows would refuse fails before the rest are rendered.
    private AnimationSequence SampleAnimation(ISvgArtwork artwork, IReadOnlyList<SquareFit> fits, IReadOnlyList<FrameSummary> summaries, BleedPercentage bleed, CursorImageFormat format, AnimationTimeline timeline)
    {
        AnimationSequence sequence = new();
        ArtworkSnapshot? current = null;
        try
        {
            byte[] frame = [];
            for (int index = 0; index < timeline.FrameCount; index++)
            {
                ArtworkSnapshot? previous = current;
                current = ArtworkSnapshot.Take(artwork, fits, timeline.TimeOf(index));
                using (previous)
                {
                    if (previous is null || !current.HasSamePixelsAs(previous))
                    {
                        frame = EncodeCursor(current, summaries, bleed, format);
                        AniFrameLimit.EnsureFits(frame, index, format);
                    }
                }
                sequence.Add(frame, timeline.FrameRate.Jiffies);
            }
            return sequence;
        }
        finally
        {
            current?.Dispose();
        }
    }

    // One complete .cur file: every size of the snapshot, with bled transparent pixels; the snapshot itself stays unchanged.
    private byte[] EncodeCursor(ArtworkSnapshot snapshot, IReadOnlyList<FrameSummary> summaries, BleedPercentage bleed, CursorImageFormat format)
    {
        List<CursorFrame> frames = [];
        try
        {
            for (int index = 0; index < snapshot.Images.Count; index++)
            {
                CursorFrame frame = new(snapshot.Images[index].Clone(), summaries[index].Hotspot);
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

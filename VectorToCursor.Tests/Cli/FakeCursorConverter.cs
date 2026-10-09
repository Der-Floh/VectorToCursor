using VectorToCursor.Application;
using VectorToCursor.Domain;

namespace VectorToCursor.Tests.Cli;

internal sealed class FakeCursorConverter : ICursorConverter
{
    private readonly Exception? _failure;

    public FakeCursorConverter(Exception? failure = null) => _failure = failure;

    public ConversionRequest? LastRequest { get; private set; }

    /// <summary>What the fake reports as written; <see langword="null"/> pretends the SVG was static.</summary>
    public AnimationSummary? Animation { get; init; }

    /// <summary>The image format the fake reports as written.</summary>
    public CursorImageFormat ImageFormat { get; init; } = CursorImageFormat.Bmp;

    public ConversionResult Convert(ConversionRequest request)
    {
        LastRequest = request;
        if (_failure is not null)
            throw _failure;

        string outputPath = request.OutputPath ?? Path.ChangeExtension(request.InputPath, Animation is null ? CursorFileExtensions.Static : CursorFileExtensions.Animated);
        return new ConversionResult(outputPath, [new FrameSummary(32, new PixelHotspot(3, 2)), new FrameSummary(256, new PixelHotspot(24, 16))], Animation, ImageFormat);
    }
}

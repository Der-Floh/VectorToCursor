using VectorToCursor.Application;
using VectorToCursor.Domain;

namespace VectorToCursor.Tests.Cli;

internal sealed class FakeCursorConverter : ICursorConverter
{
    private readonly Exception? _failure;

    public FakeCursorConverter(Exception? failure = null) => _failure = failure;

    public ConversionRequest? LastRequest { get; private set; }

    public ConversionResult Convert(ConversionRequest request)
    {
        LastRequest = request;
        if (_failure is not null)
            throw _failure;

        return new ConversionResult(request.OutputPath, [new FrameSummary(32, new PixelHotspot(3, 2)), new FrameSummary(256, new PixelHotspot(24, 16))]);
    }
}

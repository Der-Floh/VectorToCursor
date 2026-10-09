using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SkiaSharp;
using Svg.Skia;
using VectorToCursor.Domain;

namespace VectorToCursor.Rendering;

/// <summary>An SVG loaded by Svg.Skia and rasterized with SkiaSharp; animations are played by Svg.Skia's SMIL engine.</summary>
internal sealed class SkiaSvgArtwork : ISvgArtwork
{
    // At an exact iteration boundary the engine shows the end of the finished iteration, but a loop needs the next one's start.
    private static readonly TimeSpan BoundaryNudge = TimeSpan.FromTicks(1);

    private readonly SKSvg _svg;
    private TimeSpan? _shownTime;
    private bool _disposed;

    /// <param name="svg">Draws in SVG coordinates shifted so that the bounds' origin is at (0, 0); disposed with this artwork.</param>
    /// <param name="loopDuration">The animation loop length, or <see langword="null"/> for static artwork.</param>
    public SkiaSvgArtwork(SKSvg svg, ArtworkBounds bounds, TimeSpan? loopDuration)
    {
        ArgumentNullException.ThrowIfNull(svg);
        ArgumentNullException.ThrowIfNull(bounds);

        _svg = svg;
        Bounds = bounds;
        LoopDuration = loopDuration;
    }

    public ArtworkBounds Bounds { get; }

    public TimeSpan? LoopDuration { get; }

    public Image<Rgba32> Render(SquareFit fit, TimeSpan time)
    {
        ArgumentNullException.ThrowIfNull(fit);
        ObjectDisposedException.ThrowIf(_disposed, this);

        SKPicture picture = PictureAt(time);
        SKImageInfo info = new(fit.Size, fit.Size, SKColorType.Rgba8888, SKAlphaType.Premul);
        using SKSurface surface = SKSurface.Create(info) ?? throw new InvalidOperationException($"SkiaSharp could not create a {fit.Size}x{fit.Size} px surface.");
        Draw(surface.Canvas, fit, picture);
        return ReadUnpremultiplied(surface, fit.Size);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _svg.Dispose();
    }

    // Seeking re-renders the document and disposes the previous picture, so the current one is fetched after every seek.
    private SKPicture PictureAt(TimeSpan time)
    {
        if (LoopDuration is not null && time != _shownTime)
        {
            _svg.SetAnimationTime(time + BoundaryNudge);
            _shownTime = time;
        }
        return _svg.Picture ?? throw new InvalidOperationException("Svg.Skia produced no picture for the SVG.");
    }

    private void Draw(SKCanvas canvas, SquareFit fit, SKPicture picture)
    {
        canvas.Clear(SKColors.Transparent);
        canvas.Translate((float)fit.OffsetX, (float)fit.OffsetY);
        canvas.Scale((float)fit.Scale);
        // Keeps overflow="visible" content out of the margins around non-square artwork.
        canvas.ClipRect(SKRect.Create((float)Bounds.Width, (float)Bounds.Height));
        canvas.DrawPicture(picture);
    }

    // Skia draws premultiplied; reading back unpremultiplied keeps anti-aliased edges from turning dark.
    private static Image<Rgba32> ReadUnpremultiplied(SKSurface surface, int size)
    {
        using SKBitmap bitmap = new(new SKImageInfo(size, size, SKColorType.Rgba8888, SKAlphaType.Unpremul));
        if (!surface.ReadPixels(bitmap.Info, bitmap.GetPixels(), bitmap.RowBytes, 0, 0))
            throw new InvalidOperationException("SkiaSharp could not read back the rendered pixels.");

        return Image.LoadPixelData<Rgba32>(bitmap.GetPixelSpan(), size, size);
    }
}

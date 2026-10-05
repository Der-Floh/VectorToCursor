using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SkiaSharp;
using Svg.Skia;
using VectorToCursor.Domain;

namespace VectorToCursor.Rendering;

/// <summary>An SVG loaded by Svg.Skia and rasterized with SkiaSharp.</summary>
internal sealed class SkiaSvgArtwork : ISvgArtwork
{
    private readonly SKSvg _svg;
    private readonly SKPicture _picture;
    private bool _disposed;

    /// <param name="svg">Owns <paramref name="picture"/>; disposed together with this artwork.</param>
    /// <param name="picture">Drawn in SVG coordinates shifted so that the bounds' origin is at (0, 0).</param>
    public SkiaSvgArtwork(SKSvg svg, SKPicture picture, ArtworkBounds bounds)
    {
        ArgumentNullException.ThrowIfNull(svg);
        ArgumentNullException.ThrowIfNull(picture);
        ArgumentNullException.ThrowIfNull(bounds);

        _svg = svg;
        _picture = picture;
        Bounds = bounds;
    }

    public ArtworkBounds Bounds { get; }

    public Image<Rgba32> Render(SquareFit fit)
    {
        ArgumentNullException.ThrowIfNull(fit);
        ObjectDisposedException.ThrowIf(_disposed, this);

        SKImageInfo info = new(fit.Size, fit.Size, SKColorType.Rgba8888, SKAlphaType.Premul);
        using SKSurface surface = SKSurface.Create(info) ?? throw new InvalidOperationException($"SkiaSharp could not create a {fit.Size}x{fit.Size} px surface.");
        Draw(surface.Canvas, fit);
        return ReadUnpremultiplied(surface, fit.Size);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _svg.Dispose();
    }

    private void Draw(SKCanvas canvas, SquareFit fit)
    {
        canvas.Clear(SKColors.Transparent);
        canvas.Translate((float)fit.OffsetX, (float)fit.OffsetY);
        canvas.Scale((float)fit.Scale);
        // Keeps overflow="visible" content out of the margins around non-square artwork.
        canvas.ClipRect(SKRect.Create((float)Bounds.Width, (float)Bounds.Height));
        canvas.DrawPicture(_picture);
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

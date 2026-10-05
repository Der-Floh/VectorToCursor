using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using VectorToCursor.Domain;

namespace VectorToCursor.Cursors;

/// <summary>One square cursor image and its hotspot. Owns the image.</summary>
internal sealed class CursorFrame : IDisposable
{
    public CursorFrame(Image<Rgba32> image, PixelHotspot hotspot)
    {
        ArgumentNullException.ThrowIfNull(image);
        if (image.Width != image.Height)
            throw new ArgumentException($"A cursor frame must be square, but the image is {image.Width}x{image.Height} px.", nameof(image));
        if (hotspot.X < 0 || hotspot.X >= image.Width || hotspot.Y < 0 || hotspot.Y >= image.Height)
            throw new ArgumentOutOfRangeException(nameof(hotspot), hotspot, $"The hotspot must lie inside the {image.Width}x{image.Height} px image.");

        Image = image;
        Hotspot = hotspot;
    }

    public Image<Rgba32> Image { get; }

    public PixelHotspot Hotspot { get; }

    public int Size => Image.Width;

    public void Dispose() => Image.Dispose();
}

using System.Runtime.InteropServices;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using VectorToCursor.Domain;

namespace VectorToCursor.Rendering;

/// <summary>The artwork rendered at one moment, one image per size. Owns the images.</summary>
internal sealed class ArtworkSnapshot : IDisposable
{
    private readonly List<Image<Rgba32>> _images;

    private ArtworkSnapshot(List<Image<Rgba32>> images) => _images = images;

    /// <summary>One image per fit, in the order of the fits.</summary>
    public IReadOnlyList<Image<Rgba32>> Images => _images;

    public static ArtworkSnapshot Take(ISvgArtwork artwork, IReadOnlyList<SquareFit> fits, TimeSpan time)
    {
        ArgumentNullException.ThrowIfNull(artwork);
        ArgumentNullException.ThrowIfNull(fits);

        List<Image<Rgba32>> images = new(fits.Count);
        try
        {
            foreach (SquareFit fit in fits)
                images.Add(artwork.Render(fit, time));
            return new ArtworkSnapshot(images);
        }
        catch
        {
            images.ForEach(image => image.Dispose());
            throw;
        }
    }

    /// <summary>Whether both snapshots hold images of the same sizes that match pixel for pixel.</summary>
    public bool HasSamePixelsAs(ArtworkSnapshot other)
    {
        ArgumentNullException.ThrowIfNull(other);

        return _images.Count == other._images.Count && _images.Zip(other._images).All(pair => HaveSamePixels(pair.First, pair.Second));
    }

    public void Dispose() => _images.ForEach(image => image.Dispose());

    private static bool HaveSamePixels(Image<Rgba32> first, Image<Rgba32> second)
    {
        if (first.Size != second.Size)
            return false;

        bool same = true;
        first.ProcessPixelRows(second, (firstRows, secondRows) =>
        {
            for (int y = 0; same && y < firstRows.Height; y++)
                same = MemoryMarshal.AsBytes(firstRows.GetRowSpan(y)).SequenceEqual(MemoryMarshal.AsBytes(secondRows.GetRowSpan(y)));
        });
        return same;
    }
}

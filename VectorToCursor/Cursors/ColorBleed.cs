using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace VectorToCursor.Cursors;

/// <summary>
/// Colors the transparent pixels around the artwork. Windows scales cursors without premultiplying alpha, so the color of
/// transparent pixels bleeds into the edges; left black, it shows up as a dark fringe.
/// </summary>
internal static class ColorBleed
{
    // Below this alpha, a pixel's un-premultiplied color is mostly rounding noise and must not spread.
    internal const byte MinimumColorAlpha = 16;

    /// <summary>
    /// Pixels within <paramref name="bandWidth"/> pixels of the artwork copy the nearest edge color; all farther ones share the
    /// average color of the band's outermost ring. Only pixels without a reliable color of their own change, and alpha never
    /// changes. A band width of 0 leaves the image untouched.
    /// </summary>
    public static void Apply(Image<Rgba32> image, int bandWidth)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentOutOfRangeException.ThrowIfNegative(bandWidth);
        if (bandWidth == 0)
            return;

        Rgba32[] pixels = new Rgba32[image.Width * image.Height];
        image.CopyPixelDataTo(pixels);
        if (!new BleedPass(pixels, image.Width, image.Height).TryRun(bandWidth))
            return;

        image.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < accessor.Height; y++)
                pixels.AsSpan(y * accessor.Width, accessor.Width).CopyTo(accessor.GetRowSpan(y));
        });
    }

    private sealed class BleedPass
    {
        private const int Source = 0;
        private const int Unreached = -1;

        // Straight neighbors first, so pixels equally near two edges follow the axes instead of skewing diagonally.
        private static readonly (int Dx, int Dy)[] NeighborOffsets = [(0, -1), (-1, 0), (1, 0), (0, 1), (-1, -1), (1, -1), (-1, 1), (1, 1)];

        private readonly Rgba32[] _pixels;
        private readonly int _width;
        private readonly int _height;
        private readonly int[] _ring;
        private readonly Rgba32[] _colors;

        public BleedPass(Rgba32[] pixels, int width, int height)
        {
            _pixels = pixels;
            _width = width;
            _height = height;
            _ring = new int[pixels.Length];
            Array.Fill(_ring, Unreached);
            _colors = [.. pixels];
        }

        /// <returns><see langword="false"/> when the image has no visible pixel to take colors from.</returns>
        public bool TryRun(int bandWidth)
        {
            List<int> sources = FindSources();
            if (sources.Count == 0)
                return false;

            sources.ForEach(index => _ring[index] = Source);
            List<int> outermostRing = SpreadBand(sources, bandWidth);
            FillBeyondBand(Average(outermostRing));
            WriteTargetColors();
            return true;
        }

        // Pixels whose own color is reliable; when the whole artwork is fainter than that, every visible pixel counts.
        private List<int> FindSources()
        {
            List<int> reliable = IndicesWhere(pixel => pixel.A >= MinimumColorAlpha);
            return reliable.Count > 0 ? reliable : IndicesWhere(pixel => pixel.A > 0);
        }

        private List<int> IndicesWhere(Func<Rgba32, bool> predicate) => [.. Enumerable.Range(0, _pixels.Length).Where(index => predicate(_pixels[index]))];

        // Ring k only reads ring k - 1, so the result does not depend on the order in which pixels are discovered.
        private List<int> SpreadBand(List<int> sources, int bandWidth)
        {
            List<int> previous = sources;
            for (int distance = 1; distance <= bandWidth; distance++)
            {
                List<int> current = DiscoverRing(previous, distance);
                if (current.Count == 0)
                    break;

                foreach (int index in current)
                    _colors[index] = distance == 1 ? MostOpaqueSourceNeighbor(index) : _colors[FirstNeighborInRing(index, distance - 1)];
                previous = current;
            }
            return previous;
        }

        private List<int> DiscoverRing(List<int> previous, int distance)
        {
            List<int> ring = [];
            foreach (int index in previous)
            {
                foreach (int neighbor in Neighbors(index))
                {
                    if (_ring[neighbor] != Unreached)
                        continue;

                    _ring[neighbor] = distance;
                    ring.Add(neighbor);
                }
            }
            return ring;
        }

        // Faint anti-aliasing pixels carry the least reliable color, so the first ring copies from the most opaque edge pixel.
        private Rgba32 MostOpaqueSourceNeighbor(int index) =>
            _pixels[Neighbors(index).Where(neighbor => _ring[neighbor] == Source).MaxBy(neighbor => _pixels[neighbor].A)];

        private int FirstNeighborInRing(int index, int ring) => Neighbors(index).First(neighbor => _ring[neighbor] == ring);

        // No scaler reads that far from an edge, so one flat color keeps the area compressible.
        private void FillBeyondBand(Rgba32 color)
        {
            for (int index = 0; index < _ring.Length; index++)
            {
                if (_ring[index] == Unreached)
                    _colors[index] = color;
            }
        }

        private Rgba32 Average(List<int> indices)
        {
            double red = 0, green = 0, blue = 0;
            foreach (int index in indices)
            {
                red += _colors[index].R;
                green += _colors[index].G;
                blue += _colors[index].B;
            }
            return new Rgba32(ToByte(red / indices.Count), ToByte(green / indices.Count), ToByte(blue / indices.Count));
        }

        private static byte ToByte(double channel) => (byte)Math.Round(channel);

        private void WriteTargetColors()
        {
            for (int index = 0; index < _pixels.Length; index++)
            {
                if (_ring[index] != Source)
                    _pixels[index] = new Rgba32(_colors[index].R, _colors[index].G, _colors[index].B, _pixels[index].A);
            }
        }

        private IEnumerable<int> Neighbors(int index)
        {
            int x = index % _width;
            int y = index / _width;
            foreach ((int dx, int dy) in NeighborOffsets)
            {
                int neighborX = x + dx;
                int neighborY = y + dy;
                if (neighborX >= 0 && neighborY >= 0 && neighborX < _width && neighborY < _height)
                    yield return neighborY * _width + neighborX;
            }
        }
    }
}

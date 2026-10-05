using System.Globalization;

namespace VectorToCursor.Domain;

/// <summary>
/// Places the artwork into a square cursor image: scaled to fit while keeping its aspect ratio, and centered on
/// whole pixels. Rendering and hotspot mapping both use it, so the hotspot always matches the drawn artwork.
/// </summary>
internal sealed class SquareFit
{
    // Absorbs floating-point noise such as 0.29 * 100 = 28.999999999999996 before flooring to whole pixels.
    private const double Epsilon = 1e-9;

    private SquareFit(ArtworkBounds bounds, int size, double scale)
    {
        Bounds = bounds;
        Size = size;
        Scale = scale;
        OffsetX = CenteringOffset(size, bounds.Width * scale);
        OffsetY = CenteringOffset(size, bounds.Height * scale);
    }

    public ArtworkBounds Bounds { get; }

    /// <summary>Width and height of the cursor image in pixels.</summary>
    public int Size { get; }

    /// <summary>Pixels per SVG user unit.</summary>
    public double Scale { get; }

    /// <summary>Whole-pixel margin left of the artwork.</summary>
    public double OffsetX { get; }

    /// <summary>Whole-pixel margin above the artwork.</summary>
    public double OffsetY { get; }

    public static SquareFit Create(ArtworkBounds bounds, int size)
    {
        ArgumentNullException.ThrowIfNull(bounds);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(size);

        return new SquareFit(bounds, size, Math.Min(size / bounds.Width, size / bounds.Height));
    }

    /// <summary>Returns the pixel that contains <paramref name="point"/>.</summary>
    /// <exception cref="CursorConversionException">The point lies outside the cursor image.</exception>
    public PixelHotspot MapHotspot(SvgPoint point)
    {
        double x = ToPixelSpace(point.X, Bounds.MinX, OffsetX);
        double y = ToPixelSpace(point.Y, Bounds.MinY, OffsetY);
        if (!IsInsideImage(x) || !IsInsideImage(y))
            throw new CursorConversionException(DescribeOutOfRange(point));

        return new PixelHotspot(ToPixelIndex(x), ToPixelIndex(y));
    }

    private static double CenteringOffset(int size, double extent) => Math.Max(0, Math.Floor((size - extent) / 2 + Epsilon));

    private double ToPixelSpace(double coordinate, double min, double offset) => (coordinate - min) * Scale + offset;

    private double ToSvgSpace(double pixel, double min, double offset) => min + (pixel - offset) / Scale;

    private bool IsInsideImage(double pixel) => pixel >= -Epsilon && pixel <= Size + Epsilon;

    private int ToPixelIndex(double pixel) => Math.Clamp((int)Math.Floor(pixel + Epsilon), 0, Size - 1);

    private string DescribeOutOfRange(SvgPoint point)
    {
        double minX = ToSvgSpace(0, Bounds.MinX, OffsetX);
        double maxX = ToSvgSpace(Size, Bounds.MinX, OffsetX);
        double minY = ToSvgSpace(0, Bounds.MinY, OffsetY);
        double maxY = ToSvgSpace(Size, Bounds.MinY, OffsetY);
        return string.Create(CultureInfo.InvariantCulture, $"The hotspot ({point.X}, {point.Y}) lies outside the {Size}x{Size} px cursor. In SVG coordinates it must be within x {minX:0.###} to {maxX:0.###} and y {minY:0.###} to {maxY:0.###}.");
    }
}

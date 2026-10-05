using VectorToCursor.Domain;

namespace VectorToCursor.Tests.Domain;

public sealed class SquareFitTests
{
    [Theory]
    [InlineData(32, 3, 2)]
    [InlineData(48, 4, 3)]
    [InlineData(64, 6, 4)]
    [InlineData(96, 9, 6)]
    [InlineData(128, 12, 8)]
    [InlineData(256, 24, 16)]
    public void MapHotspot_SquareViewBox_ScalesToEveryCursorSize(int size, int expectedX, int expectedY)
    {
        SquareFit fit = SquareFit.Create(new ArtworkBounds(0, 0, 32, 32), size);

        PixelHotspot hotspot = fit.MapHotspot(new SvgPoint(3, 2));

        Assert.Equal(new PixelHotspot(expectedX, expectedY), hotspot);
    }

    [Fact]
    public void Create_TallViewBox_CentersHorizontallyOnWholePixels()
    {
        SquareFit fit = SquareFit.Create(new ArtworkBounds(0, 0, 15, 32), 32);

        Assert.Equal(1, fit.Scale);
        Assert.Equal(8, fit.OffsetX);
        Assert.Equal(0, fit.OffsetY);
    }

    [Fact]
    public void Create_WideViewBox_CentersVertically()
    {
        SquareFit fit = SquareFit.Create(new ArtworkBounds(0, 0, 64, 32), 32);

        Assert.Equal(0.5, fit.Scale);
        Assert.Equal(0, fit.OffsetX);
        Assert.Equal(8, fit.OffsetY);
    }

    [Fact]
    public void Create_ExtentRoundsAboveSize_KeepsOffsetAtZero()
    {
        // 47.3 * (48 / 47.3) evaluates to 48.000000000000007.
        SquareFit fit = SquareFit.Create(new ArtworkBounds(0, 0, 47.3, 47.3), 48);

        Assert.Equal(0, fit.OffsetX);
        Assert.Equal(0, fit.OffsetY);
    }

    [Fact]
    public void MapHotspot_OffsetViewBox_IsRelativeToViewBoxOrigin()
    {
        SquareFit fit = SquareFit.Create(new ArtworkBounds(-16, -16, 32, 32), 64);

        Assert.Equal(new PixelHotspot(0, 0), fit.MapHotspot(new SvgPoint(-16, -16)));
        Assert.Equal(new PixelHotspot(32, 32), fit.MapHotspot(new SvgPoint(0, 0)));
    }

    [Fact]
    public void MapHotspot_TallViewBox_IncludesCenteringOffset()
    {
        SquareFit fit = SquareFit.Create(new ArtworkBounds(0, 0, 16, 32), 32);

        Assert.Equal(new PixelHotspot(8, 0), fit.MapHotspot(new SvgPoint(0, 0)));
    }

    [Fact]
    public void MapHotspot_PointOnFarEdge_ClampsToLastPixel()
    {
        SquareFit fit = SquareFit.Create(new ArtworkBounds(0, 0, 32, 32), 32);

        Assert.Equal(new PixelHotspot(31, 31), fit.MapHotspot(new SvgPoint(32, 32)));
    }

    [Fact]
    public void MapHotspot_FloatingPointNoise_DoesNotFloorOnePixelLow()
    {
        // 0.29 * 100 evaluates to 28.999999999999996.
        SquareFit fit = SquareFit.Create(new ArtworkBounds(0, 0, 1, 1), 100);

        Assert.Equal(new PixelHotspot(29, 29), fit.MapHotspot(new SvgPoint(0.29, 0.29)));
    }

    [Theory]
    [InlineData(-0.5, 0)]
    [InlineData(0, 32.5)]
    [InlineData(double.NaN, 0)]
    public void MapHotspot_PointOutsideImage_ThrowsConversionException(double x, double y)
    {
        SquareFit fit = SquareFit.Create(new ArtworkBounds(0, 0, 32, 32), 32);

        CursorConversionException exception = Assert.Throws<CursorConversionException>(() => fit.MapHotspot(new SvgPoint(x, y)));

        Assert.Contains("x 0 to 32 and y 0 to 32", exception.Message);
    }

    [Fact]
    public void MapHotspot_PointInMarginOfTallViewBox_IsAccepted()
    {
        SquareFit fit = SquareFit.Create(new ArtworkBounds(0, 0, 16, 32), 32);

        Assert.Equal(new PixelHotspot(0, 0), fit.MapHotspot(new SvgPoint(-8, 0)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-32)]
    public void Create_NonPositiveSize_Throws(int size)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => SquareFit.Create(new ArtworkBounds(0, 0, 32, 32), size));
    }
}

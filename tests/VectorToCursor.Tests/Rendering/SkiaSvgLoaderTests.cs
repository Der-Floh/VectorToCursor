using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using VectorToCursor.Domain;
using VectorToCursor.Rendering;

namespace VectorToCursor.Tests.Rendering;

public sealed class SkiaSvgLoaderTests
{
    private const byte Opaque = 255;
    private const byte Transparent = 0;

    private readonly SkiaSvgLoader _loader = new();

    [Theory]
    [InlineData(32)]
    [InlineData(48)]
    [InlineData(64)]
    [InlineData(96)]
    [InlineData(128)]
    [InlineData(256)]
    public void Render_DrawsArtworkScaledToSize(int size)
    {
        using Image<Rgba32> image = Render("left-half.svg", size);

        Assert.Equal(new Rgba32(0, 0, 0, Opaque), image[size / 4, size / 2]);
        Assert.Equal(Transparent, image[size * 3 / 4, size / 2].A);
    }

    [Theory]
    [InlineData("left-half-scaled.svg")]
    [InlineData("left-half-offset.svg")]
    [InlineData("left-half-no-viewbox.svg")]
    public void Render_EquivalentCoordinateSystems_ProduceIdenticalPixels(string fileName)
    {
        foreach (int size in CursorSizes.All)
        {
            using Image<Rgba32> expected = Render("left-half.svg", size);
            using Image<Rgba32> actual = Render(fileName, size);

            AssertPixelsEqual(expected, actual);
        }
    }

    [Fact]
    public void Load_ViewBox_DefinesBounds()
    {
        using ISvgArtwork artwork = _loader.Load(TestFiles.PathOf("left-half-offset.svg"));

        Assert.Equal(new ArtworkBounds(-16, -16, 32, 32), artwork.Bounds);
    }

    [Fact]
    public void Load_ViewBoxWithDifferentSize_UsesViewBoxUnits()
    {
        using ISvgArtwork artwork = _loader.Load(TestFiles.PathOf("left-half-scaled.svg"));

        Assert.Equal(new ArtworkBounds(0, 0, 32, 32), artwork.Bounds);
    }

    [Fact]
    public void Load_NoViewBox_UsesPixelSize()
    {
        using ISvgArtwork artwork = _loader.Load(TestFiles.PathOf("left-half-no-viewbox.svg"));

        Assert.Equal(new ArtworkBounds(0, 0, 32, 32), artwork.Bounds);
    }

    [Fact]
    public void Render_TallArtwork_IsCenteredWithTransparentMargins()
    {
        using Image<Rgba32> image = Render("tall.svg", 32);

        Assert.Equal(Transparent, image[7, 16].A);
        Assert.Equal(Opaque, image[8, 16].A);
        Assert.Equal(Opaque, image[23, 16].A);
        Assert.Equal(Transparent, image[24, 16].A);
    }

    [Fact]
    public void Render_OverflowVisibleContent_StaysOutOfMargins()
    {
        using Image<Rgba32> image = Render("overflow-visible.svg", 32);

        Assert.Equal(Transparent, image[0, 16].A);
        Assert.Equal(Opaque, image[16, 16].A);
        Assert.Equal(Transparent, image[31, 16].A);
    }

    [Fact]
    public void Render_SemiTransparentFill_HasStraightAlpha()
    {
        using Image<Rgba32> image = Render("semi-transparent.svg", 32);

        Rgba32 pixel = image[16, 16];
        Assert.InRange(pixel.R, (byte)254, (byte)255);
        Assert.InRange(pixel.A, (byte)127, (byte)128);
    }

    [Fact]
    public void Render_MissingImage_DrawsNoPlaceholder()
    {
        using Image<Rgba32> image = Render("missing-image.svg", 32);

        Assert.True(IsFullyTransparent(image));
    }

    [Theory]
    [InlineData("malformed.svg")]
    [InlineData("no-size.svg")]
    [InlineData("degenerate-viewbox.svg")]
    [InlineData("root-transform.svg")]
    public void Load_UnusableSvg_ThrowsConversionException(string fileName)
    {
        Assert.Throws<CursorConversionException>(() => _loader.Load(TestFiles.PathOf(fileName)));
    }

    [Fact]
    public void Render_AfterDispose_Throws()
    {
        ISvgArtwork artwork = _loader.Load(TestFiles.PathOf("left-half.svg"));
        SquareFit fit = SquareFit.Create(artwork.Bounds, 32);
        artwork.Dispose();

        Assert.Throws<ObjectDisposedException>(() => artwork.Render(fit));
    }

    private Image<Rgba32> Render(string fileName, int size)
    {
        using ISvgArtwork artwork = _loader.Load(TestFiles.PathOf(fileName));
        return artwork.Render(SquareFit.Create(artwork.Bounds, size));
    }

    private static void AssertPixelsEqual(Image<Rgba32> expected, Image<Rgba32> actual)
    {
        Assert.Equal(expected.Size, actual.Size);
        for (int y = 0; y < expected.Height; y++)
        {
            for (int x = 0; x < expected.Width; x++)
                Assert.Equal(expected[x, y], actual[x, y]);
        }
    }

    private static bool IsFullyTransparent(Image<Rgba32> image)
    {
        bool transparent = true;
        image.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < accessor.Height && transparent; y++)
            {
                foreach (Rgba32 pixel in accessor.GetRowSpan(y))
                    transparent &= pixel.A == Transparent;
            }
        });
        return transparent;
    }
}

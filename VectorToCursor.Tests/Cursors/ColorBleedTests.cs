using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using VectorToCursor.Cursors;

namespace VectorToCursor.Tests.Cursors;

public sealed class ColorBleedTests
{
    private static readonly Rgba32 Red = new(255, 0, 0, 255);
    private static readonly Rgba32 Blue = new(0, 0, 255, 255);

    [Fact]
    public void Apply_CopiesNearestEdgeColorWithinBand()
    {
        using Image<Rgba32> image = new(32, 32);
        Fill(image, 0, 0, 3, 31, Red);
        Fill(image, 28, 0, 31, 31, Blue);

        ColorBleed.Apply(image, 3);

        Assert.Equal(new Rgba32(255, 0, 0, 0), image[5, 10]);
        Assert.Equal(new Rgba32(0, 0, 255, 0), image[26, 10]);
    }

    [Fact]
    public void Apply_GivesPixelsBeyondBandTheAverageColorOfTheBandEdge()
    {
        using Image<Rgba32> image = new(32, 32);
        Fill(image, 0, 0, 3, 3, Red);
        Fill(image, 28, 28, 31, 31, Blue);

        ColorBleed.Apply(image, 2);

        Rgba32 beyondBand = image[16, 16];
        Assert.True(beyondBand.R > 0 && beyondBand.B > 0, $"Expected a mix of red and blue, got {beyondBand}.");
        Assert.Equal(beyondBand, image[31, 0]);
        Assert.Equal(beyondBand, image[0, 31]);
    }

    [Fact]
    public void Apply_SingleColorArtwork_ColorsEveryTransparentPixel()
    {
        using Image<Rgba32> image = new(32, 32);
        Fill(image, 12, 12, 19, 19, Red);

        ColorBleed.Apply(image, 2);

        Assert.All(Pixels(image), pixel => Assert.Equal(new Rgba32(255, 0, 0, pixel.A), pixel));
    }

    [Fact]
    public void Apply_KeepsAlphaAndReliableColors()
    {
        using Image<Rgba32> image = new(16, 16);
        Fill(image, 4, 4, 11, 11, new Rgba32(10, 200, 30, 255));
        image[3, 4] = new Rgba32(200, 100, 50, 128);
        image[12, 11] = new Rgba32(1, 2, 3, ColorBleed.MinimumColorAlpha);
        Rgba32[] before = Pixels(image);

        ColorBleed.Apply(image, 3);

        Rgba32[] after = Pixels(image);
        for (int index = 0; index < before.Length; index++)
        {
            Assert.Equal(before[index].A, after[index].A);
            if (before[index].A >= ColorBleed.MinimumColorAlpha)
                Assert.Equal(before[index], after[index]);
        }
    }

    [Fact]
    public void Apply_FaintNoisyPixel_DoesNotSpreadItsColor()
    {
        using Image<Rgba32> image = new(16, 16);
        Fill(image, 0, 0, 7, 15, Blue);
        image[8, 8] = new Rgba32(0, 255, 255, 1);

        ColorBleed.Apply(image, 3);

        Assert.Equal(new Rgba32(0, 0, 255, 1), image[8, 8]);
        Assert.Equal(new Rgba32(0, 0, 255, 0), image[9, 8]);
    }

    [Fact]
    public void Apply_ArtworkFainterThanThreshold_StillBleeds()
    {
        using Image<Rgba32> image = new(16, 16);
        Fill(image, 4, 4, 11, 11, new Rgba32(255, 0, 0, 10));

        ColorBleed.Apply(image, 2);

        Assert.Equal(new Rgba32(255, 0, 0, 0), image[1, 1]);
    }

    [Fact]
    public void Apply_BandWidthZero_LeavesImageUnchanged()
    {
        using Image<Rgba32> image = new(16, 16);
        Fill(image, 4, 4, 11, 11, Red);
        Rgba32[] before = Pixels(image);

        ColorBleed.Apply(image, 0);

        Assert.Equal(before, Pixels(image));
    }

    [Fact]
    public void Apply_FullyTransparentImage_LeavesImageUnchanged()
    {
        using Image<Rgba32> image = new(16, 16, new Rgba32(5, 6, 7, 0));
        Rgba32[] before = Pixels(image);

        ColorBleed.Apply(image, 3);

        Assert.Equal(before, Pixels(image));
    }

    [Fact]
    public void Apply_NegativeBandWidth_Throws()
    {
        using Image<Rgba32> image = new(4, 4);

        Assert.Throws<ArgumentOutOfRangeException>(() => ColorBleed.Apply(image, -1));
    }

    private static void Fill(Image<Rgba32> image, int left, int top, int right, int bottom, Rgba32 color)
    {
        for (int y = top; y <= bottom; y++)
        {
            for (int x = left; x <= right; x++)
                image[x, y] = color;
        }
    }

    private static Rgba32[] Pixels(Image<Rgba32> image)
    {
        Rgba32[] pixels = new Rgba32[image.Width * image.Height];
        image.CopyPixelDataTo(pixels);
        return pixels;
    }
}

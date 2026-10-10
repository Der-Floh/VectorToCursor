using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using VectorToCursor.Domain;
using VectorToCursor.Rendering;

namespace VectorToCursor.Tests.Rendering;

public sealed class ArtworkSnapshotTests
{
    private static readonly ArtworkBounds Bounds = new(0, 0, 32, 32);
    private static readonly Rgba32 Color = new(0x49, 0x87, 0xEE, 0xFF);

    [Fact]
    public void Take_RendersOneImagePerFitInTheirOrder()
    {
        using ArtworkSnapshot snapshot = ArtworkSnapshot.Take(new FakeArtwork(), FitsFor(48, 16, 32), TimeSpan.Zero);

        Assert.Equal([48, 16, 32], snapshot.Images.Select(image => image.Width));
    }

    [Fact]
    public void Take_RenderingFails_DisposesTheImagesAlreadyRendered()
    {
        FakeArtwork artwork = new() { FailingSize = 48 };

        Assert.Throws<InvalidOperationException>(() => ArtworkSnapshot.Take(artwork, FitsFor(16, 48), TimeSpan.Zero));

        Image<Rgba32> rendered = Assert.Single(artwork.Rendered);
        Assert.Throws<ObjectDisposedException>(() => rendered.Clone());
    }

    [Fact]
    public void HasSamePixelsAs_SameMoment_IsTrue()
    {
        FakeArtwork artwork = new();
        using ArtworkSnapshot first = ArtworkSnapshot.Take(artwork, FitsFor(16, 48), TimeSpan.Zero);
        using ArtworkSnapshot second = ArtworkSnapshot.Take(artwork, FitsFor(16, 48), TimeSpan.Zero);

        Assert.True(first.HasSamePixelsAs(second));
    }

    [Fact]
    public void HasSamePixelsAs_OnePixelDiffersInOneSize_IsFalse()
    {
        FakeArtwork artwork = new() { ChangingSize = 48 };
        using ArtworkSnapshot first = ArtworkSnapshot.Take(artwork, FitsFor(16, 48), TimeSpan.Zero);
        using ArtworkSnapshot second = ArtworkSnapshot.Take(artwork, FitsFor(16, 48), FakeArtwork.ChangeTime);

        Assert.False(first.HasSamePixelsAs(second));
    }

    [Theory]
    [InlineData(32)]
    [InlineData(48, 64)]
    public void HasSamePixelsAs_OtherSizes_IsFalse(params int[] otherSizes)
    {
        FakeArtwork artwork = new();
        using ArtworkSnapshot first = ArtworkSnapshot.Take(artwork, FitsFor(16, 48), TimeSpan.Zero);
        using ArtworkSnapshot second = ArtworkSnapshot.Take(artwork, FitsFor([16, .. otherSizes]), TimeSpan.Zero);

        Assert.False(first.HasSamePixelsAs(second));
    }

    [Fact]
    public void HasSamePixelsAs_SvgDuringAPause_IsTrueOnlyWhileNothingMoves()
    {
        // smil-hold.svg slides during the first quarter of its 1 s loop, then stands still.
        using ISvgArtwork artwork = new SkiaSvgLoader().Load(TestFiles.PathOf("smil-hold.svg"));
        SquareFit[] fits = [.. CursorSizes.Default.Values.Select(size => SquareFit.Create(artwork.Bounds, size))];
        using ArtworkSnapshot sliding = ArtworkSnapshot.Take(artwork, fits, TimeSpan.FromSeconds(0.1));
        using ArtworkSnapshot pauseStart = ArtworkSnapshot.Take(artwork, fits, TimeSpan.FromSeconds(0.3));
        using ArtworkSnapshot pauseEnd = ArtworkSnapshot.Take(artwork, fits, TimeSpan.FromSeconds(0.9));

        Assert.True(pauseStart.HasSamePixelsAs(pauseEnd));
        Assert.False(sliding.HasSamePixelsAs(pauseStart));
    }

    private static SquareFit[] FitsFor(params int[] sizes) => [.. sizes.Select(size => SquareFit.Create(Bounds, size))];

    /// <summary>Draws every size in one color, except where configured otherwise.</summary>
    private sealed class FakeArtwork : ISvgArtwork
    {
        public static readonly TimeSpan ChangeTime = TimeSpan.FromSeconds(1);

        public ArtworkBounds Bounds => ArtworkSnapshotTests.Bounds;

        public TimeSpan? LoopDuration => ChangeTime * 2;

        /// <summary>From <see cref="ChangeTime"/> on, the image of this size has one pixel that is a little more transparent.</summary>
        public int? ChangingSize { get; init; }

        /// <summary>Rendering this size throws.</summary>
        public int? FailingSize { get; init; }

        public List<Image<Rgba32>> Rendered { get; } = [];

        public Image<Rgba32> Render(SquareFit fit, TimeSpan time)
        {
            if (fit.Size == FailingSize)
                throw new InvalidOperationException("Rendering failed on purpose.");

            Image<Rgba32> image = new(fit.Size, fit.Size, Color);
            if (fit.Size == ChangingSize && time >= ChangeTime)
                image[fit.Size - 1, fit.Size - 1] = Color with { A = 0xFE };
            Rendered.Add(image);
            return image;
        }

        public void Dispose()
        {
        }
    }
}

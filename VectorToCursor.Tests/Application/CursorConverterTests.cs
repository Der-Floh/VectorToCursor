using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Cur;
using SixLabors.ImageSharp.Formats.Icon;
using SixLabors.ImageSharp.PixelFormats;
using VectorToCursor.Application;
using VectorToCursor.Cursors;
using VectorToCursor.Domain;
using VectorToCursor.Rendering;
using VectorToCursor.Tests.Cursors;

namespace VectorToCursor.Tests.Application;

public sealed class CursorConverterTests : IDisposable
{
    private const int AnimationFrameCount = 6;

    private static readonly string InputPath = TestFiles.PathOf("left-half.svg");
    private static readonly string AnimatedInputPath = TestFiles.PathOf("css-blink.svg");
    private static readonly TimeSpan AnimationLoop = TimeSpan.FromMilliseconds(200);
    private static readonly Rgba32 SquareColor = new(0x49, 0x87, 0xEE, 0);

    private readonly TemporaryDirectory _directory = new();
    private readonly CursorConverter _converter = new(new SkiaSvgLoader(), new ImageSharpCursorEncoder(), new AniEncoder());

    public void Dispose() => _directory.Dispose();

    [Fact]
    public void Convert_WritesCursorWithScaledHotspotPerSize()
    {
        string outputPath = _directory.PathOf("left-half.cur");
        int[] expectedX = [3, 4, 6, 9, 12, 24];
        int[] expectedY = [2, 3, 4, 6, 8, 16];

        ConversionResult result = _converter.Convert(Request(InputPath, outputPath, new SvgPoint(3, 2)));

        Assert.Equal(outputPath, result.OutputPath);
        Assert.Null(result.Animation);
        using Image<Rgba32> cursor = Image.Load<Rgba32>(outputPath);
        Assert.Equal(CursorSizes.Default.Values.Count, cursor.Frames.Count);
        for (int index = 0; index < CursorSizes.Default.Values.Count; index++)
        {
            PixelHotspot expectedHotspot = new(expectedX[index], expectedY[index]);
            CurFrameMetadata metadata = cursor.Frames[index].Metadata.GetCurMetadata();

            Assert.Equal(new FrameSummary(CursorSizes.Default.Values[index], expectedHotspot), result.Frames[index]);
            Assert.Equal(expectedHotspot, new PixelHotspot(metadata.HotspotX, metadata.HotspotY));
        }
    }

    [Fact]
    public void Convert_GivesTransparentPixelsTheEdgeColor()
    {
        string outputPath = _directory.PathOf("blue-square.cur");

        _converter.Convert(Request(TestFiles.PathOf("blue-square.svg"), outputPath, new SvgPoint(0, 0)));

        Assert.All(TransparentPixelsPerFrame(outputPath), pixel => Assert.Equal(SquareColor, pixel));
    }

    [Fact]
    public void Convert_BleedOff_LeavesTransparentPixelsBlack()
    {
        string outputPath = _directory.PathOf("blue-square.cur");

        _converter.Convert(Request(TestFiles.PathOf("blue-square.svg"), outputPath, new SvgPoint(0, 0), bleed: new BleedPercentage(0)));

        Assert.All(TransparentPixelsPerFrame(outputPath), pixel => Assert.Equal(new Rgba32(0, 0, 0, 0), pixel));
    }

    [Fact]
    public void Convert_MissingOutputDirectory_IsCreated()
    {
        string outputPath = _directory.PathOf("nested", "folder", "left-half.cur");

        _converter.Convert(Request(InputPath, outputPath, new SvgPoint(0, 0)));

        Assert.True(File.Exists(outputPath));
    }

    [Fact]
    public void Convert_ExistingOutputFile_IsOverwritten()
    {
        string outputPath = _directory.PathOf("left-half.cur");
        File.WriteAllText(outputPath, "previous content");

        _converter.Convert(Request(InputPath, outputPath, new SvgPoint(0, 0)));

        using Image<Rgba32> cursor = Image.Load<Rgba32>(outputPath);
        Assert.Equal(CursorSizes.Default.Values.Count, cursor.Frames.Count);
    }

    [Fact]
    public void Convert_HotspotOutsideCursor_ThrowsAndWritesNothing()
    {
        string outputPath = _directory.PathOf("left-half.cur");

        Assert.Throws<CursorConversionException>(() => _converter.Convert(Request(InputPath, outputPath, new SvgPoint(40, 2))));

        Assert.Empty(Directory.EnumerateFileSystemEntries(_directory.FullPath));
    }

    [Fact]
    public void Convert_UnusableSvg_ThrowsAndWritesNothing()
    {
        string outputPath = _directory.PathOf("malformed.cur");

        Assert.Throws<CursorConversionException>(() => _converter.Convert(Request(TestFiles.PathOf("malformed.svg"), outputPath, new SvgPoint(0, 0))));

        Assert.Empty(Directory.EnumerateFileSystemEntries(_directory.FullPath));
    }

    [Theory]
    [InlineData("css-blink.svg")]
    [InlineData("smil-blink.svg")]
    public void Convert_AnimatedSvg_WritesAnimatedCursorWithOneFramePerSample(string fileName)
    {
        string outputPath = _directory.PathOf("blink.ani");

        ConversionResult result = _converter.Convert(Request(TestFiles.PathOf(fileName), outputPath, new SvgPoint(3, 2)));

        AniFile ani = AniFile.Read(outputPath);
        Assert.Equal(new AnimationSummary(AnimationFrameCount, AnimationFrameCount, FrameRate.Default, AnimationLoop, AnimationLoop), result.Animation);
        Assert.Equal(AnimationFrameCount, ani.Frames.Count);
        Assert.Equal(FrameRate.Default.Jiffies, ani.Jiffies);
        Assert.Null(ani.Rates);
        Assert.Null(ani.Sequence);
    }

    [Fact]
    public void Convert_AnimatedSvgWithAPause_ShowsTheHeldFrameInOneLongStep()
    {
        string outputPath = _directory.PathOf("hold.ani");

        ConversionResult result = _converter.Convert(Request(TestFiles.PathOf("smil-hold.svg"), outputPath, new SvgPoint(0, 0)));

        // smil-hold.svg slides during the first quarter of its 1 s loop, which takes 8 of its 30 frames, then stands still.
        AnimationStep[] expected = [.. Enumerable.Range(0, 8).Select(index => new AnimationStep(index, 2)), new AnimationStep(8, 22 * 2)];
        AniFile ani = AniFile.Read(outputPath);
        Assert.Equal(new AnimationSummary(30, 9, FrameRate.Default, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1)), result.Animation);
        Assert.Equal(9, ani.Frames.Count);
        Assert.Equal(expected, ani.Steps);
    }

    [Fact]
    public void Convert_AnimatedSvgShowingAFrameAgain_StoresItOnce()
    {
        string outputPath = _directory.PathOf("repeat.ani");

        ConversionResult result = _converter.Convert(Request(TestFiles.PathOf("smil-repeat.svg"), outputPath, new SvgPoint(0, 0)));

        // smil-repeat.svg shows the opacities 1, 0.2, 1 and 0.6 for an eighth of a second each: 4, 4, 4 and 3 frames.
        AnimationStep[] expected = [new(0, 4 * 2), new(1, 4 * 2), new(0, 4 * 2), new(2, 3 * 2)];
        AniFile ani = AniFile.Read(outputPath);
        Assert.Equal(new AnimationSummary(15, 3, FrameRate.Default, TimeSpan.FromSeconds(0.5), TimeSpan.FromSeconds(0.5)), result.Animation);
        Assert.Equal(3, ani.Frames.Count);
        Assert.Equal(expected, ani.Steps);
    }

    [Fact]
    public void Convert_LoopLongerThanAMinute_HoldsItsPauseInOneStep()
    {
        string outputPath = _directory.PathOf("long-hold.ani");

        ConversionResult result = _converter.Convert(Request(TestFiles.PathOf("smil-long-hold.svg"), outputPath, new SvgPoint(0, 0), frameRate: new FrameRate(1)));

        // smil-long-hold.svg slides within the first half second of its 100 s loop, then stands still.
        AniFile ani = AniFile.Read(outputPath);
        Assert.Equal(new AnimationSummary(100, 2, new FrameRate(1), TimeSpan.FromSeconds(100), TimeSpan.FromSeconds(100)), result.Animation);
        Assert.Equal([new AnimationStep(0, 60), new AnimationStep(1, 99 * 60)], ani.Steps);
    }

    [Fact]
    public void Convert_AnimatedSvg_StoresEveryFrameAsPngCursorWithTheSameHotspots()
    {
        string outputPath = _directory.PathOf("blink.ani");

        ConversionResult result = _converter.Convert(Request(AnimatedInputPath, outputPath, new SvgPoint(3, 2)));

        Assert.All(AniFile.Read(outputPath).Frames, frame =>
        {
            using Image<Rgba32> cursor = Image.Load<Rgba32>(frame);
            Assert.Equal(CursorSizes.Default.Values.Count, cursor.Frames.Count);
            for (int index = 0; index < CursorSizes.Default.Values.Count; index++)
            {
                CurFrameMetadata metadata = cursor.Frames[index].Metadata.GetCurMetadata();
                Assert.Equal(result.Frames[index].Hotspot, new PixelHotspot(metadata.HotspotX, metadata.HotspotY));
                Assert.Equal(IconFrameCompression.Png, metadata.Compression);
            }
        });
    }

    // The format is passed by name: a public test method can't take the internal enum (CS0051).
    [Theory]
    [InlineData("left-half.svg", "left-half.cur", null, IconFrameCompression.Bmp)]
    [InlineData("left-half.svg", "left-half.cur", nameof(CursorImageFormat.Png), IconFrameCompression.Png)]
    [InlineData("css-blink.svg", "blink.ani", null, IconFrameCompression.Png)]
    [InlineData("css-blink.svg", "blink.ani", nameof(CursorImageFormat.Png), IconFrameCompression.Png)]
    public void Convert_StoresEveryImageInTheRequestedOrDefaultFormat(string inputFile, string outputFile, string? imageFormat, IconFrameCompression expected)
    {
        string outputPath = _directory.PathOf(outputFile);
        CursorImageFormat? format = imageFormat is null ? null : Enum.Parse<CursorImageFormat>(imageFormat);

        ConversionResult result = _converter.Convert(Request(TestFiles.PathOf(inputFile), outputPath, new SvgPoint(0, 0), imageFormat: format));

        Assert.Equal(expected.ToString(), result.ImageFormat.ToString());
        Assert.All(CursorFilesIn(outputPath), cursorFile =>
        {
            using Image<Rgba32> cursor = Image.Load<Rgba32>(cursorFile);
            Assert.Equal(CursorSizes.Default.Values.Count, cursor.Frames.Count);
            Assert.All<ImageFrame<Rgba32>>(cursor.Frames, frame => Assert.Equal(expected, frame.Metadata.GetCurMetadata().Compression));
        });
    }

    [Theory]
    [InlineData(null)]
    [InlineData(nameof(CursorImageFormat.Bmp))]
    public void Convert_AnimatedSvgWithSizesThatFitAsBmp_StoresBmpFrames(string? imageFormat)
    {
        string outputPath = _directory.PathOf("blink.ani");
        CursorImageFormat? format = imageFormat is null ? null : Enum.Parse<CursorImageFormat>(imageFormat);

        ConversionResult result = _converter.Convert(Request(AnimatedInputPath, outputPath, new SvgPoint(0, 0), sizes: Sizes(32, 48, 64), imageFormat: format));

        Assert.Equal(CursorImageFormat.Bmp, result.ImageFormat);
        Assert.All(AniFile.Read(outputPath).Frames, frame =>
        {
            using Image<Rgba32> cursor = Image.Load<Rgba32>(frame);
            Assert.Equal(3, cursor.Frames.Count);
            Assert.All<ImageFrame<Rgba32>>(cursor.Frames, image => Assert.Equal(IconFrameCompression.Bmp, image.Metadata.GetCurMetadata().Compression));
        });
    }

    [Fact]
    public void Convert_AnimatedSvgWithBmpThatDoesNotFit_ThrowsNamingTheImageAndWritesNothing()
    {
        CursorConversionException exception = Assert.Throws<CursorConversionException>(() =>
            _converter.Convert(Request(AnimatedInputPath, _directory.PathOf("blink.ani"), new SvgPoint(0, 0), imageFormat: CursorImageFormat.Bmp)));

        Assert.Contains("frame 1 stores its 128 px image at byte 68,998", exception.Message);
        Assert.Contains("--image-format png", exception.Message);
        Assert.Empty(Directory.EnumerateFileSystemEntries(_directory.FullPath));
    }

    [Fact]
    public void Convert_Sizes_WritesOneImagePerSizeWithItsScaledHotspot()
    {
        string outputPath = _directory.PathOf("left-half.cur");
        FrameSummary[] expected = [new(16, new PixelHotspot(1, 1)), new(40, new PixelHotspot(3, 2))];

        ConversionResult result = _converter.Convert(Request(InputPath, outputPath, new SvgPoint(3, 2), sizes: Sizes(40, 16)));

        Assert.Equal(expected, result.Frames);
        using Image<Rgba32> cursor = Image.Load<Rgba32>(outputPath);
        Assert.Equal([16, 40], Enumerable.Range(0, cursor.Frames.Count).Select(index => (int)(cursor.Frames[index].Metadata.GetCurMetadata().EncodingWidth ?? 0)));
    }

    [Fact]
    public void Convert_AnimatedSvg_SamplesTheLoopEvenly()
    {
        string outputPath = _directory.PathOf("blink.ani");

        _converter.Convert(Request(AnimatedInputPath, outputPath, new SvgPoint(0, 0)));

        // css-blink.svg fades linearly from opaque to transparent within each loop.
        IReadOnlyList<byte[]> frames = AniFile.Read(outputPath).Frames;
        for (int index = 0; index < frames.Count; index++)
        {
            double expectedAlpha = byte.MaxValue * (1 - (double)index / frames.Count);
            Assert.InRange(CenterAlphaOfSmallestImage(frames[index]), expectedAlpha - 2, expectedAlpha + 2);
        }
    }

    [Theory]
    [InlineData(60, 12, 1)]
    [InlineData(10, 2, 6)]
    public void Convert_FrameRate_DecidesFrameCountAndFrameLength(int framesPerSecond, int expectedFrames, int expectedJiffies)
    {
        string outputPath = _directory.PathOf("blink.ani");

        ConversionResult result = _converter.Convert(Request(AnimatedInputPath, outputPath, new SvgPoint(0, 0), frameRate: new FrameRate(framesPerSecond)));

        AniFile ani = AniFile.Read(outputPath);
        Assert.Equal(expectedFrames, result.Animation?.FrameCount);
        Assert.Equal(expectedFrames, ani.Frames.Count);
        Assert.Equal(expectedJiffies, ani.Jiffies);
    }

    [Theory]
    [InlineData("css-blink.svg", "blink.cur", ".ani")]
    [InlineData("left-half.svg", "left-half.ani", ".cur")]
    [InlineData("left-half.svg", "left-half.png", ".cur")]
    [InlineData("css-blink.svg", "blink", ".ani")]
    public void Convert_OutputExtensionNotMatchingAnimation_ThrowsAndWritesNothing(string inputFile, string outputFile, string expectedExtension)
    {
        CursorConversionException exception = Assert.Throws<CursorConversionException>(() => _converter.Convert(Request(TestFiles.PathOf(inputFile), _directory.PathOf(outputFile), new SvgPoint(0, 0))));

        Assert.Contains($"must end in {expectedExtension}", exception.Message);
        Assert.Empty(Directory.EnumerateFileSystemEntries(_directory.FullPath));
    }

    [Theory]
    [InlineData("left-half.svg", "LEFT-HALF.CUR")]
    [InlineData("css-blink.svg", "Blink.Ani")]
    public void Convert_OutputExtension_IgnoresCase(string inputFile, string outputFile)
    {
        string outputPath = _directory.PathOf(outputFile);

        _converter.Convert(Request(TestFiles.PathOf(inputFile), outputPath, new SvgPoint(0, 0)));

        Assert.True(File.Exists(outputPath));
    }

    [Theory]
    [InlineData("left-half.svg", "left-half.cur")]
    [InlineData("css-blink.svg", "css-blink.ani")]
    public void Convert_NoOutputPath_WritesNextToInputWithExtensionOfItsKind(string inputFile, string expectedOutputFile)
    {
        string inputPath = _directory.PathOf(inputFile);
        File.Copy(TestFiles.PathOf(inputFile), inputPath);

        ConversionResult result = _converter.Convert(Request(inputPath, null, new SvgPoint(0, 0)));

        Assert.Equal(_directory.PathOf(expectedOutputFile), result.OutputPath);
        Assert.True(File.Exists(result.OutputPath));
    }

    private static ConversionRequest Request(string inputPath, string? outputPath, SvgPoint hotspot, BleedPercentage? bleed = null, FrameRate? frameRate = null, CursorSizes? sizes = null, CursorImageFormat? imageFormat = null) =>
        new(inputPath, outputPath, hotspot, bleed ?? BleedPercentage.Default, frameRate ?? FrameRate.Default, sizes ?? CursorSizes.Default, imageFormat);

    private static CursorSizes Sizes(params int[] values) =>
        CursorSizes.TryCreate(values, out CursorSizes? sizes) ? sizes : throw new ArgumentException("The test sizes are invalid.", nameof(values));

    private static IReadOnlyList<byte[]> CursorFilesIn(string outputPath) =>
        Path.GetExtension(outputPath) == CursorFileExtensions.Animated ? AniFile.Read(outputPath).Frames : [File.ReadAllBytes(outputPath)];

    private static byte CenterAlphaOfSmallestImage(byte[] cursorFile)
    {
        using Image<Rgba32> cursor = Image.Load<Rgba32>(cursorFile);
        int size = CursorSizes.Default.Values[0];
        return cursor.Frames[0][size / 2, size / 2].A;
    }

    // The decoder places every frame top-left on a canvas of the largest size, so only that region belongs to the frame.
    private static List<Rgba32> TransparentPixelsPerFrame(string cursorPath)
    {
        using Image<Rgba32> cursor = Image.Load<Rgba32>(cursorPath);
        List<Rgba32> transparent = [];
        for (int index = 0; index < CursorSizes.Default.Values.Count; index++)
        {
            int size = CursorSizes.Default.Values[index];
            ImageFrame<Rgba32> frame = cursor.Frames[index];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    if (frame[x, y].A == 0)
                        transparent.Add(frame[x, y]);
                }
            }
        }
        Assert.NotEmpty(transparent);
        return transparent;
    }
}

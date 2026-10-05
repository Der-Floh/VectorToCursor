using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Cur;
using SixLabors.ImageSharp.PixelFormats;
using VectorToCursor.Application;
using VectorToCursor.Cursors;
using VectorToCursor.Domain;
using VectorToCursor.Rendering;

namespace VectorToCursor.Tests.Application;

public sealed class CursorConverterTests : IDisposable
{
    private static readonly string InputPath = TestFiles.PathOf("left-half.svg");

    private readonly TemporaryDirectory _directory = new();
    private readonly CursorConverter _converter = new(new SkiaSvgLoader(), new ImageSharpCursorEncoder());

    public void Dispose() => _directory.Dispose();

    [Fact]
    public void Convert_WritesCursorWithScaledHotspotPerSize()
    {
        string outputPath = _directory.PathOf("left-half.cur");
        int[] expectedX = [3, 4, 6, 9, 12, 24];
        int[] expectedY = [2, 3, 4, 6, 8, 16];

        ConversionResult result = _converter.Convert(new ConversionRequest(InputPath, outputPath, new SvgPoint(3, 2)));

        Assert.Equal(outputPath, result.OutputPath);
        using Image<Rgba32> cursor = Image.Load<Rgba32>(outputPath);
        Assert.Equal(CursorSizes.All.Count, cursor.Frames.Count);
        for (int index = 0; index < CursorSizes.All.Count; index++)
        {
            PixelHotspot expectedHotspot = new(expectedX[index], expectedY[index]);
            CurFrameMetadata metadata = cursor.Frames[index].Metadata.GetCurMetadata();

            Assert.Equal(new FrameSummary(CursorSizes.All[index], expectedHotspot), result.Frames[index]);
            Assert.Equal(expectedHotspot, new PixelHotspot(metadata.HotspotX, metadata.HotspotY));
        }
    }

    [Fact]
    public void Convert_MissingOutputDirectory_IsCreated()
    {
        string outputPath = _directory.PathOf("nested", "folder", "left-half.cur");

        _converter.Convert(new ConversionRequest(InputPath, outputPath, new SvgPoint(0, 0)));

        Assert.True(File.Exists(outputPath));
    }

    [Fact]
    public void Convert_ExistingOutputFile_IsOverwritten()
    {
        string outputPath = _directory.PathOf("left-half.cur");
        File.WriteAllText(outputPath, "previous content");

        _converter.Convert(new ConversionRequest(InputPath, outputPath, new SvgPoint(0, 0)));

        using Image<Rgba32> cursor = Image.Load<Rgba32>(outputPath);
        Assert.Equal(CursorSizes.All.Count, cursor.Frames.Count);
    }

    [Fact]
    public void Convert_HotspotOutsideCursor_ThrowsAndWritesNothing()
    {
        string outputPath = _directory.PathOf("left-half.cur");

        Assert.Throws<CursorConversionException>(() => _converter.Convert(new ConversionRequest(InputPath, outputPath, new SvgPoint(40, 2))));

        Assert.Empty(Directory.EnumerateFileSystemEntries(_directory.FullPath));
    }

    [Fact]
    public void Convert_UnusableSvg_ThrowsAndWritesNothing()
    {
        string outputPath = _directory.PathOf("malformed.cur");

        Assert.Throws<CursorConversionException>(() => _converter.Convert(new ConversionRequest(TestFiles.PathOf("malformed.svg"), outputPath, new SvgPoint(0, 0))));

        Assert.Empty(Directory.EnumerateFileSystemEntries(_directory.FullPath));
    }
}

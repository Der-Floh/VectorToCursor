using System.Buffers.Binary;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Cur;
using SixLabors.ImageSharp.PixelFormats;
using VectorToCursor.Cursors;
using VectorToCursor.Domain;

namespace VectorToCursor.Tests.Cursors;

public sealed class ImageSharpCursorEncoderTests
{
    private const int DirectoryHeaderSize = 6;
    private const int DirectoryEntrySize = 16;
    private const ushort CursorResourceType = 2;
    private const int BitmapInfoHeaderSize = 40;

    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    [Fact]
    public void Encode_WritesCursorDirectoryHeader()
    {
        byte[] cursor = EncodeAllSizes();

        Assert.Equal(0, ReadUInt16(cursor, 0));
        Assert.Equal(CursorResourceType, ReadUInt16(cursor, 2));
        Assert.Equal(CursorSizes.Default.Values.Count, ReadUInt16(cursor, 4));
    }

    [Fact]
    public void Encode_WritesSizeAndHotspotPerDirectoryEntry()
    {
        byte[] cursor = EncodeAllSizes();

        for (int index = 0; index < CursorSizes.Default.Values.Count; index++)
        {
            int size = CursorSizes.Default.Values[index];
            int entry = DirectoryHeaderSize + index * DirectoryEntrySize;
            byte expectedDimension = size == 256 ? (byte)0 : (byte)size;

            Assert.Equal(expectedDimension, cursor[entry]);
            Assert.Equal(expectedDimension, cursor[entry + 1]);
            Assert.Equal(HotspotFor(size), new PixelHotspot(ReadUInt16(cursor, entry + 4), ReadUInt16(cursor, entry + 6)));
        }
    }

    [Fact]
    public void Encode_BmpFormat_StoresEveryFrameAsBmp()
    {
        byte[] cursor = EncodeAllSizes(CursorImageFormat.Bmp);

        for (int index = 0; index < CursorSizes.Default.Values.Count; index++)
            Assert.Equal(BitmapInfoHeaderSize, BinaryPrimitives.ReadInt32LittleEndian(ReadPayload(cursor, index)));
    }

    [Fact]
    public void Encode_PngFormat_StoresEveryFrameAsPng()
    {
        byte[] cursor = EncodeAllSizes(CursorImageFormat.Png);

        for (int index = 0; index < CursorSizes.Default.Values.Count; index++)
            Assert.Equal(PngSignature, ReadPayload(cursor, index)[..PngSignature.Length].ToArray());
    }

    // The format is passed by name: a public test method can't take the internal enum (CS0051).
    [Theory]
    [InlineData(nameof(CursorImageFormat.Bmp))]
    [InlineData(nameof(CursorImageFormat.Png))]
    public void Encode_RoundTripsPixelsAndHotspots(string format)
    {
        byte[] cursor = EncodeAllSizes(Enum.Parse<CursorImageFormat>(format));

        using Image<Rgba32> decoded = Image.Load<Rgba32>(cursor);

        Assert.Equal(CursorSizes.Default.Values.Count, decoded.Frames.Count);
        for (int index = 0; index < CursorSizes.Default.Values.Count; index++)
        {
            int size = CursorSizes.Default.Values[index];
            ImageFrame<Rgba32> frame = decoded.Frames[index];
            CurFrameMetadata metadata = frame.Metadata.GetCurMetadata();

            Assert.Equal(HotspotFor(size), new PixelHotspot(metadata.HotspotX, metadata.HotspotY));
            using Image<Rgba32> expected = CreatePattern(size);
            AssertTopLeftPixelsEqual(expected, frame);
        }
    }

    [Theory]
    [InlineData(nameof(CursorImageFormat.Bmp))]
    [InlineData(nameof(CursorImageFormat.Png))]
    public void Encode_KeepsColorOfTransparentPixelsInEveryFrame(string format)
    {
        Rgba32 transparentColor = new(10, 20, 30, 0);
        byte[] cursor = EncodeAllSizes(size => CreateHalfTransparent(size, transparentColor), Enum.Parse<CursorImageFormat>(format));

        using Image<Rgba32> decoded = Image.Load<Rgba32>(cursor);

        for (int index = 0; index < CursorSizes.Default.Values.Count; index++)
            Assert.Equal(transparentColor, decoded.Frames[index][CursorSizes.Default.Values[index] - 1, 0]);
    }

    [Fact]
    public void Encode_NoFrames_Throws()
    {
        using MemoryStream stream = new();

        Assert.Throws<ArgumentException>(() => new ImageSharpCursorEncoder().Encode([], stream, CursorImageFormat.Bmp));
    }

    [Fact]
    public void Encode_FrameLargerThan256_Throws()
    {
        using CursorFrame frame = new(new Image<Rgba32>(257, 257), new PixelHotspot(0, 0));
        using MemoryStream stream = new();

        Assert.Throws<ArgumentException>(() => new ImageSharpCursorEncoder().Encode([frame], stream, CursorImageFormat.Bmp));
    }

    [Fact]
    public void Encode_ReadOnlyStream_Throws()
    {
        using CursorFrame frame = new(CreatePattern(32), new PixelHotspot(0, 0));
        using MemoryStream stream = new([], writable: false);

        Assert.Throws<ArgumentException>(() => new ImageSharpCursorEncoder().Encode([frame], stream, CursorImageFormat.Bmp));
    }

    private static byte[] EncodeAllSizes(CursorImageFormat format = CursorImageFormat.Bmp) => EncodeAllSizes(CreatePattern, format);

    private static byte[] EncodeAllSizes(Func<int, Image<Rgba32>> createImage, CursorImageFormat format = CursorImageFormat.Bmp)
    {
        List<CursorFrame> frames = [.. CursorSizes.Default.Values.Select(size => new CursorFrame(createImage(size), HotspotFor(size)))];
        try
        {
            using MemoryStream stream = new();
            new ImageSharpCursorEncoder().Encode(frames, stream, format);
            return stream.ToArray();
        }
        finally
        {
            frames.ForEach(frame => frame.Dispose());
        }
    }

    private static PixelHotspot HotspotFor(int size) => new(size / 8, size / 4);

    // Distinct per pixel and never fully transparent: a 32-bit BMP without any alpha decodes as opaque.
    private static Image<Rgba32> CreatePattern(int size)
    {
        Image<Rgba32> image = new(size, size);
        image.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < accessor.Height; y++)
            {
                Span<Rgba32> row = accessor.GetRowSpan(y);
                for (int x = 0; x < row.Length; x++)
                    row[x] = new Rgba32((byte)x, (byte)y, (byte)size, (byte)(55 + (x + y) % 200));
            }
        });
        return image;
    }

    // Opaque left half: a 32-bit BMP whose alpha is zero everywhere would decode as opaque.
    private static Image<Rgba32> CreateHalfTransparent(int size, Rgba32 transparentColor)
    {
        Image<Rgba32> image = new(size, size, transparentColor);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size / 2; x++)
                image[x, y] = new Rgba32(200, 0, 0, 255);
        }
        return image;
    }

    private static void AssertTopLeftPixelsEqual(Image<Rgba32> expected, ImageFrame<Rgba32> actual)
    {
        for (int y = 0; y < expected.Height; y++)
        {
            for (int x = 0; x < expected.Width; x++)
                Assert.Equal(expected[x, y], actual[x, y]);
        }
    }

    private static ReadOnlySpan<byte> ReadPayload(byte[] cursor, int index)
    {
        int entry = DirectoryHeaderSize + index * DirectoryEntrySize;
        int length = (int)BinaryPrimitives.ReadUInt32LittleEndian(cursor.AsSpan(entry + 8));
        int offset = (int)BinaryPrimitives.ReadUInt32LittleEndian(cursor.AsSpan(entry + 12));
        return cursor.AsSpan(offset, length);
    }

    private static ushort ReadUInt16(byte[] bytes, int offset) => BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(offset));
}

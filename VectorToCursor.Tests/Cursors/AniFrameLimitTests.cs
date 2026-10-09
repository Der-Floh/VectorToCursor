using System.Buffers.Binary;
using VectorToCursor.Cursors;
using VectorToCursor.Domain;

namespace VectorToCursor.Tests.Cursors;

public sealed class AniFrameLimitTests
{
    private const int DirectoryHeaderSize = 6;
    private const int DirectoryEntrySize = 16;

    [Fact]
    public void FindImageBeyondLimit_EveryImageStartsWithin64KB_ReturnsNull()
    {
        byte[] cursor = CursorDirectory((32, 38), (128, 65_535));

        Assert.Null(AniFrameLimit.FindImageBeyondLimit(cursor));
    }

    [Fact]
    public void FindImageBeyondLimit_ReturnsTheFirstImageStartingAt64KB()
    {
        byte[] cursor = CursorDirectory((32, 54), (128, 65_536), (0, 70_000));

        Assert.Equal(new AniFrameLimit.FrameImage(128, 65_536), AniFrameLimit.FindImageBeyondLimit(cursor));
    }

    [Fact]
    public void FindImageBeyondLimit_ReportsAStoredWidthOfZeroAs256()
    {
        byte[] cursor = CursorDirectory((0, 100_000));

        Assert.Equal(256, AniFrameLimit.FindImageBeyondLimit(cursor)?.Size);
    }

    [Fact]
    public void EnsureFits_EveryImageStartsWithin64KB_DoesNotThrow()
    {
        byte[] cursor = CursorDirectory((32, 38), (128, 65_535));

        AniFrameLimit.EnsureFits(cursor, 0, CursorImageFormat.Bmp);
    }

    // The format is passed by name: a public test method can't take the internal enum (CS0051).
    [Theory]
    [InlineData(nameof(CursorImageFormat.Bmp), true)]
    [InlineData(nameof(CursorImageFormat.Png), false)]
    public void EnsureFits_ImageBeyondLimit_ThrowsNamingFrameImageAndOffset(string format, bool suggestsPng)
    {
        byte[] cursor = CursorDirectory((32, 54), (128, 68_998));

        CursorConversionException exception = Assert.Throws<CursorConversionException>(() => AniFrameLimit.EnsureFits(cursor, 2, Enum.Parse<CursorImageFormat>(format)));

        Assert.Contains("frame 3 stores its 128 px image at byte 68,998", exception.Message);
        Assert.Contains("65,536 bytes", exception.Message);
        Assert.Contains("Use fewer or smaller --sizes", exception.Message);
        Assert.Equal(suggestsPng, exception.Message.Contains("--image-format png", StringComparison.Ordinal));
    }

    private static byte[] CursorDirectory(params (byte Width, uint Offset)[] images)
    {
        byte[] cursor = new byte[DirectoryHeaderSize + images.Length * DirectoryEntrySize];
        BinaryPrimitives.WriteUInt16LittleEndian(cursor.AsSpan(2), 2);
        BinaryPrimitives.WriteUInt16LittleEndian(cursor.AsSpan(4), (ushort)images.Length);
        for (int index = 0; index < images.Length; index++)
        {
            Span<byte> entry = cursor.AsSpan(DirectoryHeaderSize + index * DirectoryEntrySize, DirectoryEntrySize);
            entry[0] = images[index].Width;
            entry[1] = images[index].Width;
            BinaryPrimitives.WriteUInt32LittleEndian(entry[12..], images[index].Offset);
        }
        return cursor;
    }
}

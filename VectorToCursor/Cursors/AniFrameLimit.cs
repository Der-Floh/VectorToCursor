using System.Buffers.Binary;
using System.Globalization;
using VectorToCursor.Domain;

namespace VectorToCursor.Cursors;

/// <summary>
/// Windows refuses an animated cursor when an image starts 64 KB or more into its frame, however small or large the file
/// is. Every frame is a complete cursor file, so the limit applies to the image offsets in its directory.
/// </summary>
internal static class AniFrameLimit
{
    public const int MaximumImageOffset = ushort.MaxValue;

    private const int ImageCountOffset = 4;
    private const int DirectoryHeaderSize = 6;
    private const int DirectoryEntrySize = 16;
    private const int ImageOffsetInEntry = 12;

    // The cursor directory stores each dimension in one byte, where 0 means 256.
    private const int SizeStoredAsZero = 256;

    /// <returns>The first image that starts beyond <see cref="MaximumImageOffset"/>, or <see langword="null"/> when all of them fit.</returns>
    public static FrameImage? FindImageBeyondLimit(ReadOnlySpan<byte> cursorFile)
    {
        int count = BinaryPrimitives.ReadUInt16LittleEndian(cursorFile[ImageCountOffset..]);
        for (int index = 0; index < count; index++)
        {
            ReadOnlySpan<byte> entry = cursorFile.Slice(DirectoryHeaderSize + index * DirectoryEntrySize, DirectoryEntrySize);
            uint offset = BinaryPrimitives.ReadUInt32LittleEndian(entry[ImageOffsetInEntry..]);
            if (offset > MaximumImageOffset)
                return new FrameImage(entry[0] == 0 ? SizeStoredAsZero : entry[0], offset);
        }
        return null;
    }

    /// <exception cref="CursorConversionException">An image of the frame starts beyond <see cref="MaximumImageOffset"/>.</exception>
    public static void EnsureFits(ReadOnlySpan<byte> cursorFile, int frameIndex, CursorImageFormat format)
    {
        if (FindImageBeyondLimit(cursorFile) is not FrameImage image)
            return;

        string advice = format == CursorImageFormat.Bmp ? "Use fewer or smaller --sizes, or --image-format png." : "Use fewer or smaller --sizes.";
        throw new CursorConversionException(string.Create(CultureInfo.InvariantCulture, $"Windows can't load this animated cursor: frame {frameIndex + 1} stores its {image.Size} px image at byte {image.Offset:N0}, but Windows refuses an image that starts 64 KB ({MaximumImageOffset + 1:N0} bytes) or more into its frame. {advice}"));
    }

    /// <summary>An image of a cursor file: its size in pixels and the byte at which it starts.</summary>
    internal readonly record struct FrameImage(int Size, uint Offset);
}

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Cur;
using SixLabors.ImageSharp.Formats.Icon;
using SixLabors.ImageSharp.PixelFormats;
using VectorToCursor.Domain;

namespace VectorToCursor.Cursors;

/// <summary>Encodes cursors with ImageSharp's CUR encoder.</summary>
internal sealed class ImageSharpCursorEncoder : ICursorEncoder
{
    // The cursor directory stores each dimension in one byte, where 0 means 256.
    private const int MaxFrameSize = 256;

    // Preserve keeps the colors ColorBleed gave transparent pixels in both the BMP and the PNG frames.
    private static readonly CurEncoder Encoder = new() { SkipMetadata = true, TransparentColorMode = TransparentColorMode.Preserve };

    public void Encode(IReadOnlyList<CursorFrame> frames, Stream destination, CursorImageFormat format)
    {
        ValidateFrames(frames);
        ArgumentNullException.ThrowIfNull(destination);
        if (!destination.CanWrite || !destination.CanSeek)
            throw new ArgumentException("The destination stream must be writable and seekable.", nameof(destination));

        int canvasSize = frames.Max(frame => frame.Size);
        using Image<Rgba32> cursor = new(canvasSize, canvasSize);
        for (int index = 0; index < frames.Count; index++)
        {
            ImageFrame<Rgba32> target = index == 0 ? cursor.Frames.RootFrame : cursor.Frames.CreateFrame();
            CopyToTopLeft(frames[index].Image.Frames.RootFrame, target);
            Configure(target.Metadata.GetCurMetadata(), frames[index], format);
        }

        cursor.Save(destination, Encoder);
    }

    private static void ValidateFrames(IReadOnlyList<CursorFrame> frames)
    {
        ArgumentNullException.ThrowIfNull(frames);
        if (frames.Count == 0)
            throw new ArgumentException("A cursor needs at least one frame.", nameof(frames));

        CursorFrame? oversized = frames.FirstOrDefault(frame => frame.Size > MaxFrameSize);
        if (oversized is not null)
            throw new ArgumentException($"Cursor frames can be at most {MaxFrameSize} px, but one is {oversized.Size} px.", nameof(frames));
    }

    // All frames of an ImageSharp image share one size; the encoder crops each to its encoding size from the top-left.
    private static void CopyToTopLeft(ImageFrame<Rgba32> source, ImageFrame<Rgba32> target) =>
        source.ProcessPixelRows(target, (sourceAccessor, targetAccessor) =>
        {
            for (int y = 0; y < sourceAccessor.Height; y++)
                sourceAccessor.GetRowSpan(y).CopyTo(targetAccessor.GetRowSpan(y));
        });

    private static void Configure(CurFrameMetadata metadata, CursorFrame frame, CursorImageFormat format)
    {
        byte encodedSize = frame.Size == MaxFrameSize ? (byte)0 : (byte)frame.Size;
        metadata.EncodingWidth = encodedSize;
        metadata.EncodingHeight = encodedSize;
        metadata.HotspotX = (ushort)frame.Hotspot.X;
        metadata.HotspotY = (ushort)frame.Hotspot.Y;
        metadata.Compression = format == CursorImageFormat.Png ? IconFrameCompression.Png : IconFrameCompression.Bmp;
        metadata.BmpBitsPerPixel = BmpBitsPerPixel.Bit32;
    }
}

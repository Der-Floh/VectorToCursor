using System.Buffers.Binary;
using System.Text;
using VectorToCursor.Domain;

namespace VectorToCursor.Cursors;

/// <summary>
/// Writes the RIFF 'ACON' container of animated cursors: an 'anih' header, then a 'LIST fram' with one 'icon' chunk per
/// frame, each holding a complete .cur file.
/// </summary>
internal sealed class AniEncoder : IAnimatedCursorEncoder
{
    private const int ChunkHeaderSize = 8;
    private const int FourCcSize = 4;
    private const int HeaderSize = 36;
    private const int BitCount = 32;
    private const int Planes = 1;

    // AF_ICON: the frames are icon or cursor resources rather than raw bitmaps.
    private const int FramesAreIcons = 0x1;

    public void Encode(IReadOnlyList<byte[]> frames, FrameRate frameRate, Stream destination)
    {
        ArgumentNullException.ThrowIfNull(frames);
        ArgumentNullException.ThrowIfNull(destination);
        if (frames.Count == 0 || frames.Any(frame => frame is null || frame.Length == 0))
            throw new ArgumentException("An animated cursor needs at least one frame, and no frame may be empty.", nameof(frames));
        if (!destination.CanWrite)
            throw new ArgumentException("The destination stream must be writable.", nameof(destination));

        // RIFF sizes are 32-bit; checked so an oversized animation fails instead of writing a corrupt file.
        int listSize = checked(FourCcSize + frames.Sum(frame => ChunkHeaderSize + Padded(frame.Length)));
        int riffSize = checked(FourCcSize + ChunkHeaderSize + HeaderSize + ChunkHeaderSize + listSize);

        WriteChunkHeader(destination, "RIFF", riffSize);
        WriteFourCc(destination, "ACON");
        WriteChunkHeader(destination, "anih", HeaderSize);
        WriteHeader(destination, frames.Count, frameRate);
        WriteChunkHeader(destination, "LIST", listSize);
        WriteFourCc(destination, "fram");
        foreach (byte[] frame in frames)
            WriteChunk(destination, "icon", frame);
    }

    // nSteps equals nFrames because there is no 'seq ' chunk; width, height 0 means "use the sizes in each frame".
    private static void WriteHeader(Stream destination, int frameCount, FrameRate frameRate)
    {
        Span<byte> header = stackalloc byte[HeaderSize];
        int[] fields = [HeaderSize, frameCount, frameCount, 0, 0, BitCount, Planes, frameRate.Jiffies, FramesAreIcons];
        for (int index = 0; index < fields.Length; index++)
            BinaryPrimitives.WriteInt32LittleEndian(header[(index * sizeof(int))..], fields[index]);
        destination.Write(header);
    }

    // RIFF chunks start on even offsets; the pad byte counts towards the parent's size but not the chunk's own.
    private static void WriteChunk(Stream destination, string fourCc, byte[] data)
    {
        WriteChunkHeader(destination, fourCc, data.Length);
        destination.Write(data);
        if (data.Length % 2 != 0)
            destination.WriteByte(0);
    }

    private static void WriteChunkHeader(Stream destination, string fourCc, int size)
    {
        WriteFourCc(destination, fourCc);
        Span<byte> sizeBytes = stackalloc byte[sizeof(int)];
        BinaryPrimitives.WriteInt32LittleEndian(sizeBytes, size);
        destination.Write(sizeBytes);
    }

    private static void WriteFourCc(Stream destination, string fourCc) => destination.Write(Encoding.ASCII.GetBytes(fourCc));

    private static int Padded(int size) => size + size % 2;
}

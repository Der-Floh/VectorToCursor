using System.Buffers.Binary;
using System.Text;
using VectorToCursor.Domain;

namespace VectorToCursor.Cursors;

/// <summary>
/// Writes the RIFF 'ACON' container of animated cursors: an 'anih' header, a 'rate' chunk when the steps differ in length,
/// a 'seq ' chunk when they don't show each frame once in stored order, then a 'LIST fram' with one 'icon' chunk per
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

    // AF_SEQUENCE: a 'seq ' chunk names the frame of each step.
    private const int StepsAreSequenced = 0x2;

    public void Encode(AnimationSequence sequence, Stream destination)
    {
        ArgumentNullException.ThrowIfNull(sequence);
        ArgumentNullException.ThrowIfNull(destination);
        if (sequence.Steps.Count == 0)
            throw new ArgumentException("An animated cursor needs at least one frame.", nameof(sequence));
        if (!destination.CanWrite)
            throw new ArgumentException("The destination stream must be writable.", nameof(destination));

        IReadOnlyList<AnimationStep> steps = sequence.Steps;
        int[]? rates = steps.All(step => step.Jiffies == steps[0].Jiffies) ? null : [.. steps.Select(step => step.Jiffies)];
        int[]? order = ShowsFramesInStoredOrder(sequence) ? null : [.. steps.Select(step => step.FrameIndex)];

        // RIFF sizes are 32-bit; checked so an oversized animation fails instead of writing a corrupt file.
        int listSize = checked(FourCcSize + sequence.Frames.Sum(frame => ChunkHeaderSize + Padded(frame.Length)));
        int riffSize = checked(FourCcSize + ChunkHeaderSize + HeaderSize + ArrayChunkSize(rates) + ArrayChunkSize(order) + ChunkHeaderSize + listSize);

        WriteChunkHeader(destination, "RIFF", riffSize);
        WriteFourCc(destination, "ACON");
        WriteChunkHeader(destination, "anih", HeaderSize);
        WriteHeader(destination, sequence, order is null ? FramesAreIcons : FramesAreIcons | StepsAreSequenced);
        if (rates is not null)
            WriteArrayChunk(destination, "rate", rates);
        if (order is not null)
            WriteArrayChunk(destination, "seq ", order);
        WriteChunkHeader(destination, "LIST", listSize);
        WriteFourCc(destination, "fram");
        foreach (byte[] frame in sequence.Frames)
            WriteChunk(destination, "icon", frame);
    }

    private static bool ShowsFramesInStoredOrder(AnimationSequence sequence) =>
        sequence.Steps.Count == sequence.Frames.Count && Enumerable.Range(0, sequence.Steps.Count).All(index => sequence.Steps[index].FrameIndex == index);

    // Width and height 0 mean "use the sizes in each frame". Readers that ignore a 'rate' chunk show every step for the
    // header's rate; the shortest step keeps motion at its real speed.
    private static void WriteHeader(Stream destination, AnimationSequence sequence, int flags)
    {
        int displayRate = sequence.Steps.Min(step => step.Jiffies);
        int[] fields = [HeaderSize, sequence.Frames.Count, sequence.Steps.Count, 0, 0, BitCount, Planes, displayRate, flags];
        foreach (int field in fields)
            WriteInt32(destination, field);
    }

    private static void WriteArrayChunk(Stream destination, string fourCc, int[] values)
    {
        WriteChunkHeader(destination, fourCc, values.Length * sizeof(int));
        foreach (int value in values)
            WriteInt32(destination, value);
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
        WriteInt32(destination, size);
    }

    private static void WriteInt32(Stream destination, int value)
    {
        Span<byte> bytes = stackalloc byte[sizeof(int)];
        BinaryPrimitives.WriteInt32LittleEndian(bytes, value);
        destination.Write(bytes);
    }

    private static void WriteFourCc(Stream destination, string fourCc) => destination.Write(Encoding.ASCII.GetBytes(fourCc));

    private static int ArrayChunkSize(int[]? values) => values is null ? 0 : checked(ChunkHeaderSize + values.Length * sizeof(int));

    private static int Padded(int size) => size + size % 2;
}

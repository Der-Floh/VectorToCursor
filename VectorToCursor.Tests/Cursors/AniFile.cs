using System.Buffers.Binary;
using System.Text;
using VectorToCursor.Domain;

namespace VectorToCursor.Tests.Cursors;

/// <summary>An animated cursor read back for assertions; reading fails on any violation of the RIFF structure.</summary>
internal sealed class AniFile
{
    public const int ChunkHeaderSize = 8;
    public const int FourCcSize = 4;
    public const int HeaderFieldCount = 9;
    public const int HeaderOffset = ChunkHeaderSize + FourCcSize + ChunkHeaderSize;

    /// <summary>Where the 'LIST fram' chunk starts in a file without 'rate' and 'seq ' chunks.</summary>
    public const int ListOffset = HeaderOffset + HeaderFieldCount * sizeof(int);

    public const int FramesAreIcons = 0x1;
    public const int StepsAreSequenced = 0x2;

    private AniFile(IReadOnlyList<int> header, IReadOnlyList<int>? rates, IReadOnlyList<int>? sequence, IReadOnlyList<byte[]> frames)
    {
        Header = header;
        Rates = rates;
        Sequence = sequence;
        Frames = frames;
    }

    /// <summary>The nine fields of the 'anih' chunk: cbSize, nFrames, nSteps, cx, cy, bitCount, planes, jifRate, flags.</summary>
    public IReadOnlyList<int> Header { get; }

    /// <summary>The length of each step in jiffies from the 'rate' chunk, or <see langword="null"/> without one.</summary>
    public IReadOnlyList<int>? Rates { get; }

    /// <summary>The frame of each step from the 'seq ' chunk, or <see langword="null"/> without one.</summary>
    public IReadOnlyList<int>? Sequence { get; }

    /// <summary>The contents of the 'icon' chunks, without padding.</summary>
    public IReadOnlyList<byte[]> Frames { get; }

    public int Jiffies => Header[7];

    public int Flags => Header[8];

    /// <summary>What Windows plays: each step's frame and length, from the 'seq ' and 'rate' chunks or else the header.</summary>
    public IReadOnlyList<AnimationStep> Steps =>
        [.. Enumerable.Range(0, Header[2]).Select(step => new AnimationStep(Sequence?[step] ?? step, Rates?[step] ?? Jiffies))];

    public static AniFile Read(string path) => Parse(File.ReadAllBytes(path));

    public static AniFile Parse(byte[] bytes)
    {
        AssertChunkHeader(bytes, 0, "RIFF", bytes.Length - ChunkHeaderSize);
        Assert.Equal("ACON", FourCc(bytes, ChunkHeaderSize));
        AssertChunkHeader(bytes, ChunkHeaderSize + FourCcSize, "anih", HeaderFieldCount * sizeof(int));
        int[] header = ReadInt32s(bytes, HeaderOffset, HeaderFieldCount);
        int stepCount = header[2];

        int position = ListOffset;
        int[]? rates = ReadOptionalArrayChunk(bytes, ref position, "rate", stepCount);
        int[]? sequence = ReadOptionalArrayChunk(bytes, ref position, "seq ", stepCount);
        List<byte[]> frames = ReadFrameList(bytes, position);

        Assert.Equal(header[1], frames.Count);
        Assert.Equal(sequence is not null, (header[8] & StepsAreSequenced) != 0);
        if (sequence is null)
            Assert.Equal(frames.Count, stepCount);
        else
            Assert.All(sequence, frameIndex => Assert.InRange(frameIndex, 0, frames.Count - 1));
        return new AniFile(header, rates, sequence, frames);
    }

    public static string FourCc(byte[] bytes, int offset) => Encoding.ASCII.GetString(bytes, offset, FourCcSize);

    public static int ReadInt32(byte[] bytes, int offset) => BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(offset));

    private static int[]? ReadOptionalArrayChunk(byte[] bytes, ref int position, string fourCc, int count)
    {
        if (FourCc(bytes, position) != fourCc)
            return null;

        AssertChunkHeader(bytes, position, fourCc, count * sizeof(int));
        int[] values = ReadInt32s(bytes, position + ChunkHeaderSize, count);
        position += ChunkHeaderSize + count * sizeof(int);
        return values;
    }

    private static List<byte[]> ReadFrameList(byte[] bytes, int listOffset)
    {
        AssertChunkHeader(bytes, listOffset, "LIST", bytes.Length - listOffset - ChunkHeaderSize);
        Assert.Equal("fram", FourCc(bytes, listOffset + ChunkHeaderSize));

        List<byte[]> frames = [];
        int position = listOffset + ChunkHeaderSize + FourCcSize;
        while (position < bytes.Length)
        {
            Assert.True(position % 2 == 0, $"The chunk at {position} doesn't start on an even offset.");
            Assert.Equal("icon", FourCc(bytes, position));
            int size = ReadInt32(bytes, position + FourCcSize);
            frames.Add(bytes[(position + ChunkHeaderSize)..(position + ChunkHeaderSize + size)]);
            position += ChunkHeaderSize + size + size % 2;
        }
        Assert.Equal(bytes.Length, position);
        return frames;
    }

    private static int[] ReadInt32s(byte[] bytes, int offset, int count) =>
        [.. Enumerable.Range(0, count).Select(index => ReadInt32(bytes, offset + index * sizeof(int)))];

    private static void AssertChunkHeader(byte[] bytes, int offset, string fourCc, int size)
    {
        Assert.Equal(fourCc, FourCc(bytes, offset));
        Assert.Equal(size, ReadInt32(bytes, offset + FourCcSize));
    }
}

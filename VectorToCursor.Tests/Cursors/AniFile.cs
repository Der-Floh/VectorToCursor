using System.Buffers.Binary;
using System.Text;

namespace VectorToCursor.Tests.Cursors;

/// <summary>An animated cursor read back for assertions; reading fails on any violation of the RIFF structure.</summary>
internal sealed class AniFile
{
    public const int ChunkHeaderSize = 8;
    public const int FourCcSize = 4;
    public const int HeaderFieldCount = 9;
    public const int HeaderOffset = ChunkHeaderSize + FourCcSize + ChunkHeaderSize;
    public const int ListOffset = HeaderOffset + HeaderFieldCount * sizeof(int);

    private AniFile(IReadOnlyList<int> header, IReadOnlyList<byte[]> frames)
    {
        Header = header;
        Frames = frames;
    }

    /// <summary>The nine fields of the 'anih' chunk: cbSize, nFrames, nSteps, cx, cy, bitCount, planes, jifRate, flags.</summary>
    public IReadOnlyList<int> Header { get; }

    /// <summary>The contents of the 'icon' chunks, without padding.</summary>
    public IReadOnlyList<byte[]> Frames { get; }

    public int Jiffies => Header[7];

    public static AniFile Read(string path) => Parse(File.ReadAllBytes(path));

    public static AniFile Parse(byte[] bytes)
    {
        AssertChunkHeader(bytes, 0, "RIFF", bytes.Length - ChunkHeaderSize);
        Assert.Equal("ACON", FourCc(bytes, ChunkHeaderSize));
        AssertChunkHeader(bytes, ChunkHeaderSize + FourCcSize, "anih", HeaderFieldCount * sizeof(int));
        int[] header = [.. Enumerable.Range(0, HeaderFieldCount).Select(index => ReadInt32(bytes, HeaderOffset + index * sizeof(int)))];
        AssertChunkHeader(bytes, ListOffset, "LIST", bytes.Length - ListOffset - ChunkHeaderSize);
        Assert.Equal("fram", FourCc(bytes, ListOffset + ChunkHeaderSize));

        List<byte[]> frames = [];
        int position = ListOffset + ChunkHeaderSize + FourCcSize;
        while (position < bytes.Length)
        {
            Assert.True(position % 2 == 0, $"The chunk at {position} doesn't start on an even offset.");
            Assert.Equal("icon", FourCc(bytes, position));
            int size = ReadInt32(bytes, position + FourCcSize);
            frames.Add(bytes[(position + ChunkHeaderSize)..(position + ChunkHeaderSize + size)]);
            position += ChunkHeaderSize + size + size % 2;
        }
        Assert.Equal(bytes.Length, position);
        return new AniFile(header, frames);
    }

    public static string FourCc(byte[] bytes, int offset) => Encoding.ASCII.GetString(bytes, offset, FourCcSize);

    public static int ReadInt32(byte[] bytes, int offset) => BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(offset));

    private static void AssertChunkHeader(byte[] bytes, int offset, string fourCc, int size)
    {
        Assert.Equal(fourCc, FourCc(bytes, offset));
        Assert.Equal(size, ReadInt32(bytes, offset + FourCcSize));
    }
}

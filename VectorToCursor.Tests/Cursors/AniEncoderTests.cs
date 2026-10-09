using VectorToCursor.Cursors;
using VectorToCursor.Domain;

namespace VectorToCursor.Tests.Cursors;

public sealed class AniEncoderTests
{
    [Fact]
    public void Encode_WritesAnimationHeader()
    {
        AniFile ani = AniFile.Parse(Encode([[1, 2], [3, 4], [5, 6]], FrameRate.Default));

        // cbSize, nFrames, nSteps, cx and cy (0: taken from the frames), bitCount, planes, jifRate, flags (AF_ICON)
        int[] expected = [36, 3, 3, 0, 0, 32, 1, 2, 1];
        Assert.Equal(expected, ani.Header);
    }

    [Theory]
    [InlineData(60, 1)]
    [InlineData(30, 2)]
    [InlineData(12, 5)]
    [InlineData(1, 60)]
    public void Encode_FrameLengthFollowsFrameRate(int framesPerSecond, int expectedJiffies)
    {
        AniFile ani = AniFile.Parse(Encode([[1, 2]], new FrameRate(framesPerSecond)));

        Assert.Equal(expectedJiffies, ani.Jiffies);
    }

    [Fact]
    public void Encode_StoresFramesInOrderInIconChunks()
    {
        byte[][] frames = [[1, 2, 3, 4], [5, 6], [7, 8, 9, 10, 11, 12]];

        AniFile ani = AniFile.Parse(Encode(frames, FrameRate.Default));

        Assert.Equal(frames, ani.Frames);
    }

    [Fact]
    public void Encode_OddSizedFrame_IsPaddedSoTheNextChunkStartsEven()
    {
        byte[][] frames = [[1, 2, 3], [4, 5]];

        byte[] ani = Encode(frames, FrameRate.Default);

        int firstChunk = AniFile.ListOffset + AniFile.ChunkHeaderSize + AniFile.FourCcSize;
        int padding = firstChunk + AniFile.ChunkHeaderSize + frames[0].Length;
        Assert.Equal(frames[0].Length, AniFile.ReadInt32(ani, firstChunk + AniFile.FourCcSize));
        Assert.Equal(0, ani[padding]);
        Assert.Equal("icon", AniFile.FourCc(ani, padding + 1));
        Assert.Equal(frames, AniFile.Parse(ani).Frames);
    }

    [Fact]
    public void Encode_NoFrames_Throws()
    {
        Assert.Throws<ArgumentException>(() => Encode([], FrameRate.Default));
    }

    [Fact]
    public void Encode_EmptyFrame_Throws()
    {
        Assert.Throws<ArgumentException>(() => Encode([[1, 2], []], FrameRate.Default));
    }

    [Fact]
    public void Encode_ReadOnlyStream_Throws()
    {
        using MemoryStream stream = new([], writable: false);

        Assert.Throws<ArgumentException>(() => new AniEncoder().Encode([[1, 2]], FrameRate.Default, stream));
    }

    private static byte[] Encode(IReadOnlyList<byte[]> frames, FrameRate frameRate)
    {
        using MemoryStream stream = new();
        new AniEncoder().Encode(frames, frameRate, stream);
        return stream.ToArray();
    }
}

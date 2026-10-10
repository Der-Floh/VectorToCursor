using VectorToCursor.Cursors;
using VectorToCursor.Domain;

namespace VectorToCursor.Tests.Cursors;

public sealed class AniEncoderTests
{
    private static readonly byte[] A = [1, 2];
    private static readonly byte[] B = [3, 4, 5, 6];
    private static readonly byte[] C = [7, 8];

    [Fact]
    public void Encode_WritesAnimationHeader()
    {
        AniFile ani = AniFile.Parse(Encode(Sequence((A, 2), (B, 2), (C, 2))));

        // cbSize, nFrames, nSteps, cx and cy (0: taken from the frames), bitCount, planes, jifRate, flags (AF_ICON)
        int[] expected = [36, 3, 3, 0, 0, 32, 1, 2, 1];
        Assert.Equal(expected, ani.Header);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(60)]
    public void Encode_StepsOfEqualLength_StoreTheLengthInTheHeaderOnly(int jiffies)
    {
        AniFile ani = AniFile.Parse(Encode(Sequence((A, jiffies), (B, jiffies))));

        Assert.Equal(jiffies, ani.Jiffies);
        Assert.Null(ani.Rates);
        Assert.Null(ani.Sequence);
    }

    [Fact]
    public void Encode_StepsOfDifferentLength_WritesRateChunkAndTheShortestLengthAsDefault()
    {
        AniFile ani = AniFile.Parse(Encode(Sequence((A, 4), (B, 2), (C, 48))));

        Assert.Equal([4, 2, 48], ani.Rates);
        Assert.Equal(2, ani.Jiffies);
        Assert.Null(ani.Sequence);
        Assert.Equal(AniFile.FramesAreIcons, ani.Flags);
        Assert.Equal([new AnimationStep(0, 4), new AnimationStep(1, 2), new AnimationStep(2, 48)], ani.Steps);
    }

    [Fact]
    public void Encode_FrameShownAgain_WritesSequenceChunkAndStoresTheFrameOnce()
    {
        AniFile ani = AniFile.Parse(Encode(Sequence((A, 6), (B, 6), (A, 6))));

        Assert.Equal([A, B], ani.Frames);
        Assert.Equal([0, 1, 0], ani.Sequence);
        Assert.Null(ani.Rates);
        Assert.Equal(AniFile.FramesAreIcons | AniFile.StepsAreSequenced, ani.Flags);
        Assert.Equal(2, ani.Header[1]);
        Assert.Equal(3, ani.Header[2]);
    }

    [Fact]
    public void Encode_FrameShownAgainWithStepsOfDifferentLength_WritesRateBeforeSequenceChunk()
    {
        AniFile ani = AniFile.Parse(Encode(Sequence((A, 8), (B, 8), (A, 8), (C, 6))));

        Assert.Equal([new AnimationStep(0, 8), new AnimationStep(1, 8), new AnimationStep(0, 8), new AnimationStep(2, 6)], ani.Steps);
        Assert.NotNull(ani.Rates);
        Assert.NotNull(ani.Sequence);
    }

    [Fact]
    public void Encode_StoresFramesInOrderInIconChunks()
    {
        byte[][] frames = [[1, 2, 3, 4], [5, 6], [7, 8, 9, 10, 11, 12]];

        AniFile ani = AniFile.Parse(Encode(Sequence([.. frames.Select(frame => (frame, 2))])));

        Assert.Equal(frames, ani.Frames);
    }

    [Fact]
    public void Encode_OddSizedFrame_IsPaddedSoTheNextChunkStartsEven()
    {
        byte[][] frames = [[1, 2, 3], [4, 5]];

        byte[] ani = Encode(Sequence((frames[0], 2), (frames[1], 2)));

        int firstChunk = AniFile.ListOffset + AniFile.ChunkHeaderSize + AniFile.FourCcSize;
        int padding = firstChunk + AniFile.ChunkHeaderSize + frames[0].Length;
        Assert.Equal(frames[0].Length, AniFile.ReadInt32(ani, firstChunk + AniFile.FourCcSize));
        Assert.Equal(0, ani[padding]);
        Assert.Equal("icon", AniFile.FourCc(ani, padding + 1));
        Assert.Equal(frames, AniFile.Parse(ani).Frames);
    }

    [Fact]
    public void Encode_EmptySequence_Throws()
    {
        Assert.Throws<ArgumentException>(() => Encode(new AnimationSequence()));
    }

    [Fact]
    public void Encode_ReadOnlyStream_Throws()
    {
        using MemoryStream stream = new([], writable: false);

        Assert.Throws<ArgumentException>(() => new AniEncoder().Encode(Sequence((A, 2)), stream));
    }

    private static AnimationSequence Sequence(params (byte[] Frame, int Jiffies)[] shown)
    {
        AnimationSequence sequence = new();
        foreach ((byte[] frame, int jiffies) in shown)
            sequence.Add(frame, jiffies);
        return sequence;
    }

    private static byte[] Encode(AnimationSequence sequence)
    {
        using MemoryStream stream = new();
        new AniEncoder().Encode(sequence, stream);
        return stream.ToArray();
    }
}

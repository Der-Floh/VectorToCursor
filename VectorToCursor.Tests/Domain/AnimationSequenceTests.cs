using VectorToCursor.Domain;

namespace VectorToCursor.Tests.Domain;

public sealed class AnimationSequenceTests
{
    private static readonly byte[] A = [1, 2];
    private static readonly byte[] B = [3, 4, 5];
    private static readonly byte[] C = [6];

    [Fact]
    public void Add_DifferentFrames_GivesEachItsOwnStep()
    {
        AnimationSequence sequence = Sequence((A, 2), (B, 2), (C, 4));

        Assert.Equal([A, B, C], sequence.Frames);
        Assert.Equal([Step(0, 2), Step(1, 2), Step(2, 4)], sequence.Steps);
    }

    [Fact]
    public void Add_FrameEqualToThePreviousOne_LengthensItsStep()
    {
        AnimationSequence sequence = Sequence((A, 2), (B, 2), (B, 2), (B, 6));

        Assert.Equal([A, B], sequence.Frames);
        Assert.Equal([Step(0, 2), Step(1, 10)], sequence.Steps);
    }

    [Fact]
    public void Add_FrameSeenEarlier_IsStoredOnceAndShownAgain()
    {
        AnimationSequence sequence = Sequence((A, 2), (B, 2), (A, 2), (C, 2), (A, 2));

        Assert.Equal([A, B, C], sequence.Frames);
        Assert.Equal([Step(0, 2), Step(1, 2), Step(0, 2), Step(2, 2), Step(0, 2)], sequence.Steps);
    }

    [Fact]
    public void Add_SameBytesInAnotherArray_CountAsTheSameFrame()
    {
        AnimationSequence sequence = Sequence((A, 2), ([.. A], 2));

        Assert.Same(A, Assert.Single(sequence.Frames));
        Assert.Equal(Step(0, 4), Assert.Single(sequence.Steps));
    }

    [Fact]
    public void Add_MaximumNumberOfDistinctFrames_IsAllowedAndTheyCanBeShownAgain()
    {
        AnimationSequence sequence = SequenceOfDistinctFrames(AnimationSequence.MaximumFrameCount);

        sequence.Add(BitConverter.GetBytes(0), 2);

        Assert.Equal(AnimationSequence.MaximumFrameCount, sequence.Frames.Count);
        Assert.Equal(AnimationSequence.MaximumFrameCount + 1, sequence.Steps.Count);
    }

    [Fact]
    public void Add_MoreDistinctFramesThanTheMaximum_Throws()
    {
        AnimationSequence sequence = SequenceOfDistinctFrames(AnimationSequence.MaximumFrameCount);

        CursorConversionException exception = Assert.Throws<CursorConversionException>(() => sequence.Add(BitConverter.GetBytes(-1), 2));

        Assert.Contains("more than 1,800 different frames", exception.Message);
        Assert.Contains("--fps", exception.Message);
    }

    [Fact]
    public void Add_EmptyFrame_Throws()
    {
        Assert.Throws<ArgumentException>(() => new AnimationSequence().Add([], 2));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    public void Add_NonPositiveLength_Throws(int jiffies)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new AnimationSequence().Add(A, jiffies));
    }

    private static AnimationSequence Sequence(params (byte[] Frame, int Jiffies)[] shown)
    {
        AnimationSequence sequence = new();
        foreach ((byte[] frame, int jiffies) in shown)
            sequence.Add(frame, jiffies);
        return sequence;
    }

    private static AnimationSequence SequenceOfDistinctFrames(int count) => Sequence([.. Enumerable.Range(0, count).Select(index => (BitConverter.GetBytes(index), 2))]);

    private static AnimationStep Step(int frameIndex, int jiffies) => new(frameIndex, jiffies);
}

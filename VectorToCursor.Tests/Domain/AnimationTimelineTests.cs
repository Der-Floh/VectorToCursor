using VectorToCursor.Domain;

namespace VectorToCursor.Tests.Domain;

public sealed class AnimationTimelineTests
{
    [Fact]
    public void Create_LoopOfWholeFrames_PlaysExactlyAsLongAsTheLoop()
    {
        AnimationTimeline timeline = AnimationTimeline.Create(TimeSpan.FromMilliseconds(5500), FrameRate.Default);

        Assert.Equal(165, timeline.FrameCount);
        Assert.Equal(TimeSpan.FromMilliseconds(5500), timeline.EffectiveDuration);
    }

    [Theory]
    [InlineData(110, 30, 3, 100)]
    [InlineData(250, 10, 3, 300)]
    [InlineData(10, 30, 1, 33)]
    public void Create_LoopBetweenFrames_RoundsToNearestFrameCount(int loopMilliseconds, int framesPerSecond, int expectedFrames, int expectedMilliseconds)
    {
        AnimationTimeline timeline = AnimationTimeline.Create(TimeSpan.FromMilliseconds(loopMilliseconds), new FrameRate(framesPerSecond));

        Assert.Equal(expectedFrames, timeline.FrameCount);
        Assert.Equal(expectedMilliseconds, (int)timeline.EffectiveDuration.TotalMilliseconds);
    }

    [Fact]
    public void Create_LongestLoopAtHighestFrameRate_SamplesEveryFrame()
    {
        AnimationTimeline timeline = AnimationTimeline.Create(AnimationLoop.MaximumLength, new FrameRate(60));

        Assert.Equal(36_000, timeline.FrameCount);
        Assert.Equal(AnimationLoop.MaximumLength, timeline.EffectiveDuration);
    }

    [Fact]
    public void Create_LoopLongerThanAnimationLoopAllows_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => AnimationTimeline.Create(AnimationLoop.MaximumLength + TimeSpan.FromTicks(1), FrameRate.Default));
    }

    [Fact]
    public void Create_NonPositiveLoop_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => AnimationTimeline.Create(TimeSpan.Zero, FrameRate.Default));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 333_333)]
    [InlineData(3, 1_000_000)]
    [InlineData(15, 5_000_000)]
    [InlineData(164, 54_666_666)]
    public void TimeOf_SpreadsFramesEvenlyOverTheLoopWithoutDrift(int index, long expectedTicks)
    {
        AnimationTimeline timeline = AnimationTimeline.Create(TimeSpan.FromMilliseconds(5500), FrameRate.Default);

        Assert.Equal(TimeSpan.FromTicks(expectedTicks), timeline.TimeOf(index));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(165)]
    public void TimeOf_IndexOutsideTheLoop_Throws(int index)
    {
        AnimationTimeline timeline = AnimationTimeline.Create(TimeSpan.FromMilliseconds(5500), FrameRate.Default);

        Assert.Throws<ArgumentOutOfRangeException>(() => timeline.TimeOf(index));
    }
}

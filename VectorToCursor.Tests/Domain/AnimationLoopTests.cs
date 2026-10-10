using VectorToCursor.Domain;

namespace VectorToCursor.Tests.Domain;

public sealed class AnimationLoopTests
{
    [Fact]
    public void Calculate_NoAnimations_ReturnsNull()
    {
        Assert.Null(AnimationLoop.Calculate([]));
    }

    [Fact]
    public void Calculate_SingleEndlessAnimation_LoopsAfterOneIteration()
    {
        Assert.Equal(Milliseconds(1100), AnimationLoop.Calculate([Endless(1100)]));
    }

    [Theory]
    [InlineData(5500, 1100, 5500)]
    [InlineData(400, 600, 1200)]
    [InlineData(700, 300, 2100)]
    [InlineData(1000, 1000, 1000)]
    public void Calculate_EndlessAnimations_LoopAtLeastCommonMultiple(int first, int second, int expected)
    {
        Assert.Equal(Milliseconds(expected), AnimationLoop.Calculate([Endless(first), Endless(second)]));
    }

    [Fact]
    public void Calculate_EndlessAnimationWithOffset_KeepsItsPeriod()
    {
        Assert.Equal(Milliseconds(1000), AnimationLoop.Calculate([Endless(1000, begin: -250)]));
    }

    [Fact]
    public void Calculate_FiniteAnimations_LastUntilTheLastOneEnds()
    {
        Assert.Equal(Milliseconds(1500), AnimationLoop.Calculate([Finite(1000, active: 1000, begin: 500), Finite(400, active: 800)]));
    }

    [Fact]
    public void Calculate_FiniteAndEndless_RoundsUpToWholeIterations()
    {
        Assert.Equal(Milliseconds(3000), AnimationLoop.Calculate([Finite(500, active: 2500), Endless(1000)]));
    }

    [Fact]
    public void Calculate_FiniteEndingWithinFirstIteration_KeepsPeriod()
    {
        Assert.Equal(Milliseconds(1000), AnimationLoop.Calculate([Finite(400, active: 400), Endless(1000)]));
    }

    [Theory]
    [InlineData(1000, 0, 0)]
    [InlineData(1000, 1000, -2000)]
    public void Calculate_NothingMovesAfterTheStart_ReturnsNull(int duration, int active, int begin)
    {
        Assert.Null(AnimationLoop.Calculate([Finite(duration, active, begin)]));
    }

    [Fact]
    public void Calculate_LoopOfSeveralMinutes_IsAllowed()
    {
        Assert.Equal(Milliseconds(167_000), AnimationLoop.Calculate([Endless(167_000), Endless(500)]));
    }

    [Fact]
    public void Calculate_LoopOfMaximumLength_IsAllowed()
    {
        Assert.Equal(AnimationLoop.MaximumLength, AnimationLoop.Calculate([Endless(120_000), Endless(200_000)]));
    }

    [Fact]
    public void Calculate_EndlessLoopLongerThanMaximum_Throws()
    {
        CursorConversionException exception = Assert.Throws<CursorConversionException>(() => AnimationLoop.Calculate([Endless(70_000), Endless(110_000)]));

        Assert.Contains("770 s", exception.Message);
        Assert.Contains("10 minutes", exception.Message);
    }

    [Fact]
    public void Calculate_FiniteAnimationEndingAfterMaximum_Throws()
    {
        Assert.Throws<CursorConversionException>(() => AnimationLoop.Calculate([Finite(1000, active: 1000, begin: (int)AnimationLoop.MaximumLength.TotalMilliseconds)]));
    }

    [Fact]
    public void Calculate_ManyUnrelatedDurations_Throws()
    {
        AnimationTiming[] timings = [.. new[] { 1009, 1013, 1019, 1021, 1031, 1033, 1039, 1049 }.Select(duration => Endless(duration))];

        Assert.Throws<CursorConversionException>(() => AnimationLoop.Calculate(timings));
    }

    [Fact]
    public void Calculate_NonPositiveDuration_Throws()
    {
        Assert.Throws<ArgumentException>(() => AnimationLoop.Calculate([Endless(0)]));
    }

    private static AnimationTiming Endless(int duration, int begin = 0) => new(Milliseconds(begin), Milliseconds(duration), null);

    private static AnimationTiming Finite(int duration, int active, int begin = 0) => new(Milliseconds(begin), Milliseconds(duration), Milliseconds(active));

    private static TimeSpan Milliseconds(int milliseconds) => TimeSpan.FromMilliseconds(milliseconds);
}

using Svg;
using VectorToCursor.Domain;
using VectorToCursor.Rendering.Animation;

namespace VectorToCursor.Tests.Rendering.Animation;

public sealed class SmilTimingReaderTests
{
    [Fact]
    public void ReadAndNormalize_EndlessAnimation_RepeatsForever()
    {
        SvgDocument document = WithAnimation("<animate attributeName='opacity' values='1;0' dur='0.7s' repeatCount='indefinite' />");

        AnimationTiming timing = Assert.Single(SmilTimingReader.ReadAndNormalize(document));

        Assert.Equal(new AnimationTiming(TimeSpan.Zero, Milliseconds(700), null), timing);
    }

    [Fact]
    public void ReadAndNormalize_RewritesTimesAsExactMilliseconds()
    {
        SvgDocument document = WithAnimation("<animate attributeName='opacity' values='1;0' begin='-0.35s' dur='0.7s' repeatCount='indefinite' />");

        SmilTimingReader.ReadAndNormalize(document);

        SvgAnimationElement animation = Assert.Single(document.Descendants().OfType<SvgAnimationElement>());
        Assert.Equal("700ms", animation.Duration);
        Assert.Equal("-350ms", animation.Begin);
    }

    [Theory]
    [InlineData("", 1000)]
    [InlineData("repeatCount='2.5'", 2500)]
    [InlineData("repeatCount='0'", 0)]
    [InlineData("repeatCount='abc'", 1000)]
    [InlineData("repeatDur='3.5s'", 3500)]
    [InlineData("repeatCount='5' repeatDur='2s'", 2000)]
    [InlineData("repeatCount='2' repeatDur='3s'", 2000)]
    [InlineData("repeatCount='2' repeatDur='indefinite'", 2000)]
    public void ReadAndNormalize_FiniteRepetition_LimitsActiveDuration(string repetition, int expectedMilliseconds)
    {
        SvgDocument document = WithAnimation($"<animate attributeName='opacity' values='1;0' dur='1s' {repetition} />");

        AnimationTiming timing = Assert.Single(SmilTimingReader.ReadAndNormalize(document));

        Assert.Equal(Milliseconds(expectedMilliseconds), timing.ActiveDuration);
    }

    [Theory]
    [InlineData("repeatCount='indefinite'")]
    [InlineData("repeatDur='indefinite'")]
    [InlineData("repeatCount='indefinite' repeatDur='indefinite'")]
    public void ReadAndNormalize_IndefiniteRepetition_HasNoActiveDuration(string repetition)
    {
        SvgDocument document = WithAnimation($"<animate attributeName='opacity' values='1;0' dur='1s' {repetition} />");

        AnimationTiming timing = Assert.Single(SmilTimingReader.ReadAndNormalize(document));

        Assert.Null(timing.ActiveDuration);
    }

    [Fact]
    public void ReadAndNormalize_LateStartOfEndlessAnimation_MovesIntoFirstIteration()
    {
        SvgDocument document = WithAnimation("<animate attributeName='opacity' values='1;0' begin='2s' dur='1.5s' repeatCount='indefinite' />");

        AnimationTiming timing = Assert.Single(SmilTimingReader.ReadAndNormalize(document));

        Assert.Equal(Milliseconds(-1000), timing.Begin);
        Assert.Equal("-1000ms", Assert.Single(document.Descendants().OfType<SvgAnimationElement>()).Begin);
    }

    [Fact]
    public void ReadAndNormalize_LateStartOfFiniteAnimation_IsKept()
    {
        SvgDocument document = WithAnimation("<animate attributeName='opacity' values='1;0' begin='2s' dur='1s' />");

        AnimationTiming timing = Assert.Single(SmilTimingReader.ReadAndNormalize(document));

        Assert.Equal(new AnimationTiming(Milliseconds(2000), Milliseconds(1000), Milliseconds(1000)), timing);
    }

    [Fact]
    public void ReadAndNormalize_AnimateTransform_IsRead()
    {
        SvgDocument document = WithAnimation("<animateTransform attributeName='transform' type='rotate' from='0' to='360' dur='2s' repeatCount='indefinite' />");

        AnimationTiming timing = Assert.Single(SmilTimingReader.ReadAndNormalize(document));

        Assert.Equal(Milliseconds(2000), timing.SimpleDuration);
    }

    [Fact]
    public void ReadAndNormalize_AnimationWithoutAttributeName_IsIgnoredLikeTheEngineDoes()
    {
        SvgDocument document = WithAnimation("<animate values='1;0' dur='1s' />");

        Assert.Empty(SmilTimingReader.ReadAndNormalize(document));
    }

    [Theory]
    [InlineData("dur='1s' end='2s'")]
    [InlineData("dur='1s' min='1s'")]
    [InlineData("dur='1s' max='2s'")]
    [InlineData("dur='1s' begin='click'")]
    [InlineData("dur='1s' begin='other.end'")]
    [InlineData("dur='1s' begin='0s;1s'")]
    [InlineData("")]
    [InlineData("dur='indefinite'")]
    [InlineData("dur='0s'")]
    [InlineData("dur='-1s'")]
    [InlineData("dur='1s' repeatDur='abc'")]
    public void ReadAndNormalize_TimingALoopCantPlay_Throws(string timing)
    {
        SvgDocument document = WithAnimation($"<animate attributeName='opacity' values='1;0' {timing} />");

        CursorConversionException exception = Assert.Throws<CursorConversionException>(() => SmilTimingReader.ReadAndNormalize(document));

        Assert.Contains("'opacity' on '#box'", exception.Message);
    }

    private static SvgDocument WithAnimation(string animation) => TestSvg.Create(string.Empty, $"<rect id='box' width='32' height='32'>{animation}</rect>");

    private static TimeSpan Milliseconds(int milliseconds) => TimeSpan.FromMilliseconds(milliseconds);
}

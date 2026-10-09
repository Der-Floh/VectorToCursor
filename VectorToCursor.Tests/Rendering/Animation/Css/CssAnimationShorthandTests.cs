using VectorToCursor.Domain;
using VectorToCursor.Rendering.Animation.Css;

namespace VectorToCursor.Tests.Rendering.Animation.Css;

public sealed class CssAnimationShorthandTests
{
    [Fact]
    public void Expand_SeveralAnimations_SplitsOnlyOnTopLevelCommas()
    {
        Dictionary<string, string> longhands = CssAnimationShorthand.Expand("spin 5.5s linear infinite, grow 1.1s cubic-bezier(.8, 0, .35, .8) infinite, color 5.5s step-end infinite");

        Assert.Equal("spin, grow, color", longhands[CssAnimationProperty.Name]);
        Assert.Equal("5.5s, 1.1s, 5.5s", longhands[CssAnimationProperty.Duration]);
        Assert.Equal("linear, cubic-bezier(.8, 0, .35, .8), step-end", longhands[CssAnimationProperty.TimingFunction]);
        Assert.Equal("infinite, infinite, infinite", longhands[CssAnimationProperty.IterationCount]);
    }

    [Fact]
    public void Expand_SecondTime_IsTheDelay()
    {
        Dictionary<string, string> longhands = CssAnimationShorthand.Expand("spin 1s 250ms");

        Assert.Equal("1s", longhands[CssAnimationProperty.Duration]);
        Assert.Equal("250ms", longhands[CssAnimationProperty.Delay]);
    }

    [Fact]
    public void Expand_MissingLonghands_TakeTheirInitialValues()
    {
        Dictionary<string, string> longhands = CssAnimationShorthand.Expand("spin 1s");

        Assert.Equal("ease", longhands[CssAnimationProperty.TimingFunction]);
        Assert.Equal("0s", longhands[CssAnimationProperty.Delay]);
        Assert.Equal("1", longhands[CssAnimationProperty.IterationCount]);
        Assert.Equal("normal", longhands[CssAnimationProperty.Direction]);
        Assert.Equal("none", longhands[CssAnimationProperty.FillMode]);
        Assert.Equal("running", longhands[CssAnimationProperty.PlayState]);
    }

    [Fact]
    public void Expand_KeywordsOfOtherLonghands_AreNotTakenAsTheName()
    {
        Dictionary<string, string> longhands = CssAnimationShorthand.Expand("alternate 2s forwards reverse 3");

        Assert.Equal("alternate", longhands[CssAnimationProperty.Direction]);
        Assert.Equal("forwards", longhands[CssAnimationProperty.FillMode]);
        Assert.Equal("reverse", longhands[CssAnimationProperty.Name]);
        Assert.Equal("3", longhands[CssAnimationProperty.IterationCount]);
    }

    [Fact]
    public void Expand_None_LeavesTheNameAtNone()
    {
        Assert.Equal("none", CssAnimationShorthand.Expand("none")[CssAnimationProperty.Name]);
    }

    [Theory]
    [InlineData("spin bounce 1s")]
    [InlineData("spin 1s 2s 3s")]
    [InlineData("spin linear ease")]
    public void Expand_TokenFittingNoFreeLonghand_Throws(string value)
    {
        Assert.Throws<CursorConversionException>(() => CssAnimationShorthand.Expand(value));
    }
}

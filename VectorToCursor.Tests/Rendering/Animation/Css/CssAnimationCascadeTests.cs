using Svg;
using VectorToCursor.Domain;
using VectorToCursor.Rendering.Animation.Css;

namespace VectorToCursor.Tests.Rendering.Animation.Css;

public sealed class CssAnimationCascadeTests
{
    private const string ArcWithInlineDuration = """<circle id="arc" class="arc" r="10" style="animation-duration: 3s" />""";

    [Fact]
    public void Resolve_Shorthand_DefinesEveryAnimation()
    {
        List<CssAnimation> animations = Resolve(TestSvg.BusyCss);

        Assert.Equal(4, animations.Count);
        Assert.Equal(new CssAnimation("spin", Milliseconds(5500), CssTimingFunction.Linear, TimeSpan.Zero, null, CssAnimationDirection.Normal, CssAnimationFillMode.None, false), animations[0]);
        Assert.Equal(new CssAnimation("grow", Milliseconds(1100), new CssTimingFunction.CubicBezier(0.8, 0, 0.35, 0.8), TimeSpan.Zero, null, CssAnimationDirection.Normal, CssAnimationFillMode.None, false), animations[1]);
        Assert.Equal(new[] { "spin", "grow", "move", "color" }, animations.Select(animation => animation.Name));
    }

    [Fact]
    public void Resolve_LonghandsWithoutShorthand_UseInitialValuesForTheRest()
    {
        CssAnimation animation = Assert.Single(Resolve(".arc { animation-name: fade; animation-duration: 2s; animation-direction: alternate; }"));

        Assert.Equal(new CssAnimation("fade", Milliseconds(2000), CssTimingFunction.Ease, TimeSpan.Zero, 1, CssAnimationDirection.Alternate, CssAnimationFillMode.None, false), animation);
    }

    [Fact]
    public void Resolve_HigherSpecificity_Wins()
    {
        CssAnimation animation = Assert.Single(Resolve("#arc { animation-duration: 3s; } .arc { animation: fade 1s; }"));

        Assert.Equal(Milliseconds(3000), animation.Duration);
    }

    [Fact]
    public void Resolve_EqualSpecificity_LaterRuleWins()
    {
        CssAnimation animation = Assert.Single(Resolve(".arc { animation: fade 1s; } .arc { animation-duration: 2s; }"));

        Assert.Equal(Milliseconds(2000), animation.Duration);
    }

    [Fact]
    public void Resolve_LaterShorthand_ResetsEarlierLonghands()
    {
        CssAnimation animation = Assert.Single(Resolve(".arc { animation-timing-function: linear; } .arc { animation: fade 1s; }"));

        Assert.Equal(CssTimingFunction.Ease, animation.TimingFunction);
    }

    [Theory]
    [InlineData(".arc { animation: fade 1s; animation-duration: 2s; }", 2000)]
    [InlineData(".arc { animation-duration: 2s; animation: fade 1s; }", 1000)]
    public void Resolve_WithinOneRule_LaterDeclarationWins(string css, int expectedMilliseconds)
    {
        CssAnimation animation = Assert.Single(Resolve(css));

        Assert.Equal(Milliseconds(expectedMilliseconds), animation.Duration);
    }

    [Fact]
    public void Resolve_InlineStyle_BeatsTheStyleSheet()
    {
        CssAnimation animation = Assert.Single(Resolve("#arc { animation: fade 1s; }", ArcWithInlineDuration));

        Assert.Equal(Milliseconds(3000), animation.Duration);
    }

    [Fact]
    public void Resolve_Important_BeatsInlineStyleAndSpecificity()
    {
        CssAnimation animation = Assert.Single(Resolve(".arc { animation-duration: 4s !important; } #arc { animation: fade 1s; }", ArcWithInlineDuration));

        Assert.Equal(Milliseconds(4000), animation.Duration);
    }

    [Fact]
    public void Resolve_ShorterLists_RepeatForEveryName()
    {
        List<CssAnimation> animations = Resolve(".arc { animation-name: a, b, c; animation-duration: 1s, 2s; }");

        Assert.Equal(new[] { Milliseconds(1000), Milliseconds(2000), Milliseconds(1000) }, animations.Select(animation => animation.Duration));
    }

    [Fact]
    public void Resolve_NoneInTheNameList_IsSkipped()
    {
        List<CssAnimation> animations = Resolve(".arc { animation-name: a, none, b; animation-duration: 1s; }");

        Assert.Equal(new[] { "a", "b" }, animations.Select(animation => animation.Name));
    }

    [Theory]
    [InlineData(".arc { animation: none; }")]
    [InlineData(".arc { animation-duration: 1s; }")]
    [InlineData(".other { animation: fade 1s; }")]
    public void Resolve_NoNamedAnimation_ReturnsNothing(string css)
    {
        Assert.Empty(Resolve(css));
    }

    [Theory]
    [InlineData(".arc { animation: fade -1s; }", "negative duration")]
    [InlineData(".arc { animation: fade 1s; animation-duration: soon; }", "invalid duration")]
    [InlineData(".arc { animation: fade 1s; animation-delay: later; }", "invalid delay")]
    [InlineData(".arc { animation: fade 1s; animation-iteration-count: -1; }", "invalid iteration count")]
    [InlineData(".arc { animation: fade 1s; animation-direction: sideways; }", "invalid direction")]
    [InlineData(".arc { animation: fade 1s; animation-fill-mode: partial; }", "invalid fill mode")]
    [InlineData(".arc { animation: fade 1s; animation-play-state: stopped; }", "invalid play state")]
    [InlineData(".arc { animation: fade 1s; animation-timing-function: bounce; }", "'bounce' is not supported")]
    [InlineData(".arc { animation: fade spin 1s; }", "Could not read")]
    public void Resolve_InvalidValue_Throws(string css, string expectedMessage)
    {
        CursorConversionException exception = Assert.Throws<CursorConversionException>(() => Resolve(css));

        Assert.Contains(expectedMessage, exception.Message);
    }

    [Theory]
    [InlineData("g .arc { animation: fade 1s; }")]
    [InlineData("g > .arc { animation: fade 1s; }")]
    [InlineData(".arc:hover { animation: fade 1s; }")]
    [InlineData("[class] { animation: fade 1s; }")]
    public void FromStyleSheet_AnimationRuleWithUnsupportedSelector_Throws(string css)
    {
        Assert.Throws<CursorConversionException>(() => CssAnimationRule.FromStyleSheet(CssStyleSheetParser.Parse(css)));
    }

    [Fact]
    public void FromStyleSheet_OtherRulesWithUnsupportedSelectors_AreIgnored()
    {
        List<CssAnimationRule> rules = CssAnimationRule.FromStyleSheet(CssStyleSheetParser.Parse("g > circle:first-child { fill: red; } .arc { animation: fade 1s; }"));

        Assert.Single(rules);
    }

    private static List<CssAnimation> Resolve(string css, string content = TestSvg.Arc)
    {
        SvgDocument document = TestSvg.Create(css, content);
        return CssAnimationCascade.Resolve(document.GetElementById("arc"), CssAnimationRule.FromStyleSheet(CssStyleSheetParser.Parse(css)));
    }

    private static TimeSpan Milliseconds(int milliseconds) => TimeSpan.FromMilliseconds(milliseconds);
}

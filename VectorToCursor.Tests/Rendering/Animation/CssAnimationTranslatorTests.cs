using Svg;
using VectorToCursor.Domain;
using VectorToCursor.Rendering.Animation;

namespace VectorToCursor.Tests.Rendering.Animation;

public sealed class CssAnimationTranslatorTests
{
    private const string RejectedKeyframes = """
        @keyframes fade { to { opacity: 0; } }
        @keyframes spin { from { transform: rotate(0deg); } to { transform: rotate(360deg); } }
        @keyframes mixed { 0% { opacity: 1; animation-timing-function: step-end; } 50% { opacity: 0.5; } 100% { opacity: 0; } }
        @keyframes paint { from { fill: none; } to { fill: red; } }
        @keyframes radius { to { r: 5; } }
        @keyframes morph { from { transform: rotate(0deg); } to { transform: translate(4px); } }
        @keyframes beyond { 150% { opacity: 0; } }
        """;

    [Fact]
    public void Translate_Spin_RotatesFromTheIdentity()
    {
        SvgDocument document = Translate(".arc { animation: spin 5.5s linear infinite; } @keyframes spin { to { transform: rotate(360deg); } }");

        SvgAnimateTransform spin = Assert.IsType<SvgAnimateTransform>(Assert.Single(AnimationsOfArc(document)));
        Assert.Equal("transform", spin.AnimationAttributeName);
        Assert.Equal(SvgAnimateTransformType.Rotate, spin.TransformType);
        Assert.Equal(SvgAnimationAdditive.Replace, spin.Additive);
        Assert.Equal("0;360", spin.Values);
        AssertTiming(spin, [0, 1], "0 0 1 1", "5500ms", "indefinite");
    }

    [Fact]
    public void Translate_Grow_EasesEveryIntervalWithTheAnimationsCurve()
    {
        SvgDocument document = Translate(".arc { animation: grow 1.1s cubic-bezier(.8, 0, .35, .8) infinite; } @keyframes grow { 0%, 100% { stroke-dasharray: 15 345; } 40% { stroke-dasharray: 270 90; } }");

        SvgAnimate grow = Assert.IsType<SvgAnimate>(Assert.Single(AnimationsOfArc(document)));
        Assert.Equal("stroke-dasharray", grow.AnimationAttributeName);
        Assert.Equal("15 345;270 90;15 345", grow.Values);
        AssertTiming(grow, [0, 0.4f, 1], "0.8 0 0.35 0.8;0.8 0 0.35 0.8", "1100ms", "indefinite");
    }

    [Fact]
    public void Translate_Move_UsesTheTimingFunctionOfItsKeyframe()
    {
        SvgDocument document = Translate(".arc { animation: move 1.1s cubic-bezier(.8, .2, .35, .7) infinite; } @keyframes move { 0% { stroke-dashoffset: 0; animation-timing-function: linear; } 40% { stroke-dashoffset: -40; } 100% { stroke-dashoffset: -360; } }");

        SvgAnimate move = Assert.IsType<SvgAnimate>(Assert.Single(AnimationsOfArc(document)));
        Assert.Equal("stroke-dashoffset", move.AnimationAttributeName);
        Assert.Equal("0;-40;-360", move.Values);
        AssertTiming(move, [0, 0.4f, 1], "0 0 1 1;0.8 0.2 0.35 0.7", "1100ms", "indefinite");
    }

    [Fact]
    public void Translate_Color_StepsDiscretelyAndEndsWithTheElementsOwnColor()
    {
        SvgDocument document = Translate(".arc { animation: color 5.5s step-end infinite; } @keyframes color { 0% { stroke: #30aa51; } 20% { stroke: #ef4d38; } 40% { stroke: #458aff; } 60% { stroke: #ffbe00; } 80% { stroke: #ef4d38; } }");

        SvgAnimate color = Assert.IsType<SvgAnimate>(Assert.Single(AnimationsOfArc(document)));
        Assert.Equal("stroke", color.AnimationAttributeName);
        Assert.Equal("#30aa51;#ef4d38;#458aff;#ffbe00;#ef4d38;#30aa51", color.Values);
        AssertTiming(color, [0, 0.2f, 0.4f, 0.6f, 0.8f, 1], keySplines: null, "5500ms", "indefinite");
    }

    [Fact]
    public void Translate_SeveralAnimations_AddOneElementEachInDeclarationOrder()
    {
        SvgDocument document = Translate(TestSvg.BusyCss);

        string[] expected = ["transform", "stroke-dasharray", "stroke-dashoffset", "stroke"];
        Assert.Equal(expected, AnimationsOfArc(document).Select(animation => ((SvgAnimationAttributeElement)animation).AnimationAttributeName));
    }

    [Fact]
    public void Translate_MissingKeyframes_TakeTheElementsOwnValue()
    {
        SvgDocument document = Translate(".arc { animation: thin 1s linear infinite; } @keyframes thin { 50% { stroke-width: 1; } }");

        SvgAnimate thin = Assert.IsType<SvgAnimate>(Assert.Single(AnimationsOfArc(document)));
        Assert.Equal("3.25;1;3.25", thin.Values);
        Assert.Equal([0, 0.5f, 1], thin.KeyTimes);
    }

    [Fact]
    public void Translate_PxLengths_BecomeUserUnits()
    {
        SvgDocument document = Translate(".arc { animation: widen 1s infinite; } @keyframes widen { from { stroke-width: 2px; } to { stroke-width: 4px; } }");

        Assert.Equal("2;4", Assert.IsType<SvgAnimate>(Assert.Single(AnimationsOfArc(document))).Values);
    }

    [Fact]
    public void Translate_DashArraysOfDifferentLengths_RepeatToACommonLength()
    {
        SvgDocument document = Translate(".arc { animation: dash 1s infinite; } @keyframes dash { from { stroke-dasharray: 1 2 3; } to { stroke-dasharray: 4, 5; } }");

        Assert.Equal("1 2 3 1 2 3;4 5 4 5 4 5", Assert.IsType<SvgAnimate>(Assert.Single(AnimationsOfArc(document))).Values);
    }

    [Fact]
    public void Translate_SeveralTransformFunctions_BecomeOneAnimateTransformEach()
    {
        SvgDocument document = Translate(".arc { animation: wobble 1s linear infinite; } @keyframes wobble { from { transform: translate(0, 0) rotate(0deg); } to { transform: translate(4px, 2px) rotate(0.25turn); } }");

        List<SvgAnimationValueElement> animations = AnimationsOfArc(document);
        Assert.Equal(2, animations.Count);
        SvgAnimateTransform translate = Assert.IsType<SvgAnimateTransform>(animations[0]);
        SvgAnimateTransform rotate = Assert.IsType<SvgAnimateTransform>(animations[1]);
        Assert.Equal((SvgAnimateTransformType.Translate, "0 0;4 2", SvgAnimationAdditive.Replace), (translate.TransformType, translate.Values, translate.Additive));
        Assert.Equal((SvgAnimateTransformType.Rotate, "0;90", SvgAnimationAdditive.Sum), (rotate.TransformType, rotate.Values, rotate.Additive));
    }

    [Fact]
    public void Translate_Reverse_PlaysKeyframesBackwardsWithMirroredCurves()
    {
        SvgDocument document = Translate(".arc { animation: move 1.1s cubic-bezier(.8, .2, .35, .7) infinite reverse; } @keyframes move { 0% { stroke-dashoffset: 0; animation-timing-function: linear; } 40% { stroke-dashoffset: -40; } 100% { stroke-dashoffset: -360; } }");

        SvgAnimate move = Assert.IsType<SvgAnimate>(Assert.Single(AnimationsOfArc(document)));
        Assert.Equal("-360;-40;0", move.Values);
        AssertTiming(move, [0, 0.6f, 1], "0.65 0.3 0.2 0.8;0 0 1 1", "1100ms", "indefinite");
    }

    [Fact]
    public void Translate_Alternate_PlaysForwardsThenBackwardsInOneIterationOfTwiceTheLength()
    {
        SvgDocument document = Translate(".arc { animation: fade 1s ease-in 3 alternate; } @keyframes fade { from { opacity: 1; } to { opacity: 0; } }");

        SvgAnimate fade = Assert.IsType<SvgAnimate>(Assert.Single(AnimationsOfArc(document)));
        Assert.Equal("1;0;1", fade.Values);
        AssertTiming(fade, [0, 0.5f, 1], "0.42 0 1 1;0 0 0.58 1", "2000ms", "1.5");
    }

    [Fact]
    public void Translate_AlternateReverse_StartsBackwards()
    {
        SvgDocument document = Translate(".arc { animation: fade 1s linear infinite alternate-reverse; } @keyframes fade { from { opacity: 1; } to { opacity: 0; } }");

        Assert.Equal("0;1;0", Assert.IsType<SvgAnimate>(Assert.Single(AnimationsOfArc(document))).Values);
    }

    [Fact]
    public void Translate_StepStart_ShowsTheNextValueFromEachKeyTime()
    {
        SvgDocument document = Translate(".arc { animation: dim 1s step-start infinite; } @keyframes dim { 0% { opacity: 1; } 50% { opacity: 0.5; } 100% { opacity: 0; } }");

        SvgAnimate dim = Assert.IsType<SvgAnimate>(Assert.Single(AnimationsOfArc(document)));
        Assert.Equal("0.5;0;0", dim.Values);
        AssertTiming(dim, [0, 0.5f, 1], keySplines: null, "1000ms", "indefinite");
    }

    [Fact]
    public void Translate_DelayCountAndForwardsFill_BecomeBeginRepeatCountAndFreeze()
    {
        SvgDocument document = Translate(".arc { animation: fade 1s linear 250ms 2 forwards; } @keyframes fade { to { opacity: 0; } }");

        SvgAnimate fade = Assert.IsType<SvgAnimate>(Assert.Single(AnimationsOfArc(document)));
        Assert.Equal("250ms", fade.Begin);
        Assert.Equal("2", fade.RepeatCount);
        Assert.Equal(SvgAnimationFill.Freeze, fade.AnimationFill);
    }

    [Fact]
    public void Translate_InlineStyleAnimation_IsPlayed()
    {
        SvgDocument document = Translate("@keyframes fade { to { opacity: 0; } }", """<circle id="arc" r="10" style="animation: fade 2s linear infinite" />""");

        Assert.Equal("2000ms", Assert.IsType<SvgAnimate>(Assert.Single(AnimationsOfArc(document))).Duration);
    }

    [Theory]
    [InlineData(".arc { animation: fade 0s infinite; }")]
    [InlineData(".arc { animation: fade 1s 0; }")]
    [InlineData(".arc { animation: none; }")]
    [InlineData(".arc { animation: fade 1s infinite; } .arc { animation-name: none; }")]
    public void Translate_AnimationThatChangesNothing_AddsNothing(string rules)
    {
        SvgDocument document = Translate(rules + " @keyframes fade { to { opacity: 0; } }");

        Assert.Empty(AnimationsOfArc(document));
    }

    [Fact]
    public void Translate_StyleSheetWithoutAnimations_IgnoresWhatItCantRead()
    {
        SvgDocument document = Translate("g > circle:first-child { fill: red; } @keyframes unused { to { width: 10px; } } @media print { .arc { animation: unused 1s; } } @font-face { font-family: x; }");

        Assert.Empty(document.Descendants().OfType<SvgAnimationElement>());
    }

    [Theory]
    [InlineData(".arc { animation: nope 1s; }", "no @keyframes")]
    [InlineData(".arc { animation: fade 1s paused; }", "paused")]
    [InlineData(".arc { animation: fade 1s steps(3); }", "steps(3)")]
    [InlineData(".arc { animation: fade 1s cubic-bezier(.5, -0.5, .5, 1.5); }", "overshoots")]
    [InlineData(".arc { animation: fade 1s 1s backwards; }", "backwards")]
    [InlineData(".arc { animation: fade 0s forwards; }", "no duration")]
    [InlineData(".arc { animation: mixed 1s linear; }", "mixes steps")]
    [InlineData(".arc { animation: paint 1s linear; }", "can't be interpolated")]
    [InlineData(".arc { animation: radius 1s; }", "'r'")]
    [InlineData(".arc { animation: morph 1s; }", "different transform functions")]
    [InlineData(".arc { animation: beyond 1s; }", "invalid keyframe '150%'")]
    [InlineData(".arc { animation: spin 1s; transform-origin: center; }", "transform-origin")]
    [InlineData("g .arc { animation: fade 1s; }", "selector")]
    public void Translate_AnimationThatCantBeReproduced_Throws(string rules, string expectedMessage)
    {
        CursorConversionException exception = Assert.Throws<CursorConversionException>(() => Translate(rules + RejectedKeyframes));

        Assert.Contains(expectedMessage, exception.Message);
    }

    [Fact]
    public void Translate_ImplicitTransformKeyframeOnTransformedElement_Throws()
    {
        string content = """<circle id="arc" class="arc" r="10" transform="translate(1 1)" />""";

        CursorConversionException exception = Assert.Throws<CursorConversionException>(() => Translate(".arc { animation: spin 1s; } @keyframes spin { to { transform: rotate(360deg); } }", content));

        Assert.Contains("own transform", exception.Message);
    }

    [Fact]
    public void Translate_AnimationInsideClipPath_Throws()
    {
        string content = """<clipPath id="clip"><circle id="arc" class="arc" r="10" /></clipPath><rect width="10" height="10" clip-path="url(#clip)" />""";

        Assert.Throws<CursorConversionException>(() => Translate(".arc { animation: fade 1s; } @keyframes fade { to { opacity: 0; } }", content));
    }

    private static SvgDocument Translate(string css, string content = TestSvg.Arc)
    {
        SvgDocument document = TestSvg.Create(css, content);
        CssAnimationTranslator.Translate(document);
        return document;
    }

    private static List<SvgAnimationValueElement> AnimationsOfArc(SvgDocument document) => [.. document.GetElementById("arc").Children.OfType<SvgAnimationValueElement>()];

    private static void AssertTiming(SvgAnimationValueElement animation, float[] keyTimes, string? keySplines, string duration, string repeatCount)
    {
        Assert.Equal(keyTimes, animation.KeyTimes);
        Assert.Equal(keySplines is null ? SvgAnimationCalcMode.Discrete : SvgAnimationCalcMode.Spline, animation.CalcMode);
        if (keySplines is not null)
            Assert.Equal(keySplines, animation.KeySplines);
        Assert.Equal(duration, animation.Duration);
        Assert.Equal("0ms", animation.Begin);
        Assert.Equal(repeatCount, animation.RepeatCount);
        Assert.Equal(SvgAnimationFill.Remove, animation.AnimationFill);
    }
}

using VectorToCursor.Domain;
using VectorToCursor.Rendering.Animation.Css;

namespace VectorToCursor.Tests.Rendering.Animation.Css;

public sealed class CssTimingFunctionTests
{
    [Theory]
    [InlineData("linear", 0, 0, 1, 1)]
    [InlineData("ease", 0.25, 0.1, 0.25, 1)]
    [InlineData("ease-in", 0.42, 0, 1, 1)]
    [InlineData("ease-out", 0, 0, 0.58, 1)]
    [InlineData("ease-in-out", 0.42, 0, 0.58, 1)]
    [InlineData("cubic-bezier(.8, 0, .35, .8)", 0.8, 0, 0.35, 0.8)]
    [InlineData(" CUBIC-BEZIER(0.1,0.2,0.3,0.4) ", 0.1, 0.2, 0.3, 0.4)]
    public void Parse_Curve_IsACubicBezier(string text, double x1, double y1, double x2, double y2)
    {
        Assert.Equal(new CssTimingFunction.CubicBezier(x1, y1, x2, y2), CssTimingFunction.Parse(text));
    }

    [Theory]
    [InlineData("step-start", true)]
    [InlineData("step-end", false)]
    [InlineData("steps(1, start)", true)]
    [InlineData("steps(1, jump-start)", true)]
    [InlineData("steps(1, end)", false)]
    [InlineData("steps(1)", false)]
    public void Parse_SingleStep_IsAStep(string text, bool jumpsAtStart)
    {
        Assert.Equal(new CssTimingFunction.Step(jumpsAtStart), CssTimingFunction.Parse(text));
    }

    [Theory]
    [InlineData("bounce")]
    [InlineData("linear(0, 0.5, 1)")]
    [InlineData("steps(3)")]
    [InlineData("steps(2, end)")]
    [InlineData("steps(1, jump-both)")]
    [InlineData("cubic-bezier(.5, -0.5, .5, 1.5)")]
    [InlineData("cubic-bezier(1.2, 0, .5, 1)")]
    [InlineData("cubic-bezier(.1, .2, .3)")]
    [InlineData("cubic-bezier(a, b, c, d)")]
    public void Parse_FunctionThatCantBeReproduced_Throws(string text)
    {
        Assert.Throws<CursorConversionException>(() => CssTimingFunction.Parse(text));
    }

    [Fact]
    public void Reversed_Curve_IsPointMirrored()
    {
        CssTimingFunction.CubicBezier reversed = Assert.IsType<CssTimingFunction.CubicBezier>(new CssTimingFunction.CubicBezier(0.8, 0.2, 0.35, 0.7).Reversed());

        Assert.Equal("0.65 0.3 0.2 0.8", reversed.ToKeySpline());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Reversed_Step_JumpsAtTheOtherEnd(bool jumpsAtStart)
    {
        Assert.Equal(new CssTimingFunction.Step(!jumpsAtStart), new CssTimingFunction.Step(jumpsAtStart).Reversed());
    }

    [Fact]
    public void ToKeySpline_WritesInvariantNumbers()
    {
        Assert.Equal("0.25 0.1 0.25 1", ((CssTimingFunction.CubicBezier)CssTimingFunction.Ease).ToKeySpline());
    }

    [Theory]
    [InlineData("linear", true)]
    [InlineData("Ease-In-Out", true)]
    [InlineData("cubic-bezier(0,0,1,1)", true)]
    [InlineData("steps(1)", true)]
    [InlineData("infinite", false)]
    [InlineData("spin", false)]
    public void IsTimingFunction_RecognisesKeywordsAndFunctions(string token, bool expected)
    {
        Assert.Equal(expected, CssTimingFunction.IsTimingFunction(token));
    }
}

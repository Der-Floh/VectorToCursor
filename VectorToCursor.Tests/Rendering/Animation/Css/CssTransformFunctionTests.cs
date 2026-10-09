using Svg;
using VectorToCursor.Domain;
using VectorToCursor.Rendering.Animation.Css;

namespace VectorToCursor.Tests.Rendering.Animation.Css;

public sealed class CssTransformFunctionTests
{
    [Theory]
    [InlineData("rotate(90deg)", SvgAnimateTransformType.Rotate, "90")]
    [InlineData("rotate(0.5turn)", SvgAnimateTransformType.Rotate, "180")]
    [InlineData("rotate(200grad)", SvgAnimateTransformType.Rotate, "180")]
    [InlineData("rotate(0)", SvgAnimateTransformType.Rotate, "0")]
    [InlineData("translate(4px, 2)", SvgAnimateTransformType.Translate, "4 2")]
    [InlineData("translate(3px)", SvgAnimateTransformType.Translate, "3 0")]
    [InlineData("translateX(5)", SvgAnimateTransformType.Translate, "5 0")]
    [InlineData("translateY(-2px)", SvgAnimateTransformType.Translate, "0 -2")]
    [InlineData("scale(2)", SvgAnimateTransformType.Scale, "2 2")]
    [InlineData("scale(2, 0.5)", SvgAnimateTransformType.Scale, "2 0.5")]
    [InlineData("scaleX(3)", SvgAnimateTransformType.Scale, "3 1")]
    [InlineData("scaleY(3)", SvgAnimateTransformType.Scale, "1 3")]
    [InlineData("skewX(10deg)", SvgAnimateTransformType.SkewX, "10")]
    [InlineData("skewY(-10deg)", SvgAnimateTransformType.SkewY, "-10")]
    public void ParseList_Function_BecomesSvgTransform(string text, SvgAnimateTransformType expectedType, string expectedValue)
    {
        CssTransformFunction function = Assert.Single(CssTransformFunction.ParseList(text));

        Assert.Equal(expectedType, function.Type);
        Assert.Equal(expectedValue, function.ToSmilValue());
    }

    [Fact]
    public void ParseList_Radians_BecomeDegrees()
    {
        CssTransformFunction function = Assert.Single(CssTransformFunction.ParseList("rotate(1rad)"));

        Assert.Equal(180 / Math.PI, Assert.Single(function.Arguments), 1e-9);
    }

    [Fact]
    public void ParseList_SeveralFunctions_KeepTheirOrder()
    {
        List<CssTransformFunction> functions = CssTransformFunction.ParseList("translate(4px, 0) rotate(45deg)");

        Assert.Equal(new[] { SvgAnimateTransformType.Translate, SvgAnimateTransformType.Rotate }, functions.Select(function => function.Type));
    }

    [Theory]
    [InlineData("none")]
    [InlineData(" NONE ")]
    public void ParseList_None_IsEmpty(string text)
    {
        Assert.Empty(CssTransformFunction.ParseList(text));
    }

    [Theory]
    [InlineData("matrix(1, 0, 0, 1, 0, 0)")]
    [InlineData("rotate3d(1, 1, 1, 45deg)")]
    [InlineData("rotate(45)")]
    [InlineData("rotate(1em)")]
    [InlineData("translate(10%)")]
    [InlineData("rotate(45deg")]
    [InlineData("scale()")]
    public void ParseList_FunctionThatCantBeReproduced_Throws(string text)
    {
        Assert.Throws<CursorConversionException>(() => CssTransformFunction.ParseList(text));
    }

    [Theory]
    [InlineData(SvgAnimateTransformType.Translate, "0 0")]
    [InlineData(SvgAnimateTransformType.Scale, "1 1")]
    [InlineData(SvgAnimateTransformType.Rotate, "0")]
    [InlineData(SvgAnimateTransformType.SkewX, "0")]
    public void Identity_ChangesNothing(SvgAnimateTransformType type, string expectedValue)
    {
        Assert.Equal(expectedValue, CssTransformFunction.Identity(type).ToSmilValue());
    }
}

using VectorToCursor.Domain;
using VectorToCursor.Rendering.Animation.Css;

namespace VectorToCursor.Tests.Rendering.Animation.Css;

public sealed class CssAnimatedPropertyTests
{
    [Theory]
    [InlineData("opacity", " .5 ", "0.5")]
    [InlineData("fill-opacity", "1", "1")]
    [InlineData("stroke-width", "2px", "2")]
    [InlineData("stroke-dashoffset", "-40", "-40")]
    [InlineData("stroke", "#ef4d38", "#ef4d38")]
    [InlineData("fill", " rgb(1, 2, 3) ", "rgb(1, 2, 3)")]
    public void ToSmilValue_ConvertsCssValues(string property, string value, string expected)
    {
        Assert.Equal(expected, CssAnimatedProperty.ToSmilValue(property, value));
    }

    [Theory]
    [InlineData("opacity", "50%")]
    [InlineData("stroke-width", "1em")]
    [InlineData("stroke-dashoffset", "calc(1px + 2px)")]
    public void ToSmilValue_ValueThatIsNoPlainNumber_Throws(string property, string value)
    {
        Assert.Throws<CursorConversionException>(() => CssAnimatedProperty.ToSmilValue(property, value));
    }

    [Theory]
    [InlineData("opacity", true)]
    [InlineData("transform", true)]
    [InlineData("stroke-dasharray", true)]
    [InlineData("r", false)]
    [InlineData("width", false)]
    public void IsSupported_KnowsTheAnimatableProperties(string property, bool expected)
    {
        Assert.Equal(expected, CssAnimatedProperty.IsSupported(property));
    }
}

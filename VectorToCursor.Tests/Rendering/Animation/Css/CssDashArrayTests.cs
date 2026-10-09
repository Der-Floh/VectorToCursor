using VectorToCursor.Domain;
using VectorToCursor.Rendering.Animation.Css;

namespace VectorToCursor.Tests.Rendering.Animation.Css;

public sealed class CssDashArrayTests
{
    [Fact]
    public void ToSmilValues_ListsOfEqualLength_AreKept()
    {
        Assert.Equal(new[] { "15 345", "270 90", "15 345" }, CssDashArray.ToSmilValues(["15 345", "270 90", "15 345"]));
    }

    [Fact]
    public void ToSmilValues_ListsOfDifferentLength_RepeatToTheirLeastCommonMultiple()
    {
        Assert.Equal(new[] { "1 2 3 1 2 3", "4 5 4 5 4 5" }, CssDashArray.ToSmilValues(["1 2 3", "4,5"]));
    }

    [Fact]
    public void ToSmilValues_PxLengths_BecomeUserUnits()
    {
        Assert.Equal(new[] { "2 4", "6.5 8" }, CssDashArray.ToSmilValues(["2px 4px", "6.5PX, 8"]));
    }

    [Theory]
    [InlineData("none")]
    [InlineData("")]
    [InlineData("1 -2")]
    [InlineData("1 2%")]
    [InlineData("abc")]
    public void ToSmilValues_KeyframeWithoutLengths_Throws(string value)
    {
        Assert.Throws<CursorConversionException>(() => CssDashArray.ToSmilValues(["1 2", value]));
    }
}

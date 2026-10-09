using Svg;
using VectorToCursor.Rendering.Animation.Css;

namespace VectorToCursor.Tests.Rendering.Animation.Css;

public sealed class CssSelectorTests
{
    [Theory]
    [InlineData("circle", "circle", null, "", 1)]
    [InlineData("circle.arc", "circle", null, "arc", 11)]
    [InlineData("#spinner", null, "spinner", "", 100)]
    [InlineData(".a.b", null, null, "a b", 20)]
    [InlineData("rect#box.a", "rect", "box", "a", 111)]
    [InlineData("*", null, null, "", 0)]
    [InlineData(" .arc ", null, null, "arc", 10)]
    public void TryParse_CompoundSelector_ReadsItsParts(string text, string? type, string? id, string classes, int specificity)
    {
        Assert.True(CssSelector.TryParse(text, out CssSelector? selector));
        Assert.Equal(type, selector.Type);
        Assert.Equal(id, selector.Id);
        Assert.Equal(classes.Split(' ', StringSplitOptions.RemoveEmptyEntries), selector.Classes);
        Assert.Equal(specificity, selector.Specificity);
    }

    [Theory]
    [InlineData("g circle")]
    [InlineData("g > circle")]
    [InlineData("a + b")]
    [InlineData("[fill]")]
    [InlineData("circle:hover")]
    [InlineData("circle::before")]
    [InlineData("#a#b")]
    [InlineData("")]
    [InlineData(".")]
    [InlineData("#")]
    public void TryParse_UnsupportedSelector_ReturnsFalse(string text)
    {
        Assert.False(CssSelector.TryParse(text, out _));
    }

    [Theory]
    [InlineData("circle", true)]
    [InlineData("CIRCLE", true)]
    [InlineData("rect", false)]
    [InlineData("#arc", true)]
    [InlineData("#ARC", false)]
    [InlineData(".arc", true)]
    [InlineData(".arc.big", true)]
    [InlineData(".arc.small", false)]
    [InlineData("circle#arc.big", true)]
    [InlineData("*", true)]
    public void Matches_ComparesTypeIdAndClasses(string text, bool expected)
    {
        SvgDocument document = TestSvg.Create(string.Empty, """<circle id="arc" class="arc big" r="10" />""");

        Assert.True(CssSelector.TryParse(text, out CssSelector? selector));
        Assert.Equal(expected, selector.Matches(document.GetElementById("arc")));
    }
}

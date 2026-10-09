using VectorToCursor.Rendering.Animation.Css;

namespace VectorToCursor.Tests.Rendering.Animation.Css;

public sealed class CssStyleSheetParserTests
{
    [Fact]
    public void Parse_ReadsRulesAndKeyframes()
    {
        CssStyleSheet styleSheet = CssStyleSheetParser.Parse(TestSvg.BusyCss);

        CssStyleRule rule = Assert.Single(styleSheet.Rules);
        Assert.Equal(new[] { ".arc" }, rule.Selectors);
        Assert.Equal("animation", Assert.Single(rule.Declarations).Property);
        Assert.Equal(new[] { "color", "grow", "move", "spin" }, styleSheet.Keyframes.Keys.Order());
    }

    [Fact]
    public void Parse_KeyframeSelectorLists_AreKeptAsWritten()
    {
        CssStyleSheet styleSheet = CssStyleSheetParser.Parse(TestSvg.BusyCss);

        Assert.Equal(new[] { "0%, 100%", "40%" }, styleSheet.Keyframes["grow"].Blocks.Select(block => block.Selector));
    }

    [Fact]
    public void Parse_KeepsUnitlessValuesAndKeyframeTimingFunctions()
    {
        CssStyleSheet styleSheet = CssStyleSheetParser.Parse(TestSvg.BusyCss);

        IReadOnlyList<CssKeyframeBlock> move = styleSheet.Keyframes["move"].Blocks;
        Assert.Equal([new CssDeclaration("stroke-dashoffset", "0", false), new CssDeclaration("animation-timing-function", "linear", false)], move[0].Declarations);
        Assert.Equal([new CssDeclaration("stroke-dashoffset", "-40", false)], move[1].Declarations);
    }

    [Fact]
    public void Parse_ImportantFlag_IsSeparatedFromTheValue()
    {
        CssStyleSheet styleSheet = CssStyleSheetParser.Parse(".a { animation-duration: 2s !important; animation-delay: 1s ! IMPORTANT; animation-name: spin; }");

        CssDeclaration[] expected = [new("animation-duration", "2s", true), new("animation-delay", "1s", true), new("animation-name", "spin", false)];
        Assert.Equal(expected, Assert.Single(styleSheet.Rules).Declarations);
    }

    [Fact]
    public void Parse_SkipsCommentsImportsAndOtherAtRules()
    {
        CssStyleSheet styleSheet = CssStyleSheetParser.Parse("/* .x { animation: a 1s } */ @import url(x.css); @media (min-width: 1px) { .y { animation: b 1s; } } @font-face { font-family: x; } .z { animation: c 1s; }");

        Assert.Equal(new[] { ".z" }, Assert.Single(styleSheet.Rules).Selectors);
        Assert.Empty(styleSheet.Keyframes);
    }

    [Fact]
    public void Parse_SelectorList_KeepsEverySelector()
    {
        CssStyleSheet styleSheet = CssStyleSheetParser.Parse("circle.arc, #spinner { animation: spin 1s; }");

        Assert.Equal(new[] { "circle.arc", "#spinner" }, Assert.Single(styleSheet.Rules).Selectors);
    }

    [Fact]
    public void Parse_QuotedKeyframesName_IsUnquoted()
    {
        CssStyleSheet styleSheet = CssStyleSheetParser.Parse("@keyframes \"spin\" { to { opacity: 0; } }");

        Assert.True(styleSheet.Keyframes.ContainsKey("spin"));
    }

    [Fact]
    public void Parse_LaterKeyframesWithTheSameName_ReplaceEarlierOnes()
    {
        CssStyleSheet styleSheet = CssStyleSheetParser.Parse("@keyframes fade { to { opacity: 0; } } @keyframes fade { to { opacity: 0.5; } }");

        Assert.Equal("0.5", Assert.Single(Assert.Single(styleSheet.Keyframes["fade"].Blocks).Declarations).Value);
    }

    [Fact]
    public void Parse_PropertyNames_AreLowerCased()
    {
        CssStyleSheet styleSheet = CssStyleSheetParser.Parse(".a { Animation-Duration: 2s; }");

        Assert.Equal("animation-duration", Assert.Single(Assert.Single(styleSheet.Rules).Declarations).Property);
    }

    [Fact]
    public void Parse_UnterminatedBlock_KeepsWhatWasRead()
    {
        CssStyleSheet styleSheet = CssStyleSheetParser.Parse(".a { animation: spin 1s");

        Assert.Equal(new CssDeclaration("animation", "spin 1s", false), Assert.Single(Assert.Single(styleSheet.Rules).Declarations));
    }
}

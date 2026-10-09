using Svg;
using VectorToCursor.Domain;

namespace VectorToCursor.Rendering.Animation.Css;

/// <summary>A style rule that sets <c>animation</c> properties, with its selectors parsed.</summary>
internal sealed record CssAnimationRule(IReadOnlyList<CssSelector> Selectors, IReadOnlyList<CssDeclaration> Declarations, int Order)
{
    /// <summary>The rules of a style sheet that declare animations; all other rules are irrelevant here.</summary>
    /// <exception cref="CursorConversionException">An animation rule uses a selector that isn't supported.</exception>
    public static List<CssAnimationRule> FromStyleSheet(CssStyleSheet styleSheet)
    {
        ArgumentNullException.ThrowIfNull(styleSheet);

        return [.. styleSheet.Rules.Where(DeclaresAnimation).Select(rule => new CssAnimationRule([.. rule.Selectors.Select(ParseSelector)], rule.Declarations, rule.Order))];
    }

    /// <summary>The highest specificity among the selectors that match <paramref name="element"/>, or null when none does.</summary>
    public int? MatchSpecificity(SvgElement element) => Selectors.Where(selector => selector.Matches(element)).Select(selector => (int?)selector.Specificity).Max();

    private static bool DeclaresAnimation(CssStyleRule rule) => rule.Declarations.Any(declaration => CssAnimationProperty.IsAnimationProperty(declaration.Property));

    private static CssSelector ParseSelector(string text) =>
        CssSelector.TryParse(text, out CssSelector? selector)
            ? selector
            : throw new CursorConversionException($"The animated rule '{text}' uses a selector that is not supported; use element names, .classes and #ids.");
}

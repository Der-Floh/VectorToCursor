namespace VectorToCursor.Rendering.Animation.Css;

/// <param name="Selectors">The comma-separated selectors as written; they are parsed only when a rule matters for animation.</param>
/// <param name="Order">The rule's position in the style sheets, used to break cascade ties.</param>
internal sealed record CssStyleRule(IReadOnlyList<string> Selectors, IReadOnlyList<CssDeclaration> Declarations, int Order);

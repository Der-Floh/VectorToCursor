namespace VectorToCursor.Rendering.Animation.Css;

/// <summary>The style rules and keyframes of an SVG's style sheets; other at-rules are dropped.</summary>
internal sealed record CssStyleSheet(IReadOnlyList<CssStyleRule> Rules, IReadOnlyDictionary<string, CssKeyframes> Keyframes);

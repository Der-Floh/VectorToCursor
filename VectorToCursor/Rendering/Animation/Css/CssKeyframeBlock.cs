namespace VectorToCursor.Rendering.Animation.Css;

/// <param name="Selector">The keyframe selector as written, such as <c>0%, 100%</c> or <c>to</c>.</param>
internal sealed record CssKeyframeBlock(string Selector, IReadOnlyList<CssDeclaration> Declarations);

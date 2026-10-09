namespace VectorToCursor.Rendering.Animation.Css;

/// <summary>An <c>@keyframes</c> rule.</summary>
internal sealed record CssKeyframes(string Name, IReadOnlyList<CssKeyframeBlock> Blocks);

namespace VectorToCursor.Rendering.Animation.Css;

/// <summary>One resolved CSS animation of an element.</summary>
/// <param name="Name">The <c>@keyframes</c> it plays.</param>
/// <param name="IterationCount">How often it plays, or <see langword="null"/> for <c>infinite</c>.</param>
internal sealed record CssAnimation(string Name, TimeSpan Duration, CssTimingFunction TimingFunction, TimeSpan Delay, double? IterationCount, CssAnimationDirection Direction, CssAnimationFillMode FillMode, bool IsPaused);

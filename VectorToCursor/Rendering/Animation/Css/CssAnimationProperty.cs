namespace VectorToCursor.Rendering.Animation.Css;

/// <summary>The <c>animation</c> shorthand and its longhands, with their CSS initial values.</summary>
internal static class CssAnimationProperty
{
    public const string Shorthand = "animation";
    public const string Name = "animation-name";
    public const string Duration = "animation-duration";
    public const string TimingFunction = "animation-timing-function";
    public const string Delay = "animation-delay";
    public const string IterationCount = "animation-iteration-count";
    public const string Direction = "animation-direction";
    public const string FillMode = "animation-fill-mode";
    public const string PlayState = "animation-play-state";

    public static IReadOnlyDictionary<string, string> InitialValues { get; } = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        [Name] = "none",
        [Duration] = "0s",
        [TimingFunction] = "ease",
        [Delay] = "0s",
        [IterationCount] = "1",
        [Direction] = "normal",
        [FillMode] = "none",
        [PlayState] = "running",
    };

    public static bool IsAnimationProperty(string property) => property == Shorthand || InitialValues.ContainsKey(property);
}

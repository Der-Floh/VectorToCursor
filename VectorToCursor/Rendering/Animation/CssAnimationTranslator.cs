using Svg;
using VectorToCursor.Domain;
using VectorToCursor.Rendering.Animation.Css;

namespace VectorToCursor.Rendering.Animation;

/// <summary>
/// Turns the CSS animations of an SVG into SMIL elements. Svg.Skia's animation engine only plays SMIL and ignores
/// <c>@keyframes</c>, so without this the cursor would stay static.
/// </summary>
internal static class CssAnimationTranslator
{
    /// <exception cref="CursorConversionException">An animation that applies to an element uses something unsupported.</exception>
    public static void Translate(SvgDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        CssStyleSheet styleSheet = CssStyleSheetParser.Parse(string.Join('\n', document.Descendants().OfType<SvgStyle>().Select(style => style.Content ?? string.Empty)));
        List<CssAnimationRule> rules = CssAnimationRule.FromStyleSheet(styleSheet);

        // Materialized first: adding the SMIL elements changes the tree being enumerated.
        foreach (SvgElement element in document.Descendants().Where(element => element is not (SvgAnimationElement or SvgStyle)).ToList())
        {
            List<CssAnimation> animations = CssAnimationCascade.Resolve(element, rules);
            if (animations.Count == 0)
                continue;

            RejectResourceContent(element);
            foreach (CssAnimation animation in animations)
                SmilAnimationBuilder.Add(element, animation, FindKeyframes(styleSheet, animation.Name));
        }
    }

    // Content of gradients, clip paths, masks, patterns and markers is drawn by reference, which the engine doesn't animate reliably.
    private static void RejectResourceContent(SvgElement element)
    {
        for (SvgElement? current = element; current is not null; current = current.Parent)
        {
            if (current is SvgGradientServer or SvgGradientStop or SvgClipPath or SvgMask or SvgPatternServer or SvgMarker)
                throw new CursorConversionException("Animations inside gradients, clip paths, masks, patterns or markers are not supported.");
        }
    }

    // Browsers ignore an unknown name, but here it's most likely a typo that would silently produce a static cursor.
    private static CssKeyframes FindKeyframes(CssStyleSheet styleSheet, string name) =>
        styleSheet.Keyframes.TryGetValue(name, out CssKeyframes? keyframes)
            ? keyframes
            : throw new CursorConversionException($"The animation '{name}' is used, but there is no @keyframes with that name.");
}

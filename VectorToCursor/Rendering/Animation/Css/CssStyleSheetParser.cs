namespace VectorToCursor.Rendering.Animation.Css;

/// <summary>
/// Reads the style rules and <c>@keyframes</c> of a style sheet. It never throws: anything it can't read is skipped, because
/// most style sheets only style static content, which Svg.Skia already handles.
/// </summary>
internal static class CssStyleSheetParser
{
    private const string KeyframesAtRule = "@keyframes";
    private const string ImportantFlag = "important";

    public static CssStyleSheet Parse(string css)
    {
        ArgumentNullException.ThrowIfNull(css);

        string text = CssText.StripComments(css);
        List<CssStyleRule> rules = [];
        Dictionary<string, CssKeyframes> keyframes = new(StringComparer.Ordinal);
        int position = 0;
        while (TryReadBlock(text, ref position, out string prelude, out string? body))
        {
            if (body is null)
                continue;
            if (prelude.StartsWith('@'))
            {
                // Later @keyframes with the same name replace earlier ones; all other at-rules (@media, @font-face, ...) are ignored.
                if (TryGetKeyframesName(prelude, out string name))
                    keyframes[name] = new CssKeyframes(name, ParseKeyframeBlocks(body));
                continue;
            }
            rules.Add(new CssStyleRule(CssText.SplitTopLevel(prelude, ','), ParseDeclarations(body), rules.Count));
        }
        return new CssStyleSheet(rules, keyframes);
    }

    public static List<CssDeclaration> ParseDeclarations(string block)
    {
        List<CssDeclaration> declarations = [];
        foreach (string part in CssText.SplitTopLevel(block, ';'))
        {
            int colon = part.IndexOf(':', StringComparison.Ordinal);
            if (colon <= 0)
                continue;

            string property = part[..colon].Trim().ToLowerInvariant();
            string value = part[(colon + 1)..].Trim();
            bool important = TryStripImportant(ref value);
            if (value.Length > 0)
                declarations.Add(new CssDeclaration(property, value, important));
        }
        return declarations;
    }

    // Reads either "prelude { body }" or a "prelude;" statement such as @import, whose body is null.
    private static bool TryReadBlock(string text, ref int position, out string prelude, out string? body)
    {
        prelude = string.Empty;
        body = null;
        int end = CssText.IndexOfTopLevel(text, position, '{', ';');
        if (end < 0)
            return false;

        prelude = text[position..end].Trim();
        if (text[end] == ';')
        {
            position = end + 1;
            return true;
        }

        int close = CssText.FindBlockEnd(text, end);
        body = text[(end + 1)..Math.Min(close, text.Length)];
        position = Math.Min(close + 1, text.Length);
        return true;
    }

    private static bool TryGetKeyframesName(string prelude, out string name)
    {
        name = string.Empty;
        if (!prelude.StartsWith(KeyframesAtRule, StringComparison.OrdinalIgnoreCase) || prelude.Length == KeyframesAtRule.Length || !char.IsWhiteSpace(prelude[KeyframesAtRule.Length]))
            return false;

        name = CssText.Unquote(prelude[KeyframesAtRule.Length..].Trim());
        return name.Length > 0;
    }

    private static List<CssKeyframeBlock> ParseKeyframeBlocks(string body)
    {
        List<CssKeyframeBlock> blocks = [];
        int position = 0;
        while (TryReadBlock(body, ref position, out string selector, out string? declarations))
        {
            if (declarations is not null)
                blocks.Add(new CssKeyframeBlock(selector, ParseDeclarations(declarations)));
        }
        return blocks;
    }

    private static bool TryStripImportant(ref string value)
    {
        if (!value.EndsWith(ImportantFlag, StringComparison.OrdinalIgnoreCase))
            return false;

        string rest = value[..^ImportantFlag.Length].TrimEnd();
        if (!rest.EndsWith('!'))
            return false;

        value = rest[..^1].TrimEnd();
        return true;
    }
}

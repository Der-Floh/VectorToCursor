namespace VectorToCursor.Rendering.Animation.Css;

/// <summary>Splitting helpers for CSS text that respect parentheses, quotes and comments.</summary>
internal static class CssText
{
    public static string StripComments(string css)
    {
        System.Text.StringBuilder result = new(css.Length);
        char quote = '\0';
        for (int index = 0; index < css.Length; index++)
        {
            char character = css[index];
            if (quote == '\0' && character == '/' && index + 1 < css.Length && css[index + 1] == '*')
            {
                int end = css.IndexOf("*/", index + 2, StringComparison.Ordinal);
                index = end < 0 ? css.Length : end + 1;
                result.Append(' ');
                continue;
            }
            if (quote == '\0' && character is '"' or '\'')
                quote = character;
            else if (character == quote)
                quote = '\0';
            result.Append(character);
        }
        return result.ToString();
    }

    /// <summary>Splits on <paramref name="separator"/> outside parentheses and quotes; parts are trimmed and empty ones dropped.</summary>
    public static List<string> SplitTopLevel(string text, char separator) => Split(text, character => character == separator);

    /// <summary>Splits on whitespace outside parentheses and quotes, so <c>cubic-bezier(.8, 0, .35, .8)</c> stays one part.</summary>
    public static List<string> SplitOnWhitespace(string text) => Split(text, char.IsWhiteSpace);

    /// <summary>The index of the first top-level <paramref name="target"/> at or after <paramref name="start"/>, or -1.</summary>
    public static int IndexOfTopLevel(string text, int start, params char[] targets)
    {
        int depth = 0;
        char quote = '\0';
        for (int index = start; index < text.Length; index++)
        {
            char character = text[index];
            if (quote != '\0')
            {
                if (character == quote)
                    quote = '\0';
                continue;
            }
            if (character is '"' or '\'')
                quote = character;
            else if (character == '(')
                depth++;
            else if (character == ')')
                depth = Math.Max(0, depth - 1);
            else if (depth == 0 && Array.IndexOf(targets, character) >= 0)
                return index;
        }
        return -1;
    }

    /// <summary>The index of the brace closing the block opened at <paramref name="openBrace"/>, or the text length when it's missing.</summary>
    public static int FindBlockEnd(string text, int openBrace)
    {
        int depth = 0;
        int index = openBrace;
        while (index >= 0 && index < text.Length)
        {
            index = IndexOfTopLevel(text, index, '{', '}');
            if (index < 0)
                break;
            depth += text[index] == '{' ? 1 : -1;
            if (depth == 0)
                return index;
            index++;
        }
        return text.Length;
    }

    public static string Unquote(string text) => text.Length >= 2 && text[0] is '"' or '\'' && text[^1] == text[0] ? text[1..^1] : text;

    private static List<string> Split(string text, Func<char, bool> isSeparator)
    {
        List<string> parts = [];
        int start = 0;
        int depth = 0;
        char quote = '\0';
        for (int index = 0; index < text.Length; index++)
        {
            char character = text[index];
            if (quote != '\0')
            {
                if (character == quote)
                    quote = '\0';
                continue;
            }
            if (character is '"' or '\'')
                quote = character;
            else if (character == '(')
                depth++;
            else if (character == ')')
                depth = Math.Max(0, depth - 1);
            else if (depth == 0 && isSeparator(character))
            {
                AddTrimmed(parts, text[start..index]);
                start = index + 1;
            }
        }
        AddTrimmed(parts, text[start..]);
        return parts;
    }

    private static void AddTrimmed(List<string> parts, string part)
    {
        string trimmed = part.Trim();
        if (trimmed.Length > 0)
            parts.Add(trimmed);
    }
}

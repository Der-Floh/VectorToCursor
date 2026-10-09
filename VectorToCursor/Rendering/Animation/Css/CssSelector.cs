using System.Diagnostics.CodeAnalysis;
using Svg;

namespace VectorToCursor.Rendering.Animation.Css;

/// <summary>
/// A compound selector such as <c>circle.arc</c>, <c>#spinner</c> or <c>*</c>. Combinators, attribute selectors and
/// pseudo-classes are not supported.
/// </summary>
internal sealed record CssSelector(string? Type, string? Id, IReadOnlyList<string> Classes)
{
    private const int IdWeight = 100;
    private const int ClassWeight = 10;
    private const int TypeWeight = 1;
    private const string Universal = "*";

    public int Specificity => (Id is null ? 0 : IdWeight) + Classes.Count * ClassWeight + (Type is null ? 0 : TypeWeight);

    public static bool TryParse(string text, [NotNullWhen(true)] out CssSelector? selector)
    {
        ArgumentNullException.ThrowIfNull(text);

        selector = null;
        string trimmed = text.Trim();
        if (trimmed == Universal)
        {
            selector = new CssSelector(null, null, []);
            return true;
        }

        int index = 0;
        string? type = ReadName(trimmed, ref index) is { Length: > 0 } name ? name : null;
        string? id = null;
        List<string> classes = [];
        while (index < trimmed.Length)
        {
            char marker = trimmed[index++];
            string part = ReadName(trimmed, ref index);
            if (part.Length == 0)
                return false;
            if (marker == '.')
                classes.Add(part);
            else if (marker == '#' && id is null)
                id = part;
            else
                return false;
        }

        if (type is null && id is null && classes.Count == 0)
            return false;

        selector = new CssSelector(type, id, classes);
        return true;
    }

    public bool Matches(SvgElement element)
    {
        ArgumentNullException.ThrowIfNull(element);

        return (Type is null || string.Equals(SvgElementNames.NameOf(element), Type, StringComparison.OrdinalIgnoreCase))
            && (Id is null || string.Equals(element.ID, Id, StringComparison.Ordinal))
            && (Classes.Count == 0 || ContainsAll(ClassesOf(element), Classes));
    }

    private static string ReadName(string text, ref int index)
    {
        int start = index;
        while (index < text.Length && (char.IsLetterOrDigit(text[index]) || text[index] is '-' or '_'))
            index++;
        return text[start..index];
    }

    private static HashSet<string> ClassesOf(SvgElement element) =>
        element.TryGetAttribute("class", out string classes) ? [.. classes.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)] : [];

    private static bool ContainsAll(HashSet<string> available, IReadOnlyList<string> required) => required.All(available.Contains);
}

using System.CommandLine.Parsing;
using VectorToCursor.Domain;

namespace VectorToCursor.Cli;

/// <summary>Parses image format names regardless of case, so "PNG" means the same as "png".</summary>
internal static class ImageFormatParser
{
    private static readonly (string Name, CursorImageFormat Format)[] Formats = [("bmp", CursorImageFormat.Bmp), ("png", CursorImageFormat.Png)];

    public static string HelpName { get; } = string.Join('|', Names);

    private static IEnumerable<string> Names => Formats.Select(entry => entry.Name);

    public static string NameOf(CursorImageFormat format) => Formats.First(entry => entry.Format == format).Name;

    public static CursorImageFormat? Parse(ArgumentResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        string token = result.Tokens[0].Value;
        foreach ((string name, CursorImageFormat format) in Formats)
        {
            if (string.Equals(token, name, StringComparison.OrdinalIgnoreCase))
                return format;
        }

        string optionName = result.Parent is OptionResult optionResult ? optionResult.Option.Name : "the option";
        result.AddError($"Invalid value '{token}' for {optionName}. Expected {string.Join(" or ", Names)}.");
        return null;
    }
}

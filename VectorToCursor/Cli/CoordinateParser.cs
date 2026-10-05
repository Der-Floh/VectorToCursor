using System.CommandLine.Parsing;
using System.Globalization;

namespace VectorToCursor.Cli;

/// <summary>Parses hotspot coordinates independently of the machine's culture, so "3.5" means the same everywhere.</summary>
internal static class CoordinateParser
{
    public static double Parse(ArgumentResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        string token = result.Tokens[0].Value;
        // NumberStyles.Float excludes thousands separators, so "3,5" is rejected instead of being read as 35.
        if (double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out double value) && double.IsFinite(value))
            return value;

        result.AddError($"Invalid value '{token}' for {OptionName(result)}. Expected a number with '.' as the decimal separator, e.g. 3.5.");
        return default;
    }

    private static string OptionName(ArgumentResult result) => result.Parent is OptionResult optionResult ? optionResult.Option.Name : "the coordinate";
}

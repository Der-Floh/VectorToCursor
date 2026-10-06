using System.CommandLine.Parsing;
using System.Globalization;
using VectorToCursor.Domain;

namespace VectorToCursor.Cli;

/// <summary>Parses numeric option values independently of the machine's culture, so "3.5" means the same everywhere.</summary>
internal static class NumberParser
{
    public static double ParseCoordinate(ArgumentResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (TryParse(result, out double value))
            return value;

        result.AddError(InvalidNumberMessage(result));
        return default;
    }

    // Errors must be reported via AddError: an exception thrown here would escape Parse() instead of becoming a parse error.
    public static BleedPercentage ParseBleedPercentage(ArgumentResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (!TryParse(result, out double value))
        {
            result.AddError(InvalidNumberMessage(result));
            return default;
        }
        if (BleedPercentage.TryCreate(value, out BleedPercentage percentage))
            return percentage;

        result.AddError($"Invalid value '{Token(result)}' for {OptionName(result)}. Expected a percentage from {BleedPercentage.Minimum} to {BleedPercentage.Maximum}.");
        return default;
    }

    // NumberStyles.Float excludes thousands separators, so "3,5" is rejected instead of being read as 35.
    private static bool TryParse(ArgumentResult result, out double value) =>
        double.TryParse(Token(result), NumberStyles.Float, CultureInfo.InvariantCulture, out value) && double.IsFinite(value);

    private static string InvalidNumberMessage(ArgumentResult result) => $"Invalid value '{Token(result)}' for {OptionName(result)}. Expected a number with '.' as the decimal separator, e.g. 3.5.";

    private static string Token(ArgumentResult result) => result.Tokens[0].Value;

    private static string OptionName(ArgumentResult result) => result.Parent is OptionResult optionResult ? optionResult.Option.Name : "the option";
}

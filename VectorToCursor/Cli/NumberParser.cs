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

    public static FrameRate ParseFrameRate(ArgumentResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (int.TryParse(Token(result), NumberStyles.None, CultureInfo.InvariantCulture, out int framesPerSecond) && FrameRate.TryCreate(framesPerSecond, out FrameRate frameRate))
            return frameRate;

        IEnumerable<int> allowed = Enumerable.Range(1, FrameRate.JiffiesPerSecond).Where(rate => FrameRate.JiffiesPerSecond % rate == 0).Reverse();
        result.AddError($"Invalid value '{Token(result)}' for {OptionName(result)}. Expected a frame rate that divides {FrameRate.JiffiesPerSecond}: {string.Join(", ", allowed)}.");
        return default;
    }

    public static CursorSizes? ParseCursorSizes(ArgumentResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        List<int> sizes = [];
        foreach (string part in Token(result).Split(',', StringSplitOptions.TrimEntries))
        {
            if (!int.TryParse(part, NumberStyles.None, CultureInfo.InvariantCulture, out int size))
                return RejectCursorSizes(result);
            sizes.Add(size);
        }
        return CursorSizes.TryCreate(sizes, out CursorSizes? cursorSizes) ? cursorSizes : RejectCursorSizes(result);
    }

    private static CursorSizes? RejectCursorSizes(ArgumentResult result)
    {
        result.AddError($"Invalid value '{Token(result)}' for {OptionName(result)}. Expected comma-separated sizes from {CursorSizes.Minimum} to {CursorSizes.Maximum} px, each listed once, e.g. 32,48,64.");
        return null;
    }

    // NumberStyles.Float excludes thousands separators, so "3,5" is rejected instead of being read as 35.
    private static bool TryParse(ArgumentResult result, out double value) =>
        double.TryParse(Token(result), NumberStyles.Float, CultureInfo.InvariantCulture, out value) && double.IsFinite(value);

    private static string InvalidNumberMessage(ArgumentResult result) => $"Invalid value '{Token(result)}' for {OptionName(result)}. Expected a number with '.' as the decimal separator, e.g. 3.5.";

    private static string Token(ArgumentResult result) => result.Tokens[0].Value;

    private static string OptionName(ArgumentResult result) => result.Parent is OptionResult optionResult ? optionResult.Option.Name : "the option";
}

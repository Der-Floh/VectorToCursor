using System.Globalization;

namespace VectorToCursor.Rendering.Animation.Css;

/// <summary>CSS times such as "1.1s" or "250ms", evaluated exactly and rounded to whole milliseconds.</summary>
internal static class CssTime
{
    private const decimal MillisecondsPerSecond = 1000;

    public static bool TryParse(string text, out TimeSpan value)
    {
        ArgumentNullException.ThrowIfNull(text);

        value = default;
        string trimmed = text.Trim();
        decimal scale;
        string number;
        if (trimmed.EndsWith("ms", StringComparison.OrdinalIgnoreCase))
        {
            (scale, number) = (1, trimmed[..^2]);
        }
        else if (trimmed.EndsWith('s') || trimmed.EndsWith('S'))
        {
            (scale, number) = (MillisecondsPerSecond, trimmed[..^1]);
        }
        else
        {
            return false;
        }

        // The limit is far beyond any loop length, and small enough that the conversion to ticks can't overflow.
        if (!decimal.TryParse(number, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal amount) || Math.Abs(amount) > MaximumMilliseconds / scale)
            return false;

        decimal milliseconds = Math.Round(amount * scale, MidpointRounding.AwayFromZero);
        value = TimeSpan.FromTicks((long)milliseconds * TimeSpan.TicksPerMillisecond);
        return true;
    }

    private static decimal MaximumMilliseconds => (decimal)TimeSpan.MaxValue.TotalMilliseconds / 2;
}

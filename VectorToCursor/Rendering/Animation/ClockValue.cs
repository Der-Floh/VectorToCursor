using System.Globalization;

namespace VectorToCursor.Rendering.Animation;

/// <summary>
/// SMIL clock values such as "1.5s", "300ms" or "0:01.5". The syntax matches Svg.Skia's animation engine, but values are
/// evaluated exactly and rounded to whole milliseconds; <see cref="Format"/> writes them back in a form the engine reads
/// without floating-point loss (it would read "0.7s" as one tick short of 700 ms).
/// </summary>
internal static class ClockValue
{
    private const string Indefinite = "indefinite";
    private const decimal MillisecondsPerSecond = 1000;
    private const decimal MillisecondsPerMinute = 60 * MillisecondsPerSecond;
    private const decimal MillisecondsPerHour = 60 * MillisecondsPerMinute;
    private const int MinutesPerHour = 60;
    private const int SecondsPerMinute = 60;

    // Checked in this order, like the engine: "ms" before "s", "min" before "h".
    private static readonly (string Suffix, decimal Milliseconds)[] Units = [("ms", 1), ("min", MillisecondsPerMinute), ("h", MillisecondsPerHour), ("s", MillisecondsPerSecond)];

    public static bool TryParse(string? text, out TimeSpan value)
    {
        value = default;
        ReadOnlySpan<char> part = text.AsSpan().Trim();
        if (part.IsEmpty || IsIndefinite(part))
            return false;

        decimal sign = 1;
        if (part[0] is '+' or '-')
        {
            sign = part[0] == '-' ? -1 : 1;
            part = part[1..].Trim();
            if (part.IsEmpty || IsIndefinite(part))
                return false;
        }

        return TryParseMilliseconds(part, out decimal milliseconds) && TryCreate(sign * milliseconds, out value);
    }

    /// <summary>Formats a whole-millisecond time such as "1100ms" or "-550ms".</summary>
    public static string Format(TimeSpan value) => value.TotalMilliseconds.ToString("0.####", CultureInfo.InvariantCulture) + "ms";

    public static bool IsIndefinite(ReadOnlySpan<char> text) => text.Trim().Equals(Indefinite, StringComparison.OrdinalIgnoreCase);

    private static bool TryParseMilliseconds(ReadOnlySpan<char> text, out decimal milliseconds)
    {
        if (text.Contains(':'))
            return TryParseColonClock(text, out milliseconds);

        foreach ((string suffix, decimal scale) in Units)
        {
            if (text.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                return TryScale(text[..^suffix.Length], scale, out milliseconds);
        }

        return TryScale(text, MillisecondsPerSecond, out milliseconds);
    }

    private static bool TryScale(ReadOnlySpan<char> number, decimal scale, out decimal milliseconds)
    {
        milliseconds = 0;
        if (!TryParseDecimal(number, out decimal scalar) || Math.Abs(scalar) > MaximumMilliseconds / scale)
            return false;

        milliseconds = scalar * scale;
        return true;
    }

    // "hh:mm:ss.f" or "mm:ss.f", with minutes and seconds below 60.
    private static bool TryParseColonClock(ReadOnlySpan<char> text, out decimal milliseconds)
    {
        milliseconds = 0;
        string[] parts = text.ToString().Split(':');
        if (parts.Length is < 2 or > 3)
            return false;

        int hours = 0;
        if (parts.Length == 3 && !int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out hours))
            return false;
        if (!int.TryParse(parts[^2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int minutes) || !TryParseDecimal(parts[^1], out decimal seconds))
            return false;
        if (hours < 0 || minutes is < 0 or >= MinutesPerHour || seconds < 0 || seconds >= SecondsPerMinute)
            return false;

        milliseconds = hours * MillisecondsPerHour + minutes * MillisecondsPerMinute + seconds * MillisecondsPerSecond;
        return true;
    }

    private static bool TryParseDecimal(ReadOnlySpan<char> text, out decimal value) => decimal.TryParse(text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value);

    private static bool TryCreate(decimal milliseconds, out TimeSpan value)
    {
        value = default;
        decimal whole = Math.Round(milliseconds, MidpointRounding.AwayFromZero);
        if (Math.Abs(whole) > MaximumMilliseconds)
            return false;

        value = TimeSpan.FromTicks((long)whole * TimeSpan.TicksPerMillisecond);
        return true;
    }

    // Far beyond any loop length, and small enough that the conversion to ticks can't overflow.
    private static decimal MaximumMilliseconds => (decimal)TimeSpan.MaxValue.TotalMilliseconds / 2;
}

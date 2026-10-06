using System.Globalization;

namespace VectorToCursor.Domain;

/// <summary>
/// Width of the color bleed band around the artwork, as a percentage of the cursor size. Within the band, transparent
/// pixels take the nearest edge color; 0 turns bleeding off.
/// </summary>
internal readonly record struct BleedPercentage
{
    public const double Minimum = 0;

    public const double Maximum = 100;

    public BleedPercentage(double value)
    {
        if (!IsValid(value))
            throw new ArgumentOutOfRangeException(nameof(value), value, $"The bleed must be a percentage from {Minimum} to {Maximum}.");

        Value = value;
    }

    public static BleedPercentage Default { get; } = new(5);

    public double Value { get; }

    public static bool TryCreate(double value, out BleedPercentage result)
    {
        bool valid = IsValid(value);
        result = valid ? new BleedPercentage(value) : default;
        return valid;
    }

    /// <summary>The band width in whole pixels for a cursor image of <paramref name="size"/> pixels, rounded up.</summary>
    public int BandWidthFor(int size)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(size);

        return (int)Math.Ceiling(size * Value / 100);
    }

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);

    private static bool IsValid(double value) => double.IsFinite(value) && value >= Minimum && value <= Maximum;
}

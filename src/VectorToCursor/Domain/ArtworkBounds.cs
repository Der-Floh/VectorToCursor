using System.Runtime.CompilerServices;

namespace VectorToCursor.Domain;

/// <summary>The rectangle of the SVG's user coordinate system that is drawn into the cursor.</summary>
internal sealed record ArtworkBounds
{
    public ArtworkBounds(double minX, double minY, double width, double height)
    {
        MinX = RequireFinite(minX);
        MinY = RequireFinite(minY);
        Width = RequirePositive(width);
        Height = RequirePositive(height);
    }

    public double MinX { get; }

    public double MinY { get; }

    public double Width { get; }

    public double Height { get; }

    private static double RequireFinite(double value, [CallerArgumentExpression(nameof(value))] string? parameterName = null) =>
        double.IsFinite(value) ? value : throw new ArgumentOutOfRangeException(parameterName, value, "The value must be a finite number.");

    private static double RequirePositive(double value, [CallerArgumentExpression(nameof(value))] string? parameterName = null) =>
        double.IsFinite(value) && value > 0 ? value : throw new ArgumentOutOfRangeException(parameterName, value, "The value must be a positive finite number.");
}

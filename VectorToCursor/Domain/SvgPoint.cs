namespace VectorToCursor.Domain;

/// <summary>A point in the SVG's user coordinate system (viewBox units).</summary>
internal readonly record struct SvgPoint(double X, double Y);

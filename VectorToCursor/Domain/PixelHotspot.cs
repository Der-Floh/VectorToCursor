namespace VectorToCursor.Domain;

/// <summary>The hotspot of one cursor image, as pixel column and row from its top-left corner.</summary>
internal readonly record struct PixelHotspot(int X, int Y);

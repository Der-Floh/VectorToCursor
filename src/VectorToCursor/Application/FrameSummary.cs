using VectorToCursor.Domain;

namespace VectorToCursor.Application;

/// <summary>The size and pixel hotspot of one image in a written cursor.</summary>
internal readonly record struct FrameSummary(int Size, PixelHotspot Hotspot);

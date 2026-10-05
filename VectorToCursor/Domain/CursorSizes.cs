namespace VectorToCursor.Domain;

internal static class CursorSizes
{
    /// <summary>The square pixel sizes written into every cursor, smallest first.</summary>
    public static IReadOnlyList<int> All { get; } = [32, 48, 64, 96, 128, 256];
}

namespace VectorToCursor.Tests;

internal static class TestFiles
{
    public static string DataDirectory { get; } = Path.Combine(AppContext.BaseDirectory, "TestData");

    public static string PathOf(string fileName) => Path.Combine(DataDirectory, fileName);
}

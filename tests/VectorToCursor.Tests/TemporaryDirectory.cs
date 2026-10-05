namespace VectorToCursor.Tests;

internal sealed class TemporaryDirectory : IDisposable
{
    public TemporaryDirectory()
    {
        FullPath = Path.Combine(Path.GetTempPath(), "VectorToCursor.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(FullPath);
    }

    public string FullPath { get; }

    public string PathOf(params string[] relativeParts) => Path.Combine([FullPath, .. relativeParts]);

    public void Dispose() => Directory.Delete(FullPath, recursive: true);
}

using VectorToCursor.Installation;

namespace VectorToCursor.Tests.Installation;

internal sealed class FakeUserPathStore : IUserPathStore
{
    public FakeUserPathStore(string value = "") => Value = value;

    public string Value { get; private set; }

    public int WriteCount { get; private set; }

    public string Read() => Value;

    public void Write(string path)
    {
        Value = path;
        WriteCount++;
    }
}

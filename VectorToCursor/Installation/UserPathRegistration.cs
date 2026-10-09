namespace VectorToCursor.Installation;

/// <summary>Lists a folder in the current user's PATH, so the programs in it can be run by name from any new terminal.</summary>
internal sealed class UserPathRegistration
{
    private readonly IUserPathStore _store;
    private readonly string _folder;

    public UserPathRegistration(IUserPathStore store, string folder)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentException.ThrowIfNullOrWhiteSpace(folder);

        _store = store;
        _folder = Path.TrimEndingDirectorySeparator(folder);
    }

    /// <summary>Adds the folder as the last entry of the PATH, unless the PATH already lists it.</summary>
    public void Register()
    {
        string path = _store.Read();
        if (!PathEntries.Contains(path, _folder))
            _store.Write(PathEntries.Append(path, _folder));
    }

    /// <summary>Removes every entry of the PATH that names the folder.</summary>
    public void Unregister()
    {
        string path = _store.Read();
        if (PathEntries.Contains(path, _folder))
            _store.Write(PathEntries.Remove(path, _folder));
    }
}

namespace VectorToCursor.Installation;

/// <summary>
/// Edits a Windows PATH value, a list of folders separated by semicolons, leaving every entry it doesn't add or remove as
/// it is. Folders are compared the way Windows resolves them: ignoring case, surrounding quotes, a trailing backslash and
/// environment variables such as %LOCALAPPDATA%.
/// </summary>
internal static class PathEntries
{
    public const char Separator = ';';

    public static bool Contains(string path, string folder)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentException.ThrowIfNullOrWhiteSpace(folder);

        string target = Normalize(folder);
        return path.Split(Separator).Any(entry => IsSameFolder(entry, target));
    }

    /// <returns>
    /// <paramref name="path"/> with <paramref name="folder"/> added as its last entry, before a trailing separator, so that
    /// <see cref="Remove"/> gives back <paramref name="path"/> exactly.
    /// </returns>
    public static string Append(string path, string folder)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentException.ThrowIfNullOrWhiteSpace(folder);

        if (path.Length == 0)
            return folder;
        return path[^1] == Separator ? path + folder + Separator : path + Separator + folder;
    }

    /// <returns><paramref name="path"/> without the entries that name <paramref name="folder"/>.</returns>
    public static string Remove(string path, string folder)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentException.ThrowIfNullOrWhiteSpace(folder);

        string target = Normalize(folder);
        return string.Join(Separator, path.Split(Separator).Where(entry => !IsSameFolder(entry, target)));
    }

    private static bool IsSameFolder(string entry, string normalizedFolder) => string.Equals(Normalize(entry), normalizedFolder, StringComparison.OrdinalIgnoreCase);

    private static string Normalize(string entry) => Environment.ExpandEnvironmentVariables(entry.Trim().Trim('"')).TrimEnd('\\', '/');
}

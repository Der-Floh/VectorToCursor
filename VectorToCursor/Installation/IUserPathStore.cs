namespace VectorToCursor.Installation;

/// <summary>Where the current user's PATH is stored.</summary>
internal interface IUserPathStore
{
    /// <returns>The stored PATH with its environment variables unexpanded, or an empty string when there is none.</returns>
    string Read();

    /// <summary>Stores <paramref name="path"/>, or removes the value when it is empty, and tells running programs that it changed.</summary>
    void Write(string path);
}

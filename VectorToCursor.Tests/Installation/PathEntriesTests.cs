using VectorToCursor.Installation;

namespace VectorToCursor.Tests.Installation;

public sealed class PathEntriesTests
{
    private const string Folder = @"C:\Users\Test\AppData\Local\VectorToCursor\current";

    [Theory]
    [InlineData(Folder)]
    [InlineData(@"c:\users\test\appdata\local\vectortocursor\CURRENT")]
    [InlineData(Folder + @"\")]
    [InlineData("\"" + Folder + "\"")]
    [InlineData(" " + Folder + " ")]
    public void Contains_FolderListedInAnySpelling_ReturnsTrue(string entry) =>
        Assert.True(PathEntries.Contains($@"C:\Tools;{entry};%USERPROFILE%\bin", Folder));

    [Fact]
    public void Contains_EntryReferencingAnEnvironmentVariable_ComparesItsValue()
    {
        const string variable = "VECTORTOCURSOR_TESTS_PATH_ROOT";
        Environment.SetEnvironmentVariable(variable, @"C:\Users\Test\AppData\Local");
        try
        {
            Assert.True(PathEntries.Contains($@"C:\Tools;%{variable}%\VectorToCursor\current", Folder));
        }
        finally
        {
            Environment.SetEnvironmentVariable(variable, null);
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData(@"C:\Tools;;D:\Bin")]
    [InlineData(@"C:\Users\Test\AppData\Local\VectorToCursor")]
    [InlineData(Folder + @"\bin")]
    public void Contains_FolderNotListed_ReturnsFalse(string path) => Assert.False(PathEntries.Contains(path, Folder));

    [Theory]
    [InlineData("", Folder)]
    [InlineData(@"C:\Tools", @"C:\Tools;" + Folder)]
    [InlineData(@"C:\Tools;", @"C:\Tools;" + Folder + ";")]
    [InlineData(@"%USERPROFILE%\bin;C:\Tools", @"%USERPROFILE%\bin;C:\Tools;" + Folder)]
    public void Append_AddsFolderAsLastEntry(string path, string expected) => Assert.Equal(expected, PathEntries.Append(path, Folder));

    [Theory]
    [InlineData(Folder, "")]
    [InlineData(@"C:\Tools;" + Folder, @"C:\Tools")]
    [InlineData(Folder + @";C:\Tools", @"C:\Tools")]
    [InlineData(@"C:\Tools;" + Folder + @";%USERPROFILE%\bin", @"C:\Tools;%USERPROFILE%\bin")]
    [InlineData(@"C:\Tools;" + Folder + ";", @"C:\Tools;")]
    [InlineData(Folder + @"\;C:\Tools;" + Folder, @"C:\Tools")]
    [InlineData(@"C:\Tools;;D:\Bin", @"C:\Tools;;D:\Bin")]
    public void Remove_RemovesEveryEntryOfTheFolderAndKeepsTheRest(string path, string expected) => Assert.Equal(expected, PathEntries.Remove(path, Folder));

    [Theory]
    [InlineData("")]
    [InlineData(@"C:\Tools")]
    [InlineData(@"C:\Tools;")]
    [InlineData(@"%USERPROFILE%\bin;;C:\Tools")]
    public void Remove_AfterAppend_GivesBackThePath(string path) => Assert.Equal(path, PathEntries.Remove(PathEntries.Append(path, Folder), Folder));
}

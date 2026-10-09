using VectorToCursor.Installation;

namespace VectorToCursor.Tests.Installation;

public sealed class UserPathRegistrationTests
{
    private const string Folder = @"C:\Users\Test\AppData\Local\VectorToCursor\current";

    [Fact]
    public void Register_AppendsTheFolderWithoutItsTrailingSeparator()
    {
        FakeUserPathStore store = new(@"C:\Tools");

        new UserPathRegistration(store, Folder + Path.DirectorySeparatorChar).Register();

        Assert.Equal(@"C:\Tools;" + Folder, store.Value);
    }

    [Fact]
    public void Register_FolderAlreadyListed_LeavesThePathUnwritten()
    {
        FakeUserPathStore store = new(@"C:\Tools;" + Folder.ToUpperInvariant());

        new UserPathRegistration(store, Folder).Register();

        Assert.Equal(0, store.WriteCount);
    }

    [Fact]
    public void Register_Twice_ListsTheFolderOnce()
    {
        FakeUserPathStore store = new(@"C:\Tools");
        UserPathRegistration registration = new(store, Folder);

        registration.Register();
        registration.Register();

        Assert.Equal(@"C:\Tools;" + Folder, store.Value);
        Assert.Equal(1, store.WriteCount);
    }

    [Fact]
    public void Unregister_RemovesTheFolderAndKeepsTheOtherEntries()
    {
        FakeUserPathStore store = new($@"%USERPROFILE%\bin;{Folder};C:\Tools");

        new UserPathRegistration(store, Folder).Unregister();

        Assert.Equal(@"%USERPROFILE%\bin;C:\Tools", store.Value);
    }

    [Fact]
    public void Unregister_FolderNotListed_LeavesThePathUnwritten()
    {
        FakeUserPathStore store = new(@"C:\Tools");

        new UserPathRegistration(store, Folder).Unregister();

        Assert.Equal(0, store.WriteCount);
    }

    [Theory]
    [InlineData("")]
    [InlineData(@"C:\Tools")]
    [InlineData(@"%USERPROFILE%\bin;C:\Tools;")]
    public void Unregister_AfterRegister_RestoresThePath(string path)
    {
        FakeUserPathStore store = new(path);
        UserPathRegistration registration = new(store, Folder);

        registration.Register();
        registration.Unregister();

        Assert.Equal(path, store.Value);
    }
}

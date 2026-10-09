using System.Runtime.Versioning;
using Microsoft.Win32;
using VectorToCursor.Installation;

namespace VectorToCursor.Tests.Installation;

/// <summary>Works on a scratch key below <c>HKEY_CURRENT_USER\Software</c>, never on the real PATH.</summary>
[SupportedOSPlatform("windows")]
public sealed class RegistryUserPathStoreTests : IDisposable
{
    private const string ValueName = "Path";

    private readonly string _keyName = $@"Software\VectorToCursor.Tests-{Guid.NewGuid():N}";

    [Fact]
    public void Read_NoPathValue_ReturnsEmptyString() => Assert.Equal("", CreateStore().Read());

    [Fact]
    public void Read_ReturnsReferencesToOtherVariablesUnexpanded()
    {
        RegistryUserPathStore store = CreateStore();
        SetPath(@"%USERPROFILE%\bin", RegistryValueKind.ExpandString);

        Assert.Equal(@"%USERPROFILE%\bin", store.Read());
    }

    [Fact]
    public void Write_NoPathValue_CreatesAnExpandableString()
    {
        RegistryUserPathStore store = CreateStore();

        store.Write(@"%USERPROFILE%\bin");

        Assert.Equal((RegistryValueKind.ExpandString, @"%USERPROFILE%\bin"), StoredPath());
    }

    [Theory]
    [InlineData(RegistryValueKind.ExpandString)]
    [InlineData(RegistryValueKind.String)]
    public void Write_ExistingPath_KeepsItsKindAndItsReferences(RegistryValueKind kind)
    {
        RegistryUserPathStore store = CreateStore();
        SetPath(@"%USERPROFILE%\bin", kind);

        store.Write(store.Read() + @";C:\Tools");

        Assert.Equal((kind, @"%USERPROFILE%\bin;C:\Tools"), StoredPath());
    }

    [Fact]
    public void Write_EmptyPath_RemovesTheValue()
    {
        RegistryUserPathStore store = CreateStore();
        SetPath(@"C:\Tools", RegistryValueKind.ExpandString);

        store.Write("");

        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(_keyName);
        Assert.Null(key?.GetValue(ValueName));
    }

    public void Dispose()
    {
        if (OperatingSystem.IsWindows())
            Registry.CurrentUser.DeleteSubKeyTree(_keyName, throwOnMissingSubKey: false);
    }

    private RegistryUserPathStore CreateStore()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "The user's PATH is stored in the Windows registry.");
        return new RegistryUserPathStore(_keyName);
    }

    private void SetPath(string value, RegistryValueKind kind)
    {
        using RegistryKey key = Registry.CurrentUser.CreateSubKey(_keyName);
        key.SetValue(ValueName, value, kind);
    }

    private (RegistryValueKind Kind, object? Value) StoredPath()
    {
        using RegistryKey key = Registry.CurrentUser.OpenSubKey(_keyName) ?? throw new InvalidOperationException($"The key {_keyName} doesn't exist.");
        return (key.GetValueKind(ValueName), key.GetValue(ValueName, defaultValue: null, RegistryValueOptions.DoNotExpandEnvironmentNames));
    }
}

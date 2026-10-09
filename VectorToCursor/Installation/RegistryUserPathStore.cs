using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32;

namespace VectorToCursor.Installation;

/// <summary>
/// The current user's PATH in <c>HKEY_CURRENT_USER\Environment</c>. Writing keeps the value's registry type, so references
/// such as %USERPROFILE% stay references, which <see cref="Environment.SetEnvironmentVariable(string, string, EnvironmentVariableTarget)"/>
/// would replace with their current values.
/// </summary>
[SupportedOSPlatform("windows")]
internal sealed partial class RegistryUserPathStore : IUserPathStore
{
    public const string EnvironmentKeyName = "Environment";

    private const string ValueName = "Path";

    private const nint BroadcastWindow = 0xFFFF;
    private const uint SettingChangeMessage = 0x001A;
    private const string EnvironmentSection = "Environment";
    private const uint AbortIfHung = 0x0002;
    private const uint BroadcastTimeoutMilliseconds = 1000;

    private readonly string _keyName;

    /// <param name="keyName">The registry key below <c>HKEY_CURRENT_USER</c> that holds the PATH value.</param>
    public RegistryUserPathStore(string keyName = EnvironmentKeyName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(keyName);

        _keyName = keyName;
    }

    public string Read()
    {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(_keyName);
        return key?.GetValue(ValueName, defaultValue: null, RegistryValueOptions.DoNotExpandEnvironmentNames) as string ?? "";
    }

    public void Write(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        using (RegistryKey key = Registry.CurrentUser.CreateSubKey(_keyName))
        {
            if (path.Length == 0)
                key.DeleteValue(ValueName, throwOnMissingValue: false);
            else
                key.SetValue(ValueName, path, KindOf(key));
        }
        NotifyEnvironmentChanged();
    }

    // Windows itself creates the user's PATH as REG_EXPAND_SZ.
    private static RegistryValueKind KindOf(RegistryKey key) => key.GetValue(ValueName) is null ? RegistryValueKind.ExpandString : key.GetValueKind(ValueName);

    // Explorer reloads the environment on this broadcast, so terminals started afterwards see the new PATH without a sign-out.
    private static void NotifyEnvironmentChanged() => SendMessageTimeout(BroadcastWindow, SettingChangeMessage, 0, EnvironmentSection, AbortIfHung, BroadcastTimeoutMilliseconds, out _);

    [LibraryImport("user32.dll", EntryPoint = "SendMessageTimeoutW", StringMarshalling = StringMarshalling.Utf16)]
    private static partial nint SendMessageTimeout(nint window, uint message, nuint wParam, string lParam, uint flags, uint timeoutMilliseconds, out nuint result);
}

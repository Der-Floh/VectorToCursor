using VectorToCursor.Application;
using VectorToCursor.Cli;

namespace VectorToCursor.Tests.Cli;

/// <summary>The shims ship next to the app, so the build copies them into the output folder of this test project too.</summary>
public sealed class ShortCommandShimTests
{
    private const string CmdShim = FirstRunMessage.ShortCommandName + ".cmd";
    private const string ShellShim = FirstRunMessage.ShortCommandName;

    private static readonly string AppExecutable = typeof(CursorConverter).Assembly.GetName().Name + ".exe";

    [Fact]
    public void CmdShim_StartsTheAppNextToItWithAllArguments() =>
        Assert.Contains($"\"%~dp0{AppExecutable}\" %*", ReadShim(CmdShim));

    [Fact]
    public void ShellShim_StartsTheAppNextToItWithAllArguments() =>
        Assert.Contains($"exec \"$(dirname \"$0\")/{AppExecutable}\" \"$@\"", ReadShim(ShellShim));

    [Fact]
    public void ShellShim_HasAShebangAndUnixLineEndings()
    {
        string script = ReadShim(ShellShim);

        Assert.StartsWith("#!", script);
        Assert.DoesNotContain('\r', script);
    }

    private static string ReadShim(string name) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, name));
}

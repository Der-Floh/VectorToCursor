using System.CommandLine;

namespace VectorToCursor.Cli;

/// <summary>
/// What the app shows when Setup starts it right after installing it. Setup opens a console window of its own for it,
/// which would close again as soon as the app exits.
/// </summary>
internal static class FirstRunMessage
{
    /// <summary>The short command that vtc.cmd and the vtc script next to the app provide.</summary>
    public const string ShortCommandName = "vtc";

    public static int Show(string version)
    {
        Write(Console.Out, version);
        if (!Console.IsInputRedirected)
            Console.ReadKey(intercept: true);
        return ExitCodes.Success;
    }

    public static void Write(TextWriter output, string version)
    {
        ArgumentNullException.ThrowIfNull(output);
        ArgumentException.ThrowIfNullOrWhiteSpace(version);

        output.WriteLine($"{RootCommand.ExecutableName} {version} is installed.");
        output.WriteLine($"Open a new terminal and run \"{RootCommand.ExecutableName} --help\", or \"{ShortCommandName} --help\" for short, to see how to convert an SVG file into a cursor.");
        output.WriteLine();
        output.WriteLine("Press any key to close this window.");
    }
}

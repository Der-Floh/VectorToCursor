using System.CommandLine;

namespace VectorToCursor.Cli;

/// <summary>
/// What the app shows when Setup starts it right after installing it. Setup opens a console window of its own for it,
/// which would close again as soon as the app exits.
/// </summary>
internal static class FirstRunMessage
{
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
        output.WriteLine($"Open a new terminal and run \"{RootCommand.ExecutableName} --help\" to see how to convert an SVG file into a cursor.");
        output.WriteLine();
        output.WriteLine("Press any key to close this window.");
    }
}

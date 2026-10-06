using System.CommandLine;
using System.CommandLine.Parsing;
using VectorToCursor.Application;
using VectorToCursor.Domain;

namespace VectorToCursor.Cli;

/// <summary>Defines the command line: <c>VectorToCursor &lt;input&gt; -x &lt;x&gt; -y &lt;y&gt; [-o &lt;file&gt;] [--bleed &lt;percent&gt;]</c>.</summary>
internal static class RootCommandFactory
{
    private const string CursorExtension = ".cur";

    public static RootCommand Create(ICursorConverter converter)
    {
        ArgumentNullException.ThrowIfNull(converter);

        Argument<FileInfo> inputArgument = CreateInputArgument();
        Option<double> hotspotXOption = CreateCoordinateOption("--hotspot-x", "-x", "X");
        Option<double> hotspotYOption = CreateCoordinateOption("--hotspot-y", "-y", "Y");
        Option<FileInfo> outputOption = CreateOutputOption();
        Option<BleedPercentage> bleedOption = CreateBleedOption();

        RootCommand rootCommand = new($"Converts an SVG file into a Windows cursor (.cur) with the sizes {string.Join(", ", CursorSizes.All)} px.")
        {
            inputArgument,
            hotspotXOption,
            hotspotYOption,
            outputOption,
            bleedOption,
        };
        rootCommand.SetAction(parseResult =>
        {
            FileInfo input = parseResult.GetRequiredValue(inputArgument);
            string outputPath = parseResult.GetValue(outputOption)?.FullName ?? Path.ChangeExtension(input.FullName, CursorExtension);
            SvgPoint hotspot = new(parseResult.GetRequiredValue(hotspotXOption), parseResult.GetRequiredValue(hotspotYOption));
            ConversionRequest request = new(input.FullName, outputPath, hotspot, parseResult.GetRequiredValue(bleedOption));
            return Execute(converter, request, parseResult.InvocationConfiguration);
        });
        return rootCommand;
    }

    private static Argument<FileInfo> CreateInputArgument() => new("input")
    {
        Description = "The SVG file to convert.",
        CustomParser = ParseExistingFile,
    };

    private static Option<double> CreateCoordinateOption(string name, string alias, string axis) => new(name, alias)
    {
        Description = $"Hotspot {axis} coordinate in the SVG's viewBox units; scaled to every cursor size. Use '.' as the decimal separator.",
        HelpName = axis.ToLowerInvariant(),
        Required = true,
        CustomParser = NumberParser.ParseCoordinate,
    };

    private static Option<BleedPercentage> CreateBleedOption() => new("--bleed")
    {
        Description = "Width of the band around the artwork, in percent of each cursor size, in which transparent pixels take the nearest edge color; pixels beyond it get the average color of the band's edge. Prevents dark fringes when Windows scales the cursor. 0 turns it off.",
        HelpName = "percent",
        DefaultValueFactory = _ => BleedPercentage.Default,
        CustomParser = NumberParser.ParseBleedPercentage,
    };

    private static Option<FileInfo> CreateOutputOption()
    {
        Option<FileInfo> option = new("--output", "-o")
        {
            Description = $"The cursor file to write. Defaults to the input path with a {CursorExtension} extension. An existing file is overwritten.",
            HelpName = "file",
        };
        return option.AcceptLegalFilePathsOnly();
    }

    // AcceptExistingOnly() would also accept a directory.
    private static FileInfo? ParseExistingFile(ArgumentResult result)
    {
        string path = result.Tokens[0].Value;
        if (File.Exists(path))
            return new FileInfo(path);

        result.AddError(Directory.Exists(path) ? $"'{path}' is a directory, not an SVG file." : $"The file '{path}' does not exist.");
        return null;
    }

    private static int Execute(ICursorConverter converter, ConversionRequest request, InvocationConfiguration configuration)
    {
        try
        {
            WriteSummary(configuration.Output, converter.Convert(request));
            return ExitCodes.Success;
        }
        catch (Exception exception) when (exception is CursorConversionException or IOException or UnauthorizedAccessException)
        {
            configuration.Error.WriteLine($"Error: {exception.Message}");
            return ExitCodes.Failure;
        }
    }

    private static void WriteSummary(TextWriter output, ConversionResult result)
    {
        output.WriteLine($"Created {result.OutputPath}");
        output.WriteLine("  Size  Hotspot");
        foreach (FrameSummary frame in result.Frames)
            output.WriteLine($"  {frame.Size,4}  {frame.Hotspot.X},{frame.Hotspot.Y}");
    }
}

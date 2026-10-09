using System.CommandLine;
using System.CommandLine.Parsing;
using System.Globalization;
using VectorToCursor.Application;
using VectorToCursor.Domain;

namespace VectorToCursor.Cli;

/// <summary>
/// Defines the command line: <c>VectorToCursor &lt;input&gt; -x &lt;x&gt; -y &lt;y&gt; [-o &lt;file&gt;] [--sizes &lt;list&gt;] [--image-format &lt;bmp|png&gt;] [--bleed &lt;percent&gt;] [--fps &lt;rate&gt;]</c>.
/// </summary>
internal static class RootCommandFactory
{
    public static RootCommand Create(ICursorConverter converter)
    {
        ArgumentNullException.ThrowIfNull(converter);

        Argument<FileInfo> inputArgument = CreateInputArgument();
        Option<double> hotspotXOption = CreateCoordinateOption("--hotspot-x", "-x", "X");
        Option<double> hotspotYOption = CreateCoordinateOption("--hotspot-y", "-y", "Y");
        Option<FileInfo> outputOption = CreateOutputOption();
        Option<CursorSizes> sizesOption = CreateSizesOption();
        Option<CursorImageFormat?> imageFormatOption = CreateImageFormatOption();
        Option<BleedPercentage> bleedOption = CreateBleedOption();
        Option<FrameRate> frameRateOption = CreateFrameRateOption();

        RootCommand rootCommand = new($"Converts an SVG file into a Windows cursor (.cur), or an animated cursor (.ani) when the SVG is animated, with one image per size, by default {string.Join(", ", CursorSizes.Default.Values)} px.")
        {
            inputArgument,
            hotspotXOption,
            hotspotYOption,
            outputOption,
            sizesOption,
            imageFormatOption,
            bleedOption,
            frameRateOption,
        };
        rootCommand.SetAction(parseResult =>
        {
            FileInfo input = parseResult.GetRequiredValue(inputArgument);
            SvgPoint hotspot = new(parseResult.GetRequiredValue(hotspotXOption), parseResult.GetRequiredValue(hotspotYOption));
            ConversionRequest request = new(
                input.FullName,
                parseResult.GetValue(outputOption)?.FullName,
                hotspot,
                parseResult.GetRequiredValue(bleedOption),
                parseResult.GetRequiredValue(frameRateOption),
                parseResult.GetRequiredValue(sizesOption),
                parseResult.GetValue(imageFormatOption));
            return Execute(converter, request, parseResult.InvocationConfiguration);
        });
        return rootCommand;
    }

    private static Argument<FileInfo> CreateInputArgument() => new("input")
    {
        Description = "The SVG file to convert. CSS (@keyframes) or SMIL animations make it an animated cursor.",
        CustomParser = ParseExistingFile,
    };

    private static Option<double> CreateCoordinateOption(string name, string alias, string axis) => new(name, alias)
    {
        Description = $"Hotspot {axis} coordinate in the SVG's viewBox units; scaled to every cursor size and shared by all animation frames. Use '.' as the decimal separator.",
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

    private static Option<FrameRate> CreateFrameRateOption() => new("--fps")
    {
        Description = $"Frames per second of an animated cursor; must divide {FrameRate.JiffiesPerSecond}. Ignored for static SVGs.",
        HelpName = "rate",
        DefaultValueFactory = _ => FrameRate.Default,
        CustomParser = NumberParser.ParseFrameRate,
    };

    private static Option<CursorSizes> CreateSizesOption() => new("--sizes")
    {
        Description = $"The sizes of the images in the cursor file, in px from {CursorSizes.Minimum} to {CursorSizes.Maximum}, separated by commas, e.g. 32,48,64. Each size gets its own scaled hotspot.",
        HelpName = "list",
        DefaultValueFactory = _ => CursorSizes.Default,
        CustomParser = NumberParser.ParseCursorSizes,
    };

    private static Option<CursorImageFormat?> CreateImageFormatOption() => new("--image-format")
    {
        Description = $"How every image in the cursor file is stored: bmp, which every program can load, or png, a fraction of the size, which looks the same in Windows but which some programs such as WinForms can't load. Defaults to {ImageFormatParser.NameOf(ImageFormats.StaticDefault)} for {CursorFileExtensions.Static} files. For {CursorFileExtensions.Animated} files it defaults to {ImageFormatParser.NameOf(ImageFormats.AnimatedPreferred)} when every frame fits Windows' limit of images starting within 64 KB with the chosen --sizes (e.g. 32,48,64), otherwise {ImageFormatParser.NameOf(ImageFormats.AnimatedFallback)}.",
        HelpName = ImageFormatParser.HelpName,
        CustomParser = ImageFormatParser.Parse,
    };

    private static Option<FileInfo> CreateOutputOption()
    {
        Option<FileInfo> option = new("--output", "-o")
        {
            Description = $"The cursor file to write: {CursorFileExtensions.Static} for a static SVG, {CursorFileExtensions.Animated} for an animated one. Defaults to the input path with that extension. An existing file is overwritten.",
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
        string created = $"Created {result.OutputPath} with {ImageFormatParser.NameOf(result.ImageFormat).ToUpperInvariant()} images";
        output.WriteLine(result.Animation is { } animation ? $"{created}: {DescribeAnimation(animation)}" : created);
        output.WriteLine("  Size  Hotspot");
        foreach (FrameSummary frame in result.Frames)
            output.WriteLine($"  {frame.Size,4}  {frame.Hotspot.X},{frame.Hotspot.Y}");
    }

    private static string DescribeAnimation(AnimationSummary animation)
    {
        string description = string.Create(CultureInfo.InvariantCulture, $"{animation.FrameCount} frames at {animation.FrameRate} fps ({animation.EffectiveDuration.TotalSeconds:0.###} s)");
        return animation.EffectiveDuration == animation.LoopDuration
            ? description
            : description + string.Create(CultureInfo.InvariantCulture, $"; the SVG's loop is {animation.LoopDuration.TotalSeconds:0.###} s, which isn't a whole number of frames");
    }
}

using System.CommandLine;
using VectorToCursor.Application;
using VectorToCursor.Cli;
using VectorToCursor.Domain;

namespace VectorToCursor.Tests.Cli;

public sealed class RootCommandTests
{
    private static readonly string InputPath = TestFiles.PathOf("left-half.svg");

    [Fact]
    public void ValidArguments_LeaveOutputPathAndDefaultsToConverter()
    {
        FakeCursorConverter converter = new();

        CommandRun run = Run(converter, InputPath, "-x", "3.5", "--hotspot-y", "2");

        Assert.Equal(ExitCodes.Success, run.ExitCode);
        Assert.Equal(new ConversionRequest(InputPath, null, new SvgPoint(3.5, 2), BleedPercentage.Default, FrameRate.Default, CursorSizes.Default, ImageFormat: null), converter.LastRequest);
    }

    [Theory]
    [InlineData("60")]
    [InlineData("12")]
    [InlineData("1")]
    public void FrameRateOption_IsPassedToConverter(string value)
    {
        FakeCursorConverter converter = new();

        CommandRun run = Run(converter, InputPath, "-x", "3", "-y", "2", "--fps", value);

        Assert.Equal(ExitCodes.Success, run.ExitCode);
        Assert.Equal(value, converter.LastRequest?.FrameRate.ToString());
    }

    [Theory]
    [InlineData("0")]
    [InlineData("7")]
    [InlineData("-30")]
    [InlineData("2.5")]
    [InlineData("abc")]
    public void InvalidFrameRate_IsRejected(string value)
    {
        FakeCursorConverter converter = new();

        CommandRun run = Run(converter, InputPath, "-x", "3", "-y", "2", "--fps", value);

        Assert.Equal(ExitCodes.Failure, run.ExitCode);
        Assert.Contains($"Invalid value '{value}' for --fps", run.Error);
        Assert.Contains("60, 30, 20, 15, 12, 10, 6, 5, 4, 3, 2, 1", run.Error);
        Assert.Null(converter.LastRequest);
    }

    [Fact]
    public void Help_ShowsFrameRateDefault()
    {
        CommandRun run = Run(new FakeCursorConverter(), "--help");

        string help = string.Join(' ', run.Output.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        Assert.Contains("--fps <rate>", help);
        Assert.Contains("[default: 30]", help);
    }

    [Fact]
    public void AnimatedSuccess_PrintsFramesAndDuration()
    {
        AnimationSummary animation = new(165, FrameRate.Default, TimeSpan.FromMilliseconds(5500), TimeSpan.FromMilliseconds(5500));

        CommandRun run = Run(new FakeCursorConverter { Animation = animation, ImageFormat = CursorImageFormat.Png }, InputPath, "-x", "3", "-y", "2");

        Assert.Contains($"Created {Path.ChangeExtension(InputPath, ".ani")} with PNG images: 165 frames at 30 fps (5.5 s){Environment.NewLine}", run.Output);
        Assert.Contains("   256  24,16", run.Output);
    }

    [Fact]
    public void AnimatedSuccess_LoopNotWholeFrames_PrintsBothDurations()
    {
        AnimationSummary animation = new(3, FrameRate.Default, TimeSpan.FromMilliseconds(110), TimeSpan.FromMilliseconds(100));

        CommandRun run = Run(new FakeCursorConverter { Animation = animation }, InputPath, "-x", "3", "-y", "2");

        Assert.Contains("3 frames at 30 fps (0.1 s); the SVG's loop is 0.11 s", run.Output);
    }

    [Theory]
    [InlineData("2.5", 2.5)]
    [InlineData("0", 0)]
    [InlineData("100", 100)]
    public void BleedOption_IsPassedToConverter(string value, double expected)
    {
        FakeCursorConverter converter = new();

        CommandRun run = Run(converter, InputPath, "-x", "3", "-y", "2", "--bleed", value);

        Assert.Equal(ExitCodes.Success, run.ExitCode);
        Assert.Equal(expected, converter.LastRequest?.Bleed.Value);
    }

    [Theory]
    [InlineData("-1", "Expected a percentage from 0 to 100")]
    [InlineData("101", "Expected a percentage from 0 to 100")]
    [InlineData("3,5", "'.' as the decimal separator")]
    [InlineData("abc", "'.' as the decimal separator")]
    public void InvalidBleed_IsRejected(string value, string expectedHint)
    {
        FakeCursorConverter converter = new();

        CommandRun run = Run(converter, InputPath, "-x", "3", "-y", "2", "--bleed", value);

        Assert.Equal(ExitCodes.Failure, run.ExitCode);
        Assert.Contains($"Invalid value '{value}' for --bleed", run.Error);
        Assert.Contains(expectedHint, run.Error);
        Assert.Null(converter.LastRequest);
    }

    [Fact]
    public void Help_ShowsBleedDefault()
    {
        CommandRun run = Run(new FakeCursorConverter(), "--help");

        string help = string.Join(' ', run.Output.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        Assert.Contains("--bleed <percent>", help);
        Assert.Contains("[default: 5]", help);
    }

    [Theory]
    [InlineData("bmp", nameof(CursorImageFormat.Bmp))]
    [InlineData("png", nameof(CursorImageFormat.Png))]
    [InlineData("PNG", nameof(CursorImageFormat.Png))]
    public void ImageFormatOption_IsPassedToConverter(string value, string expected)
    {
        FakeCursorConverter converter = new();

        CommandRun run = Run(converter, InputPath, "-x", "3", "-y", "2", "--image-format", value);

        Assert.Equal(ExitCodes.Success, run.ExitCode);
        Assert.Equal(expected, converter.LastRequest?.ImageFormat?.ToString());
    }

    [Theory]
    [InlineData("gif")]
    [InlineData("1")]
    [InlineData("bmp|png")]
    public void InvalidImageFormat_IsRejected(string value)
    {
        FakeCursorConverter converter = new();

        CommandRun run = Run(converter, InputPath, "-x", "3", "-y", "2", "--image-format", value);

        Assert.Equal(ExitCodes.Failure, run.ExitCode);
        Assert.Contains($"Invalid value '{value}' for --image-format. Expected bmp or png.", run.Error);
        Assert.Null(converter.LastRequest);
    }

    [Fact]
    public void Help_ShowsImageFormatDefaults()
    {
        CommandRun run = Run(new FakeCursorConverter(), "--help");

        string help = string.Join(' ', run.Output.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        Assert.Contains("--image-format <bmp|png>", help);
        Assert.Contains("Defaults to bmp for .cur files. For .ani files it defaults to bmp when every frame fits", help);
    }

    [Theory]
    [InlineData("48,32,64", "32,48,64")]
    [InlineData("256", "256")]
    [InlineData(" 32 , 48 ", "32,48")]
    public void SizesOption_IsPassedToConverterSmallestFirst(string value, string expected)
    {
        FakeCursorConverter converter = new();

        CommandRun run = Run(converter, InputPath, "-x", "3", "-y", "2", "--sizes", value);

        Assert.Equal(ExitCodes.Success, run.ExitCode);
        Assert.Equal(expected, converter.LastRequest?.Sizes.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("0")]
    [InlineData("257")]
    [InlineData("32,32")]
    [InlineData("32,")]
    [InlineData("32.5")]
    [InlineData("32;48")]
    [InlineData("abc")]
    public void InvalidSizes_AreRejected(string value)
    {
        FakeCursorConverter converter = new();

        CommandRun run = Run(converter, InputPath, "-x", "3", "-y", "2", "--sizes", value);

        Assert.Equal(ExitCodes.Failure, run.ExitCode);
        Assert.Contains($"Invalid value '{value}' for --sizes. Expected comma-separated sizes from 1 to 256 px, each listed once", run.Error);
        Assert.Null(converter.LastRequest);
    }

    [Fact]
    public void Help_ShowsSizesDefault()
    {
        CommandRun run = Run(new FakeCursorConverter(), "--help");

        string help = string.Join(' ', run.Output.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        Assert.Contains("--sizes <list>", help);
        Assert.Contains("[default: 32,48,64,96,128,256]", help);
    }

    [Fact]
    public void OutputOption_OverridesOutputPath()
    {
        FakeCursorConverter converter = new();
        string outputPath = Path.Combine(Path.GetTempPath(), "custom.cur");

        CommandRun run = Run(converter, InputPath, "--hotspot-x", "0", "-y", "0", "-o", outputPath);

        Assert.Equal(ExitCodes.Success, run.ExitCode);
        Assert.Equal(outputPath, converter.LastRequest?.OutputPath);
    }

    [Fact]
    public void NegativeCoordinates_AreAccepted()
    {
        FakeCursorConverter converter = new();

        CommandRun run = Run(converter, InputPath, "-x", "-1.5", "-y", "-16");

        Assert.Equal(ExitCodes.Success, run.ExitCode);
        Assert.Equal(new SvgPoint(-1.5, -16), converter.LastRequest?.Hotspot);
    }

    [Fact]
    public void Success_PrintsHotspotPerSize()
    {
        CommandRun run = Run(new FakeCursorConverter(), InputPath, "-x", "3", "-y", "2");

        Assert.Contains($"Created {Path.ChangeExtension(InputPath, ".cur")} with BMP images{Environment.NewLine}", run.Output);
        Assert.Contains("   256  24,16", run.Output);
    }

    [Theory]
    [InlineData("3,5")]
    [InlineData("abc")]
    [InlineData("NaN")]
    [InlineData("Infinity")]
    public void InvalidCoordinate_IsRejected(string value)
    {
        FakeCursorConverter converter = new();

        CommandRun run = Run(converter, InputPath, "-x", value, "-y", "2");

        Assert.Equal(ExitCodes.Failure, run.ExitCode);
        Assert.Contains($"Invalid value '{value}' for --hotspot-x", run.Error);
        Assert.Contains("'.' as the decimal separator", run.Error);
        Assert.Null(converter.LastRequest);
    }

    [Fact]
    public void MissingHotspotY_IsRejected()
    {
        FakeCursorConverter converter = new();

        CommandRun run = Run(converter, InputPath, "-x", "3");

        Assert.Equal(ExitCodes.Failure, run.ExitCode);
        Assert.Contains("--hotspot-y", run.Error);
        Assert.Null(converter.LastRequest);
    }

    [Fact]
    public void MissingInputFile_IsRejected()
    {
        FakeCursorConverter converter = new();

        CommandRun run = Run(converter, TestFiles.PathOf("does-not-exist.svg"), "-x", "3", "-y", "2");

        Assert.Equal(ExitCodes.Failure, run.ExitCode);
        Assert.Contains("does not exist", run.Error);
        Assert.Null(converter.LastRequest);
    }

    [Fact]
    public void DirectoryInput_IsRejected()
    {
        FakeCursorConverter converter = new();

        CommandRun run = Run(converter, TestFiles.DataDirectory, "-x", "3", "-y", "2");

        Assert.Equal(ExitCodes.Failure, run.ExitCode);
        Assert.Contains("is a directory", run.Error);
        Assert.Null(converter.LastRequest);
    }

    [Theory]
    [InlineData(typeof(CursorConversionException))]
    [InlineData(typeof(IOException))]
    [InlineData(typeof(UnauthorizedAccessException))]
    public void ConversionFailure_IsReportedWithoutStackTrace(Type exceptionType)
    {
        Exception failure = (Exception)Activator.CreateInstance(exceptionType, "conversion went wrong")!;

        CommandRun run = Run(new FakeCursorConverter(failure), InputPath, "-x", "3", "-y", "2");

        Assert.Equal(ExitCodes.Failure, run.ExitCode);
        Assert.Equal("Error: conversion went wrong", run.Error.Trim());
    }

    private static CommandRun Run(FakeCursorConverter converter, params string[] args)
    {
        StringWriter output = new();
        StringWriter error = new();
        int exitCode = RootCommandFactory.Create(converter).Parse(args).Invoke(new InvocationConfiguration { Output = output, Error = error });
        return new CommandRun(exitCode, output.ToString(), error.ToString());
    }

    private sealed record CommandRun(int ExitCode, string Output, string Error);
}

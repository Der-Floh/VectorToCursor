using System.CommandLine;
using VectorToCursor.Application;
using VectorToCursor.Cli;
using VectorToCursor.Domain;

namespace VectorToCursor.Tests.Cli;

public sealed class RootCommandTests
{
    private static readonly string InputPath = TestFiles.PathOf("left-half.svg");

    [Fact]
    public void ValidArguments_ConvertToCursorNextToInput()
    {
        FakeCursorConverter converter = new();

        CommandRun run = Run(converter, InputPath, "-x", "3.5", "--hotspot-y", "2");

        Assert.Equal(ExitCodes.Success, run.ExitCode);
        Assert.Equal(new ConversionRequest(InputPath, Path.ChangeExtension(InputPath, ".cur"), new SvgPoint(3.5, 2), BleedPercentage.Default), converter.LastRequest);
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

        Assert.Contains("Created ", run.Output);
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

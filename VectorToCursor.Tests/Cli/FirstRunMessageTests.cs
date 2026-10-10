using VectorToCursor.Cli;

namespace VectorToCursor.Tests.Cli;

public sealed class FirstRunMessageTests
{
    [Fact]
    public void Write_NamesTheInstalledVersionAndHowToGetHelp()
    {
        StringWriter output = new();

        FirstRunMessage.Write(output, "1.2.3");

        string message = output.ToString();
        Assert.Contains(" 1.2.3 is installed.", message);
        Assert.Contains("--help", message);
        Assert.Contains($"\"{FirstRunMessage.ShortCommandName} --help\"", message);
        Assert.Contains("Press any key to close this window.", message);
    }
}

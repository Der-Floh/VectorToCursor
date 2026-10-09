using VectorToCursor.Rendering.Animation;

namespace VectorToCursor.Tests.Rendering.Animation;

public sealed class ClockValueTests
{
    [Theory]
    [InlineData("1.5s", 1500)]
    [InlineData("300ms", 300)]
    [InlineData("0.7s", 700)]
    [InlineData("2", 2000)]
    [InlineData("1.5min", 90000)]
    [InlineData("0.5h", 1800000)]
    [InlineData("0:01.5", 1500)]
    [InlineData("1:00:00", 3600000)]
    [InlineData("-0.55s", -550)]
    [InlineData("+2s", 2000)]
    [InlineData(" 1S ", 1000)]
    [InlineData("0.0004s", 0)]
    [InlineData("0.0005s", 1)]
    public void TryParse_ClockValue_IsExactWholeMilliseconds(string text, int expectedMilliseconds)
    {
        Assert.True(ClockValue.TryParse(text, out TimeSpan value));
        Assert.Equal(TimeSpan.FromMilliseconds(expectedMilliseconds), value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("indefinite")]
    [InlineData("-indefinite")]
    [InlineData("-")]
    [InlineData("s")]
    [InlineData("abc")]
    [InlineData("1:60")]
    [InlineData("60:00")]
    [InlineData("1:2:3:4")]
    [InlineData("1e20s")]
    public void TryParse_NoClockValue_ReturnsFalse(string text)
    {
        Assert.False(ClockValue.TryParse(text, out _));
    }

    [Theory]
    [InlineData(1100, "1100ms")]
    [InlineData(-550, "-550ms")]
    [InlineData(0, "0ms")]
    public void Format_WritesWholeMilliseconds(int milliseconds, string expected)
    {
        Assert.Equal(expected, ClockValue.Format(TimeSpan.FromMilliseconds(milliseconds)));
    }

    [Theory]
    [InlineData("indefinite")]
    [InlineData(" INDEFINITE ")]
    public void IsIndefinite_IgnoresCaseAndSpaces(string text)
    {
        Assert.True(ClockValue.IsIndefinite(text));
    }
}

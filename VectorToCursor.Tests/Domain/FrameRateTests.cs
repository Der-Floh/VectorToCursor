using VectorToCursor.Domain;

namespace VectorToCursor.Tests.Domain;

public sealed class FrameRateTests
{
    [Theory]
    [InlineData(60, 1)]
    [InlineData(30, 2)]
    [InlineData(20, 3)]
    [InlineData(15, 4)]
    [InlineData(12, 5)]
    [InlineData(10, 6)]
    [InlineData(6, 10)]
    [InlineData(5, 12)]
    [InlineData(4, 15)]
    [InlineData(3, 20)]
    [InlineData(2, 30)]
    [InlineData(1, 60)]
    public void Jiffies_IsTheFrameLengthInSixtiethsOfASecond(int framesPerSecond, int expectedJiffies)
    {
        Assert.Equal(expectedJiffies, new FrameRate(framesPerSecond).Jiffies);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-30)]
    [InlineData(7)]
    [InlineData(25)]
    [InlineData(120)]
    public void Constructor_RateNotDividingSixty_Throws(int framesPerSecond)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new FrameRate(framesPerSecond));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-30)]
    [InlineData(7)]
    [InlineData(25)]
    [InlineData(120)]
    public void TryCreate_RateNotDividingSixty_ReturnsFalse(int framesPerSecond)
    {
        Assert.False(FrameRate.TryCreate(framesPerSecond, out _));
    }

    [Fact]
    public void TryCreate_ValidRate_ReturnsFrameRate()
    {
        Assert.True(FrameRate.TryCreate(15, out FrameRate frameRate));
        Assert.Equal(15, frameRate.FramesPerSecond);
    }

    [Fact]
    public void Default_IsThirtyFramesPerSecond()
    {
        Assert.Equal(30, FrameRate.Default.FramesPerSecond);
    }

    [Fact]
    public void ToString_IsThePlainNumber()
    {
        Assert.Equal("12", new FrameRate(12).ToString());
    }
}

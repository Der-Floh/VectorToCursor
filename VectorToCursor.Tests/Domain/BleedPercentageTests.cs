using VectorToCursor.Domain;

namespace VectorToCursor.Tests.Domain;

public sealed class BleedPercentageTests
{
    [Theory]
    [InlineData(32, 2)]
    [InlineData(48, 3)]
    [InlineData(64, 4)]
    [InlineData(96, 5)]
    [InlineData(128, 7)]
    [InlineData(256, 13)]
    public void BandWidthFor_DefaultPercentage_RoundsUpToWholePixels(int size, int expected)
    {
        Assert.Equal(expected, BleedPercentage.Default.BandWidthFor(size));
    }

    [Fact]
    public void BandWidthFor_ZeroPercent_IsZero()
    {
        Assert.Equal(0, new BleedPercentage(0).BandWidthFor(256));
    }

    [Fact]
    public void BandWidthFor_NonPositiveSize_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => BleedPercentage.Default.BandWidthFor(0));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void Constructor_InvalidValue_Throws(double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new BleedPercentage(value));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void TryCreate_InvalidValue_ReturnsFalse(double value)
    {
        Assert.False(BleedPercentage.TryCreate(value, out _));
    }

    [Fact]
    public void TryCreate_ValidValue_ReturnsPercentage()
    {
        Assert.True(BleedPercentage.TryCreate(2.5, out BleedPercentage percentage));
        Assert.Equal(2.5, percentage.Value);
    }

    [Fact]
    public void ToString_IsThePlainNumber()
    {
        Assert.Equal("2.5", new BleedPercentage(2.5).ToString());
    }
}

using VectorToCursor.Domain;

namespace VectorToCursor.Tests.Domain;

public sealed class ArtworkBoundsTests
{
    [Theory]
    [InlineData(0, 32)]
    [InlineData(-1, 32)]
    [InlineData(double.NaN, 32)]
    [InlineData(32, double.PositiveInfinity)]
    public void Constructor_NonPositiveOrNonFiniteSize_Throws(double width, double height)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ArtworkBounds(0, 0, width, height));
    }

    [Fact]
    public void Constructor_NonFiniteOrigin_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ArtworkBounds(double.NaN, 0, 32, 32));
    }
}

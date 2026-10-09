using VectorToCursor.Domain;

namespace VectorToCursor.Tests.Domain;

public sealed class CursorSizesTests
{
    [Fact]
    public void Default_HoldsTheSixStandardSizesSmallestFirst()
    {
        Assert.Equal([32, 48, 64, 96, 128, 256], CursorSizes.Default.Values);
    }

    [Fact]
    public void TryCreate_SortsTheSizesSmallestFirst()
    {
        Assert.True(CursorSizes.TryCreate([64, 32, 48], out CursorSizes? sizes));
        Assert.Equal([32, 48, 64], sizes.Values);
    }

    [Fact]
    public void TryCreate_AcceptsTheSmallestAndLargestSize()
    {
        Assert.True(CursorSizes.TryCreate([CursorSizes.Maximum, CursorSizes.Minimum], out CursorSizes? sizes));
        Assert.Equal([1, 256], sizes.Values);
    }

    [Theory]
    [InlineData(new int[0])]
    [InlineData(new[] { 0 })]
    [InlineData(new[] { -32 })]
    [InlineData(new[] { 257 })]
    [InlineData(new[] { 32, 48, 32 })]
    public void TryCreate_InvalidSizes_ReturnsFalse(int[] values)
    {
        Assert.False(CursorSizes.TryCreate(values, out CursorSizes? sizes));
        Assert.Null(sizes);
    }

    [Fact]
    public void Equals_ComparesTheSizesNotTheInstance()
    {
        Assert.True(CursorSizes.TryCreate([48, 32], out CursorSizes? first));
        Assert.True(CursorSizes.TryCreate([32, 48], out CursorSizes? second));
        Assert.True(CursorSizes.TryCreate([32], out CursorSizes? other));

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.NotEqual(first, other);
    }

    [Fact]
    public void ToString_ListsTheSizesLikeTheOption()
    {
        Assert.Equal("32,48,64,96,128,256", CursorSizes.Default.ToString());
    }
}

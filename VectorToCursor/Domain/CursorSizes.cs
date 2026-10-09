using System.Diagnostics.CodeAnalysis;

namespace VectorToCursor.Domain;

/// <summary>The square pixel sizes of the images in a cursor file, smallest first and each at most once.</summary>
internal sealed record CursorSizes
{
    public const int Minimum = 1;

    /// <summary>The largest size, because the cursor directory stores each dimension in one byte, where 0 means 256.</summary>
    public const int Maximum = 256;

    private CursorSizes(IReadOnlyList<int> values) => Values = values;

    public static CursorSizes Default { get; } = new([32, 48, 64, 96, 128, 256]);

    /// <summary>The sizes in ascending order.</summary>
    public IReadOnlyList<int> Values { get; }

    /// <returns>
    /// <see langword="false"/> when the list is empty, holds a size twice, or holds a size outside <see cref="Minimum"/> to
    /// <see cref="Maximum"/>.
    /// </returns>
    public static bool TryCreate(IReadOnlyCollection<int> sizes, [NotNullWhen(true)] out CursorSizes? result)
    {
        ArgumentNullException.ThrowIfNull(sizes);

        bool valid = sizes.Count > 0 && sizes.All(size => size is >= Minimum and <= Maximum) && sizes.Distinct().Count() == sizes.Count;
        result = valid ? new CursorSizes([.. sizes.Order()]) : null;
        return valid;
    }

    public bool Equals(CursorSizes? other) => other is not null && Values.SequenceEqual(other.Values);

    public override int GetHashCode() => Values.Aggregate(0, (hash, size) => HashCode.Combine(hash, size));

    public override string ToString() => string.Join(',', Values);
}

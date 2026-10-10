using System.Globalization;

namespace VectorToCursor.Domain;

/// <summary>
/// The frames of an animated cursor and the steps that show them. Every distinct frame is stored once: a frame equal to the
/// previous one lengthens the current step, and a frame seen earlier gets a new step that shows the stored one again.
/// </summary>
internal sealed class AnimationSequence
{
    public const int MaximumFrameCount = 1800;

    private readonly List<byte[]> _frames = [];
    private readonly Dictionary<byte[], int> _frameIndexes = new(ContentComparer.Instance);
    private readonly List<AnimationStep> _steps = [];

    /// <summary>The distinct frames, in the order they first appear.</summary>
    public IReadOnlyList<byte[]> Frames => _frames;

    /// <summary>The steps in playback order; no two neighbors show the same frame.</summary>
    public IReadOnlyList<AnimationStep> Steps => _steps;

    /// <summary>Shows <paramref name="frame"/> after everything added so far, for <paramref name="jiffies"/>.</summary>
    /// <exception cref="CursorConversionException">The frame would be distinct frame number <see cref="MaximumFrameCount"/> + 1.</exception>
    public void Add(byte[] frame, int jiffies)
    {
        ArgumentNullException.ThrowIfNull(frame);
        if (frame.Length == 0)
            throw new ArgumentException("A frame can't be empty.", nameof(frame));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(jiffies);

        int frameIndex = Store(frame);
        if (_steps.Count > 0 && _steps[^1].FrameIndex == frameIndex)
            _steps[^1] = _steps[^1] with { Jiffies = _steps[^1].Jiffies + jiffies };
        else
            _steps.Add(new AnimationStep(frameIndex, jiffies));
    }

    private int Store(byte[] frame)
    {
        if (_frameIndexes.TryGetValue(frame, out int index))
            return index;
        if (_frames.Count == MaximumFrameCount)
            throw new CursorConversionException(string.Create(CultureInfo.InvariantCulture, $"The animation has more than {MaximumFrameCount:N0} different frames, but at most {MaximumFrameCount:N0} are supported. Use a lower --fps or a shorter animation."));

        _frameIndexes.Add(frame, _frames.Count);
        _frames.Add(frame);
        return _frames.Count - 1;
    }

    private sealed class ContentComparer : IEqualityComparer<byte[]>
    {
        public static ContentComparer Instance { get; } = new();

        public bool Equals(byte[]? first, byte[]? second) =>
            ReferenceEquals(first, second) || (first is not null && second is not null && first.AsSpan().SequenceEqual(second));

        public int GetHashCode(byte[] bytes)
        {
            HashCode hash = new();
            hash.AddBytes(bytes);
            return hash.ToHashCode();
        }
    }
}

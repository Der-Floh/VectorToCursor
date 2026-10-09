using System.Globalization;

namespace VectorToCursor.Domain;

/// <summary>
/// Frames per second of an animated cursor. Animated cursors count time in jiffies (1/60 s), so only rates that divide 60
/// evenly are exact.
/// </summary>
internal readonly record struct FrameRate
{
    public const int JiffiesPerSecond = 60;

    public FrameRate(int framesPerSecond)
    {
        if (!IsValid(framesPerSecond))
            throw new ArgumentOutOfRangeException(nameof(framesPerSecond), framesPerSecond, $"The frame rate must divide {JiffiesPerSecond} evenly.");

        FramesPerSecond = framesPerSecond;
    }

    public static FrameRate Default { get; } = new(30);

    public int FramesPerSecond { get; }

    /// <summary>How long each frame is shown, in jiffies.</summary>
    public int Jiffies => JiffiesPerSecond / FramesPerSecond;

    public static bool TryCreate(int framesPerSecond, out FrameRate result)
    {
        bool valid = IsValid(framesPerSecond);
        result = valid ? new FrameRate(framesPerSecond) : default;
        return valid;
    }

    public override string ToString() => FramesPerSecond.ToString(CultureInfo.InvariantCulture);

    private static bool IsValid(int framesPerSecond) => framesPerSecond > 0 && JiffiesPerSecond % framesPerSecond == 0;
}

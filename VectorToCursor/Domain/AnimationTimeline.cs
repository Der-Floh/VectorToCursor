using System.Globalization;

namespace VectorToCursor.Domain;

/// <summary>The frames an animated cursor samples from one loop of an animation.</summary>
internal sealed class AnimationTimeline
{
    public const int MaximumFrameCount = 1800;

    private AnimationTimeline(TimeSpan loopDuration, FrameRate frameRate, int frameCount)
    {
        LoopDuration = loopDuration;
        FrameRate = frameRate;
        FrameCount = frameCount;
    }

    public TimeSpan LoopDuration { get; }

    public FrameRate FrameRate { get; }

    public int FrameCount { get; }

    /// <summary>
    /// How long one loop takes when played back; equal to <see cref="LoopDuration"/> exactly when the loop is a whole
    /// number of frames, thanks to integer tick arithmetic.
    /// </summary>
    public TimeSpan EffectiveDuration => TimeSpan.FromTicks((long)FrameCount * FrameRate.Jiffies * TimeSpan.TicksPerSecond / FrameRate.JiffiesPerSecond);

    /// <exception cref="CursorConversionException">The loop needs more than <see cref="MaximumFrameCount"/> frames.</exception>
    public static AnimationTimeline Create(TimeSpan loopDuration, FrameRate frameRate)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(loopDuration, TimeSpan.Zero);

        double exactFrames = (double)loopDuration.Ticks * frameRate.FramesPerSecond / TimeSpan.TicksPerSecond;
        long frameCount = Math.Max(1, (long)Math.Round(exactFrames, MidpointRounding.AwayFromZero));
        if (frameCount > MaximumFrameCount)
            throw new CursorConversionException(string.Create(CultureInfo.InvariantCulture, $"The animation needs {frameCount} frames at {frameRate} fps, but at most {MaximumFrameCount} are supported. Use a lower --fps or a shorter animation."));

        return new AnimationTimeline(loopDuration, frameRate, (int)frameCount);
    }

    /// <summary>The animation time of frame <paramref name="index"/>, computed in whole ticks so frames never drift.</summary>
    public TimeSpan TimeOf(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, FrameCount);

        return TimeSpan.FromTicks(LoopDuration.Ticks * index / FrameCount);
    }
}

namespace VectorToCursor.Domain;

/// <summary>The frames an animated cursor samples from one loop of an animation.</summary>
internal sealed class AnimationTimeline
{
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

    /// <param name="loopDuration">Positive and at most <see cref="AnimationLoop.MaximumLength"/>.</param>
    public static AnimationTimeline Create(TimeSpan loopDuration, FrameRate frameRate)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(loopDuration, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(loopDuration, AnimationLoop.MaximumLength);

        double exactFrames = (double)loopDuration.Ticks * frameRate.FramesPerSecond / TimeSpan.TicksPerSecond;
        int frameCount = Math.Max(1, (int)Math.Round(exactFrames, MidpointRounding.AwayFromZero));
        return new AnimationTimeline(loopDuration, frameRate, frameCount);
    }

    /// <summary>The animation time of frame <paramref name="index"/>, computed in whole ticks so frames never drift.</summary>
    public TimeSpan TimeOf(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, FrameCount);

        return TimeSpan.FromTicks(LoopDuration.Ticks * index / FrameCount);
    }
}

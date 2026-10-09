using VectorToCursor.Domain;

namespace VectorToCursor.Application;

/// <summary>What was written for an animated SVG.</summary>
/// <param name="LoopDuration">How long one loop of the SVG's animations lasts.</param>
/// <param name="EffectiveDuration">How long one loop of the cursor plays; differs when the loop isn't a whole number of frames.</param>
internal sealed record AnimationSummary(int FrameCount, FrameRate FrameRate, TimeSpan LoopDuration, TimeSpan EffectiveDuration);

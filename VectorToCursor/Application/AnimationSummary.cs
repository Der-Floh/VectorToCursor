using VectorToCursor.Domain;

namespace VectorToCursor.Application;

/// <summary>What was written for an animated SVG.</summary>
/// <param name="FrameCount">How many frames one loop plays at <paramref name="FrameRate"/>.</param>
/// <param name="DistinctFrameCount">How many of those frames differ and are stored; the others show a stored frame again.</param>
/// <param name="LoopDuration">How long one loop of the SVG's animations lasts.</param>
/// <param name="EffectiveDuration">How long one loop of the cursor plays; differs when the loop isn't a whole number of frames.</param>
internal sealed record AnimationSummary(int FrameCount, int DistinctFrameCount, FrameRate FrameRate, TimeSpan LoopDuration, TimeSpan EffectiveDuration);

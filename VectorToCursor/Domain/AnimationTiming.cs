namespace VectorToCursor.Domain;

/// <summary>When one animation plays.</summary>
/// <param name="Begin">When the first iteration starts; may be negative.</param>
/// <param name="SimpleDuration">The length of one iteration.</param>
/// <param name="ActiveDuration">How long it plays in total, or <see langword="null"/> when it repeats forever.</param>
internal readonly record struct AnimationTiming(TimeSpan Begin, TimeSpan SimpleDuration, TimeSpan? ActiveDuration);

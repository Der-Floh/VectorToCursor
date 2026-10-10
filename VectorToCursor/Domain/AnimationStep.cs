namespace VectorToCursor.Domain;

/// <summary>One step of an animated cursor: which stored frame it shows, and for how many jiffies (1/60 s).</summary>
internal readonly record struct AnimationStep(int FrameIndex, int Jiffies);

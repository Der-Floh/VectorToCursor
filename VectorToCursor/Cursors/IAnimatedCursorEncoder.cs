using VectorToCursor.Domain;

namespace VectorToCursor.Cursors;

/// <summary>Writes animation frames in the Windows animated cursor (.ani) format.</summary>
internal interface IAnimatedCursorEncoder
{
    /// <param name="sequence">The frames, each a complete .cur file, and the steps that show them; at least one step.</param>
    /// <param name="destination">A writable stream.</param>
    void Encode(AnimationSequence sequence, Stream destination);
}

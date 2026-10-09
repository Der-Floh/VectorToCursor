using VectorToCursor.Domain;

namespace VectorToCursor.Cursors;

/// <summary>Writes animation frames in the Windows animated cursor (.ani) format.</summary>
internal interface IAnimatedCursorEncoder
{
    /// <param name="frames">Each frame as a complete .cur file, in playback order.</param>
    /// <param name="frameRate">How fast the frames are played.</param>
    /// <param name="destination">A writable stream.</param>
    void Encode(IReadOnlyList<byte[]> frames, FrameRate frameRate, Stream destination);
}

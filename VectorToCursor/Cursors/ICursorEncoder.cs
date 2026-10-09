using VectorToCursor.Domain;

namespace VectorToCursor.Cursors;

/// <summary>Writes cursor frames in the Windows cursor (.cur) format.</summary>
internal interface ICursorEncoder
{
    /// <param name="frames">The frames in the order they are stored in the cursor.</param>
    /// <param name="destination">A writable, seekable stream.</param>
    /// <param name="format">How the images are stored.</param>
    void Encode(IReadOnlyList<CursorFrame> frames, Stream destination, CursorImageFormat format);
}

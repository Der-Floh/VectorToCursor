using VectorToCursor.Cursors;
using VectorToCursor.Domain;

namespace VectorToCursor.Application;

/// <summary>The image formats cursor files are stored in when none is requested.</summary>
internal static class ImageFormats
{
    public const CursorImageFormat StaticDefault = CursorImageFormat.Bmp;

    /// <summary>The format of an animated cursor whose BMP frames fit <see cref="AniFrameLimit"/>.</summary>
    public const CursorImageFormat AnimatedPreferred = CursorImageFormat.Bmp;

    /// <summary>
    /// The format of an animated cursor whose BMP frames would not fit <see cref="AniFrameLimit"/>, as with the default sizes,
    /// where the 128 px image already starts beyond 64 KB.
    /// </summary>
    public const CursorImageFormat AnimatedFallback = CursorImageFormat.Png;
}

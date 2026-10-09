namespace VectorToCursor.Domain;

/// <summary>
/// How the images of a cursor file are stored. Every image of a file uses the same format, because Windows ignores the
/// PNG images of a cursor that also holds BMP images.
/// </summary>
internal enum CursorImageFormat
{
    /// <summary>32-bit BMP, which every program that reads cursor files can load.</summary>
    Bmp,

    /// <summary>PNG, a fraction of the size; Windows draws it exactly like BMP, but some programs, such as WinForms, can't load it.</summary>
    Png,
}

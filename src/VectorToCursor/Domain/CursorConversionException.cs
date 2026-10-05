namespace VectorToCursor.Domain;

/// <summary>A conversion failure caused by the input, which the user can fix; reported as a plain message.</summary>
internal sealed class CursorConversionException : Exception
{
    public CursorConversionException(string message) : base(message)
    {
    }

    public CursorConversionException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

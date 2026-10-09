namespace VectorToCursor.Rendering.Animation.Css;

/// <param name="Property">The property name in lower case.</param>
/// <param name="Value">The value without a trailing <c>!important</c>.</param>
internal sealed record CssDeclaration(string Property, string Value, bool Important);

using SkiaSharp;
using Svg;
using Svg.Model;
using Svg.Model.Services;
using Svg.Skia;
using VectorToCursor.Domain;
using VectorToCursor.Rendering.Animation;

namespace VectorToCursor.Rendering;

/// <summary>
/// Loads SVG files with Svg.Skia. External resources are limited to files next to the SVG and <c>data:</c> URIs,
/// so a conversion never touches the network.
/// </summary>
internal sealed class SkiaSvgLoader : ISvgLoader
{
    public SkiaSvgLoader()
    {
        // <image> and <use> references honour only these process-wide switches, not the per-document policy.
        SvgDocument.ResolveExternalImages = ExternalType.Local;
        SvgDocument.ResolveExternalElements = ExternalType.Local;
    }

    public ISvgArtwork Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        SvgDocument document = Open(path);
        ArtworkBounds bounds = ResolveBounds(document, path);
        NormalizeViewport(document, bounds);
        TimeSpan? loopDuration = PrepareAnimations(document, path);
        return CreateArtwork(document, bounds, loopDuration, path);
    }

    private static SvgDocument Open(string path)
    {
        SvgParameters parameters = new(Entities: null, Css: null, LoadOptions: new SvgDocumentLoadOptions { ExternalResources = SvgExternalResourcePolicy.SameOrigin });
        try
        {
            // Capturing the compatibility style state would freeze every animation of an SVG that has both a <style> and a <use>.
            return SvgService.Open(path, parameters, captureCompatibilityStyleState: false) ?? throw new CursorConversionException($"'{path}' could not be read as an SVG file.");
        }
        catch (Exception exception) when (IsParserFailure(exception))
        {
            throw new CursorConversionException($"'{path}' is not a valid SVG file: {exception.Message}", exception);
        }
    }

    private static ArtworkBounds ResolveBounds(SvgDocument document, string path)
    {
        if (document.Transforms is { Count: > 0 })
            throw new CursorConversionException($"'{path}' has a transform on its root <svg> element, which is not supported. Move it onto a <g> element around the content.");

        SvgViewBox viewBox = document.ViewBox;
        if (!viewBox.Equals(SvgViewBox.Empty))
            return viewBox is { Width: > 0, Height: > 0 }
                ? new ArtworkBounds(viewBox.MinX, viewBox.MinY, viewBox.Width, viewBox.Height)
                : throw new CursorConversionException($"'{path}' has an invalid viewBox: its width and height must be positive.");

        if (document.Width.Type == SvgUnitType.Percentage || document.Height.Type == SvgUnitType.Percentage)
            throw new CursorConversionException($"'{path}' needs a viewBox or an absolute width and height to define its coordinate system.");

        // Without a viewBox, SVG user units are pixels; GetDimensions converts mm, pt and friends at 96 DPI.
        ShimSkiaSharp.SKSize size = SvgService.GetDimensions(document);
        return size is { Width: > 0, Height: > 0 }
            ? new ArtworkBounds(0, 0, size.Width, size.Height)
            : throw new CursorConversionException($"'{path}' has an empty width or height.");
    }

    // A viewport exactly as large as the viewBox makes the picture draw plain SVG coordinates, shifted by the viewBox origin.
    private static void NormalizeViewport(SvgDocument document, ArtworkBounds bounds)
    {
        document.ViewBox = new SvgViewBox((float)bounds.MinX, (float)bounds.MinY, (float)bounds.Width, (float)bounds.Height);
        document.Width = new SvgUnit(SvgUnitType.Pixel, (float)bounds.Width);
        document.Height = new SvgUnit(SvgUnitType.Pixel, (float)bounds.Height);
    }

    // Svg.Skia's engine only plays SMIL, so CSS animations are turned into SMIL first; the loop then covers both kinds.
    private static TimeSpan? PrepareAnimations(SvgDocument document, string path)
    {
        // The engine finds animated elements by their position below the root; a wrapped root would silently stop all animation.
        if (document.Parent is not null)
            throw new InvalidOperationException("SVG documents must be loaded through SvgService so that their root has no parent.");

        try
        {
            CssAnimationTranslator.Translate(document);
            return AnimationLoop.Calculate(SmilTimingReader.ReadAndNormalize(document));
        }
        catch (CursorConversionException exception)
        {
            throw new CursorConversionException($"'{path}': {exception.Message}", exception);
        }
    }

    private static SkiaSvgArtwork CreateArtwork(SvgDocument document, ArtworkBounds bounds, TimeSpan? loopDuration, string path)
    {
        SKSvg svg = new();
        try
        {
            // Otherwise a blocked or missing <image> is drawn as a placeholder glyph inside the cursor.
            svg.Settings.EnableBrokenImagePlaceholders = false;
            _ = svg.FromSvgDocument(document) ?? throw new CursorConversionException($"'{path}' could not be rendered.");
            return new SkiaSvgArtwork(svg, bounds, svg.HasAnimations ? loopDuration : null);
        }
        catch (Exception exception) when (IsParserFailure(exception))
        {
            svg.Dispose();
            throw new CursorConversionException($"'{path}' could not be rendered: {exception.Message}", exception);
        }
        catch
        {
            svg.Dispose();
            throw;
        }
    }

    // Svg.Skia reports malformed input through many exception types (XmlException, FormatException, ...).
    private static bool IsParserFailure(Exception exception) => exception is not (CursorConversionException or OutOfMemoryException);
}

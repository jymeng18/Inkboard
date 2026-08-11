#nullable enable
namespace Inkboard.Application.Interfaces;

public interface IImageValidator
{
    /// <summary>
    /// Confirms the payload actually decodes as a valid image,
    /// catching truncated, malformed, or polyglot files that pass
    /// the cheap signature and header checks.
    /// </summary>
    Task<bool> IsDecodablePngAsync(Stream imageData);

    /// <summary>
    /// Downscales an already validated PNG to a thumbnail whose longest edge is at
    /// most maxDimension, re-encoded as PNG. Never upscales. Returns null when the
    /// payload cannot be decoded.
    /// </summary>
    Task<Stream?> CreatePngThumbnailAsync(Stream pngData, int maxDimension);
}

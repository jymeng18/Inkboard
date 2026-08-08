namespace Inkboard.Application.Interfaces;

public interface IImageValidator
{
    // Full decode gate: confirms the payload actually decodes as a valid image,
    // catching truncated, malformed, or polyglot files that pass the cheap
    // signature and header checks.
    Task<bool> IsDecodablePngAsync(Stream imageData);
}

using Inkboard.Application.Interfaces;
using SkiaSharp;

namespace Inkboard.Infra.Imaging;

public class SkiaSharpValidator : IImageValidator
{
    public Task<bool> IsDecodablePngAsync(Stream imageData)
    {
        long originalPos = imageData.CanSeek ? imageData.Position : 0;

        try
        {
            // disposeManagedStream: false so Skia never disposes the caller's stream,
            using var skStream = new SKManagedStream(imageData, disposeManagedStream: false);
            using var codec = SKCodec.Create(skStream);

            // Unreadable, or decoded as some other format despite the PNG signature check.
            if (codec is null || codec.EncodedFormat != SKEncodedImageFormat.Png)
            {
                return Task.FromResult(false);
            }

            // Fully decode the pixels. This is what catches truncated or corrupt payloads
            // that pass the cheap signature and header checks. Dimensions were already
            // bounded by the caller, so the allocation here is capped.
            using var bitmap = SKBitmap.Decode(codec);
            return Task.FromResult(bitmap is not null);
        }
        catch
        {
            return Task.FromResult(false);
        }
        finally
        {
            if (imageData.CanSeek)
            {
                imageData.Position = originalPos;
            }
        }
    }
}

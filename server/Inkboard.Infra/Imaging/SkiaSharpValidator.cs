#nullable enable
using Inkboard.Application.Interfaces;
using SkiaSharp;

namespace Inkboard.Infra.Imaging;

/// <summary>
/// Image processing is completely CPU-bound, cannot make it asynchronous
/// The following code is assisted by Claude
/// </summary>
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

    public Task<Stream?> CreatePngThumbnailAsync(Stream pngData, int maxDimension)
    {
        long originalPos = pngData.CanSeek ? pngData.Position : 0;

        try
        {
            using var skStream = new SKManagedStream(pngData, disposeManagedStream: false);
            using var original = SKBitmap.Decode(skStream);
            if (original is null)
            {
                return Task.FromResult<Stream?>(null);
            }

            int longestEdge = Math.Max(original.Width, original.Height);
            float ratio = longestEdge > maxDimension ? (float)maxDimension / longestEdge : 1f;

            int width = Math.Max(1, (int)Math.Round(original.Width * ratio));
            int height = Math.Max(1, (int)Math.Round(original.Height * ratio));

            // Only allocate a resized copy when actually shrinking; a same-size
            // resample would just blur an already small snapshot.
            SKBitmap target = original;
            SKBitmap? resized = null;
            if (ratio < 1f)
            {
                var sampling = new SKSamplingOptions(SKCubicResampler.Mitchell);
                resized = original.Resize(new SKImageInfo(width, height), sampling);
                if (resized is not null)
                {
                    target = resized;
                }
            }

            try
            {
                using var image = SKImage.FromBitmap(target);
                using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);

                var output = new MemoryStream();
                encoded.SaveTo(output);
                output.Position = 0;
                return Task.FromResult<Stream?>(output);
            }
            finally
            {
                resized?.Dispose();
            }
        }
        catch
        {
            return Task.FromResult<Stream?>(null);
        }
        finally
        {
            if (pngData.CanSeek)
            {
                pngData.Position = originalPos;
            }
        }
    }
}

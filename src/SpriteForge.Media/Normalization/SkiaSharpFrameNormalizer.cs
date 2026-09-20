using SkiaSharp;
using SpriteForge.Core.Contracts;
using SpriteForge.Core.Models;

namespace SpriteForge.Media.Normalization;

public sealed class SkiaSharpFrameNormalizer : IFrameNormalizer
{
    public Task NormalizeAsync(NormalizationRequest request, CancellationToken cancellationToken)
    {
        Validate(request);
        return Task.Run(() => NormalizeCore(request, cancellationToken), cancellationToken);
    }

    private static void NormalizeCore(NormalizationRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var source = SKBitmap.Decode(request.InputPath)
            ?? throw new InvalidDataException($"Unable to decode image '{request.InputPath}'.");

        var sourceBounds = request.Settings.AutoTrim
            ? AlphaBounds.Find(source)
            : SKRectI.Create(0, 0, source.Width, source.Height);
        var scale = CalculateScale(sourceBounds.Width, sourceBounds.Height, request.Settings) * request.Transform.Scale;
        var targetWidth = Math.Max(1, (int)Math.Round(sourceBounds.Width * scale));
        var targetHeight = Math.Max(1, (int)Math.Round(sourceBounds.Height * scale));

        var imageInfo = new SKImageInfo(
            request.Settings.CanvasWidth,
            request.Settings.CanvasHeight,
            SKColorType.Rgba8888,
            SKAlphaType.Premul);
        using var output = new SKBitmap(imageInfo);
        using var canvas = new SKCanvas(output);
        canvas.Clear(SKColors.Transparent);

        var anchor = AnchorCalculator.Calculate(
            request.Settings.Anchor,
            request.Settings.CanvasWidth,
            request.Settings.CanvasHeight,
            targetWidth,
            targetHeight);
        var x = anchor.X + (int)Math.Round(request.Transform.OffsetX);
        var y = anchor.Y + (int)Math.Round(request.Transform.OffsetY);
        var sourceRect = new SKRect(sourceBounds.Left, sourceBounds.Top, sourceBounds.Right, sourceBounds.Bottom);
        var destinationRect = SKRect.Create(x, y, targetWidth, targetHeight);

        using var paint = new SKPaint { IsAntialias = true };
        canvas.DrawBitmap(source, sourceRect, destinationRect, paint);
        canvas.Flush();
        cancellationToken.ThrowIfCancellationRequested();

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(request.OutputPath))!);
        using var image = SKImage.FromBitmap(output);
        using var encoded = image.Encode(SKEncodedImageFormat.Png, 100)
            ?? throw new InvalidOperationException("SkiaSharp failed to encode normalized PNG output.");
        using var stream = new FileStream(request.OutputPath, FileMode.Create, FileAccess.Write, FileShare.None);
        encoded.SaveTo(stream);
    }

    private static double CalculateScale(int width, int height, NormalizationSettings settings)
    {
        if (width <= 0 || height <= 0) return 1;
        return settings.Fit.Trim().ToLowerInvariant() switch
        {
            "original" => 1,
            "contain" => Math.Min(settings.CanvasWidth / (double)width, settings.CanvasHeight / (double)height),
            "cover" => Math.Max(settings.CanvasWidth / (double)width, settings.CanvasHeight / (double)height),
            _ => throw new ArgumentException($"Unknown fit mode '{settings.Fit}'.", nameof(settings))
        };
    }

    private static void Validate(NormalizationRequest request)
    {
        if (!File.Exists(request.InputPath))
            throw new FileNotFoundException("Input frame was not found.", request.InputPath);
        if (request.Settings.CanvasWidth <= 0 || request.Settings.CanvasHeight <= 0)
            throw new ArgumentOutOfRangeException(nameof(request), "Canvas dimensions must be positive.");
        if (request.Transform.Scale <= 0)
            throw new ArgumentOutOfRangeException(nameof(request), "Scale must be positive.");
    }
}

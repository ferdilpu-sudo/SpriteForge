using SkiaSharp;
using SpriteForge.Core.Contracts;
using SpriteForge.Core.Models;

namespace SpriteForge.Pipeline.Tests;

internal sealed class SyntheticAlphaCutoutService : IBackgroundRemovalService
{
    public Task<ProcessedFrame> RemoveAsync(
        string inputPath,
        string outputPath,
        BackgroundRemovalOptions options,
        CancellationToken cancellationToken) =>
        Task.Run(() => Cutout(inputPath, outputPath, cancellationToken), cancellationToken);

    private static ProcessedFrame Cutout(
        string inputPath,
        string outputPath,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var source = SKBitmap.Decode(inputPath)
            ?? throw new InvalidDataException($"Unable to decode test frame '{inputPath}'.");

        var imageInfo = new SKImageInfo(
            source.Width,
            source.Height,
            SKColorType.Rgba8888,
            SKAlphaType.Premul);
        using var output = new SKBitmap(imageInfo);
        using var canvas = new SKCanvas(output);
        canvas.Clear(SKColors.Transparent);

        var border = Math.Max(1, Math.Min(source.Width, source.Height) / 8);
        var destination = SKRect.Create(
            border,
            border,
            Math.Max(1, source.Width - border * 2),
            Math.Max(1, source.Height - border * 2));
        var sampling = new SKSamplingOptions(SKFilterMode.Nearest);
        canvas.DrawBitmap(source, destination, sampling);
        canvas.Flush();
        cancellationToken.ThrowIfCancellationRequested();

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath))!);
        using var image = SKImage.FromBitmap(output);
        using var encoded = image.Encode(SKEncodedImageFormat.Png, 100)
            ?? throw new InvalidOperationException("Unable to encode test cutout PNG.");
        using var stream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);
        encoded.SaveTo(stream);
        return new ProcessedFrame(inputPath, outputPath);
    }
}

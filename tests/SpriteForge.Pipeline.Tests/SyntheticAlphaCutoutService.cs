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
        using var output = source.Copy(SKColorType.Rgba8888)
            ?? throw new InvalidOperationException("Unable to create test cutout bitmap.");

        var border = Math.Max(1, Math.Min(output.Width, output.Height) / 8);
        for (var y = 0; y < output.Height; y++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            for (var x = 0; x < output.Width; x++)
            {
                if (x >= border && x < output.Width - border &&
                    y >= border && y < output.Height - border)
                {
                    continue;
                }

                var pixel = output.GetPixel(x, y);
                output.SetPixel(x, y, new SKColor(pixel.Red, pixel.Green, pixel.Blue, 0));
            }
        }

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath))!);
        using var image = SKImage.FromBitmap(output);
        using var encoded = image.Encode(SKEncodedImageFormat.Png, 100)
            ?? throw new InvalidOperationException("Unable to encode test cutout PNG.");
        using var stream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);
        encoded.SaveTo(stream);
        return new ProcessedFrame(inputPath, outputPath);
    }
}

using SkiaSharp;

namespace SpriteForge.Media.Looping;

internal sealed record ImageDifferenceSample(byte[] Rgba);

internal sealed class ImageDifferenceScorer
{
    private const int SampleSize = 64;

    public Task<ImageDifferenceSample> LoadAsync(string path, CancellationToken cancellationToken) =>
        Task.Run(() => LoadCore(path, cancellationToken), cancellationToken);

    public double Score(
        ImageDifferenceSample first,
        ImageDifferenceSample second,
        CancellationToken cancellationToken)
    {
        if (first.Rgba.Length != second.Rgba.Length)
            throw new InvalidOperationException("Loop-analysis samples must have matching dimensions.");

        long total = 0;
        for (var index = 0; index < first.Rgba.Length; index++)
        {
            if ((index & 0x0FFF) == 0)
                cancellationToken.ThrowIfCancellationRequested();

            total += Math.Abs(first.Rgba[index] - second.Rgba[index]);
        }

        return total / (first.Rgba.Length * 255d);
    }

    private static ImageDifferenceSample LoadCore(string path, CancellationToken cancellationToken)
    {
        using var source = SKBitmap.Decode(path)
            ?? throw new InvalidDataException($"Unable to decode frame '{path}'.");
        using var sample = new SKBitmap(new SKImageInfo(
            SampleSize,
            SampleSize,
            SKColorType.Rgba8888,
            SKAlphaType.Premul));
        using var canvas = new SKCanvas(sample);
        canvas.Clear(SKColors.Transparent);
        using var paint = new SKPaint { IsAntialias = true };
        var sampling = new SKSamplingOptions(SKFilterMode.Nearest);
        canvas.DrawBitmap(source, SKRect.Create(0, 0, SampleSize, SampleSize), sampling, paint);
        canvas.Flush();

        var pixels = new byte[SampleSize * SampleSize * 4];
        var offset = 0;
        for (var y = 0; y < SampleSize; y++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            for (var x = 0; x < SampleSize; x++)
            {
                var color = sample.GetPixel(x, y);
                pixels[offset++] = color.Red;
                pixels[offset++] = color.Green;
                pixels[offset++] = color.Blue;
                pixels[offset++] = color.Alpha;
            }
        }

        return new ImageDifferenceSample(pixels);
    }
}

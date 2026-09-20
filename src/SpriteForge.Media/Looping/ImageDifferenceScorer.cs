using SkiaSharp;

namespace SpriteForge.Media.Looping;

internal sealed class ImageDifferenceScorer
{
    private const int SampleSize = 64;

    public Task<double> ScoreAsync(string firstPath, string secondPath, CancellationToken cancellationToken) =>
        Task.Run(() => ScoreCore(firstPath, secondPath, cancellationToken), cancellationToken);

    private static double ScoreCore(string firstPath, string secondPath, CancellationToken cancellationToken)
    {
        using var first = LoadSample(firstPath);
        using var second = LoadSample(secondPath);

        long total = 0;
        var samples = SampleSize * SampleSize * 4L;
        for (var y = 0; y < SampleSize; y++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            for (var x = 0; x < SampleSize; x++)
            {
                var a = first.GetPixel(x, y);
                var b = second.GetPixel(x, y);
                total += Math.Abs(a.Red - b.Red);
                total += Math.Abs(a.Green - b.Green);
                total += Math.Abs(a.Blue - b.Blue);
                total += Math.Abs(a.Alpha - b.Alpha);
            }
        }

        return total / (samples * 255d);
    }

    private static SKBitmap LoadSample(string path)
    {
        using var source = SKBitmap.Decode(path)
            ?? throw new InvalidDataException($"Unable to decode frame '{path}'.");
        var sample = new SKBitmap(new SKImageInfo(SampleSize, SampleSize, SKColorType.Rgba8888, SKAlphaType.Premul));
        using var canvas = new SKCanvas(sample);
        canvas.Clear(SKColors.Transparent);
        using var paint = new SKPaint { IsAntialias = true };
        canvas.DrawBitmap(source, SKRect.Create(0, 0, SampleSize, SampleSize), paint);
        canvas.Flush();
        return sample;
    }
}

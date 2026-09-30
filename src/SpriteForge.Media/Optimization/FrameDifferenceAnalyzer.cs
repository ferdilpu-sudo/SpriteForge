using SkiaSharp;

namespace SpriteForge.Media.Optimization;

internal sealed class FrameDifferenceAnalyzer
{
    private const int SampleSize = 64;
    private const double FocusFraction = 0.25;

    public Task<double> DifferenceAsync(string firstPath, string secondPath, CancellationToken cancellationToken) =>
        Task.Run(() => DifferenceCore(firstPath, secondPath, cancellationToken), cancellationToken);

    private static double DifferenceCore(string firstPath, string secondPath, CancellationToken cancellationToken)
    {
        using var first = LoadSample(firstPath);
        using var second = LoadSample(secondPath);

        var pixelDifferences = new double[SampleSize * SampleSize];
        var offset = 0;
        for (var y = 0; y < SampleSize; y++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            for (var x = 0; x < SampleSize; x++)
            {
                var a = first.GetPixel(x, y);
                var b = second.GetPixel(x, y);

                // Extracted video frames are opaque. Including their unchanged alpha channel
                // dilutes visible RGB motion by 25% and can collapse subtle animation to one frame.
                pixelDifferences[offset++] =
                    (Math.Abs(a.Red - b.Red) +
                     Math.Abs(a.Green - b.Green) +
                     Math.Abs(a.Blue - b.Blue)) / (3d * 255d);
            }
        }

        Array.Sort(pixelDifferences);
        var focusCount = Math.Max(1, (int)Math.Ceiling(pixelDifferences.Length * FocusFraction));
        var start = pixelDifferences.Length - focusCount;
        double total = 0;
        for (var index = start; index < pixelDifferences.Length; index++)
            total += pixelDifferences[index];

        return total / focusCount;
    }

    private static SKBitmap LoadSample(string path)
    {
        var encodedBytes = File.ReadAllBytes(path);
        using var encodedStream = new MemoryStream(encodedBytes, writable: false);
        using var source = SKBitmap.Decode(encodedStream)
            ?? throw new InvalidDataException($"Unable to decode frame '{path}'.");

        var sample = new SKBitmap(new SKImageInfo(
            SampleSize,
            SampleSize,
            SKColorType.Rgba8888,
            SKAlphaType.Premul));
        using var canvas = new SKCanvas(sample);
        canvas.Clear(SKColors.Transparent);
        using var paint = new SKPaint { IsAntialias = true };
        canvas.DrawBitmap(
            source,
            SKRect.Create(0, 0, SampleSize, SampleSize),
            new SKSamplingOptions(SKFilterMode.Linear),
            paint);
        canvas.Flush();
        return sample;
    }
}

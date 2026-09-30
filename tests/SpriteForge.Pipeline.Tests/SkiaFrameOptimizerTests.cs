using SkiaSharp;
using SpriteForge.Core.Models;
using SpriteForge.Media.Optimization;

namespace SpriteForge.Pipeline.Tests;

public sealed class SkiaFrameOptimizerTests
{
    [Fact]
    public async Task Balanced_FillsMotionRange_AndUsesEvenTiming()
    {
        var root = CreateTempDirectory();
        try
        {
            var colors = new[]
            {
                SKColors.Red,
                SKColors.Blue,
                SKColors.Green,
                SKColors.Yellow,
                SKColors.Purple
            };
            var paths = new List<string>();
            foreach (var color in colors)
            {
                for (var repeat = 0; repeat < 4; repeat++)
                    paths.Add(CreateSolidFrame(root, paths.Count, color));
            }

            var result = await new SkiaFrameOptimizer().OptimizeAsync(
                new FrameOptimizationRequest(
                    paths,
                    80,
                    FrameOptimizationSettings.BalancedDefault),
                null,
                TestContext.Current.CancellationToken);

            var enabled = result.Decisions
                .Where(decision => decision.Enabled)
                .ToArray();

            Assert.Equal(10, enabled.Length);
            Assert.Equal(3, enabled[0].SourceIndex);
            Assert.Equal(17, enabled[^1].SourceIndex);
            Assert.All(enabled, decision =>
                Assert.Equal(120d, decision.DurationMs, precision: 6));
            Assert.Equal(
                15 * 80d,
                enabled.Sum(decision => decision.DurationMs),
                precision: 6);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task Balanced_DoesNotInheritLongStaticPreOrPostRoll()
    {
        var root = CreateTempDirectory();
        try
        {
            var paths = new List<string>();
            for (var index = 0; index < 24; index++)
            {
                var color = index switch
                {
                    < 6 => SKColors.Navy,
                    6 => SKColors.DarkRed,
                    7 => SKColors.OrangeRed,
                    8 => SKColors.Gold,
                    9 => SKColors.Green,
                    10 => SKColors.Blue,
                    _ => SKColors.Blue
                };
                paths.Add(CreateSolidFrame(root, index, color));
            }

            var result = await new SkiaFrameOptimizer().OptimizeAsync(
                new FrameOptimizationRequest(
                    paths,
                    80,
                    FrameOptimizationSettings.BalancedDefault),
                null,
                TestContext.Current.CancellationToken);

            var enabled = result.Decisions
                .Where(decision => decision.Enabled)
                .ToArray();

            Assert.Equal(7, enabled.Length);
            Assert.Equal(5, enabled[0].SourceIndex);
            Assert.Equal(11, enabled[^1].SourceIndex);
            Assert.All(enabled, decision =>
            {
                Assert.InRange(decision.SourceIndex, 5, 11);
                Assert.Equal(80d, decision.DurationMs, precision: 6);
            });
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }


    [Fact]
    public async Task Balanced_PreservesSubtleContinuousMotion()
    {
        var root = CreateTempDirectory();
        try
        {
            var paths = Enumerable.Range(0, 18)
                .Select(index => CreateMovingSquareFrame(root, index))
                .ToArray();

            var result = await new SkiaFrameOptimizer().OptimizeAsync(
                new FrameOptimizationRequest(
                    paths,
                    80,
                    FrameOptimizationSettings.BalancedDefault),
                null,
                TestContext.Current.CancellationToken);

            var enabled = result.Decisions
                .Where(decision => decision.Enabled)
                .ToArray();

            Assert.True(enabled.Length > 1);
            Assert.InRange(enabled.Length, 3, 10);
            Assert.True(enabled[^1].SourceIndex > enabled[0].SourceIndex);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task Raw_KeepsEveryFrame_WithOriginalTiming()
    {
        var root = CreateTempDirectory();
        try
        {
            var paths = Enumerable.Range(0, 5)
                .Select(index => CreateSolidFrame(
                    root,
                    index,
                    SKColors.CornflowerBlue))
                .ToArray();

            var result = await new SkiaFrameOptimizer().OptimizeAsync(
                new FrameOptimizationRequest(
                    paths,
                    50,
                    new FrameOptimizationSettings("raw", 0.96, true)),
                null,
                TestContext.Current.CancellationToken);

            Assert.Equal(paths.Length, result.EnabledCount);
            Assert.All(result.Decisions, decision =>
            {
                Assert.True(decision.Enabled);
                Assert.Equal(50d, decision.DurationMs);
            });
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }


    private static string CreateMovingSquareFrame(string root, int index)
    {
        var path = Path.Combine(root, $"subtle_{index:D3}.png");
        using var bitmap = new SKBitmap(64, 64);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(new SKColor(80, 80, 80));

        using var paint = new SKPaint { Color = new SKColor(96, 96, 96) };
        canvas.DrawRect(8 + index, 24, 18, 18, paint);

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = File.Create(path);
        data.SaveTo(stream);
        return path;
    }

    private static string CreateSolidFrame(
        string root,
        int index,
        SKColor color)
    {
        var path = Path.Combine(root, $"frame_{index:D3}.png");
        using var bitmap = new SKBitmap(32, 32);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(color);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = File.Create(path);
        data.SaveTo(stream);
        return path;
    }

    private static string CreateTempDirectory()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            "spriteforge-frame-optimizer",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }
}

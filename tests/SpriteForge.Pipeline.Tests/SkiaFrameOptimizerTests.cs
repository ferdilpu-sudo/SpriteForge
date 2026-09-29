using SkiaSharp;
using SpriteForge.Core.Models;
using SpriteForge.Media.Optimization;

namespace SpriteForge.Pipeline.Tests;

public sealed class SkiaFrameOptimizerTests
{
    [Fact]
    public async Task Balanced_RemovesRepeatedFrames_AndPreservesTotalDuration()
    {
        var root = CreateTempDirectory();
        try
        {
            var colors = new[] { SKColors.Red, SKColors.Blue, SKColors.Green, SKColors.Yellow, SKColors.Purple };
            var paths = new List<string>();
            foreach (var color in colors)
                for (var repeat = 0; repeat < 4; repeat++)
                    paths.Add(CreateSolidFrame(root, paths.Count, color));

            var result = await new SkiaFrameOptimizer().OptimizeAsync(
                new FrameOptimizationRequest(paths, 80, FrameOptimizationSettings.BalancedDefault),
                null,
                TestContext.Current.CancellationToken);

            Assert.InRange(result.EnabledCount, 8, 12);
            Assert.True(result.EnabledCount < paths.Count);
            Assert.True(result.Decisions[0].Enabled);
            Assert.True(result.Decisions[^1].Enabled);
            var totalDuration = result.Decisions.Where(decision => decision.Enabled).Sum(decision => decision.DurationMs);
            Assert.Equal(paths.Count * 80d, totalDuration, precision: 6);
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
                .Select(index => CreateSolidFrame(root, index, SKColors.CornflowerBlue))
                .ToArray();
            var result = await new SkiaFrameOptimizer().OptimizeAsync(
                new FrameOptimizationRequest(paths, 50, new FrameOptimizationSettings("raw", 0.96, true)),
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

    private static string CreateSolidFrame(string root, int index, SKColor color)
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
        var root = Path.Combine(Path.GetTempPath(), "spriteforge-frame-optimizer", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }
}

using SpriteForge.Core.Models;
using SpriteForge.Infrastructure.Processes;
using SpriteForge.Media.Ffmpeg;

namespace SpriteForge.Pipeline.Tests;

public sealed class FfmpegFrameExtractorIntegrationTests
{
    [Fact]
    public async Task ExtractAsync_FromTinyGeneratedVideo_ProducesOrderedFrames()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var root = Path.Combine(Path.GetTempPath(), "spriteforge-ffmpeg-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            var processRunner = new ProcessRunner();
            var sourceVideo = Path.Combine(root, "input.mp4");
            var generation = await processRunner.RunAsync(
                "ffmpeg",
                [
                    "-hide_banner", "-loglevel", "error", "-y",
                    "-f", "lavfi",
                    "-i", "color=c=red:s=32x32:r=6:d=1",
                    "-c:v", "mpeg4",
                    "-q:v", "2",
                    sourceVideo
                ],
                null,
                cancellationToken);

            Assert.True(generation.Succeeded, generation.StandardError);

            var outputDirectory = Path.Combine(root, "frames");
            var extractor = new FfmpegFrameExtractor(processRunner);
            var result = await extractor.ExtractAsync(
                new FrameExtractionRequest(
                    sourceVideo,
                    outputDirectory,
                    new ExtractionSettings(3, 0, 1)),
                progress: null,
                cancellationToken);

            Assert.Equal(3, result.FramePaths.Count);
            Assert.Equal(3, result.Fps);
            Assert.Equal(
                ["frame_000001.png", "frame_000002.png", "frame_000003.png"],
                result.FramePaths.Select(Path.GetFileName).ToArray());
            Assert.All(result.FramePaths, path => Assert.True(File.Exists(path)));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }
}

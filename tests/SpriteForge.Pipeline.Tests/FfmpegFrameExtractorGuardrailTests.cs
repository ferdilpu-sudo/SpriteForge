using SpriteForge.Core.Contracts;
using SpriteForge.Core.Errors;
using SpriteForge.Core.Models;
using SpriteForge.Media.Ffmpeg;

namespace SpriteForge.Pipeline.Tests;

public sealed class FfmpegFrameExtractorGuardrailTests
{
    [Fact]
    public async Task ExtractAsync_RejectsKnownRangeAboveCandidateLimit_BeforeStartingFfmpeg()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            "spriteforge-ffmpeg-guard-tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            var source = Path.Combine(root, "input.mp4");
            await File.WriteAllBytesAsync(
                source,
                [0x00],
                TestContext.Current.CancellationToken);

            var runner = new UnexpectedProcessRunner();
            var extractor = new FfmpegFrameExtractor(runner);

            var exception = await Assert.ThrowsAsync<SpriteForgeException>(() =>
                extractor.ExtractAsync(
                    new FrameExtractionRequest(
                        source,
                        Path.Combine(root, "frames"),
                        new ExtractionSettings(120, 0, 20)),
                    null,
                    TestContext.Current.CancellationToken));

            Assert.Equal("FRAME_EXTRACTION_LIMIT", exception.Code);
            Assert.False(runner.WasCalled);
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    private sealed class UnexpectedProcessRunner : IExternalProcessRunner
    {
        public bool WasCalled { get; private set; }

        public Task<ExternalProcessResult> RunAsync(
            string executable,
            IEnumerable<string> arguments,
            string? workingDirectory,
            CancellationToken cancellationToken)
        {
            WasCalled = true;
            throw new InvalidOperationException("FFmpeg should not start when preflight rejects the extraction.");
        }
    }
}

using SpriteForge.Core.Contracts;
using SpriteForge.Core.Errors;
using SpriteForge.Core.Models;
using SpriteForge.Media.Ffmpeg;

namespace SpriteForge.Pipeline.Tests;

public sealed class FfmpegFrameExtractorGuardrailTests
{
    [Fact]
    public async Task ExtractAsync_RejectsKnownRangeAboveCandidateLimit_BeforeStartingAnyProcess()
    {
        var root = CreateRoot();
        try
        {
            var source = await CreateDummySourceAsync(root);
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
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ExtractAsync_RejectsHighResolutionPixelBudget_BeforeStartingFfmpeg()
    {
        var root = CreateRoot();
        try
        {
            var source = await CreateDummySourceAsync(root);
            var runner = new ProbeOnlyProcessRunner(
                """{"streams":[{"width":3840,"height":2160}],"format":{"duration":"100.0"}}""");
            var extractor = new FfmpegFrameExtractor(runner);

            var exception = await Assert.ThrowsAsync<SpriteForgeException>(() =>
                extractor.ExtractAsync(
                    new FrameExtractionRequest(
                        source,
                        Path.Combine(root, "frames"),
                        new ExtractionSettings(2, 0, 100)),
                    null,
                    TestContext.Current.CancellationToken));

            Assert.Equal("FRAME_EXTRACTION_BUDGET", exception.Code);
            Assert.True(runner.ProbeWasCalled);
            Assert.False(runner.FfmpegWasCalled);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string CreateRoot()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            "spriteforge-ffmpeg-guard-tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }

    private static async Task<string> CreateDummySourceAsync(string root)
    {
        var source = Path.Combine(root, "input.mp4");
        await File.WriteAllBytesAsync(
            source,
            [0x00],
            TestContext.Current.CancellationToken);
        return source;
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
            throw new InvalidOperationException("No external process should start when preflight rejects the extraction.");
        }
    }

    private sealed class ProbeOnlyProcessRunner(string probeJson) : IExternalProcessRunner
    {
        public bool ProbeWasCalled { get; private set; }
        public bool FfmpegWasCalled { get; private set; }

        public Task<ExternalProcessResult> RunAsync(
            string executable,
            IEnumerable<string> arguments,
            string? workingDirectory,
            CancellationToken cancellationToken)
        {
            if (string.Equals(executable, "ffprobe", StringComparison.OrdinalIgnoreCase))
            {
                ProbeWasCalled = true;
                return Task.FromResult(new ExternalProcessResult(0, probeJson, string.Empty));
            }

            FfmpegWasCalled = true;
            throw new InvalidOperationException("FFmpeg should not start when the pixel budget is rejected.");
        }
    }
}

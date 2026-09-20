using SpriteForge.Application.Frames;
using SpriteForge.Application.Pipeline;
using SpriteForge.Application.Sheets;
using SpriteForge.Core.Contracts;
using SpriteForge.Core.Models;

namespace SpriteForge.Application.Tests;

public sealed class PipelinePreconditionTests
{
    [Fact]
    public async Task NormalizeFrames_RequiresTransparentArtifacts_WhenCutoutIsEnabled()
    {
        var project = new ProjectDocument
        {
            BackgroundRemoval = new BackgroundRemovalSettings(true, "local_default", 0.05)
        };
        project.Frames.Add(Frame(extracted: Guid.NewGuid(), transparent: null, normalized: null));

        var service = CreateService();
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.NormalizeFramesAsync(
            project,
            new ProjectWorkspacePaths(Path.GetTempPath()),
            progress: null,
            CancellationToken.None));

        Assert.True(exception.Message.Contains("Run Cutout before Align", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task AnalyzeLoop_RequiresNormalizedArtifacts()
    {
        var project = new ProjectDocument();
        project.Frames.Add(Frame(extracted: Guid.NewGuid(), transparent: Guid.NewGuid(), normalized: null));

        var service = CreateService();
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.AnalyzeLoopAsync(
            project,
            new ProjectWorkspacePaths(Path.GetTempPath()),
            CancellationToken.None));

        Assert.True(exception.Message.Contains("Run Align before loop analysis", StringComparison.OrdinalIgnoreCase));
    }

    private static SpritePipelineService CreateService() => new(
        new UnexpectedFrameExtractor(),
        new UnexpectedBackgroundRemovalService(),
        new UnexpectedFrameNormalizer(),
        new UnexpectedLoopAnalyzer(),
        new UnexpectedSheetExporter(),
        new UnexpectedMetadataExporter(),
        new UnexpectedIndividualFrameExporter(),
        new UnexpectedHashService(),
        new SheetLayoutCalculator(),
        new FrameSequenceSelector());

    private static FrameRecord Frame(Guid? extracted, Guid? transparent, Guid? normalized) => new(
        Guid.NewGuid(),
        0,
        0,
        true,
        83.333,
        new FrameArtifactLinks(extracted, transparent, normalized, null),
        new FrameTransform(0, 0, 1),
        new FramePivot(0.5, 1));

    private sealed class UnexpectedFrameExtractor : IFrameExtractor
    {
        public Task<FrameExtractionResult> ExtractAsync(FrameExtractionRequest request, IProgress<PipelineProgress>? progress, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Frame extractor should not be called by this test.");
    }

    private sealed class UnexpectedBackgroundRemovalService : IBackgroundRemovalService
    {
        public Task<ProcessedFrame> RemoveAsync(string inputPath, string outputPath, BackgroundRemovalOptions options, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Background removal should not be called by this test.");
    }

    private sealed class UnexpectedFrameNormalizer : IFrameNormalizer
    {
        public Task NormalizeAsync(NormalizationRequest request, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Normalizer should not be called before preconditions pass.");
    }

    private sealed class UnexpectedLoopAnalyzer : ILoopAnalyzer
    {
        public Task<IReadOnlyList<LoopCandidate>> AnalyzeAsync(IReadOnlyList<FrameRecord> frames, Func<Guid, string?> resolveFramePath, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Loop analyzer should not be called before preconditions pass.");
    }

    private sealed class UnexpectedSheetExporter : ISpriteSheetExporter
    {
        public Task ExportAsync(string outputPath, SheetLayout layout, IReadOnlyList<SheetFrame> frames, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Sheet exporter should not be called by this test.");
    }

    private sealed class UnexpectedMetadataExporter : IMetadataExporter
    {
        public Task ExportAsync(string outputPath, string imageFileName, SheetLayout layout, double defaultFps, bool loop, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Metadata exporter should not be called by this test.");
    }

    private sealed class UnexpectedIndividualFrameExporter : IIndividualFrameExporter
    {
        public Task<IReadOnlyList<string>> ExportAsync(IEnumerable<string> orderedFrames, string destinationDirectory, string baseName, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Frame exporter should not be called by this test.");
    }

    private sealed class UnexpectedHashService : IFileHashService
    {
        public Task<string> ComputeSha256Async(string path, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Hash service should not be called by this test.");
    }
}

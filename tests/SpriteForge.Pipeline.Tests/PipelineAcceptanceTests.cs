using SkiaSharp;
using SpriteForge.Application.Frames;
using SpriteForge.Application.Imports;
using SpriteForge.Application.Pipeline;
using SpriteForge.Application.Sheets;
using SpriteForge.Core.Models;
using SpriteForge.Export.Frames;
using SpriteForge.Export.Metadata;
using SpriteForge.Export.Sheets;
using SpriteForge.Infrastructure.Hashing;
using SpriteForge.Infrastructure.Persistence;
using SpriteForge.Infrastructure.Processes;
using SpriteForge.Media.Ffmpeg;
using SpriteForge.Media.Looping;
using SpriteForge.Media.Normalization;
using SpriteForge.Media.Validation;

namespace SpriteForge.Pipeline.Tests;

public sealed class PipelineAcceptanceTests
{
    [Fact]
    public async Task VideoProject_ReopenAndReexport_ProducesIdenticalOutput()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var root = Path.Combine(Path.GetTempPath(), "spriteforge-acceptance", Guid.NewGuid().ToString("N"));
        var sourceDirectory = Path.Combine(root, "input");
        var projectRoot = Path.Combine(root, "project");
        var firstExportDirectory = Path.Combine(root, "export-1");
        var secondExportDirectory = Path.Combine(root, "export-2");
        Directory.CreateDirectory(sourceDirectory);
        Directory.CreateDirectory(projectRoot);

        try
        {
            var processRunner = new ProcessRunner();
            var sourceVideo = Path.Combine(sourceDirectory, "fixture.mp4");
            await CreateVideoAsync(processRunner, sourceVideo, cancellationToken);

            var workspace = new ProjectWorkspacePaths(projectRoot);
            var project = new ProjectDocument
            {
                Name = "Acceptance Fixture",
                Extraction = new ExtractionSettings(6, 0, 1),
                BackgroundRemoval = new BackgroundRemovalSettings(true, "test_synthetic_alpha", 0.05),
                Normalization = new NormalizationSettings(64, 64, "contain", "bottom_center", true),
                Sheet = new SheetSettings(3, 64, 64, 0, 0, false)
            };

            var hashService = new FileHashService();
            var importer = new SourceImportService(
                hashService,
                new SourceAssetValidator(processRunner));
            var sourceArtifact = await importer.ImportVideoAsync(
                project,
                workspace,
                sourceVideo,
                cancellationToken);
            var importedVideo = workspace.ResolveRelative(sourceArtifact.RelativePath);

            var pipeline = CreatePipeline(processRunner, hashService);
            await pipeline.ExtractFramesAsync(project, workspace, importedVideo, null, cancellationToken);
            Assert.Equal(6, project.Frames.Count);

            await pipeline.RemoveBackgroundsAsync(project, workspace, null, cancellationToken);
            Assert.All(project.Frames, frame => Assert.NotNull(frame.Artifacts.Transparent));
            Assert.True(ContainsTransparentPixel(project, workspace));

            await pipeline.NormalizeFramesAsync(project, workspace, null, cancellationToken);
            Assert.All(project.Frames, frame => Assert.NotNull(frame.Artifacts.Normalized));

            var candidates = await pipeline.AnalyzeLoopAsync(project, workspace, cancellationToken);
            Assert.NotEmpty(candidates);
            var selectedLoop = candidates[0];
            project.Loop = project.Loop with
            {
                StartFrameId = selectedLoop.StartFrameId,
                EndFrameId = selectedLoop.EndFrameId,
                Recommended = true
            };

            var previewPath = await pipeline.BuildSheetPreviewAsync(project, workspace, cancellationToken);
            Assert.True(File.Exists(previewPath));

            const string fingerprint = "acceptance-v1-fixture";
            var firstExport = await pipeline.ExportAsync(
                project,
                workspace,
                new ExportPipelineRequest(
                    "acceptance",
                    firstExportDirectory,
                    IncludeIndividualFrames: true,
                    fingerprint),
                cancellationToken);

            Assert.True(File.Exists(firstExport.SheetPath));
            Assert.True(File.Exists(firstExport.MetadataPath));
            Assert.NotEmpty(firstExport.IndividualFramePaths);

            var repository = new JsonProjectRepository();
            var projectFile = Path.Combine(projectRoot, "project.spriteforge.json");
            await repository.SaveAsync(projectFile, project, cancellationToken);
            var reopened = await repository.LoadAsync(projectFile, cancellationToken);

            Assert.Equal(project.ProjectId, reopened.ProjectId);
            Assert.Equal(project.Frames.Count, reopened.Frames.Count);
            Assert.Equal(project.Loop.StartFrameId, reopened.Loop.StartFrameId);
            Assert.Equal(project.Loop.EndFrameId, reopened.Loop.EndFrameId);

            var secondExport = await pipeline.ExportAsync(
                reopened,
                workspace,
                new ExportPipelineRequest(
                    "acceptance",
                    secondExportDirectory,
                    IncludeIndividualFrames: true,
                    fingerprint),
                cancellationToken);

            Assert.Equal(
                await hashService.ComputeSha256Async(firstExport.SheetPath, cancellationToken),
                await hashService.ComputeSha256Async(secondExport.SheetPath, cancellationToken));
            Assert.Equal(
                await hashService.ComputeSha256Async(firstExport.MetadataPath, cancellationToken),
                await hashService.ComputeSha256Async(secondExport.MetadataPath, cancellationToken));
            Assert.Equal(firstExport.IndividualFramePaths.Count, secondExport.IndividualFramePaths.Count);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    private static SpritePipelineService CreatePipeline(
        ProcessRunner processRunner,
        FileHashService hashService) =>
        new(
            new FfmpegFrameExtractor(processRunner),
            new SyntheticAlphaCutoutService(),
            new SkiaSharpFrameNormalizer(),
            new ImageDifferenceLoopAnalyzer(),
            new PngSpriteSheetExporter(),
            new JsonMetadataExporter(),
            new IndividualFrameExporter(),
            hashService,
            new SheetLayoutCalculator(),
            new FrameSequenceSelector());

    private static async Task CreateVideoAsync(
        ProcessRunner processRunner,
        string outputPath,
        CancellationToken cancellationToken)
    {
        var result = await processRunner.RunAsync(
            "ffmpeg",
            [
                "-hide_banner", "-loglevel", "error", "-y",
                "-f", "lavfi",
                "-i", "testsrc2=size=32x32:rate=6:duration=1",
                "-c:v", "mpeg4",
                "-q:v", "2",
                outputPath
            ],
            null,
            cancellationToken);

        Assert.True(result.Succeeded, result.StandardError);
    }

    private static bool ContainsTransparentPixel(
        ProjectDocument project,
        ProjectWorkspacePaths workspace)
    {
        var artifactId = project.Frames[0].Artifacts.Transparent
            ?? throw new InvalidOperationException("Transparent artifact is missing.");
        var artifact = project.Artifacts.Single(candidate => candidate.Id == artifactId);
        using var bitmap = SKBitmap.Decode(workspace.ResolveRelative(artifact.RelativePath))
            ?? throw new InvalidDataException("Unable to decode transparent acceptance frame.");

        for (var y = 0; y < bitmap.Height; y++)
        {
            for (var x = 0; x < bitmap.Width; x++)
            {
                if (bitmap.GetPixel(x, y).Alpha == 0)
                {
                    return true;
                }
            }
        }

        return false;
    }
}

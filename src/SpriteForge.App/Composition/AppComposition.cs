using SpriteForge.Application.Fingerprints;
using SpriteForge.Application.Frames;
using SpriteForge.Application.Imports;
using SpriteForge.Application.Pipeline;
using SpriteForge.Application.Projects;
using SpriteForge.Application.Sheets;
using SpriteForge.Diagnostics.Logging;
using SpriteForge.Diagnostics.Services;
using SpriteForge.Export.Frames;
using SpriteForge.Export.Metadata;
using SpriteForge.Export.Sheets;
using SpriteForge.Infrastructure.Hashing;
using SpriteForge.Infrastructure.Persistence;
using SpriteForge.Infrastructure.Processes;
using SpriteForge.Infrastructure.Workspace;
using SpriteForge.Media.BackgroundRemoval;
using SpriteForge.Media.Ffmpeg;
using SpriteForge.Media.Looping;
using SpriteForge.Media.Normalization;
using SpriteForge.Media.Validation;

namespace SpriteForge.App.Composition;

internal static class AppComposition
{
    public static AppServices Build()
    {
        var processRunner = new ProcessRunner();
        var repository = new JsonProjectRepository();
        var projects = new ProjectService(repository);
        var hashService = new FileHashService();
        var workerScript = Path.Combine(AppContext.BaseDirectory, "workers", "background-removal", "worker.py");
        var workerPython = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SpriteForge",
            "worker",
            ".venv",
            "Scripts",
            "python.exe");

        var sourceAssetValidator = new SourceAssetValidator(processRunner);

        var pipeline = new SpritePipelineService(
            new FfmpegFrameExtractor(processRunner),
            new BackgroundRemovalWorker(processRunner, workerPython, workerScript),
            new SkiaSharpFrameNormalizer(),
            new ImageDifferenceLoopAnalyzer(),
            new PngSpriteSheetExporter(),
            new JsonMetadataExporter(),
            new IndividualFrameExporter(),
            hashService,
            new SheetLayoutCalculator(),
            new FrameSequenceSelector());

        return new AppServices(
            projects,
            new SourceImportService(hashService, sourceAssetValidator),
            pipeline,
            new PipelineJobRunner(),
            new PipelineInvalidationService(),
            new PipelineFingerprintService(),
            new WorkspaceInitializer(),
            new JobLogWriter(),
            new FfmpegDiagnosticService(processRunner),
            new BackgroundRemovalDiagnosticService(processRunner, workerPython, workerScript),
            new WorkspaceDiagnosticService());
    }
}

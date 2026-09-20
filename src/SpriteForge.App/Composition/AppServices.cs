using SpriteForge.Application.Fingerprints;
using SpriteForge.Application.Imports;
using SpriteForge.Application.Pipeline;
using SpriteForge.Application.Projects;
using SpriteForge.Diagnostics.Logging;
using SpriteForge.Diagnostics.Services;
using SpriteForge.Infrastructure.Workspace;

namespace SpriteForge.App.Composition;

public sealed record AppServices(
    ProjectService Projects,
    SourceImportService SourceImports,
    SpritePipelineService Pipeline,
    PipelineJobRunner Jobs,
    PipelineInvalidationService Invalidations,
    PipelineFingerprintService Fingerprints,
    WorkspaceInitializer Workspace,
    JobLogWriter JobLogs,
    FfmpegDiagnosticService FfmpegDiagnostics,
    BackgroundRemovalDiagnosticService BackgroundRemovalDiagnostics,
    WorkspaceDiagnosticService WorkspaceDiagnostics);

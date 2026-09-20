namespace SpriteForge.Core.Models;

public sealed record PipelineProgress(double Value, string Message, int? Current = null, int? Total = null);

public sealed record FrameExtractionRequest(
    string SourcePath,
    string OutputDirectory,
    ExtractionSettings Settings);

public sealed record FrameExtractionResult(IReadOnlyList<string> FramePaths, double Fps);

public sealed record BackgroundRemovalOptions(double AlphaThreshold);

public sealed record ProcessedFrame(string InputPath, string OutputPath);

public sealed record NormalizationRequest(
    string InputPath,
    string OutputPath,
    NormalizationSettings Settings,
    FrameTransform Transform);

public sealed record SheetFrame(string Path, Guid FrameId, double DurationMs, FramePivot Pivot);

public sealed record ExportPipelineRequest(
    string ExportName,
    string DestinationDirectory,
    bool IncludeIndividualFrames,
    string SettingsFingerprint);

public sealed record ExportPipelineResult(
    string SheetPath,
    string MetadataPath,
    IReadOnlyList<string> IndividualFramePaths,
    ExportRecord Record);

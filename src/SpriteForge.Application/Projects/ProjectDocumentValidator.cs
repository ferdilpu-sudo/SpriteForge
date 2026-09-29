using System.Diagnostics.CodeAnalysis;
using SpriteForge.Core.Errors;
using SpriteForge.Core.Models;

namespace SpriteForge.Application.Projects;

public sealed class ProjectDocumentValidator
{
    private static readonly HashSet<string> FitModes = new(StringComparer.Ordinal)
    {
        "contain", "cover", "original"
    };

    private static readonly HashSet<string> Anchors = new(StringComparer.Ordinal)
    {
        "top_left", "top_center", "top_right",
        "center_left", "center", "center_right",
        "bottom_left", "bottom_center", "bottom_right"
    };

    private static readonly HashSet<string> FrameOptimizationModes = new(StringComparer.Ordinal)
    {
        "raw", "compact", "balanced", "smooth"
    };

    public void Validate(ProjectDocument project)
    {
        ArgumentNullException.ThrowIfNull(project);

        if (project.ProjectId == Guid.Empty) Fail("Project ID is missing.");
        if (string.IsNullOrWhiteSpace(project.Name)) Fail("Project name is missing.");
        ValidateSettings(project);
        ValidateCollections(project);

        var artifacts = project.Artifacts;
        var frames = project.Frames;
        var artifactIds = artifacts.Select(artifact => artifact.Id).ToHashSet();
        var frameIds = frames.Select(frame => frame.Id).ToHashSet();

        if (artifactIds.Count != artifacts.Count) Fail("Artifact IDs must be unique.");
        if (frameIds.Count != frames.Count) Fail("Frame IDs must be unique.");
        if (frames.Select(frame => frame.Order).Distinct().Count() != frames.Count)
            Fail("Frame order values must be unique.");

        foreach (var artifact in artifacts) ValidateArtifact(artifact);
        foreach (var frame in frames) ValidateFrame(frame, artifactIds);
        ValidateReferences(project, artifactIds, frameIds);
    }

    private static void ValidateSettings(ProjectDocument project)
    {
        if (project.Extraction is null) Fail("Extraction settings are missing.");
        if (project.FrameOptimization is null) Fail("Frame-optimization settings are missing.");
        if (project.BackgroundRemoval is null) Fail("Background-removal settings are missing.");
        if (project.Normalization is null) Fail("Normalization settings are missing.");
        if (project.Loop is null) Fail("Loop settings are missing.");
        if (project.Sheet is null) Fail("Sheet settings are missing.");

        var extraction = project.Extraction;
        if (!double.IsFinite(extraction.Fps) || extraction.Fps <= 0 || extraction.Fps > 120)
            Fail("Extraction FPS must be between 0 and 120.");
        if (!double.IsFinite(extraction.StartSeconds) || extraction.StartSeconds < 0)
            Fail("Extraction start time is invalid.");
        if (extraction.EndSeconds is { } end &&
            (!double.IsFinite(end) || end <= extraction.StartSeconds))
            Fail("Extraction end time must be greater than the start time.");

        var optimization = project.FrameOptimization;
        if (optimization.Mode is null || !FrameOptimizationModes.Contains(optimization.Mode))
            Fail($"Unknown frame optimization mode '{optimization.Mode ?? "<null>"}'.");
        if (!double.IsFinite(optimization.SimilarityThreshold) || optimization.SimilarityThreshold is < 0 or > 1)
            Fail("Frame optimization similarity threshold must be between 0 and 1.");

        var background = project.BackgroundRemoval;
        if (string.IsNullOrWhiteSpace(background.ProcessorId))
            Fail("Background-removal processor ID is missing.");
        if (!double.IsFinite(background.AlphaThreshold) ||
            background.AlphaThreshold < 0 ||
            background.AlphaThreshold > 1)
            Fail("Background-removal alpha threshold must be between 0 and 1.");

        var normalization = project.Normalization;
        if (normalization.CanvasWidth is < 1 or > 16_384 ||
            normalization.CanvasHeight is < 1 or > 16_384)
            Fail("Normalization canvas dimensions are invalid.");
        if (normalization.Fit is null || !FitModes.Contains(normalization.Fit))
            Fail($"Unknown normalization fit mode '{normalization.Fit ?? "<null>"}'.");
        if (normalization.Anchor is null || !Anchors.Contains(normalization.Anchor))
            Fail($"Unknown normalization anchor '{normalization.Anchor ?? "<null>"}'.");

        var sheet = project.Sheet;
        if (sheet.CellWidth is < 1 or > 16_384 || sheet.CellHeight is < 1 or > 16_384)
            Fail("Sheet cell dimensions are invalid.");
        if (sheet.Columns is <= 0) Fail("Sheet columns must be positive when specified.");
        if (sheet.Padding < 0 || sheet.Spacing < 0)
            Fail("Sheet padding and spacing cannot be negative.");
    }

    private static void ValidateCollections(ProjectDocument project)
    {
        if (project.Frames is null) Fail("Frame collection is missing.");
        if (project.Artifacts is null) Fail("Artifact collection is missing.");
        if (project.Exports is null) Fail("Export collection is missing.");
    }

    private static void ValidateArtifact(ArtifactRecord artifact)
    {
        if (artifact is null) Fail("Project contains a null artifact.");
        if (artifact.Id == Guid.Empty) Fail("Artifact ID is missing.");
        if (string.IsNullOrWhiteSpace(artifact.RelativePath)) Fail($"Artifact {artifact.Id} has no path.");
        if (string.IsNullOrWhiteSpace(artifact.MimeType)) Fail($"Artifact {artifact.Id} has no MIME type.");
        if (string.IsNullOrWhiteSpace(artifact.Sha256)) Fail($"Artifact {artifact.Id} has no SHA-256 hash.");
    }

    private static void ValidateFrame(FrameRecord frame, IReadOnlySet<Guid> artifactIds)
    {
        if (frame is null) Fail("Project contains a null frame.");
        if (frame.Id == Guid.Empty) Fail("Frame ID is missing.");
        if (frame.SourceIndex < 0 || frame.Order < 0) Fail($"Frame {frame.Id} has an invalid index.");
        if (!double.IsFinite(frame.DurationMs) || frame.DurationMs <= 0)
            Fail($"Frame {frame.Id} has an invalid duration.");
        if (frame.Artifacts is null) Fail($"Frame {frame.Id} has no artifact links.");
        if (frame.Transform is null) Fail($"Frame {frame.Id} has no transform.");
        if (frame.Pivot is null) Fail($"Frame {frame.Id} has no pivot.");

        if (!double.IsFinite(frame.Transform.OffsetX) ||
            !double.IsFinite(frame.Transform.OffsetY) ||
            !double.IsFinite(frame.Transform.Scale) ||
            frame.Transform.Scale <= 0)
            Fail($"Frame {frame.Id} has an invalid transform.");

        if (!double.IsFinite(frame.Pivot.X) ||
            !double.IsFinite(frame.Pivot.Y) ||
            frame.Pivot.X is < 0 or > 1 ||
            frame.Pivot.Y is < 0 or > 1)
            Fail($"Frame {frame.Id} has an invalid pivot.");

        ValidateArtifactLink(frame.Id, "extracted", frame.Artifacts.Extracted, artifactIds);
        ValidateArtifactLink(frame.Id, "transparent", frame.Artifacts.Transparent, artifactIds);
        ValidateArtifactLink(frame.Id, "normalized", frame.Artifacts.Normalized, artifactIds);
        ValidateArtifactLink(frame.Id, "thumbnail", frame.Artifacts.Thumbnail, artifactIds);
    }

    private static void ValidateReferences(
        ProjectDocument project,
        IReadOnlySet<Guid> artifactIds,
        IReadOnlySet<Guid> frameIds)
    {
        if (project.Source is not null && !artifactIds.Contains(project.Source.ArtifactId))
            Fail("Project source references a missing artifact.");

        if (project.Generation?.OutputArtifactId is { } generated && !artifactIds.Contains(generated))
            Fail("Generated video references a missing artifact.");

        var loopStart = project.Loop.StartFrameId;
        var loopEnd = project.Loop.EndFrameId;
        if ((loopStart is null) != (loopEnd is null))
            Fail("Loop start and end must either both be set or both be empty.");
        if (loopStart is { } start && !frameIds.Contains(start))
            Fail("Loop start references a missing frame.");
        if (loopEnd is { } end && !frameIds.Contains(end))
            Fail("Loop end references a missing frame.");

        var exportIds = new HashSet<Guid>();
        foreach (var export in project.Exports)
        {
            if (export is null) Fail("Project contains a null export record.");
            if (export.Id == Guid.Empty || !exportIds.Add(export.Id))
                Fail("Export IDs must be present and unique.");
            if (!artifactIds.Contains(export.SheetArtifactId) ||
                !artifactIds.Contains(export.MetadataArtifactId))
                Fail($"Export {export.Id} references a missing artifact.");
        }
    }

    private static void ValidateArtifactLink(
        Guid frameId,
        string label,
        Guid? artifactId,
        IReadOnlySet<Guid> artifactIds)
    {
        if (artifactId is { } id && !artifactIds.Contains(id))
            Fail($"Frame {frameId} {label} link references a missing artifact.");
    }

    [DoesNotReturn]
    private static void Fail(string message) =>
        throw new SpriteForgeException("PROJECT_INVALID", message, recoverable: false);
}

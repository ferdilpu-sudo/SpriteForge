using SpriteForge.Core.Enums;
using SpriteForge.Core.Errors;
using SpriteForge.Core.Models;
using SpriteForge.Presentation.Frames;
using SpriteForge.Presentation.Looping;

namespace SpriteForge.App.Workflow;

internal sealed partial class DesktopWorkflowController
{
    private void ApplyLoopCandidate(LoopCandidate candidate, bool recommended)
    {
        CurrentProject.Loop = new LoopSettings(true, candidate.StartFrameId, candidate.EndFrameId, recommended);
        var enabled = CurrentProject.Frames.Where(frame => frame.Enabled).OrderBy(frame => frame.Order).ToArray();
        _viewModel.LoopStart = Array.FindIndex(enabled, frame => frame.Id == candidate.StartFrameId) + 1;
        _viewModel.LoopEnd = Array.FindIndex(enabled, frame => frame.Id == candidate.EndFrameId) + 1;
        _viewModel.LoopEnabled = true;
    }

    private void PopulateLoopCandidates(IReadOnlyList<LoopCandidate> candidates)
    {
        var enabled = CurrentProject.Frames.Where(frame => frame.Enabled).OrderBy(frame => frame.Order).ToArray();
        _viewModel.ReplaceLoopCandidates(candidates.Select(candidate => new LoopCandidateViewModel(
            candidate,
            Array.FindIndex(enabled, frame => frame.Id == candidate.StartFrameId) + 1,
            Array.FindIndex(enabled, frame => frame.Id == candidate.EndFrameId) + 1)));
    }

    private IEnumerable<string> EnabledFrameHashes(
        bool preferTransparent = false,
        bool preferNormalized = false)
    {
        foreach (var frame in CurrentProject.Frames.Where(frame => frame.Enabled).OrderBy(frame => frame.Order))
        {
            var id = preferNormalized
                ? frame.Artifacts.Normalized ?? frame.Artifacts.Transparent ?? frame.Artifacts.Extracted
                : preferTransparent
                    ? frame.Artifacts.Transparent ?? frame.Artifacts.Extracted
                    : frame.Artifacts.Extracted;
            if (id is null) continue;
            var artifact = CurrentProject.Artifacts.FirstOrDefault(candidate => candidate.Id == id.Value);
            if (artifact is not null) yield return artifact.Sha256;
        }
    }

    private ArtifactRecord ResolveSourceArtifact()
    {
        var project = CurrentProject;
        var sourceId = project.Generation?.OutputArtifactId ?? project.Source?.ArtifactId
            ?? throw new InvalidOperationException("Import a source asset first.");
        return project.Artifacts.FirstOrDefault(artifact => artifact.Id == sourceId)
            ?? throw new InvalidOperationException("The source artifact is missing from project metadata.");
    }

    private void RefreshViewModel(Guid? preferredFrameId = null, int? preferredIndex = null)
    {
        var project = CurrentProject;
        var workspace = CurrentWorkspace;
        _viewModel.ProjectName = project.Name;
        _viewModel.ProjectPath = _projectFile ?? string.Empty;
        _viewModel.PreviewFps = project.Extraction.Fps;
        _viewModel.ExtractionStartSeconds = project.Extraction.StartSeconds;
        _viewModel.ExtractionHasEndTime = project.Extraction.EndSeconds.HasValue;
        _viewModel.ExtractionEndSeconds = project.Extraction.EndSeconds ?? Math.Max(3, project.Extraction.StartSeconds + 3);
        _viewModel.BackgroundRemovalEnabled = project.BackgroundRemoval.Enabled;
        _viewModel.AlphaThreshold = project.BackgroundRemoval.AlphaThreshold;
        _viewModel.CanvasWidth = project.Normalization.CanvasWidth;
        _viewModel.CanvasHeight = project.Normalization.CanvasHeight;
        _viewModel.FitMode = project.Normalization.Fit;
        _viewModel.Anchor = project.Normalization.Anchor;
        _viewModel.AutoTrim = project.Normalization.AutoTrim;
        _viewModel.LoopEnabled = project.Loop.Enabled;
        _viewModel.SheetCellWidth = project.Sheet.CellWidth;
        _viewModel.SheetCellHeight = project.Sheet.CellHeight;
        _viewModel.SheetColumns = project.Sheet.Columns ?? 0;
        _viewModel.SheetPadding = project.Sheet.Padding;
        _viewModel.SheetSpacing = project.Sheet.Spacing;
        _viewModel.PowerOfTwoSheet = project.Sheet.PowerOfTwo;
        if (string.IsNullOrWhiteSpace(_viewModel.ExportName) || _viewModel.ExportName == "sprite")
            _viewModel.ExportName = MakeSafeExportName(project.Name);

        var source = project.Source is null
            ? null
            : project.Artifacts.FirstOrDefault(artifact => artifact.Id == project.Source.ArtifactId);
        _viewModel.SourceKind = project.Source?.Kind ?? "None";
        _viewModel.SourceSummary = source is null
            ? "No source imported"
            : $"{Path.GetFileName(source.RelativePath)} · {source.MimeType}";

        var frameViewModels = project.Frames.OrderBy(frame => frame.Order).Select(frame =>
        {
            var previewArtifactId = frame.Artifacts.Normalized ?? frame.Artifacts.Transparent ?? frame.Artifacts.Extracted;
            var artifact = previewArtifactId is null
                ? null
                : project.Artifacts.FirstOrDefault(candidate => candidate.Id == previewArtifactId.Value);
            var path = artifact is null ? null : ResolveExistingArtifactPath(artifact);
            return new FrameThumbnailViewModel(frame.Id, frame.SourceIndex, frame.Order, frame.Enabled, path);
        }).ToArray();
        _viewModel.ReplaceFrames(frameViewModels);

        if (_viewModel.Frames.Count > 0)
        {
            var selectedIndex = preferredIndex
                ?? (preferredFrameId is null ? _viewModel.CurrentFrameIndex : Array.FindIndex(frameViewModels, frame => frame.FrameId == preferredFrameId.Value));
            _viewModel.CurrentFrameIndex = Math.Clamp(selectedIndex ?? 0, 0, _viewModel.Frames.Count - 1);
            _viewModel.SelectedFrame = _viewModel.Frames[_viewModel.CurrentFrameIndex];
            _viewModel.PreviewImagePath = _viewModel.SelectedFrame.PreviewPath;
            var selectedRecord = project.Frames.First(frame => frame.Id == _viewModel.SelectedFrame.FrameId);
            _viewModel.SelectedFrameDurationMs = selectedRecord.DurationMs;
            _viewModel.SelectedFramePivotX = selectedRecord.Pivot.X;
            _viewModel.SelectedFramePivotY = selectedRecord.Pivot.Y;
        }
        else
        {
            _viewModel.SelectedFrame = null;
            _viewModel.PreviewImagePath = source?.MimeType.StartsWith("image/", StringComparison.OrdinalIgnoreCase) == true
                ? ResolveExistingArtifactPath(source)
                : null;
        }

        SyncLoopDisplay();
        RefreshStageStates();
    }

    private void SyncLoopDisplay()
    {
        var enabled = CurrentProject.Frames.Where(frame => frame.Enabled).OrderBy(frame => frame.Order).ToArray();
        if (enabled.Length == 0)
        {
            _viewModel.LoopStart = 1;
            _viewModel.LoopEnd = 1;
            return;
        }

        var start = CurrentProject.Loop.StartFrameId is { } startId
            ? Array.FindIndex(enabled, frame => frame.Id == startId)
            : 0;
        var end = CurrentProject.Loop.EndFrameId is { } endId
            ? Array.FindIndex(enabled, frame => frame.Id == endId)
            : enabled.Length - 1;
        _viewModel.LoopStart = Math.Max(0, start) + 1;
        _viewModel.LoopEnd = Math.Max(0, end) + 1;
    }

    private void RefreshStageStates()
    {
        var project = CurrentProject;
        var enabled = project.Frames.Where(frame => frame.Enabled).ToArray();
        var sourceReady = project.Source is not null && ArtifactFileAvailable(project.Source.ArtifactId);
        var extractedReady = project.Frames.Count > 0 && project.Frames.All(frame => ArtifactFileAvailable(frame.Artifacts.Extracted));
        var transparentReady = enabled.Length > 0 && enabled.All(frame => ArtifactFileAvailable(frame.Artifacts.Transparent));
        var normalizedReady = enabled.Length > 0 && enabled.All(frame => ArtifactFileAvailable(frame.Artifacts.Normalized));
        var hasTransparentMetadata = enabled.Any(frame => frame.Artifacts.Transparent is not null);
        var hasNormalizedMetadata = enabled.Any(frame => frame.Artifacts.Normalized is not null);

        _viewModel.SetStageState(PipelineStage.Source,
            project.Source is null ? StageState.Neutral : sourceReady ? StageState.Complete : StageState.Error);
        _viewModel.SetStageState(PipelineStage.Animate,
            project.Source?.Kind == "image" ? StageState.Neutral : project.Source is null ? StageState.Neutral : StageState.Skipped);
        _viewModel.SetStageState(PipelineStage.Frames,
            project.Frames.Count == 0 ? StageState.Neutral : extractedReady ? StageState.Complete : StageState.Stale);
        _viewModel.SetStageState(PipelineStage.Cutout,
            project.Frames.Count == 0 ? StageState.Neutral :
            !project.BackgroundRemoval.Enabled ? StageState.Skipped :
            transparentReady ? StageState.Complete :
            hasTransparentMetadata ? StageState.Stale : StageState.Neutral);
        _viewModel.SetStageState(PipelineStage.Align,
            normalizedReady ? StageState.Complete :
            hasNormalizedMetadata ? StageState.Stale : StageState.Neutral);
        _viewModel.SetStageState(PipelineStage.Loop,
            enabled.Length == 0 ? StageState.Neutral :
            !project.Loop.Enabled ? StageState.Skipped :
            project.Loop.StartFrameId is not null && project.Loop.EndFrameId is not null
                ? normalizedReady ? StageState.Complete : StageState.Stale
                : StageState.Neutral);

        var latestExport = project.Exports.OrderByDescending(export => export.CreatedAt).FirstOrDefault();
        var exportIsCurrent = latestExport is not null && IsExportCurrent(latestExport);
        _viewModel.SetStageState(PipelineStage.Sheet,
            latestExport is null ? StageState.Neutral : exportIsCurrent ? StageState.Complete : StageState.Stale);
        _viewModel.SetStageState(PipelineStage.Export,
            latestExport is null ? StageState.Neutral : exportIsCurrent ? StageState.Complete : StageState.Stale);
    }

    private bool IsExportCurrent(ExportRecord record)
    {
        try
        {
            if (!ArtifactFileAvailable(record.SheetArtifactId) || !ArtifactFileAvailable(record.MetadataArtifactId))
                return false;

            var includeFrames = string.Equals(record.Format, "png_json_frames", StringComparison.Ordinal);
            return string.Equals(record.SettingsFingerprint, ComputeExportFingerprint(includeFrames), StringComparison.Ordinal);
        }
        catch
        {
            return false;
        }
    }

    private bool ArtifactFileAvailable(Guid? artifactId)
    {
        if (artifactId is null) return false;
        var artifact = CurrentProject.Artifacts.FirstOrDefault(candidate => candidate.Id == artifactId.Value);
        return artifact is not null && ResolveExistingArtifactPath(artifact) is not null;
    }

    private string? ResolveExistingArtifactPath(ArtifactRecord artifact)
    {
        try
        {
            var path = CurrentWorkspace.ResolveRelative(artifact.RelativePath);
            return File.Exists(path) ? path : null;
        }
        catch
        {
            return null;
        }
    }

    private string ComputeExportFingerprint(bool includeIndividualFrames)
    {
        var project = CurrentProject;
        var sequence = _services.Pipeline.GetExportSequence(project);
        var frames = sequence.Select(frame =>
        {
            var artifact = frame.Artifacts.Normalized is { } id
                ? project.Artifacts.FirstOrDefault(candidate => candidate.Id == id)
                : null;
            return new
            {
                frame.Id,
                frame.DurationMs,
                frame.Pivot,
                RasterSha256 = artifact?.Sha256 ?? string.Empty
            };
        }).ToArray();

        return _services.Fingerprints.Compute("export", new
        {
            project.Extraction.Fps,
            project.Loop,
            project.Sheet,
            frames,
            ExportIndividualFrames = includeIndividualFrames
        });
    }

    private void MarkDownstreamStale(PipelineStage changedStage)
    {
        foreach (var stage in _services.Invalidations.GetInvalidatedStages(changedStage))
        {
            var item = _viewModel.GetStage(stage);
            if (item.State is StageState.Complete or StageState.Skipped) item.State = StageState.Stale;
        }
    }

    private void AdoptSourceName(string sourcePath)
    {
        if (CurrentProject.Name != "Untitled Sprite") return;
        var name = Path.GetFileNameWithoutExtension(sourcePath).Trim();
        if (!string.IsNullOrWhiteSpace(name)) CurrentProject.Name = name;
    }

    private void ShowError(Exception exception)
    {
        _viewModel.JobStatus.Message = exception switch
        {
            SpriteForgeException spriteError => $"{spriteError.Code}: {spriteError.Message}",
            _ => exception.Message
        };
    }

    private async Task EnsureProjectAsync()
    {
        if (_project is null) await CreateNewProjectAsync();
    }

    private ProjectDocument CurrentProject => _project ?? throw new InvalidOperationException("No project is open.");
    private ProjectWorkspacePaths CurrentWorkspace => _workspace ?? throw new InvalidOperationException("No project workspace is open.");

    private static string NormalizeFit(string value) => value.Trim().ToLowerInvariant() switch
    {
        "contain" => "contain",
        "cover" => "cover",
        "original" => "original",
        _ => "contain"
    };

    private static string NormalizeAnchor(string value) => value.Trim().ToLowerInvariant().Replace(' ', '_') switch
    {
        "top_left" => "top_left",
        "top_center" => "top_center",
        "top_right" => "top_right",
        "center_left" => "center_left",
        "center" => "center",
        "center_right" => "center_right",
        "bottom_left" => "bottom_left",
        "bottom_center" => "bottom_center",
        "bottom_right" => "bottom_right",
        _ => "bottom_center"
    };

    private static string MakeSafeExportName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars().ToHashSet();
        var chars = value.Trim().ToLowerInvariant().Select(character =>
            char.IsWhiteSpace(character) ? '_' : invalid.Contains(character) ? '_' : character);
        var result = string.Concat(chars).Trim('_');
        return string.IsNullOrWhiteSpace(result) ? "sprite" : result;
    }

}

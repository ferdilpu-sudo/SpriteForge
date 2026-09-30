using SpriteForge.Core.Enums;
using SpriteForge.Core.Models;

namespace SpriteForge.App.Workflow;

internal sealed partial class DesktopWorkflowController
{
    public async Task AnalyzeLoopAsync()
    {
        var project = CurrentProject;
        IReadOnlyList<LoopCandidate> candidates = [];
        var fingerprint = _services.Fingerprints.Compute("analyze_loop", new
        {
            frames = EnabledFrameHashes(preferNormalized: true),
            order = project.Frames.Where(frame => frame.Enabled).OrderBy(frame => frame.Order).Select(frame => frame.Id)
        });

        await RunStageAsync(
            PipelineStage.Loop,
            JobStage.AnalyzeLoop,
            fingerprint,
            async (progress, token) =>
            {
                progress.Report(new PipelineProgress(0.1, "Analyzing loop seams"));
                candidates = await _services.Pipeline.AnalyzeLoopAsync(project, CurrentWorkspace, token);
                progress.Report(new PipelineProgress(1, $"Found {candidates.Count} loop candidates"));
                return [];
            });

        if (_viewModel.GetStage(PipelineStage.Loop).State != StageState.Complete) return;

        PopulateLoopCandidates(candidates);
        if (candidates.Count == 0)
        {
            project.Loop = project.Loop with
            {
                StartFrameId = null,
                EndFrameId = null,
                Recommended = false
            };
            await SaveAsync();
            RefreshViewModel();
            _viewModel.SetStageState(PipelineStage.Loop, StageState.Neutral);
            _viewModel.JobStatus.Message =
                "No automatic loop seam was found. Choose a manual range or disable looping.";
            return;
        }

        ApplyLoopCandidate(candidates[0], recommended: true);
        await SaveAsync();
        RefreshViewModel();
        MarkDownstreamStale(PipelineStage.Loop);
        _viewModel.SetStageState(PipelineStage.Loop, StageState.Complete);
        _viewModel.NavigateTo(PipelineStage.Sheet);
    }

    public async Task ApplyManualLoopAsync()
    {
        var enabled = CurrentProject.Frames.Where(frame => frame.Enabled).OrderBy(frame => frame.Order).ToArray();
        if (enabled.Length == 0) return;
        var startNumber = NormalizeFrameNumber(_viewModel.LoopStart, 1, enabled.Length);
        var endNumber = NormalizeFrameNumber(_viewModel.LoopEnd, enabled.Length, enabled.Length);
        var start = startNumber - 1;
        var end = Math.Max(start, endNumber - 1);
        _viewModel.LoopStart = start + 1;
        _viewModel.LoopEnd = end + 1;
        CurrentProject.Loop = _viewModel.LoopEnabled
            ? new LoopSettings(true, enabled[start].Id, enabled[end].Id, false)
            : new LoopSettings(false, null, null, false);
        await SaveAsync();
        RefreshViewModel();
        _viewModel.SetStageState(PipelineStage.Loop, _viewModel.LoopEnabled ? StageState.Complete : StageState.Skipped);
        MarkDownstreamStale(PipelineStage.Loop);
    }

    public async Task UseSelectedLoopSuggestionAsync()
    {
        if (_viewModel.SelectedLoopCandidate is null) return;
        ApplyLoopCandidate(_viewModel.SelectedLoopCandidate.Candidate, recommended: true);
        await SaveAsync();
        RefreshViewModel();
        _viewModel.SetStageState(PipelineStage.Loop, StageState.Complete);
        MarkDownstreamStale(PipelineStage.Loop);
    }

    public async Task BuildSheetAsync()
    {
        var project = CurrentProject;
        project.Sheet = new SheetSettings(
            NormalizeColumns(_viewModel.SheetColumns, project.Sheet.Columns),
            project.Normalization.CanvasWidth,
            project.Normalization.CanvasHeight,
            NormalizeNonNegativeInt(_viewModel.SheetPadding, project.Sheet.Padding),
            NormalizeNonNegativeInt(_viewModel.SheetSpacing, project.Sheet.Spacing),
            _viewModel.PowerOfTwoSheet);
        _viewModel.SheetCellWidth = project.Sheet.CellWidth;
        _viewModel.SheetCellHeight = project.Sheet.CellHeight;
        _viewModel.SheetColumns = project.Sheet.Columns ?? 0;
        _viewModel.SheetPadding = project.Sheet.Padding;
        _viewModel.SheetSpacing = project.Sheet.Spacing;

        string? previewPath = null;
        var fingerprint = _services.Fingerprints.Compute("build_sheet", new
        {
            project.Sheet,
            project.Loop,
            frames = EnabledFrameHashes(preferNormalized: true)
        });
        await RunStageAsync(
            PipelineStage.Sheet,
            JobStage.BuildSheet,
            fingerprint,
            async (progress, token) =>
            {
                progress.Report(new PipelineProgress(0.1, "Composing sprite sheet"));
                previewPath = await _services.Pipeline.BuildSheetPreviewAsync(project, CurrentWorkspace, token);
                progress.Report(new PipelineProgress(1, "Sprite sheet preview ready"));
                return [];
            });

        if (_viewModel.GetStage(PipelineStage.Sheet).State == StageState.Complete && previewPath is not null)
        {
            _viewModel.PreviewImagePath = previewPath;
            var sequence = _services.Pipeline.GetExportSequence(project);
            var columns = project.Sheet.Columns ?? (int)Math.Ceiling(Math.Sqrt(sequence.Count));
            var rows = sequence.Count == 0 ? 0 : (int)Math.Ceiling(sequence.Count / (double)Math.Max(1, columns));
            _viewModel.SheetSummary = $"{sequence.Count} frames · {columns} × {rows} grid";
            await SaveAsync();
            _viewModel.NavigateTo(PipelineStage.Export);
        }
    }

    public async Task ChooseExportDestinationAsync()
    {
        var path = await _pickers.PickExportFolderAsync();
        if (!string.IsNullOrWhiteSpace(path)) _viewModel.ExportDestination = path;
    }

    public async Task ExportAsync()
    {
        var project = CurrentProject;
        if (string.IsNullOrWhiteSpace(_viewModel.ExportDestination))
            _viewModel.ExportDestination = CurrentWorkspace.Exports;

        var destinationCheck = await _services.WorkspaceDiagnostics.CheckWriteAccessAsync(_viewModel.ExportDestination);
        if (!destinationCheck.Passed)
        {
            ShowError(new IOException(destinationCheck.Detail ?? "The export destination is not writable."));
            return;
        }

        var fingerprint = ComputeExportFingerprint(_viewModel.ExportIndividualFrames);
        ExportPipelineResult? result = null;
        await RunStageAsync(
            PipelineStage.Export,
            JobStage.Export,
            fingerprint,
            async (progress, token) =>
            {
                progress.Report(new PipelineProgress(0.05, "Exporting sprite assets"));
                result = await _services.Pipeline.ExportAsync(
                    project,
                    CurrentWorkspace,
                    new ExportPipelineRequest(
                        _viewModel.ExportName,
                        _viewModel.ExportDestination,
                        _viewModel.ExportIndividualFrames,
                        fingerprint),
                    token);
                progress.Report(new PipelineProgress(1, "Export complete"));
                return [result.Record.SheetArtifactId, result.Record.MetadataArtifactId];
            });

        if (result is not null && _viewModel.GetStage(PipelineStage.Export).State == StageState.Complete)
        {
            await SaveAsync();
            _viewModel.ExportSummary = _viewModel.ExportIndividualFrames
                ? $"Exported sheet, metadata, and {result.IndividualFramePaths.Count} frames"
                : "Exported sprite sheet and metadata";
            _viewModel.JobStatus.Message = _viewModel.ExportSummary;
        }
    }
}

using SpriteForge.Core.Enums;
using SpriteForge.Core.Models;

namespace SpriteForge.App.Workflow;

internal sealed partial class DesktopWorkflowController
{
    public async Task ExtractFramesAsync()
    {
        var project = CurrentProject;
        var source = ResolveSourceArtifact();
        if (source.Kind is not (ArtifactKind.SourceVideo or ArtifactKind.GeneratedVideo))
        {
            ShowError(new InvalidOperationException("Frame extraction requires a video source."));
            return;
        }

        project.Extraction = new ExtractionSettings(
            Math.Max(0.01, _viewModel.PreviewFps),
            Math.Max(0, _viewModel.ExtractionStartSeconds),
            _viewModel.ExtractionHasEndTime ? Math.Max(0, _viewModel.ExtractionEndSeconds) : null);
        var sourcePath = CurrentWorkspace.ResolveRelative(source.RelativePath);
        var fingerprint = _services.Fingerprints.Compute("extract_frames", new { source.Sha256, project.Extraction });

        await RunStageAsync(
            PipelineStage.Frames,
            JobStage.ExtractFrames,
            fingerprint,
            async (progress, token) =>
            {
                await _services.Pipeline.ExtractFramesAsync(project, CurrentWorkspace, sourcePath, progress, token);
                return project.Frames.Select(frame => frame.Artifacts.Extracted).OfType<Guid>().ToArray();
            });

        if (_viewModel.GetStage(PipelineStage.Frames).State == StageState.Complete)
        {
            RefreshViewModel();
            MarkDownstreamStale(PipelineStage.Frames);
            _viewModel.NavigateTo(PipelineStage.Cutout);
        }
    }

    public async Task RemoveBackgroundsAsync()
    {
        var project = CurrentProject;
        project.BackgroundRemoval = project.BackgroundRemoval with
        {
            Enabled = _viewModel.BackgroundRemovalEnabled,
            AlphaThreshold = Math.Clamp(_viewModel.AlphaThreshold, 0, 1)
        };

        if (!project.BackgroundRemoval.Enabled)
        {
            _viewModel.SetStageState(PipelineStage.Cutout, StageState.Skipped);
            MarkDownstreamStale(PipelineStage.Cutout);
            await SaveAsync();
            _viewModel.NavigateTo(PipelineStage.Align);
            return;
        }

        var fingerprint = _services.Fingerprints.Compute("remove_background", new
        {
            project.BackgroundRemoval,
            frames = EnabledFrameHashes(preferTransparent: false)
        });
        await RunStageAsync(
            PipelineStage.Cutout,
            JobStage.RemoveBackground,
            fingerprint,
            async (progress, token) =>
            {
                await _services.Pipeline.RemoveBackgroundsAsync(project, CurrentWorkspace, progress, token);
                return project.Frames.Select(frame => frame.Artifacts.Transparent).OfType<Guid>().ToArray();
            });

        if (_viewModel.GetStage(PipelineStage.Cutout).State == StageState.Complete)
        {
            RefreshViewModel();
            MarkDownstreamStale(PipelineStage.Cutout);
            _viewModel.NavigateTo(PipelineStage.Align);
        }
    }

    public async Task NormalizeFramesAsync()
    {
        var project = CurrentProject;
        project.Normalization = new NormalizationSettings(
            Math.Max(1, (int)Math.Round(_viewModel.CanvasWidth)),
            Math.Max(1, (int)Math.Round(_viewModel.CanvasHeight)),
            NormalizeFit(_viewModel.FitMode),
            NormalizeAnchor(_viewModel.Anchor),
            _viewModel.AutoTrim);
        project.Sheet = project.Sheet with
        {
            CellWidth = project.Normalization.CanvasWidth,
            CellHeight = project.Normalization.CanvasHeight
        };

        var fingerprint = _services.Fingerprints.Compute("normalize_frames", new
        {
            project.Normalization,
            frames = EnabledFrameHashes(preferTransparent: project.BackgroundRemoval.Enabled),
            transforms = project.Frames.Where(frame => frame.Enabled).OrderBy(frame => frame.Order).Select(frame => frame.Transform)
        });
        await RunStageAsync(
            PipelineStage.Align,
            JobStage.NormalizeFrames,
            fingerprint,
            async (progress, token) =>
            {
                await _services.Pipeline.NormalizeFramesAsync(project, CurrentWorkspace, progress, token);
                return project.Frames.Select(frame => frame.Artifacts.Normalized).OfType<Guid>().ToArray();
            });

        if (_viewModel.GetStage(PipelineStage.Align).State == StageState.Complete)
        {
            RefreshViewModel();
            MarkDownstreamStale(PipelineStage.Align);
            _viewModel.NavigateTo(PipelineStage.Loop);
        }
    }

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
        if (candidates.Count > 0)
        {
            ApplyLoopCandidate(candidates[0], recommended: true);
            await SaveAsync();
        }
        RefreshViewModel();
        MarkDownstreamStale(PipelineStage.Loop);
        _viewModel.SetStageState(PipelineStage.Loop, StageState.Complete);
        _viewModel.NavigateTo(PipelineStage.Sheet);
    }

    public async Task ApplyManualLoopAsync()
    {
        var enabled = CurrentProject.Frames.Where(frame => frame.Enabled).OrderBy(frame => frame.Order).ToArray();
        if (enabled.Length == 0) return;
        var start = Math.Clamp((int)Math.Round(_viewModel.LoopStart) - 1, 0, enabled.Length - 1);
        var end = Math.Clamp((int)Math.Round(_viewModel.LoopEnd) - 1, start, enabled.Length - 1);
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
            _viewModel.SheetColumns <= 0 ? null : Math.Max(1, (int)Math.Round(_viewModel.SheetColumns)),
            Math.Max(1, (int)Math.Round(_viewModel.SheetCellWidth)),
            Math.Max(1, (int)Math.Round(_viewModel.SheetCellHeight)),
            Math.Max(0, (int)Math.Round(_viewModel.SheetPadding)),
            Math.Max(0, (int)Math.Round(_viewModel.SheetSpacing)),
            _viewModel.PowerOfTwoSheet);

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

    public void CancelCurrentOperation() => _operationCts?.Cancel();

    private async Task RunStageAsync(
        PipelineStage pipelineStage,
        JobStage jobStage,
        string fingerprint,
        Func<IProgress<PipelineProgress>, CancellationToken, Task<IReadOnlyList<Guid>>> work)
    {
        if (_operationCts is not null)
        {
            _viewModel.JobStatus.Message = "Another operation is already running";
            return;
        }

        _operationCts = new CancellationTokenSource();
        _viewModel.JobStatus.IsRunning = true;
        _viewModel.JobStatus.CanCancel = true;
        _viewModel.JobStatus.Progress = 0;
        _viewModel.SetStageState(pipelineStage, StageState.Active);
        var progress = new Progress<PipelineProgress>(update =>
        {
            _viewModel.JobStatus.Progress = Math.Clamp(update.Value, 0, 1);
            _viewModel.JobStatus.Message = update.Total is > 0 && update.Current is > 0
                ? $"{update.Message} ({update.Current}/{update.Total})"
                : update.Message;
        });

        try
        {
            await _services.Jobs.RunAsync(
                jobStage,
                fingerprint,
                work,
                progress,
                _operationCts.Token,
                job => _services.JobLogs.WriteAsync(CurrentWorkspace.Logs, job));
            _viewModel.SetStageState(pipelineStage, StageState.Complete);
            MarkDownstreamStale(pipelineStage);
            await SaveAsync();
        }
        catch (OperationCanceledException)
        {
            _viewModel.JobStatus.Message = "Operation cancelled";
            _viewModel.SetStageState(pipelineStage, StageState.Stale);
        }
        catch (Exception ex)
        {
            _viewModel.SetStageState(pipelineStage, StageState.Error);
            ShowError(ex);
        }
        finally
        {
            _viewModel.JobStatus.IsRunning = false;
            _viewModel.JobStatus.CanCancel = false;
            _operationCts.Dispose();
            _operationCts = null;
        }
    }


}

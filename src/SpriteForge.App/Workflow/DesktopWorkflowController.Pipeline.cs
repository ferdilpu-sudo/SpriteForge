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

        var fps = NormalizeFinite(_viewModel.PreviewFps, project.Extraction.Fps, 0.01, 120);
        var startSeconds = NormalizeFinite(
            _viewModel.ExtractionStartSeconds,
            project.Extraction.StartSeconds,
            0);
        double? endSeconds = null;
        if (_viewModel.ExtractionHasEndTime)
        {
            var fallbackEnd = project.Extraction.EndSeconds ?? Math.Max(startSeconds + 3, 3);
            endSeconds = NormalizeFinite(_viewModel.ExtractionEndSeconds, fallbackEnd, 0);
            if (endSeconds <= startSeconds)
            {
                ShowError(new ArgumentException("End time must be greater than start time."));
                return;
            }
        }

        project.Extraction = new ExtractionSettings(fps, startSeconds, endSeconds);
        project.FrameOptimization = new FrameOptimizationSettings(
            NormalizeFrameOptimizationMode(_viewModel.FrameOptimizationMode, project.FrameOptimization.Mode),
            NormalizeFinite(
                _viewModel.FrameSimilarityThreshold,
                project.FrameOptimization.SimilarityThreshold,
                0,
                1),
            _viewModel.PreserveMotionPeaks);

        _viewModel.PreviewFps = fps;
        _viewModel.ExtractionStartSeconds = startSeconds;
        _viewModel.FrameOptimizationMode = project.FrameOptimization.Mode;
        _viewModel.FrameSimilarityThreshold = project.FrameOptimization.SimilarityThreshold;
        if (endSeconds is { } end) _viewModel.ExtractionEndSeconds = end;

        var sourcePath = CurrentWorkspace.ResolveRelative(source.RelativePath);
        var fingerprint = _services.Fingerprints.Compute("extract_frames", new
        {
            source.Sha256,
            project.Extraction,
            project.FrameOptimization
        });

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
            AlphaThreshold = NormalizeFinite(
                _viewModel.AlphaThreshold,
                project.BackgroundRemoval.AlphaThreshold,
                0,
                1)
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

        try
        {
            project.Normalization = new NormalizationSettings(
                NormalizeDimension(_viewModel.CanvasWidth, project.Normalization.CanvasWidth),
                NormalizeDimension(_viewModel.CanvasHeight, project.Normalization.CanvasHeight),
                NormalizeFit(_viewModel.FitMode, project.Normalization.Fit),
                NormalizeAnchor(_viewModel.Anchor, project.Normalization.Anchor),
                _viewModel.AutoTrim);

            project.Sheet = project.Sheet with
            {
                CellWidth = project.Normalization.CanvasWidth,
                CellHeight = project.Normalization.CanvasHeight
            };

            _viewModel.CanvasWidth = project.Normalization.CanvasWidth;
            _viewModel.CanvasHeight = project.Normalization.CanvasHeight;
            _viewModel.FitMode = project.Normalization.Fit;
            _viewModel.Anchor = project.Normalization.Anchor;
        }
        catch (Exception ex)
        {
            _viewModel.SetStageState(PipelineStage.Align, StageState.Error);
            ShowError(ex);
            return;
        }

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

}

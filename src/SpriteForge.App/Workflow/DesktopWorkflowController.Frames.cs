using SpriteForge.Core.Enums;
using SpriteForge.Core.Models;
using SpriteForge.Presentation.Frames;

namespace SpriteForge.App.Workflow;

internal sealed partial class DesktopWorkflowController
{
    public async Task ToggleSelectedFrameAsync()
    {
        var selected = _viewModel.SelectedFrame;
        if (selected is null) return;
        var index = CurrentProject.Frames.FindIndex(frame => frame.Id == selected.FrameId);
        if (index < 0) return;
        var frame = CurrentProject.Frames[index];
        CurrentProject.Frames[index] = frame with { Enabled = !frame.Enabled };
        InvalidateLoopAfterFrameEdit();
        await SaveAsync();
        RefreshViewModel();
        _viewModel.SetStageState(PipelineStage.Loop, StageState.Stale);
        MarkDownstreamStale(PipelineStage.Loop);
    }

    public Task MoveSelectedFrameLeftAsync() => MoveSelectedFrameAsync(-1);
    public Task MoveSelectedFrameRightAsync() => MoveSelectedFrameAsync(1);

    public async Task DuplicateSelectedFrameAsync()
    {
        var selected = _viewModel.SelectedFrame;
        if (selected is null) return;
        var ordered = CurrentProject.Frames.OrderBy(frame => frame.Order).ToList();
        var position = ordered.FindIndex(frame => frame.Id == selected.FrameId);
        if (position < 0) return;
        var original = ordered[position];
        ordered.Insert(position + 1, original with { Id = Guid.NewGuid() });
        ReassignOrders(ordered);
        InvalidateLoopAfterFrameEdit();
        await SaveAsync();
        RefreshViewModel(original.Id, position + 1);
        _viewModel.SetStageState(PipelineStage.Loop, StageState.Stale);
        MarkDownstreamStale(PipelineStage.Loop);
    }

    public async Task RemoveSelectedFrameAsync()
    {
        var selected = _viewModel.SelectedFrame;
        if (selected is null || CurrentProject.Frames.Count <= 1) return;
        CurrentProject.Frames.RemoveAll(frame => frame.Id == selected.FrameId);
        ReassignOrders(CurrentProject.Frames.OrderBy(frame => frame.Order).ToList());
        InvalidateLoopAfterFrameEdit();
        await SaveAsync();
        RefreshViewModel();
        _viewModel.SetStageState(PipelineStage.Loop, StageState.Stale);
        MarkDownstreamStale(PipelineStage.Loop);
    }

    public async Task ResetFrameSequenceAsync()
    {
        var baseArtifacts = GetBaseFrameArtifacts();
        if (baseArtifacts.Count == 0) return;

        var existing = CurrentProject.Frames
            .GroupBy(frame => frame.SourceIndex)
            .ToDictionary(group => group.Key, group => group.OrderBy(frame => frame.Order).First());
        var restored = new List<FrameRecord>(baseArtifacts.Count);
        for (var index = 0; index < baseArtifacts.Count; index++)
        {
            var artifact = baseArtifacts[index];
            if (existing.TryGetValue(index, out var frame))
            {
                restored.Add(frame with
                {
                    Order = index,
                    Enabled = true,
                    Artifacts = frame.Artifacts with { Extracted = artifact.Id }
                });
                continue;
            }

            restored.Add(new FrameRecord(
                Guid.NewGuid(),
                index,
                index,
                true,
                1000d / Math.Max(0.01, CurrentProject.Extraction.Fps),
                new FrameArtifactLinks(artifact.Id, null, null, null),
                new FrameTransform(0, 0, 1),
                new FramePivot(0.5, 1)));
        }

        CurrentProject.Frames.Clear();
        CurrentProject.Frames.AddRange(restored);
        InvalidateLoopAfterFrameEdit();
        await SaveAsync();
        RefreshViewModel();
        MarkDownstreamStale(PipelineStage.Frames);
    }

    public async Task SetSelectedFrameDurationAsync()
    {
        var selected = _viewModel.SelectedFrame;
        if (selected is null) return;
        var durationMs = _viewModel.SelectedFrameDurationMs;
        if (!double.IsFinite(durationMs) || durationMs <= 0)
        {
            ShowError(new ArgumentOutOfRangeException(nameof(durationMs), "Frame duration must be greater than zero."));
            return;
        }

        var index = CurrentProject.Frames.FindIndex(frame => frame.Id == selected.FrameId);
        if (index < 0) return;
        CurrentProject.Frames[index] = CurrentProject.Frames[index] with { DurationMs = durationMs };
        await SaveAsync();
        RefreshViewModel(selected.FrameId);
        MarkDownstreamStale(PipelineStage.Loop);
    }

    public void SelectFrame(FrameThumbnailViewModel? frame)
    {
        if (frame is null) return;
        var index = _viewModel.Frames.IndexOf(frame);
        if (index < 0) return;
        _viewModel.CurrentFrameIndex = index;
        _viewModel.SelectedFrame = frame;
        _viewModel.PreviewImagePath = frame.PreviewPath;
        var record = CurrentProject.Frames.FirstOrDefault(candidate => candidate.Id == frame.FrameId);
        if (record is not null)
        {
            _viewModel.SelectedFrameDurationMs = record.DurationMs;
            _viewModel.SelectedFramePivotX = record.Pivot.X;
            _viewModel.SelectedFramePivotY = record.Pivot.Y;
        }
    }

    public async Task SetSelectedFramePivotAsync()
    {
        var selected = _viewModel.SelectedFrame;
        if (selected is null) return;
        var pivotX = _viewModel.SelectedFramePivotX;
        var pivotY = _viewModel.SelectedFramePivotY;
        if (!double.IsFinite(pivotX) || !double.IsFinite(pivotY) || pivotX < 0 || pivotX > 1 || pivotY < 0 || pivotY > 1)
        {
            ShowError(new ArgumentOutOfRangeException(nameof(pivotX), "Pivot coordinates must be between 0 and 1."));
            return;
        }

        var index = CurrentProject.Frames.FindIndex(frame => frame.Id == selected.FrameId);
        if (index < 0) return;
        CurrentProject.Frames[index] = CurrentProject.Frames[index] with { Pivot = new FramePivot(pivotX, pivotY) };
        await SaveAsync();
        RefreshViewModel(selected.FrameId);
        MarkDownstreamStale(PipelineStage.Loop);
    }

    public void SelectFrameAt(int index)
    {
        if (_viewModel.Frames.Count == 0) return;
        var safeIndex = Math.Clamp(index, 0, _viewModel.Frames.Count - 1);
        SelectFrame(_viewModel.Frames[safeIndex]);
    }

    public bool AdvancePreview(bool loopEnabled)
    {
        if (_project is null || _viewModel.Frames.Count == 0) return false;
        IReadOnlyList<FrameRecord> sequence;
        try
        {
            sequence = _services.Pipeline.GetExportSequence(CurrentProject);
        }
        catch
        {
            sequence = CurrentProject.Frames.Where(frame => frame.Enabled).OrderBy(frame => frame.Order).ToArray();
        }
        if (sequence.Count == 0) return false;

        var currentId = _viewModel.SelectedFrame?.FrameId;
        var current = currentId is null ? -1 : sequence.ToList().FindIndex(frame => frame.Id == currentId.Value);
        var next = current + 1;
        if (next >= sequence.Count)
        {
            if (!loopEnabled) return false;
            next = 0;
        }

        var nextViewModel = _viewModel.Frames.FirstOrDefault(frame => frame.FrameId == sequence[next].Id);
        SelectFrame(nextViewModel);
        return nextViewModel is not null;
    }

    private async Task MoveSelectedFrameAsync(int delta)
    {
        var selected = _viewModel.SelectedFrame;
        if (selected is null) return;
        var ordered = CurrentProject.Frames.OrderBy(frame => frame.Order).ToList();
        var index = ordered.FindIndex(frame => frame.Id == selected.FrameId);
        var target = index + delta;
        if (index < 0 || target < 0 || target >= ordered.Count) return;
        (ordered[index], ordered[target]) = (ordered[target], ordered[index]);
        ReassignOrders(ordered);
        InvalidateLoopAfterFrameEdit();
        await SaveAsync();
        RefreshViewModel(selected.FrameId, target);
        _viewModel.SetStageState(PipelineStage.Loop, StageState.Stale);
        MarkDownstreamStale(PipelineStage.Loop);
    }

    private void ReassignOrders(IReadOnlyList<FrameRecord> ordered)
    {
        CurrentProject.Frames.Clear();
        for (var index = 0; index < ordered.Count; index++)
            CurrentProject.Frames.Add(ordered[index] with { Order = index });
    }

    private IReadOnlyList<ArtifactRecord> GetBaseFrameArtifacts()
    {
        var project = CurrentProject;
        if (string.Equals(project.Source?.Kind, "frame_sequence", StringComparison.Ordinal))
        {
            var sourceArtifact = project.Source is null
                ? null
                : project.Artifacts.FirstOrDefault(artifact => artifact.Id == project.Source.ArtifactId);
            var sourceDirectory = sourceArtifact is null ? null : Path.GetDirectoryName(sourceArtifact.RelativePath);
            if (!string.IsNullOrWhiteSpace(sourceDirectory))
            {
                return project.Artifacts
                    .Where(artifact => artifact.Kind == ArtifactKind.SourceImage &&
                        string.Equals(Path.GetDirectoryName(artifact.RelativePath), sourceDirectory, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(artifact => artifact.RelativePath, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            }
        }

        return project.Artifacts
            .Where(artifact => artifact.Kind == ArtifactKind.ExtractedFrame)
            .OrderBy(artifact => artifact.RelativePath, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private void InvalidateLoopAfterFrameEdit()
    {
        CurrentProject.Loop = CurrentProject.Loop with { StartFrameId = null, EndFrameId = null, Recommended = false };
        _viewModel.SetStageState(PipelineStage.Loop, StageState.Stale);
        MarkDownstreamStale(PipelineStage.Loop);
    }


}

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
        if (frame.Enabled &&
            CurrentProject.Frames.Count(candidate => candidate.Enabled) <= 1)
        {
            ShowError(new InvalidOperationException("At least one frame must remain enabled."));
            return;
        }

        var targetDuration = _services.FrameTiming.GetEnabledTotalDurationMs(CurrentProject);
        CurrentProject.Frames[index] = frame with { Enabled = !frame.Enabled };
        _services.FrameTiming.PreserveEnabledTotalDuration(
            CurrentProject,
            targetDuration);
        InvalidateLoopAfterFrameEdit();
        await SaveAsync();
        RefreshViewModel();
        MarkDownstreamStale(PipelineStage.Frames);
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
        var targetDuration = _services.FrameTiming.GetEnabledTotalDurationMs(CurrentProject);
        ordered.Insert(position + 1, original with { Id = Guid.NewGuid() });
        ReassignOrders(ordered);
        _services.FrameTiming.PreserveEnabledTotalDuration(
            CurrentProject,
            targetDuration);
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

        var record = CurrentProject.Frames.FirstOrDefault(frame => frame.Id == selected.FrameId);
        if (record is null) return;
        if (record.Enabled &&
            CurrentProject.Frames.Count(frame => frame.Enabled) <= 1)
        {
            ShowError(new InvalidOperationException("At least one frame must remain enabled."));
            return;
        }

        var targetDuration = _services.FrameTiming.GetEnabledTotalDurationMs(CurrentProject);
        CurrentProject.Frames.RemoveAll(frame => frame.Id == selected.FrameId);
        ReassignOrders(CurrentProject.Frames.OrderBy(frame => frame.Order).ToList());
        _services.FrameTiming.PreserveEnabledTotalDuration(
            CurrentProject,
            targetDuration);
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
                    DurationMs = 1000d / Math.Max(0.01, CurrentProject.Extraction.Fps),
                    Artifacts = new FrameArtifactLinks(artifact.Id, null, null, null)
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
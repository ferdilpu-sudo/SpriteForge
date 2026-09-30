using SpriteForge.Presentation.Frames;

namespace SpriteForge.App.Workflow;

internal sealed partial class DesktopWorkflowController
{
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

    public IReadOnlyList<string> GetPreviewFramePaths()
    {
        if (_project is null || _viewModel.Frames.Count == 0)
            return [];

        IReadOnlyList<FrameRecord> sequence;
        try
        {
            sequence = _services.Pipeline.GetExportSequence(CurrentProject);
        }
        catch
        {
            return [];
        }

        return sequence
            .Select(frame => _viewModel.Frames.FirstOrDefault(
                viewModel => viewModel.FrameId == frame.Id)?.PreviewPath)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public bool AdvancePreview(bool loopEnabled)
    {
        if (_project is null || _viewModel.Frames.Count == 0) return false;
        IReadOnlyList<FrameRecord> sequence;
        try
        {
            sequence = _services.Pipeline.GetExportSequence(CurrentProject);
        }
        catch (Exception ex)
        {
            ShowError(ex);
            return false;
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
}

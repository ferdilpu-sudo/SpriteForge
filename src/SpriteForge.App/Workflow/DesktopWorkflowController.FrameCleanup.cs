using SpriteForge.Core.Enums;

namespace SpriteForge.App.Workflow;

internal sealed partial class DesktopWorkflowController
{
    public int GetDisabledFrameCount() =>
        CurrentProject.Frames.Count(frame => !frame.Enabled);

    public async Task DeleteDisabledFramesAsync()
    {
        var result = _services.FramePruning.DeleteDisabledFrames(
            CurrentProject,
            CurrentWorkspace);

        if (result.RemovedFrames == 0)
        {
            _viewModel.JobStatus.Message = "No disabled frames to remove";
            return;
        }

        await SaveAsync();
        RefreshViewModel();
        MarkDownstreamStale(PipelineStage.Frames);
        _viewModel.JobStatus.Message =
            $"Removed {result.RemovedFrames} unused frames · {CurrentProject.Frames.Count} keyframes remain";
    }
}

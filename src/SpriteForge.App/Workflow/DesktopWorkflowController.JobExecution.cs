using SpriteForge.Core.Enums;
using SpriteForge.Core.Models;

namespace SpriteForge.App.Workflow;

internal sealed partial class DesktopWorkflowController
{
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
        var logsDirectory = CurrentWorkspace.Logs;
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
                job => _services.JobLogs.WriteAsync(logsDirectory, job));
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

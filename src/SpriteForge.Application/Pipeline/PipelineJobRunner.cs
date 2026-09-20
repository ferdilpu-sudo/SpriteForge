using SpriteForge.Core.Enums;
using SpriteForge.Core.Errors;
using SpriteForge.Core.Models;

namespace SpriteForge.Application.Pipeline;

public sealed class PipelineJobRunner
{
    public async Task<JobRecord> RunAsync(
        JobStage stage,
        string inputFingerprint,
        Func<IProgress<PipelineProgress>, CancellationToken, Task<IReadOnlyList<Guid>>> work,
        IProgress<PipelineProgress>? outerProgress,
        CancellationToken cancellationToken,
        Func<JobRecord, Task>? onCompleted = null)
    {
        var job = new JobRecord
        {
            Stage = stage,
            Status = JobStatus.Running,
            StartedAt = DateTimeOffset.UtcNow,
            InputFingerprint = inputFingerprint
        };

        var progress = new Progress<PipelineProgress>(update =>
        {
            job.Progress = Math.Clamp(update.Value, 0, 1);
            outerProgress?.Report(update);
        });

        try
        {
            var outputs = await work(progress, cancellationToken).ConfigureAwait(false);
            job.Outputs.AddRange(outputs);
            job.Progress = 1;
            job.Status = JobStatus.Completed;
            return job;
        }
        catch (OperationCanceledException)
        {
            job.Status = JobStatus.Cancelled;
            throw;
        }
        catch (SpriteForgeException ex)
        {
            job.Status = JobStatus.Failed;
            job.Error = new PipelineError(ex.Code, ex.Message, ex.InnerException?.Message, ex.Recoverable);
            throw;
        }
        catch (Exception ex)
        {
            job.Status = JobStatus.Failed;
            job.Error = new PipelineError("PIPELINE_STAGE_FAILED", "The processing stage failed.", ex.Message, true);
            throw;
        }
        finally
        {
            job.CompletedAt = DateTimeOffset.UtcNow;
            if (onCompleted is not null)
            {
                try
                {
                    await onCompleted(job).ConfigureAwait(false);
                }
                catch
                {
                    // Logging/telemetry failure must not replace the stage result.
                }
            }
        }
    }
}

using System.Text.Json;
using SpriteForge.Core.Enums;
using SpriteForge.Core.Models;

namespace SpriteForge.Diagnostics.Logging;

public sealed class JobLogWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public async Task WriteAsync(string logsDirectory, JobRecord job, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(logsDirectory);
        var logPath = Path.Combine(logsDirectory, $"job-{job.Id:N}.jsonl");
        var payload = new
        {
            timestamp = DateTimeOffset.UtcNow,
            jobId = job.Id,
            stage = job.Stage.ToString(),
            severity = job.Status == JobStatus.Failed ? "error" : "info",
            status = job.Status.ToString(),
            progress = job.Progress,
            startedAt = job.StartedAt,
            completedAt = job.CompletedAt,
            inputFingerprint = job.InputFingerprint,
            outputs = job.Outputs,
            errorCode = job.Error?.Code,
            message = job.Error?.Message,
            technicalDetail = job.Error?.TechnicalDetail
        };
        var line = JsonSerializer.Serialize(payload, JsonOptions) + Environment.NewLine;
        await File.AppendAllTextAsync(logPath, line, cancellationToken).ConfigureAwait(false);
    }
}

using SpriteForge.Core.Enums;

namespace SpriteForge.Core.Models;

public sealed record PipelineError(
    string Code,
    string Message,
    string? TechnicalDetail,
    bool Recoverable);

public sealed class JobRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public JobStage Stage { get; init; }
    public JobStatus Status { get; set; } = JobStatus.Pending;
    public double Progress { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public string InputFingerprint { get; init; } = string.Empty;
    public List<Guid> Outputs { get; init; } = [];
    public PipelineError? Error { get; set; }
}

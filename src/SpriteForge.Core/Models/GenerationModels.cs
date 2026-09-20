namespace SpriteForge.Core.Models;

public sealed record GenerationRequest(
    string SourceImagePath,
    string Prompt,
    double DurationSeconds,
    string AspectRatio);

public sealed record GenerationJob(string ProviderId, string ProviderJobId, string Status);

public sealed record GenerationJobStatus(string ProviderJobId, string Status, double? Progress, string? Message);

public sealed record VideoArtifact(string LocalPath, string MimeType, double? DurationSeconds);

public sealed record GenerationSettings(double DurationSeconds, string AspectRatio);

public sealed record GenerationRecord(
    string ProviderId,
    string ProviderJobId,
    string Prompt,
    string Status,
    DateTimeOffset SubmittedAt,
    DateTimeOffset? CompletedAt,
    Guid? OutputArtifactId,
    GenerationSettings Settings);

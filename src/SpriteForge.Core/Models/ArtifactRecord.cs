using SpriteForge.Core.Enums;

namespace SpriteForge.Core.Models;

public sealed record ArtifactRecord(
    Guid Id,
    ArtifactKind Kind,
    string RelativePath,
    string MimeType,
    int? Width,
    int? Height,
    double? DurationSeconds,
    string Sha256,
    DateTimeOffset CreatedAt);

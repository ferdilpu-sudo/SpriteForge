namespace SpriteForge.Core.Models;

public sealed record FrameArtifactLinks(
    Guid? Extracted,
    Guid? Transparent,
    Guid? Normalized,
    Guid? Thumbnail);

public sealed record FrameTransform(double OffsetX, double OffsetY, double Scale);

public sealed record FramePivot(double X, double Y);

public sealed record FrameRecord(
    Guid Id,
    int SourceIndex,
    int Order,
    bool Enabled,
    double DurationMs,
    FrameArtifactLinks Artifacts,
    FrameTransform Transform,
    FramePivot Pivot);

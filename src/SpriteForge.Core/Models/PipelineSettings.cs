namespace SpriteForge.Core.Models;

public sealed record ExtractionSettings(double Fps, double StartSeconds, double? EndSeconds);

public sealed record BackgroundRemovalSettings(bool Enabled, string ProcessorId, double AlphaThreshold);

public sealed record NormalizationSettings(
    int CanvasWidth,
    int CanvasHeight,
    string Fit,
    string Anchor,
    bool AutoTrim);

public sealed record LoopSettings(
    bool Enabled,
    Guid? StartFrameId,
    Guid? EndFrameId,
    bool Recommended);

public sealed record SheetSettings(
    int? Columns,
    int CellWidth,
    int CellHeight,
    int Padding,
    int Spacing,
    bool PowerOfTwo);

namespace SpriteForge.Core.Models;

public sealed record FrameOptimizationSettings(
    string Mode,
    double SimilarityThreshold,
    bool PreserveMotionPeaks)
{
    public static FrameOptimizationSettings BalancedDefault => new("balanced", 0.96, true);
}

public sealed record FrameOptimizationRequest(
    IReadOnlyList<string> FramePaths,
    double SourceFrameDurationMs,
    FrameOptimizationSettings Settings);

public sealed record FrameOptimizationDecision(
    int SourceIndex,
    bool Enabled,
    double DurationMs,
    double MotionScore);

public sealed record FrameOptimizationResult(
    IReadOnlyList<FrameOptimizationDecision> Decisions)
{
    public int SourceCount => Decisions.Count;
    public int EnabledCount => Decisions.Count(decision => decision.Enabled);
}

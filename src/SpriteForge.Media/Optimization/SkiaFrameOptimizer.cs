using SpriteForge.Core.Contracts;
using SpriteForge.Core.Models;

namespace SpriteForge.Media.Optimization;

public sealed class SkiaFrameOptimizer : IFrameOptimizer
{
    private readonly FrameDifferenceAnalyzer _differenceAnalyzer = new();

    public async Task<FrameOptimizationResult> OptimizeAsync(
        FrameOptimizationRequest request,
        IProgress<PipelineProgress>? progress,
        CancellationToken cancellationToken)
    {
        Validate(request);
        var mode = request.Settings.Mode.Trim().ToLowerInvariant();
        var frameCount = request.FramePaths.Count;
        if (mode == "raw" || frameCount <= 2)
            return BuildResult(Enumerable.Range(0, frameCount).ToHashSet(), new double[frameCount], request.SourceFrameDurationMs, frameCount);

        var differences = await AnalyzeDifferencesAsync(request.FramePaths, progress, cancellationToken).ConfigureAwait(false);
        var motionScores = BuildMotionScores(differences, request.Settings.PreserveMotionPeaks);
        var (minimum, maximum) = GetTargetRange(mode, frameCount);
        var candidates = FindMeaningfulCandidates(differences, 1d - request.Settings.SimilarityThreshold, frameCount);
        var selected = candidates.Count > maximum
            ? SelectByImportance(candidates, motionScores, maximum, frameCount)
            : candidates.ToHashSet();

        if (selected.Count < minimum)
            AddByImportance(selected, Enumerable.Range(0, frameCount), motionScores, minimum, frameCount);

        selected.Add(0);
        selected.Add(frameCount - 1);
        progress?.Report(new PipelineProgress(1, $"Optimized {frameCount} frames to {selected.Count} keyframes", selected.Count, frameCount));
        return BuildResult(selected, motionScores, request.SourceFrameDurationMs, frameCount);
    }

    private async Task<double[]> AnalyzeDifferencesAsync(
        IReadOnlyList<string> framePaths,
        IProgress<PipelineProgress>? progress,
        CancellationToken cancellationToken)
    {
        var differences = new double[framePaths.Count];
        for (var index = 1; index < framePaths.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            differences[index] = await _differenceAnalyzer.DifferenceAsync(framePaths[index - 1], framePaths[index], cancellationToken).ConfigureAwait(false);
            progress?.Report(new PipelineProgress(index / (double)Math.Max(1, framePaths.Count - 1), "Analyzing frame similarity", index, framePaths.Count - 1));
        }
        return differences;
    }

    private static double[] BuildMotionScores(IReadOnlyList<double> differences, bool preserveMotionPeaks)
    {
        var scores = new double[differences.Count];
        for (var index = 0; index < scores.Length; index++)
        {
            var previous = index == 0 ? 0 : differences[index];
            var next = index + 1 < differences.Count ? differences[index + 1] : 0;
            scores[index] = preserveMotionPeaks ? Math.Max(previous, next) : previous;
        }
        return scores;
    }

    private static IReadOnlyList<int> FindMeaningfulCandidates(IReadOnlyList<double> differences, double threshold, int frameCount)
    {
        var candidates = new List<int> { 0 };
        for (var index = 1; index < frameCount - 1; index++)
        {
            if (differences[index] > threshold || differences[index + 1] > threshold)
                candidates.Add(index);
        }
        candidates.Add(frameCount - 1);
        return candidates;
    }

    private static HashSet<int> SelectByImportance(
        IEnumerable<int> pool,
        IReadOnlyList<double> motionScores,
        int targetCount,
        int frameCount)
    {
        var selected = new HashSet<int> { 0, frameCount - 1 };
        AddByImportance(selected, pool, motionScores, targetCount, frameCount);
        return selected;
    }

    private static void AddByImportance(
        HashSet<int> selected,
        IEnumerable<int> pool,
        IReadOnlyList<double> motionScores,
        int targetCount,
        int frameCount)
    {
        var available = pool.Distinct().Where(index => !selected.Contains(index)).ToArray();
        while (selected.Count < targetCount && available.Any(index => !selected.Contains(index)))
        {
            var best = available
                .Where(index => !selected.Contains(index))
                .OrderByDescending(index => CandidateScore(index, selected, motionScores, frameCount))
                .ThenBy(index => index)
                .First();
            selected.Add(best);
        }
    }

    private static double CandidateScore(int index, IReadOnlySet<int> selected, IReadOnlyList<double> motionScores, int frameCount)
    {
        var distance = selected.Count == 0 ? frameCount : selected.Min(existing => Math.Abs(existing - index));
        return motionScores[index] * 2d + distance / (double)Math.Max(1, frameCount - 1);
    }

    private static FrameOptimizationResult BuildResult(
        IReadOnlySet<int> selected,
        IReadOnlyList<double> motionScores,
        double sourceDurationMs,
        int frameCount)
    {
        var ordered = selected.OrderBy(index => index).ToArray();
        var durations = new Dictionary<int, double>(ordered.Length);
        for (var index = 0; index < ordered.Length; index++)
        {
            var current = ordered[index];
            var next = index + 1 < ordered.Length ? ordered[index + 1] : frameCount;
            durations[current] = Math.Max(1, next - current) * sourceDurationMs;
        }

        var decisions = new FrameOptimizationDecision[frameCount];
        for (var index = 0; index < frameCount; index++)
        {
            var enabled = selected.Contains(index);
            decisions[index] = new FrameOptimizationDecision(
                index,
                enabled,
                enabled ? durations[index] : sourceDurationMs,
                motionScores.Count > index ? motionScores[index] : 0);
        }
        return new FrameOptimizationResult(decisions);
    }

    private static (int Minimum, int Maximum) GetTargetRange(string mode, int frameCount)
    {
        var range = mode switch
        {
            "compact" => (6, 8),
            "balanced" => (8, 12),
            "smooth" => (12, 18),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), $"Unknown frame optimization mode '{mode}'.")
        };
        return (Math.Min(range.Item1, frameCount), Math.Min(range.Item2, frameCount));
    }

    private static void Validate(FrameOptimizationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Settings);
        if (request.FramePaths is null || request.FramePaths.Count == 0)
            throw new ArgumentException("At least one frame is required for optimization.", nameof(request));
        if (!double.IsFinite(request.SourceFrameDurationMs) || request.SourceFrameDurationMs <= 0)
            throw new ArgumentOutOfRangeException(nameof(request), "Source frame duration must be positive.");
        if (!double.IsFinite(request.Settings.SimilarityThreshold) || request.Settings.SimilarityThreshold is < 0 or > 1)
            throw new ArgumentOutOfRangeException(nameof(request), "Similarity threshold must be between 0 and 1.");
        var mode = request.Settings.Mode?.Trim().ToLowerInvariant();
        if (mode is not ("raw" or "compact" or "balanced" or "smooth"))
            throw new ArgumentOutOfRangeException(nameof(request), "Unknown frame optimization mode.");
    }
}

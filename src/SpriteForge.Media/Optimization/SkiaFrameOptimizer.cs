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
        if (mode == "raw" || frameCount <= 1)
            return BuildResult(
                Enumerable.Range(0, frameCount).ToHashSet(),
                new double[frameCount],
                request.SourceFrameDurationMs,
                frameCount);

        var differences = await AnalyzeDifferencesAsync(
            request.FramePaths,
            progress,
            cancellationToken).ConfigureAwait(false);
        var motionScores = BuildMotionScores(differences, request.Settings.PreserveMotionPeaks);
        var threshold = ResolveThreshold(differences, 1d - request.Settings.SimilarityThreshold);

        var significant = Enumerable.Range(1, frameCount - 1)
            .Where(index => differences[index] > threshold)
            .ToArray();

        HashSet<int> selected;
        if (significant.Length == 0)
        {
            selected = [0];
        }
        else
        {
            var target = Math.Min(GetTargetCount(mode), significant.Length + 1);
            selected = significant.Length + 1 <= target
                ? significant.Append(0).ToHashSet()
                : SelectByCumulativeMotion(significant, differences, target);

            if (request.Settings.PreserveMotionPeaks)
                PreserveStrongestPeak(selected, significant, differences, target);
        }

        progress?.Report(new PipelineProgress(
            1,
            $"Optimized {frameCount} frames to {selected.Count} keyframes",
            selected.Count,
            frameCount));

        return BuildResult(
            selected,
            motionScores,
            request.SourceFrameDurationMs,
            frameCount);
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
            differences[index] = await _differenceAnalyzer.DifferenceAsync(
                framePaths[index - 1],
                framePaths[index],
                cancellationToken).ConfigureAwait(false);

            progress?.Report(new PipelineProgress(
                index / (double)Math.Max(1, framePaths.Count - 1),
                "Analyzing localized frame motion",
                index,
                framePaths.Count - 1));
        }
        return differences;
    }

    private static double[] BuildMotionScores(
        IReadOnlyList<double> differences,
        bool preserveMotionPeaks)
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

    private static double ResolveThreshold(
        IReadOnlyList<double> differences,
        double configuredThreshold)
    {
        if (differences.Count <= 1) return configuredThreshold;

        var values = differences.Skip(1).OrderBy(value => value).ToArray();
        var median = values[values.Length / 2];
        var maximum = values[^1];

        if (maximum >= configuredThreshold) return configuredThreshold;
        if (maximum <= median * 1.5 || maximum - median < 0.005)
            return configuredThreshold;

        return Math.Max(0.005, median + (maximum - median) * 0.25);
    }

    private static HashSet<int> SelectByCumulativeMotion(
        IReadOnlyList<int> significant,
        IReadOnlyList<double> differences,
        int targetCount)
    {
        var selected = new HashSet<int> { 0 };
        var cumulative = new double[significant.Count];
        double total = 0;
        for (var index = 0; index < significant.Count; index++)
        {
            total += differences[significant[index]];
            cumulative[index] = total;
        }

        var motionSlots = Math.Max(1, targetCount - 1);
        for (var slot = 1; slot <= motionSlots; slot++)
        {
            var target = total * slot / motionSlots;
            var position = Array.FindIndex(cumulative, value => value >= target);
            selected.Add(significant[position < 0 ? significant.Count - 1 : position]);
        }

        foreach (var index in significant.OrderByDescending(index => differences[index]))
        {
            if (selected.Count >= targetCount) break;
            selected.Add(index);
        }

        return selected;
    }

    private static void PreserveStrongestPeak(
        HashSet<int> selected,
        IReadOnlyList<int> significant,
        IReadOnlyList<double> differences,
        int targetCount)
    {
        var peak = significant.OrderByDescending(index => differences[index]).First();
        if (selected.Contains(peak)) return;

        if (selected.Count >= targetCount)
        {
            var removable = selected
                .Where(index => index != 0)
                .OrderBy(index => differences[index])
                .FirstOrDefault();
            selected.Remove(removable);
        }
        selected.Add(peak);
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

    private static int GetTargetCount(string mode) => mode switch
    {
        "compact" => 7,
        "balanced" => 10,
        "smooth" => 16,
        _ => throw new ArgumentOutOfRangeException(nameof(mode), $"Unknown frame optimization mode '{mode}'.")
    };

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

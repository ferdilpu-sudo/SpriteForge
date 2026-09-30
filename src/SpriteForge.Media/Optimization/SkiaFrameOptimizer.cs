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
        {
            return BuildResult(
                Enumerable.Range(0, frameCount).ToHashSet(),
                new double[frameCount],
                request.SourceFrameDurationMs,
                frameCount,
                normalizeTiming: false);
        }

        var differences = await AnalyzeDifferencesAsync(
            request.FramePaths,
            progress,
            cancellationToken).ConfigureAwait(false);
        var motionScores = BuildMotionScores(
            differences,
            request.Settings.PreserveMotionPeaks);
        var threshold = ResolveThreshold(
            differences,
            1d - request.Settings.SimilarityThreshold);

        var significant = Enumerable.Range(1, frameCount - 1)
            .Where(index => differences[index] > threshold)
            .ToArray();

        var selected = significant.Length == 0
            ? new HashSet<int> { 0 }
            : SelectMotionSequence(
                significant,
                differences,
                GetTargetCount(mode),
                frameCount,
                request.Settings.PreserveMotionPeaks);

        progress?.Report(new PipelineProgress(
            1,
            $"Optimized {frameCount} frames to {selected.Count} keyframes",
            selected.Count,
            frameCount));

        return BuildResult(
            selected,
            motionScores,
            request.SourceFrameDurationMs,
            frameCount,
            normalizeTiming: true);
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

    private static HashSet<int> SelectMotionSequence(
        IReadOnlyList<int> significant,
        IReadOnlyList<double> differences,
        int maximumFrames,
        int frameCount,
        bool preserveMotionPeak)
    {
        var startAnchor = Math.Max(0, significant[0] - 1);
        var endAnchor = Math.Min(frameCount - 1, significant[^1] + 1);
        var rangeLength = endAnchor - startAnchor + 1;
        var target = Math.Min(maximumFrames, rangeLength);

        var selected = significant
            .Append(startAnchor)
            .Append(endAnchor)
            .Distinct()
            .Where(index => index >= startAnchor && index <= endAnchor)
            .ToHashSet();

        if (selected.Count > target)
        {
            selected = DownsampleMotion(
                significant,
                differences,
                target,
                startAnchor,
                endAnchor);
        }
        else if (selected.Count < target)
        {
            AddTemporalCoverage(
                selected,
                startAnchor,
                endAnchor,
                target,
                differences);
        }

        if (preserveMotionPeak)
        {
            PreserveStrongestPeak(
                selected,
                significant,
                differences,
                target,
                startAnchor,
                endAnchor);
        }

        return selected;
    }

    private static HashSet<int> DownsampleMotion(
        IReadOnlyList<int> significant,
        IReadOnlyList<double> differences,
        int target,
        int startAnchor,
        int endAnchor)
    {
        var selected = new HashSet<int> { startAnchor, endAnchor };
        var pool = significant
            .Where(index => !selected.Contains(index))
            .ToArray();
        var remaining = Math.Max(0, target - selected.Count);

        AddByCumulativeMotion(
            selected,
            pool,
            differences,
            remaining);

        return selected;
    }

    private static void AddByCumulativeMotion(
        HashSet<int> selected,
        IReadOnlyList<int> pool,
        IReadOnlyList<double> differences,
        int count)
    {
        if (count <= 0 || pool.Count == 0) return;
        if (pool.Count <= count)
        {
            selected.UnionWith(pool);
            return;
        }

        var cumulative = new double[pool.Count];
        double total = 0;
        for (var index = 0; index < pool.Count; index++)
        {
            total += differences[pool[index]];
            cumulative[index] = total;
        }

        var initialCount = selected.Count;
        for (var slot = 1; slot <= count; slot++)
        {
            var target = total * slot / (count + 1d);
            var position = Array.FindIndex(
                cumulative,
                value => value >= target);
            selected.Add(pool[position < 0 ? pool.Count - 1 : position]);
        }

        foreach (var index in pool.OrderByDescending(index => differences[index]))
        {
            if (selected.Count >= initialCount + count) break;
            selected.Add(index);
        }
    }

    private static void AddTemporalCoverage(
        HashSet<int> selected,
        int startAnchor,
        int endAnchor,
        int targetCount,
        IReadOnlyList<double> differences)
    {
        while (selected.Count < targetCount)
        {
            var candidate = Enumerable.Range(
                    startAnchor,
                    endAnchor - startAnchor + 1)
                .Where(index => !selected.Contains(index))
                .OrderByDescending(index =>
                    selected.Min(existing => Math.Abs(existing - index)))
                .ThenByDescending(index =>
                    differences.Count > index ? differences[index] : 0)
                .ThenBy(index => index)
                .FirstOrDefault(-1);

            if (candidate < 0) break;
            selected.Add(candidate);
        }
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
            scores[index] = preserveMotionPeaks
                ? Math.Max(previous, next)
                : previous;
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

        const double minimumAdaptiveThreshold = 0.001;
        if (maximum < minimumAdaptiveThreshold)
            return configuredThreshold;

        // Continuous subtle motion often has a tight distribution where max is close
        // to the median. Keeping the configured threshold in that case incorrectly
        // classifies the whole clip as static, so adapt below the median instead.
        if (maximum <= median * 1.5 || maximum - median < minimumAdaptiveThreshold)
        {
            return Math.Max(
                minimumAdaptiveThreshold,
                median * 0.8);
        }

        return Math.Max(
            minimumAdaptiveThreshold,
            median + (maximum - median) * 0.25);
    }

    private static void PreserveStrongestPeak(
        HashSet<int> selected,
        IReadOnlyList<int> significant,
        IReadOnlyList<double> differences,
        int targetCount,
        int startAnchor,
        int endAnchor)
    {
        var peak = significant
            .OrderByDescending(index => differences[index])
            .First();
        if (selected.Contains(peak)) return;

        if (selected.Count >= targetCount)
        {
            var removable = selected
                .Where(index =>
                    index != startAnchor &&
                    index != endAnchor)
                .OrderBy(index => differences[index])
                .FirstOrDefault(-1);

            if (removable >= 0)
                selected.Remove(removable);
        }

        selected.Add(peak);
    }

    private static FrameOptimizationResult BuildResult(
        IReadOnlySet<int> selected,
        IReadOnlyList<double> motionScores,
        double sourceDurationMs,
        int frameCount,
        bool normalizeTiming)
    {
        var ordered = selected.OrderBy(index => index).ToArray();
        var durations = new Dictionary<int, double>(ordered.Length);

        if (normalizeTiming && ordered.Length > 0)
        {
            var activeFrameCount = ordered[^1] - ordered[0] + 1;
            var duration = activeFrameCount * sourceDurationMs / ordered.Length;
            foreach (var index in ordered)
                durations[index] = duration;
        }
        else
        {
            for (var index = 0; index < ordered.Length; index++)
            {
                var current = ordered[index];
                var next = index + 1 < ordered.Length
                    ? ordered[index + 1]
                    : frameCount;
                durations[current] =
                    Math.Max(1, next - current) * sourceDurationMs;
            }
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
        _ => throw new ArgumentOutOfRangeException(
            nameof(mode),
            $"Unknown frame optimization mode '{mode}'.")
    };

    private static void Validate(FrameOptimizationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Settings);

        if (request.FramePaths is null || request.FramePaths.Count == 0)
            throw new ArgumentException(
                "At least one frame is required for optimization.",
                nameof(request));

        if (!double.IsFinite(request.SourceFrameDurationMs) ||
            request.SourceFrameDurationMs <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(request),
                "Source frame duration must be positive.");

        if (!double.IsFinite(request.Settings.SimilarityThreshold) ||
            request.Settings.SimilarityThreshold is < 0 or > 1)
            throw new ArgumentOutOfRangeException(
                nameof(request),
                "Similarity threshold must be between 0 and 1.");

        var mode = request.Settings.Mode?.Trim().ToLowerInvariant();
        if (mode is not ("raw" or "compact" or "balanced" or "smooth"))
            throw new ArgumentOutOfRangeException(
                nameof(request),
                "Unknown frame optimization mode.");
    }
}

using SpriteForge.Core.Contracts;
using SpriteForge.Core.Models;

namespace SpriteForge.Media.Optimization;

public sealed partial class SkiaFrameOptimizer : IFrameOptimizer
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

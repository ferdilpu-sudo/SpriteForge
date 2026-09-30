using SpriteForge.Core.Models;

namespace SpriteForge.Media.Optimization;

public sealed partial class SkiaFrameOptimizer
{
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
}

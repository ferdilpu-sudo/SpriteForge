using SpriteForge.Core.Contracts;
using SpriteForge.Core.Models;

namespace SpriteForge.Media.Looping;

public sealed class ImageDifferenceLoopAnalyzer : ILoopAnalyzer
{
    private const int MaxSamplesPerEdge = 48;
    private readonly ImageDifferenceScorer _scorer = new();

    public async Task<IReadOnlyList<LoopCandidate>> AnalyzeAsync(
        IReadOnlyList<FrameRecord> frames,
        Func<Guid, string?> resolveFramePath,
        CancellationToken cancellationToken)
    {
        var enabled = frames.Where(frame => frame.Enabled).OrderBy(frame => frame.Order).ToArray();
        if (enabled.Length < 4) return [];

        var edgeWindow = Math.Max(1, enabled.Length / 4);
        var startIndices = SampleIndices(0, edgeWindow - 1, MaxSamplesPerEdge);
        var endIndices = SampleIndices(
            Math.Max(3, enabled.Length - edgeWindow),
            enabled.Length - 1,
            MaxSamplesPerEdge);

        var requiredIndices = startIndices
            .Concat(endIndices)
            .Distinct()
            .OrderBy(index => index)
            .ToArray();

        var samples = new Dictionary<int, ImageDifferenceSample>(requiredIndices.Length);
        foreach (var index in requiredIndices)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var path = resolveFramePath(enabled[index].Id);
            if (path is null || !File.Exists(path)) continue;
            samples[index] = await _scorer.LoadAsync(path, cancellationToken).ConfigureAwait(false);
        }

        var candidates = new List<LoopCandidate>();
        foreach (var startIndex in startIndices)
        {
            if (!samples.TryGetValue(startIndex, out var startSample)) continue;

            foreach (var endIndex in endIndices)
            {
                if (endIndex < startIndex + 3 ||
                    !samples.TryGetValue(endIndex, out var endSample))
                {
                    continue;
                }

                cancellationToken.ThrowIfCancellationRequested();
                var score = _scorer.Score(startSample, endSample, cancellationToken);
                candidates.Add(new LoopCandidate(
                    enabled[startIndex].Id,
                    enabled[endIndex].Id,
                    score,
                    endIndex - startIndex + 1));
            }
        }

        return candidates
            .OrderBy(candidate => candidate.SeamScore)
            .ThenByDescending(candidate => candidate.FrameCount)
            .Take(5)
            .ToArray();
    }

    private static IReadOnlyList<int> SampleIndices(int start, int end, int maximum)
    {
        if (end < start) return [];

        var count = end - start + 1;
        if (count <= maximum)
            return Enumerable.Range(start, count).ToArray();

        var result = new SortedSet<int>();
        for (var slot = 0; slot < maximum; slot++)
        {
            var position = start + (int)Math.Round(
                slot * (end - start) / (double)(maximum - 1));
            result.Add(position);
        }

        return result.ToArray();
    }
}

using SpriteForge.Core.Contracts;
using SpriteForge.Core.Models;

namespace SpriteForge.Media.Looping;

public sealed class ImageDifferenceLoopAnalyzer : ILoopAnalyzer
{
    private readonly ImageDifferenceScorer _scorer = new();

    public async Task<IReadOnlyList<LoopCandidate>> AnalyzeAsync(
        IReadOnlyList<FrameRecord> frames,
        Func<Guid, string?> resolveFramePath,
        CancellationToken cancellationToken)
    {
        var enabled = frames.Where(frame => frame.Enabled).OrderBy(frame => frame.Order).ToArray();
        if (enabled.Length < 4) return [];

        var edgeWindow = Math.Max(1, enabled.Length / 4);
        var candidates = new List<LoopCandidate>();
        for (var startIndex = 0; startIndex < edgeWindow; startIndex++)
        {
            for (var endIndex = Math.Max(startIndex + 3, enabled.Length - edgeWindow); endIndex < enabled.Length; endIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var start = enabled[startIndex];
                var end = enabled[endIndex];
                var startPath = resolveFramePath(start.Id);
                var endPath = resolveFramePath(end.Id);
                if (startPath is null || endPath is null || !File.Exists(startPath) || !File.Exists(endPath)) continue;

                var score = await _scorer.ScoreAsync(startPath, endPath, cancellationToken).ConfigureAwait(false);
                candidates.Add(new LoopCandidate(start.Id, end.Id, score, endIndex - startIndex + 1));
            }
        }

        return candidates
            .OrderBy(candidate => candidate.SeamScore)
            .ThenByDescending(candidate => candidate.FrameCount)
            .Take(5)
            .ToArray();
    }
}

using SpriteForge.Core.Models;

namespace SpriteForge.Core.Contracts;

public interface ILoopAnalyzer
{
    Task<IReadOnlyList<LoopCandidate>> AnalyzeAsync(
        IReadOnlyList<FrameRecord> frames,
        Func<Guid, string?> resolveFramePath,
        CancellationToken cancellationToken);
}

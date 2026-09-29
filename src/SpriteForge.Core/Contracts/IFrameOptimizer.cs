using SpriteForge.Core.Models;

namespace SpriteForge.Core.Contracts;

public interface IFrameOptimizer
{
    Task<FrameOptimizationResult> OptimizeAsync(
        FrameOptimizationRequest request,
        IProgress<PipelineProgress>? progress,
        CancellationToken cancellationToken);
}

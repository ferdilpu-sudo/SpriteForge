using SpriteForge.Core.Models;

namespace SpriteForge.Core.Contracts;

public interface IFrameExtractor
{
    Task<FrameExtractionResult> ExtractAsync(
        FrameExtractionRequest request,
        IProgress<PipelineProgress>? progress,
        CancellationToken cancellationToken);
}

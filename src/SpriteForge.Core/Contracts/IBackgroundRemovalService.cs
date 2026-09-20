using SpriteForge.Core.Models;

namespace SpriteForge.Core.Contracts;

public interface IBackgroundRemovalService
{
    Task<ProcessedFrame> RemoveAsync(
        string inputPath,
        string outputPath,
        BackgroundRemovalOptions options,
        CancellationToken cancellationToken);
}

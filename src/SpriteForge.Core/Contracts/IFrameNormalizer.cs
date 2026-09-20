using SpriteForge.Core.Models;

namespace SpriteForge.Core.Contracts;

public interface IFrameNormalizer
{
    Task NormalizeAsync(NormalizationRequest request, CancellationToken cancellationToken);
}

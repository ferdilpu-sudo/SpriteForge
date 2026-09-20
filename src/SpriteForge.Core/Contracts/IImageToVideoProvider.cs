using SpriteForge.Core.Models;

namespace SpriteForge.Core.Contracts;

public interface IImageToVideoProvider
{
    string ProviderId { get; }
    bool IsConfigured { get; }
    Task<GenerationJob> SubmitAsync(GenerationRequest request, CancellationToken cancellationToken);
    Task<GenerationJobStatus> GetStatusAsync(string providerJobId, CancellationToken cancellationToken);
    Task<VideoArtifact> DownloadAsync(string providerJobId, string destinationPath, CancellationToken cancellationToken);
}

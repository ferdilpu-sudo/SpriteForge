using SpriteForge.Core.Contracts;
using SpriteForge.Core.Models;

namespace SpriteForge.Providers.Local;

public sealed class ImportOnlyProvider : IImageToVideoProvider
{
    public string ProviderId => "import_only";
    public bool IsConfigured => true;

    public Task<GenerationJob> SubmitAsync(GenerationRequest request, CancellationToken cancellationToken) =>
        Task.FromException<GenerationJob>(new NotSupportedException("Import-only mode does not submit generation jobs."));

    public Task<GenerationJobStatus> GetStatusAsync(string providerJobId, CancellationToken cancellationToken) =>
        Task.FromException<GenerationJobStatus>(new NotSupportedException("Import-only mode has no generation status."));

    public Task<VideoArtifact> DownloadAsync(string providerJobId, string destinationPath, CancellationToken cancellationToken) =>
        Task.FromException<VideoArtifact>(new NotSupportedException("Import-only mode does not download provider assets."));
}

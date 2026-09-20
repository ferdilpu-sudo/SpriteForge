namespace SpriteForge.Core.Contracts;

public interface IFileHashService
{
    Task<string> ComputeSha256Async(string path, CancellationToken cancellationToken);
}

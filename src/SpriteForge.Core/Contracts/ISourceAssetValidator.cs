namespace SpriteForge.Core.Contracts;

public interface ISourceAssetValidator
{
    Task ValidateImageAsync(string path, CancellationToken cancellationToken);
    Task ValidateVideoAsync(string path, CancellationToken cancellationToken);
}

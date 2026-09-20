using System.Security.Cryptography;
using SpriteForge.Core.Contracts;

namespace SpriteForge.Infrastructure.Hashing;

public sealed class FileHashService : IFileHashService
{
    public async Task<string> ComputeSha256Async(string path, CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 128 * 1024, true);
        var hash = await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}

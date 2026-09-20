using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SpriteForge.Application.Fingerprints;

public sealed class PipelineFingerprintService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public string Compute(string stage, object inputs)
    {
        var payload = JsonSerializer.Serialize(new { stage, inputs }, JsonOptions);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
    }
}

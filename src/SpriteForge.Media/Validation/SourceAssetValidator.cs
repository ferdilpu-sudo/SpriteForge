using SkiaSharp;
using SpriteForge.Core.Contracts;

namespace SpriteForge.Media.Validation;

public sealed class SourceAssetValidator(
    IExternalProcessRunner processRunner,
    string ffmpegPath = "ffmpeg") : ISourceAssetValidator
{
    public Task ValidateImageAsync(string path, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var bitmap = SKBitmap.Decode(path);
            if (bitmap is null || bitmap.Width <= 0 || bitmap.Height <= 0)
                throw new InvalidDataException($"Image could not be decoded: {Path.GetFileName(path)}");
        }, cancellationToken);
    }

    public async Task ValidateVideoAsync(string path, CancellationToken cancellationToken)
    {
        var result = await processRunner.RunAsync(
            ffmpegPath,
            ["-v", "error", "-i", path, "-map", "0:v:0", "-frames:v", "1", "-f", "null", "-"],
            null,
            cancellationToken).ConfigureAwait(false);
        if (!result.Succeeded)
        {
            var detail = string.IsNullOrWhiteSpace(result.StandardError)
                ? "FFmpeg could not decode the first video frame."
                : result.StandardError.Trim();
            throw new InvalidDataException($"Video could not be decoded: {Path.GetFileName(path)}. {detail}");
        }
    }
}

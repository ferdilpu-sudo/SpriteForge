using SpriteForge.Core.Contracts;
using SpriteForge.Diagnostics.Models;

namespace SpriteForge.Diagnostics.Services;

public sealed class FfmpegDiagnosticService(IExternalProcessRunner processRunner, string ffmpegPath = "ffmpeg")
{
    public async Task<DiagnosticCheck> CheckAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await processRunner.RunAsync(ffmpegPath, ["-version"], null, cancellationToken).ConfigureAwait(false);
            var firstLine = result.StandardOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim();
            return result.Succeeded
                ? new DiagnosticCheck("ffmpeg", true, "FFmpeg is available.", firstLine)
                : new DiagnosticCheck("ffmpeg", false, "FFmpeg could not be started.", result.StandardError.Trim());
        }
        catch (Exception ex)
        {
            return new DiagnosticCheck("ffmpeg", false, "FFmpeg was not found.", ex.Message);
        }
    }
}

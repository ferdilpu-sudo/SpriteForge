using SpriteForge.Core.Contracts;
using SpriteForge.Diagnostics.Models;

namespace SpriteForge.Diagnostics.Services;

public sealed class FfmpegDiagnosticService(
    IExternalProcessRunner processRunner,
    string ffmpegPath = "ffmpeg",
    string ffprobePath = "ffprobe")
{
    public async Task<DiagnosticCheck> CheckAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var ffmpeg = await processRunner.RunAsync(
                ffmpegPath,
                ["-version"],
                null,
                cancellationToken).ConfigureAwait(false);
            if (!ffmpeg.Succeeded)
            {
                return new DiagnosticCheck(
                    "ffmpeg",
                    false,
                    "FFmpeg could not be started.",
                    ffmpeg.StandardError.Trim());
            }

            var ffprobe = await processRunner.RunAsync(
                ffprobePath,
                ["-version"],
                null,
                cancellationToken).ConfigureAwait(false);
            if (!ffprobe.Succeeded)
            {
                return new DiagnosticCheck(
                    "ffmpeg",
                    false,
                    "FFprobe could not be started.",
                    ffprobe.StandardError.Trim());
            }

            var firstLine = ffmpeg.StandardOutput
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault()
                ?.Trim();

            return new DiagnosticCheck(
                "ffmpeg",
                true,
                "FFmpeg and FFprobe are available.",
                firstLine);
        }
        catch (Exception ex)
        {
            return new DiagnosticCheck(
                "ffmpeg",
                false,
                "FFmpeg/FFprobe was not found.",
                ex.Message);
        }
    }
}

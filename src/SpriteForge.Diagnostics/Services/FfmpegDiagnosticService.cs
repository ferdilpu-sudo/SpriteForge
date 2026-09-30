using SpriteForge.Core.Contracts;
using SpriteForge.Diagnostics.Models;

namespace SpriteForge.Diagnostics.Services;

public sealed class FfmpegDiagnosticService(
    IExternalProcessRunner processRunner,
    string ffmpegPath = "ffmpeg",
    string ffprobePath = "ffprobe")
{
    public DiagnosticCheck CheckInstallation()
    {
        var ffmpeg = ResolveExecutable(ffmpegPath);
        var ffprobe = ResolveExecutable(ffprobePath);

        if (ffmpeg is null || ffprobe is null)
        {
            var missing = new List<string>();
            if (ffmpeg is null) missing.Add("FFmpeg");
            if (ffprobe is null) missing.Add("FFprobe");

            return new DiagnosticCheck(
                "ffmpeg",
                false,
                $"{string.Join(" and ", missing)} was not found on PATH.");
        }

        return new DiagnosticCheck(
            "ffmpeg",
            true,
            "FFmpeg and FFprobe are installed.",
            $"ffmpeg={ffmpeg}; ffprobe={ffprobe}");
    }

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

    private static string? ResolveExecutable(string command)
    {
        if (string.IsNullOrWhiteSpace(command)) return null;

        if (Path.IsPathRooted(command))
            return File.Exists(command) ? Path.GetFullPath(command) : null;

        var pathValue = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(pathValue)) return null;

        var extensions = Path.HasExtension(command)
            ? [string.Empty]
            : GetExecutableExtensions();

        foreach (var directory in pathValue.Split(
            Path.PathSeparator,
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            foreach (var extension in extensions)
            {
                try
                {
                    var candidate = Path.Combine(directory, command + extension);
                    if (File.Exists(candidate))
                        return Path.GetFullPath(candidate);
                }
                catch
                {
                    // Ignore malformed PATH entries and keep searching.
                }
            }
        }

        return null;
    }

    private static IReadOnlyList<string> GetExecutableExtensions()
    {
        if (!OperatingSystem.IsWindows()) return [string.Empty];

        var pathExt = Environment.GetEnvironmentVariable("PATHEXT");
        if (string.IsNullOrWhiteSpace(pathExt))
            return [".exe", ".cmd", ".bat"];

        return pathExt
            .Split(
                Path.PathSeparator,
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Prepend(".exe")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}

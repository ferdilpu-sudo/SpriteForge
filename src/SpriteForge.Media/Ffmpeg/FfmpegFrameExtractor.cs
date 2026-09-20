using SpriteForge.Core.Contracts;
using SpriteForge.Core.Errors;
using SpriteForge.Core.Models;

namespace SpriteForge.Media.Ffmpeg;

public sealed class FfmpegFrameExtractor(IExternalProcessRunner processRunner, string ffmpegPath = "ffmpeg") : IFrameExtractor
{
    public async Task<FrameExtractionResult> ExtractAsync(
        FrameExtractionRequest request,
        IProgress<PipelineProgress>? progress,
        CancellationToken cancellationToken)
    {
        Validate(request);
        Directory.CreateDirectory(request.OutputDirectory);
        DeletePriorFrames(request.OutputDirectory);

        progress?.Report(new PipelineProgress(0.02, "Starting FFmpeg frame extraction"));
        var outputPattern = Path.Combine(request.OutputDirectory, "frame_%06d.png");
        var arguments = BuildArguments(request, outputPattern);
        var result = await processRunner.RunAsync(ffmpegPath, arguments, null, cancellationToken).ConfigureAwait(false);

        if (!result.Succeeded)
        {
            throw new SpriteForgeException(
                "FRAME_EXTRACTION_FAILED",
                "FFmpeg could not extract frames from the selected video.",
                inner: new InvalidOperationException(Sanitize(result.StandardError)));
        }

        var frames = Directory.EnumerateFiles(request.OutputDirectory, "frame_*.png")
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (frames.Length == 0)
        {
            throw new SpriteForgeException("FRAME_EXTRACTION_EMPTY", "FFmpeg completed but produced no frames.");
        }

        progress?.Report(new PipelineProgress(1, $"Extracted {frames.Length} frames", frames.Length, frames.Length));
        return new FrameExtractionResult(frames, request.Settings.Fps);
    }

    private static IReadOnlyList<string> BuildArguments(FrameExtractionRequest request, string outputPattern)
    {
        var args = new List<string> { "-hide_banner", "-loglevel", "error", "-y" };
        if (request.Settings.StartSeconds > 0)
        {
            args.AddRange(["-ss", request.Settings.StartSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture)]);
        }

        args.AddRange(["-i", request.SourcePath]);
        if (request.Settings.EndSeconds is { } end)
        {
            var duration = end - request.Settings.StartSeconds;
            args.AddRange(["-t", duration.ToString(System.Globalization.CultureInfo.InvariantCulture)]);
        }

        args.AddRange([
            "-vf", $"fps={request.Settings.Fps.ToString(System.Globalization.CultureInfo.InvariantCulture)}",
            "-vsync", "0",
            outputPattern
        ]);
        return args;
    }

    private static void Validate(FrameExtractionRequest request)
    {
        if (!File.Exists(request.SourcePath)) throw new FileNotFoundException("Source video was not found.", request.SourcePath);
        if (request.Settings.Fps <= 0) throw new ArgumentOutOfRangeException(nameof(request), "FPS must be positive.");
        if (request.Settings.StartSeconds < 0) throw new ArgumentOutOfRangeException(nameof(request), "Start time cannot be negative.");
        if (request.Settings.EndSeconds is { } end && end <= request.Settings.StartSeconds)
            throw new ArgumentException("End time must be greater than start time.", nameof(request));
    }

    private static void DeletePriorFrames(string directory)
    {
        foreach (var file in Directory.EnumerateFiles(directory, "frame_*.png")) File.Delete(file);
    }

    private static string Sanitize(string stderr)
    {
        const int limit = 4000;
        var normalized = stderr.Replace('\r', ' ').Replace('\n', ' ').Trim();
        return normalized.Length <= limit ? normalized : normalized[..limit];
    }
}

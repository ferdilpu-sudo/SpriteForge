using System.Globalization;
using System.Text.Json;
using SpriteForge.Core.Contracts;
using SpriteForge.Core.Errors;
using SpriteForge.Core.Models;

namespace SpriteForge.Media.Ffmpeg;

public sealed class FfmpegFrameExtractor(
    IExternalProcessRunner processRunner,
    string ffmpegPath = "ffmpeg",
    string ffprobePath = "ffprobe") : IFrameExtractor
{
    public const int MaxCandidateFrames = 2_000;
    public const long MaxEstimatedRawBytes = 6L * 1024 * 1024 * 1024;
    public const long MaxSingleFrameRawBytes = 256L * 1024 * 1024;

    private const int SentinelFrameCount = MaxCandidateFrames + 1;

    public async Task<FrameExtractionResult> ExtractAsync(
        FrameExtractionRequest request,
        IProgress<PipelineProgress>? progress,
        CancellationToken cancellationToken)
    {
        Validate(request);
        ValidateKnownRangeFrameCount(request);

        var probe = await ProbeAsync(request.SourcePath, cancellationToken).ConfigureAwait(false);
        ValidateProbeBudget(request, probe);

        Directory.CreateDirectory(request.OutputDirectory);
        DeletePriorFrames(request.OutputDirectory);

        progress?.Report(new PipelineProgress(0.02, "Starting FFmpeg frame extraction"));
        var outputPattern = Path.Combine(request.OutputDirectory, "frame_%06d.png");
        var arguments = BuildArguments(request, outputPattern);
        var result = await processRunner.RunAsync(
            ffmpegPath,
            arguments,
            null,
            cancellationToken).ConfigureAwait(false);

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
            throw new SpriteForgeException("FRAME_EXTRACTION_EMPTY", "FFmpeg completed but produced no frames.");

        if (frames.Length > MaxCandidateFrames)
        {
            throw new SpriteForgeException(
                "FRAME_EXTRACTION_LIMIT",
                $"Extraction would exceed the safety limit of {MaxCandidateFrames:N0} candidate frames. " +
                "Choose a shorter time range or lower the target FPS.");
        }

        progress?.Report(new PipelineProgress(
            1,
            $"Extracted {frames.Length} frames",
            frames.Length,
            frames.Length));

        return new FrameExtractionResult(frames, request.Settings.Fps);
    }

    private async Task<VideoProbe> ProbeAsync(
        string sourcePath,
        CancellationToken cancellationToken)
    {
        ExternalProcessResult result;
        try
        {
            result = await processRunner.RunAsync(
                ffprobePath,
                [
                    "-v", "error",
                    "-select_streams", "v:0",
                    "-show_entries", "stream=width,height:format=duration",
                    "-of", "json",
                    sourcePath
                ],
                null,
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new SpriteForgeException(
                "FFPROBE_UNAVAILABLE",
                "FFprobe is required to inspect video dimensions before extraction.",
                inner: ex);
        }

        if (!result.Succeeded)
        {
            throw new SpriteForgeException(
                "VIDEO_PROBE_FAILED",
                "FFprobe could not inspect the selected video.",
                inner: new InvalidOperationException(Sanitize(result.StandardError)));
        }

        try
        {
            using var document = JsonDocument.Parse(result.StandardOutput);
            var streams = document.RootElement.GetProperty("streams");
            if (streams.GetArrayLength() == 0)
                throw new InvalidDataException("No video stream was reported.");

            var stream = streams[0];
            var width = stream.GetProperty("width").GetInt32();
            var height = stream.GetProperty("height").GetInt32();
            if (width <= 0 || height <= 0)
                throw new InvalidDataException("Video dimensions are invalid.");

            double? durationSeconds = null;
            if (document.RootElement.TryGetProperty("format", out var format) &&
                format.TryGetProperty("duration", out var durationElement))
            {
                var durationText = durationElement.ValueKind == JsonValueKind.String
                    ? durationElement.GetString()
                    : durationElement.GetRawText();
                if (double.TryParse(
                    durationText,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out var duration) &&
                    double.IsFinite(duration) &&
                    duration > 0)
                {
                    durationSeconds = duration;
                }
            }

            return new VideoProbe(width, height, durationSeconds);
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or KeyNotFoundException or InvalidDataException)
        {
            throw new SpriteForgeException(
                "VIDEO_PROBE_INVALID",
                "FFprobe returned incomplete video metadata.",
                inner: ex);
        }
    }

    private static void ValidateKnownRangeFrameCount(FrameExtractionRequest request)
    {
        if (request.Settings.EndSeconds is not { } end) return;

        var estimatedFrames = Math.Ceiling(
            (end - request.Settings.StartSeconds) * request.Settings.Fps);

        if (estimatedFrames > MaxCandidateFrames)
        {
            throw new SpriteForgeException(
                "FRAME_EXTRACTION_LIMIT",
                $"The selected range is estimated to create {estimatedFrames:N0} candidate frames, " +
                $"above the safety limit of {MaxCandidateFrames:N0}. Lower FPS or shorten the range.");
        }
    }

    private static void ValidateProbeBudget(
        FrameExtractionRequest request,
        VideoProbe probe)
    {
        var frameBytes = checked((long)probe.Width * probe.Height * 4);
        if (frameBytes > MaxSingleFrameRawBytes)
        {
            throw new SpriteForgeException(
                "FRAME_RESOLUTION_LIMIT",
                $"Video resolution {probe.Width}×{probe.Height} requires about " +
                $"{frameBytes / (1024d * 1024d):0} MiB for one decoded RGBA frame, above the " +
                $"{MaxSingleFrameRawBytes / (1024d * 1024d):0} MiB safety limit.");
        }

        var duration = request.Settings.EndSeconds is { } end
            ? end - request.Settings.StartSeconds
            : probe.DurationSeconds is { } sourceDuration && sourceDuration > request.Settings.StartSeconds
                ? sourceDuration - request.Settings.StartSeconds
                : (double?)null;

        if (duration is null) return;

        var estimatedFrames = Math.Ceiling(duration.Value * request.Settings.Fps);
        if (estimatedFrames > MaxCandidateFrames)
        {
            throw new SpriteForgeException(
                "FRAME_EXTRACTION_LIMIT",
                $"The selected video is estimated to create {estimatedFrames:N0} candidate frames, " +
                $"above the safety limit of {MaxCandidateFrames:N0}. Lower FPS or shorten the range.");
        }

        var estimatedRawBytes = frameBytes * estimatedFrames;
        if (estimatedRawBytes <= MaxEstimatedRawBytes) return;

        throw new SpriteForgeException(
            "FRAME_EXTRACTION_BUDGET",
            $"Extraction of {probe.Width}×{probe.Height} at {request.Settings.Fps:0.##} FPS is estimated " +
            $"to process {estimatedFrames:N0} frames ({estimatedRawBytes / (1024d * 1024d * 1024d):0.0} GiB " +
            $"of decoded RGBA pixel data), above the {MaxEstimatedRawBytes / (1024d * 1024d * 1024d):0} GiB safety budget. " +
            "Lower FPS, shorten the range, or use a smaller source.");
    }

    private static IReadOnlyList<string> BuildArguments(
        FrameExtractionRequest request,
        string outputPattern)
    {
        var args = new List<string>
        {
            "-hide_banner", "-loglevel", "error", "-y"
        };

        if (request.Settings.StartSeconds > 0)
        {
            args.AddRange([
                "-ss",
                request.Settings.StartSeconds.ToString(CultureInfo.InvariantCulture)
            ]);
        }

        args.AddRange(["-i", request.SourcePath]);
        if (request.Settings.EndSeconds is { } end)
        {
            var duration = end - request.Settings.StartSeconds;
            args.AddRange(["-t", duration.ToString(CultureInfo.InvariantCulture)]);
        }

        args.AddRange([
            "-vf", $"fps={request.Settings.Fps.ToString(CultureInfo.InvariantCulture)}",
            "-frames:v", SentinelFrameCount.ToString(CultureInfo.InvariantCulture),
            "-fps_mode", "passthrough",
            outputPattern
        ]);

        return args;
    }

    private static void Validate(FrameExtractionRequest request)
    {
        if (!File.Exists(request.SourcePath))
            throw new FileNotFoundException("Source video was not found.", request.SourcePath);

        if (!double.IsFinite(request.Settings.Fps) ||
            request.Settings.Fps <= 0 ||
            request.Settings.Fps > 120)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request),
                "FPS must be between 0 and 120.");
        }

        if (!double.IsFinite(request.Settings.StartSeconds) ||
            request.Settings.StartSeconds < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request),
                "Start time cannot be negative.");
        }

        if (request.Settings.EndSeconds is not { } end)
            return;

        if (!double.IsFinite(end) || end <= request.Settings.StartSeconds)
        {
            throw new ArgumentException(
                "End time must be greater than start time.",
                nameof(request));
        }
    }

    private static void DeletePriorFrames(string directory)
    {
        foreach (var file in Directory.EnumerateFiles(directory, "frame_*.png"))
            File.Delete(file);
    }

    private static string Sanitize(string stderr)
    {
        const int limit = 4000;
        var normalized = stderr.Replace('\r', ' ').Replace('\n', ' ').Trim();
        return normalized.Length <= limit ? normalized : normalized[..limit];
    }

    private sealed record VideoProbe(int Width, int Height, double? DurationSeconds);
}

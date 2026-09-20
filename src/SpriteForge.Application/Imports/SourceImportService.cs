using SpriteForge.Core.Contracts;
using SpriteForge.Core.Enums;
using SpriteForge.Core.Models;

namespace SpriteForge.Application.Imports;

public sealed class SourceImportService(IFileHashService hashService, ISourceAssetValidator assetValidator)
{
    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".webp"
    };

    private static readonly HashSet<string> FrameExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".webp"
    };

    private static readonly HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4", ".webm", ".mov"
    };

    public Task<ArtifactRecord> ImportVideoAsync(
        ProjectDocument project,
        ProjectWorkspacePaths workspace,
        string sourcePath,
        CancellationToken cancellationToken) =>
        ImportSingleAsync(project, workspace, sourcePath, "video", ArtifactKind.SourceVideo, VideoExtensions, cancellationToken);

    public Task<ArtifactRecord> ImportImageAsync(
        ProjectDocument project,
        ProjectWorkspacePaths workspace,
        string sourcePath,
        CancellationToken cancellationToken) =>
        ImportSingleAsync(project, workspace, sourcePath, "image", ArtifactKind.SourceImage, ImageExtensions, cancellationToken);

    public async Task<IReadOnlyList<ArtifactRecord>> ImportFrameSequenceAsync(
        ProjectDocument project,
        ProjectWorkspacePaths workspace,
        IEnumerable<string> sourcePaths,
        CancellationToken cancellationToken)
    {
        var ordered = sourcePaths
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .OrderBy(path => path, NaturalPathComparer.Instance)
            .ToArray();
        if (ordered.Length == 0) throw new ArgumentException("At least one frame is required.", nameof(sourcePaths));
        foreach (var path in ordered) ValidateExistingFile(path, FrameExtensions);
        foreach (var path in ordered)
            await assetValidator.ValidateImageAsync(path, cancellationToken).ConfigureAwait(false);

        var destinationDirectory = Path.Combine(workspace.Source, "frames", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(destinationDirectory);
        var artifacts = new List<ArtifactRecord>(ordered.Length);
        var frames = new List<FrameRecord>(ordered.Length);
        try
        {
            for (var index = 0; index < ordered.Length; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var extension = Path.GetExtension(ordered[index]).ToLowerInvariant();
                var destination = Path.Combine(destinationDirectory, $"frame_{index + 1:D6}{extension}");
                await CopyFileAsync(ordered[index], destination, cancellationToken).ConfigureAwait(false);
                var artifact = await CreateArtifactAsync(destination, ArtifactKind.SourceImage, workspace, cancellationToken).ConfigureAwait(false);
                artifacts.Add(artifact);
                frames.Add(new FrameRecord(
                    Guid.NewGuid(),
                    index,
                    index,
                    true,
                    1000d / project.Extraction.Fps,
                    new FrameArtifactLinks(artifact.Id, null, null, null),
                    new FrameTransform(0, 0, 1),
                    new FramePivot(0.5, 1)));
            }
        }
        catch
        {
            if (Directory.Exists(destinationDirectory)) Directory.Delete(destinationDirectory, recursive: true);
            throw;
        }

        ResetProjectForNewSource(project);
        project.Artifacts.AddRange(artifacts);
        project.Frames.AddRange(frames);
        project.Source = new ProjectSource("frame_sequence", artifacts[0].Id);
        return artifacts;
    }

    private async Task<ArtifactRecord> ImportSingleAsync(
        ProjectDocument project,
        ProjectWorkspacePaths workspace,
        string sourcePath,
        string sourceKind,
        ArtifactKind artifactKind,
        IReadOnlySet<string> allowedExtensions,
        CancellationToken cancellationToken)
    {
        ValidateExistingFile(sourcePath, allowedExtensions);
        if (artifactKind == ArtifactKind.SourceVideo)
            await assetValidator.ValidateVideoAsync(sourcePath, cancellationToken).ConfigureAwait(false);
        else
            await assetValidator.ValidateImageAsync(sourcePath, cancellationToken).ConfigureAwait(false);
        var extension = Path.GetExtension(sourcePath).ToLowerInvariant();
        var safeStem = MakeSafeFileName(Path.GetFileNameWithoutExtension(sourcePath));
        var destination = Path.Combine(workspace.Source, $"{safeStem}_{Guid.NewGuid():N}{extension}");
        await CopyFileAsync(sourcePath, destination, cancellationToken).ConfigureAwait(false);
        var artifact = await CreateArtifactAsync(destination, artifactKind, workspace, cancellationToken).ConfigureAwait(false);

        ResetProjectForNewSource(project);
        project.Artifacts.Add(artifact);
        project.Source = new ProjectSource(sourceKind, artifact.Id);
        return artifact;
    }

    private async Task<ArtifactRecord> CreateArtifactAsync(
        string path,
        ArtifactKind kind,
        ProjectWorkspacePaths workspace,
        CancellationToken cancellationToken)
    {
        var hash = await hashService.ComputeSha256Async(path, cancellationToken).ConfigureAwait(false);
        return new ArtifactRecord(
            Guid.NewGuid(),
            kind,
            workspace.ToRelative(path),
            MimeTypeFor(path),
            null,
            null,
            null,
            hash,
            DateTimeOffset.UtcNow);
    }

    private static void ResetProjectForNewSource(ProjectDocument project)
    {
        project.Source = null;
        project.Generation = null;
        project.Frames.Clear();
        project.Artifacts.Clear();
        project.Exports.Clear();
        project.Loop = project.Loop with { StartFrameId = null, EndFrameId = null, Recommended = false };
    }

    private static async Task CopyFileAsync(string source, string destination, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(destination))!);
        await using var input = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read, 128 * 1024, true);
        await using var output = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None, 128 * 1024, true);
        await input.CopyToAsync(output, cancellationToken).ConfigureAwait(false);
    }

    private static void ValidateExistingFile(string path, IReadOnlySet<string> extensions)
    {
        if (!File.Exists(path)) throw new FileNotFoundException("Source asset was not found.", path);
        var extension = Path.GetExtension(path);
        if (!extensions.Contains(extension))
            throw new NotSupportedException($"The '{extension}' file type is not supported for this import operation.");
    }

    private static string MimeTypeFor(string path) => Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".png" => "image/png",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".webp" => "image/webp",
        ".mp4" => "video/mp4",
        ".webm" => "video/webm",
        ".mov" => "video/quicktime",
        _ => "application/octet-stream"
    };

    private static string MakeSafeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars().ToHashSet();
        var cleaned = string.Concat(value.Trim().Select(character => invalid.Contains(character) ? '_' : character));
        return string.IsNullOrWhiteSpace(cleaned) ? "source" : cleaned;
    }
}

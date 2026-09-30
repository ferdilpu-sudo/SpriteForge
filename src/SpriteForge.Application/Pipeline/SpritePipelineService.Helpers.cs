using SpriteForge.Core.Enums;
using SpriteForge.Core.Models;

namespace SpriteForge.Application.Pipeline;

public sealed partial class SpritePipelineService
{
private static IReadOnlyList<SheetFrame> BuildSheetFrames(
        ProjectDocument project,
        ProjectWorkspacePaths workspace,
        IReadOnlyList<FrameRecord> sequence)
    {
        return sequence.Select(frame =>
        {
            var artifact = ResolveArtifact(project, frame.Artifacts.Normalized);
            return new SheetFrame(
                workspace.ResolveRelative(artifact.RelativePath),
                frame.Id,
                frame.DurationMs,
                frame.Pivot);
        }).ToArray();
    }

    private async Task<ArtifactRecord> CreateArtifactAsync(
        string path,
        ArtifactKind kind,
        string mimeType,
        ProjectWorkspacePaths workspace,
        CancellationToken cancellationToken)
    {
        var hash = await hashService.ComputeSha256Async(path, cancellationToken).ConfigureAwait(false);
        return new ArtifactRecord(
            Guid.NewGuid(),
            kind,
            workspace.ToRelative(path),
            mimeType,
            null,
            null,
            null,
            hash,
            DateTimeOffset.UtcNow);
    }

    private static ArtifactRecord ResolveArtifact(ProjectDocument project, Guid? artifactId)
    {
        if (artifactId is null) throw new InvalidOperationException("Required frame artifact is missing.");
        return project.Artifacts.FirstOrDefault(artifact => artifact.Id == artifactId.Value)
            ?? throw new InvalidOperationException($"Artifact {artifactId} is missing from project metadata.");
    }

    private static void ApplyFrameArtifacts(
        ProjectDocument project,
        IEnumerable<(Guid FrameId, ArtifactRecord Artifact)> staged,
        Func<FrameArtifactLinks, Guid, FrameArtifactLinks> update)
    {
        foreach (var item in staged)
        {
            project.Artifacts.Add(item.Artifact);
            var index = project.Frames.FindIndex(frame => frame.Id == item.FrameId);
            if (index < 0) throw new InvalidOperationException($"Frame {item.FrameId} is missing from the project.");
            var frame = project.Frames[index];
            project.Frames[index] = frame with { Artifacts = update(frame.Artifacts, item.Artifact.Id) };
        }
    }

    private static void ClearFrameLinks(
        ProjectDocument project,
        bool clearTransparent,
        bool clearNormalized,
        bool clearThumbnail)
    {
        for (var index = 0; index < project.Frames.Count; index++)
        {
            var frame = project.Frames[index];
            project.Frames[index] = frame with
            {
                Artifacts = frame.Artifacts with
                {
                    Transparent = clearTransparent ? null : frame.Artifacts.Transparent,
                    Normalized = clearNormalized ? null : frame.Artifacts.Normalized,
                    Thumbnail = clearThumbnail ? null : frame.Artifacts.Thumbnail
                }
            };
        }
    }

    private void RemoveTransientArtifacts(
        ProjectDocument project,
        ProjectWorkspacePaths workspace,
        params ArtifactKind[] kinds)
    {
        var set = kinds.ToHashSet();
        var removable = project.Artifacts
            .Where(artifact => set.Contains(artifact.Kind))
            .ToArray();

        _artifactCleanup.DeleteFiles(workspace, removable);

        var removableIds = removable
            .Select(artifact => artifact.Id)
            .ToHashSet();
        project.Artifacts.RemoveAll(artifact => removableIds.Contains(artifact.Id));
    }

    private static void ResetLoopSelection(ProjectDocument project) =>
        project.Loop = project.Loop with { StartFrameId = null, EndFrameId = null, Recommended = false };

    private static void EnsureExternalTargetsAvailable(
        string sheetPath,
        string metadataPath,
        string framesDirectory,
        bool includeFrames)
    {
        if (File.Exists(sheetPath) || File.Exists(metadataPath))
            throw new IOException("One or more export targets already exist. Explicit overwrite confirmation is required.");
        if (includeFrames && Directory.Exists(framesDirectory) && Directory.EnumerateFileSystemEntries(framesDirectory).Any())
            throw new IOException($"Export frame directory already contains files: {framesDirectory}");
    }

    private static async Task CopyFileAsync(string source, string destination, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(destination))!);
        await using var input = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read, 128 * 1024, true);
        await using var output = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None, 128 * 1024, true);
        await input.CopyToAsync(output, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<IReadOnlyList<string>> CopyFrameExportsAsync(
        IReadOnlyList<string> sourcePaths,
        string destinationDirectory,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(destinationDirectory);
        var outputs = new List<string>(sourcePaths.Count);
        try
        {
            foreach (var source in sourcePaths)
            {
                var destination = Path.Combine(destinationDirectory, Path.GetFileName(source));
                await CopyFileAsync(source, destination, cancellationToken).ConfigureAwait(false);
                outputs.Add(destination);
            }
            return outputs;
        }
        catch
        {
            foreach (var output in outputs) TryDeleteFile(output);
            throw;
        }
    }

    private static string MakeSafeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars().ToHashSet();
        var cleaned = string.Concat(value.Trim().Select(character => invalid.Contains(character) ? '_' : character));
        return string.IsNullOrWhiteSpace(cleaned) ? "sprite" : cleaned;
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path)) Directory.Delete(path, recursive: true);
        }
        catch
        {
            // Cleanup failure must not hide the original processing error.
        }
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        catch
        {
            // Cleanup failure must not hide the original processing error.
        }
    }
}

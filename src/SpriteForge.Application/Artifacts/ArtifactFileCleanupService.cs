using SpriteForge.Core.Models;

namespace SpriteForge.Application.Artifacts;

public sealed record ArtifactCleanupResult(
    int DeletedFiles,
    int FailedFiles,
    int RemovedDirectories);

public sealed class ArtifactFileCleanupService
{
    public ArtifactCleanupResult DeleteFiles(
        ProjectWorkspacePaths workspace,
        IEnumerable<ArtifactRecord> artifacts)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        ArgumentNullException.ThrowIfNull(artifacts);

        var deletedFiles = 0;
        var failedFiles = 0;

        foreach (var artifact in artifacts
            .GroupBy(item => item.RelativePath, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First()))
        {
            try
            {
                var path = workspace.ResolveRelative(artifact.RelativePath);
                if (!File.Exists(path)) continue;
                File.Delete(path);
                deletedFiles++;
            }
            catch
            {
                failedFiles++;
            }
        }

        var removedDirectories = RemoveEmptyChildren(
            workspace.Source,
            workspace.GeneratedVideo,
            workspace.Extracted,
            workspace.Transparent,
            workspace.Normalized,
            workspace.Thumbnails,
            workspace.Analysis);

        return new ArtifactCleanupResult(
            deletedFiles,
            failedFiles,
            removedDirectories);
    }

    private static int RemoveEmptyChildren(params string[] roots)
    {
        var removed = 0;
        foreach (var root in roots.Where(Directory.Exists))
        {
            IEnumerable<string> directories;
            try
            {
                directories = Directory
                    .EnumerateDirectories(root, "*", SearchOption.AllDirectories)
                    .OrderByDescending(path => path.Length)
                    .ToArray();
            }
            catch
            {
                continue;
            }

            foreach (var directory in directories)
            {
                try
                {
                    if (Directory.EnumerateFileSystemEntries(directory).Any())
                        continue;

                    Directory.Delete(directory);
                    removed++;
                }
                catch
                {
                    // Cleanup is best-effort and must not replace the successful pipeline result.
                }
            }
        }

        return removed;
    }
}

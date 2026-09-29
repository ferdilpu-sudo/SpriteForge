using SpriteForge.Core.Enums;
using SpriteForge.Core.Models;

namespace SpriteForge.Application.Frames;

public sealed record FramePruneResult(
    int RemovedFrames,
    int RemovedArtifacts,
    int DeletedFiles);

public sealed class FramePruningService
{
    private static readonly HashSet<ArtifactKind> FrameArtifactKinds =
    [
        ArtifactKind.ExtractedFrame,
        ArtifactKind.TransparentFrame,
        ArtifactKind.NormalizedFrame,
        ArtifactKind.Thumbnail
    ];

    public FramePruneResult DeleteDisabledFrames(
        ProjectDocument project,
        ProjectWorkspacePaths workspace)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(workspace);

        var disabled = project.Frames.Where(frame => !frame.Enabled).ToArray();
        if (disabled.Length == 0) return new FramePruneResult(0, 0, 0);

        var retained = project.Frames.Where(frame => frame.Enabled).ToArray();
        if (retained.Length == 0)
            throw new InvalidOperationException("At least one enabled frame must remain.");

        var retainedArtifacts = retained
            .SelectMany(FrameArtifactIds)
            .ToHashSet();
        var candidates = disabled
            .SelectMany(FrameArtifactIds)
            .Distinct()
            .Where(id => !retainedArtifacts.Contains(id))
            .ToHashSet();

        var removable = project.Artifacts
            .Where(artifact => candidates.Contains(artifact.Id) && FrameArtifactKinds.Contains(artifact.Kind))
            .ToArray();

        var deletedFiles = 0;
        foreach (var artifact in removable)
        {
            if (TryDeleteArtifactFile(workspace, artifact))
                deletedFiles++;
        }

        var removableIds = removable.Select(artifact => artifact.Id).ToHashSet();
        project.Artifacts.RemoveAll(artifact => removableIds.Contains(artifact.Id));
        project.Frames.RemoveAll(frame => !frame.Enabled);

        var ordered = project.Frames.OrderBy(frame => frame.Order).ToArray();
        project.Frames.Clear();
        for (var index = 0; index < ordered.Length; index++)
            project.Frames.Add(ordered[index] with { Order = index });

        project.Loop = project.Loop with
        {
            StartFrameId = null,
            EndFrameId = null,
            Recommended = false
        };

        return new FramePruneResult(
            disabled.Length,
            removable.Length,
            deletedFiles);
    }

    private static IEnumerable<Guid> FrameArtifactIds(FrameRecord frame)
    {
        if (frame.Artifacts.Extracted is { } extracted) yield return extracted;
        if (frame.Artifacts.Transparent is { } transparent) yield return transparent;
        if (frame.Artifacts.Normalized is { } normalized) yield return normalized;
        if (frame.Artifacts.Thumbnail is { } thumbnail) yield return thumbnail;
    }

    private static bool TryDeleteArtifactFile(
        ProjectWorkspacePaths workspace,
        ArtifactRecord artifact)
    {
        try
        {
            var path = workspace.ResolveRelative(artifact.RelativePath);
            if (!File.Exists(path)) return false;
            File.Delete(path);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

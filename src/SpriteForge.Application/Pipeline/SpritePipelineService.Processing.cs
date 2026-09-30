using SpriteForge.Core.Enums;
using SpriteForge.Core.Models;

namespace SpriteForge.Application.Pipeline;

public sealed partial class SpritePipelineService
{
    public async Task ExtractFramesAsync(
        ProjectDocument project,
        ProjectWorkspacePaths workspace,
        string videoPath,
        IProgress<PipelineProgress>? progress,
        CancellationToken cancellationToken)
    {
        var outputDirectory = Path.Combine(workspace.Extracted, Guid.NewGuid().ToString("N"));
        try
        {
            var result = await frameExtractor.ExtractAsync(
                new FrameExtractionRequest(videoPath, outputDirectory, project.Extraction),
                progress,
                cancellationToken).ConfigureAwait(false);

            var sourceDurationMs = 1000d / result.Fps;
            var optimization = await frameOptimizer.OptimizeAsync(
                new FrameOptimizationRequest(result.FramePaths, sourceDurationMs, project.FrameOptimization),
                progress: null,
                cancellationToken).ConfigureAwait(false);
            if (optimization.Decisions.Count != result.FramePaths.Count)
                throw new InvalidOperationException("Frame optimizer returned an incomplete decision set.");
            var decisions = optimization.Decisions.ToDictionary(decision => decision.SourceIndex);

            var stagedArtifacts = new List<ArtifactRecord>(result.FramePaths.Count);
            var stagedFrames = new List<FrameRecord>(result.FramePaths.Count);
            for (var index = 0; index < result.FramePaths.Count; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var path = result.FramePaths[index];
                var artifact = await CreateArtifactAsync(
                    path,
                    ArtifactKind.ExtractedFrame,
                    "image/png",
                    workspace,
                    cancellationToken).ConfigureAwait(false);
                stagedArtifacts.Add(artifact);
                if (!decisions.TryGetValue(index, out var decision))
                    throw new InvalidOperationException($"Frame optimizer omitted source frame {index}.");
                stagedFrames.Add(new FrameRecord(
                    Guid.NewGuid(),
                    index,
                    index,
                    decision.Enabled,
                    decision.DurationMs,
                    new FrameArtifactLinks(artifact.Id, null, null, null),
                    new FrameTransform(0, 0, 1),
                    new FramePivot(0.5, 1)));
            }

            RemoveTransientArtifacts(project, workspace, ArtifactKind.ExtractedFrame, ArtifactKind.TransparentFrame, ArtifactKind.NormalizedFrame, ArtifactKind.Thumbnail);
            project.Artifacts.AddRange(stagedArtifacts);
            project.Frames.Clear();
            project.Frames.AddRange(stagedFrames);
            ResetLoopSelection(project);
            progress?.Report(new PipelineProgress(
                1,
                $"Extracted {stagedFrames.Count} frames · {optimization.EnabledCount} keyframes enabled",
                optimization.EnabledCount,
                stagedFrames.Count));
        }
        catch
        {
            TryDeleteDirectory(outputDirectory);
            throw;
        }
    }

    public async Task RemoveBackgroundsAsync(
        ProjectDocument project,
        ProjectWorkspacePaths workspace,
        IProgress<PipelineProgress>? progress,
        CancellationToken cancellationToken)
    {
        if (!project.BackgroundRemoval.Enabled) return;
        var enabled = project.Frames.Where(frame => frame.Enabled).OrderBy(frame => frame.Order).ToArray();
        if (enabled.Length == 0) throw new InvalidOperationException("No enabled frames are available for background removal.");

        var outputDirectory = Path.Combine(workspace.Transparent, Guid.NewGuid().ToString("N"));
        var staged = new List<(Guid FrameId, ArtifactRecord Artifact)>(enabled.Length);
        try
        {
            for (var index = 0; index < enabled.Length; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var frame = enabled[index];
                var extracted = ResolveArtifact(project, frame.Artifacts.Extracted);
                var input = workspace.ResolveRelative(extracted.RelativePath);
                var output = Path.Combine(outputDirectory, $"frame_{frame.SourceIndex + 1:D6}.png");
                await backgroundRemoval.RemoveAsync(
                    input,
                    output,
                    new BackgroundRemovalOptions(project.BackgroundRemoval.AlphaThreshold),
                    cancellationToken).ConfigureAwait(false);
                var artifact = await CreateArtifactAsync(
                    output,
                    ArtifactKind.TransparentFrame,
                    "image/png",
                    workspace,
                    cancellationToken).ConfigureAwait(false);
                staged.Add((frame.Id, artifact));
                progress?.Report(new PipelineProgress(
                    (index + 1d) / enabled.Length,
                    "Removing backgrounds",
                    index + 1,
                    enabled.Length));
            }

            RemoveTransientArtifacts(project, workspace, ArtifactKind.TransparentFrame, ArtifactKind.NormalizedFrame, ArtifactKind.Thumbnail);
            ClearFrameLinks(project, clearTransparent: true, clearNormalized: true, clearThumbnail: true);
            ApplyFrameArtifacts(project, staged, (links, id) => links with { Transparent = id });
            ResetLoopSelection(project);
        }
        catch
        {
            TryDeleteDirectory(outputDirectory);
            throw;
        }
    }

    public async Task NormalizeFramesAsync(
        ProjectDocument project,
        ProjectWorkspacePaths workspace,
        IProgress<PipelineProgress>? progress,
        CancellationToken cancellationToken)
    {
        var enabled = project.Frames.Where(frame => frame.Enabled).OrderBy(frame => frame.Order).ToArray();
        if (enabled.Length == 0) throw new InvalidOperationException("No enabled frames are available for normalization.");
        if (project.BackgroundRemoval.Enabled && enabled.Any(frame => frame.Artifacts.Transparent is null))
        {
            throw new InvalidOperationException(
                "Background removal is enabled, but one or more enabled frames do not have transparent artifacts. Run Cutout before Align.");
        }

        var outputDirectory = Path.Combine(workspace.Normalized, Guid.NewGuid().ToString("N"));
        var staged = new List<(Guid FrameId, ArtifactRecord Artifact)>(enabled.Length);
        try
        {
            for (var index = 0; index < enabled.Length; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var frame = enabled[index];
                var sourceId = project.BackgroundRemoval.Enabled
                    ? frame.Artifacts.Transparent
                    : frame.Artifacts.Extracted;
                var inputArtifact = ResolveArtifact(project, sourceId);
                var input = workspace.ResolveRelative(inputArtifact.RelativePath);
                var output = Path.Combine(outputDirectory, $"frame_{frame.SourceIndex + 1:D6}.png");
                await frameNormalizer.NormalizeAsync(
                    new NormalizationRequest(input, output, project.Normalization, frame.Transform),
                    cancellationToken).ConfigureAwait(false);
                var artifact = await CreateArtifactAsync(
                    output,
                    ArtifactKind.NormalizedFrame,
                    "image/png",
                    workspace,
                    cancellationToken).ConfigureAwait(false);
                staged.Add((frame.Id, artifact));
                progress?.Report(new PipelineProgress(
                    (index + 1d) / enabled.Length,
                    "Normalizing frames",
                    index + 1,
                    enabled.Length));
            }

            RemoveTransientArtifacts(project, workspace, ArtifactKind.NormalizedFrame, ArtifactKind.Thumbnail);
            ClearFrameLinks(project, clearTransparent: false, clearNormalized: true, clearThumbnail: true);
            ApplyFrameArtifacts(project, staged, (links, id) => links with { Normalized = id });
            ResetLoopSelection(project);
        }
        catch
        {
            TryDeleteDirectory(outputDirectory);
            throw;
        }
    }

    public Task<IReadOnlyList<LoopCandidate>> AnalyzeLoopAsync(
        ProjectDocument project,
        ProjectWorkspacePaths workspace,
        CancellationToken cancellationToken)
    {
        var enabled = project.Frames.Where(frame => frame.Enabled).OrderBy(frame => frame.Order).ToArray();
        if (enabled.Any(frame => frame.Artifacts.Normalized is null))
        {
            throw new InvalidOperationException(
                "One or more enabled frames are not normalized. Run Align before loop analysis.");
        }

        string? Resolve(Guid frameId)
        {
            var frame = project.Frames.FirstOrDefault(candidate => candidate.Id == frameId);
            if (frame?.Artifacts.Normalized is not { } artifactId) return null;
            var artifact = project.Artifacts.FirstOrDefault(candidate => candidate.Id == artifactId);
            return artifact is null ? null : workspace.ResolveRelative(artifact.RelativePath);
        }

        return loopAnalyzer.AnalyzeAsync(project.Frames, Resolve, cancellationToken);
    }
}

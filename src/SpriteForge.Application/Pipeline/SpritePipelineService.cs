using SpriteForge.Application.Frames;
using SpriteForge.Application.Sheets;
using SpriteForge.Core.Contracts;
using SpriteForge.Core.Enums;
using SpriteForge.Core.Models;

namespace SpriteForge.Application.Pipeline;

public sealed class SpritePipelineService(
    IFrameExtractor frameExtractor,
    IBackgroundRemovalService backgroundRemoval,
    IFrameNormalizer frameNormalizer,
    ILoopAnalyzer loopAnalyzer,
    ISpriteSheetExporter sheetExporter,
    IMetadataExporter metadataExporter,
    IIndividualFrameExporter individualFrameExporter,
    IFileHashService hashService,
    SheetLayoutCalculator layoutCalculator,
    FrameSequenceSelector sequenceSelector)
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
                stagedFrames.Add(new FrameRecord(
                    Guid.NewGuid(),
                    index,
                    index,
                    true,
                    1000d / result.Fps,
                    new FrameArtifactLinks(artifact.Id, null, null, null),
                    new FrameTransform(0, 0, 1),
                    new FramePivot(0.5, 1)));
            }

            RemoveTransientArtifacts(project, ArtifactKind.ExtractedFrame, ArtifactKind.TransparentFrame, ArtifactKind.NormalizedFrame, ArtifactKind.Thumbnail);
            project.Artifacts.AddRange(stagedArtifacts);
            project.Frames.Clear();
            project.Frames.AddRange(stagedFrames);
            ResetLoopSelection(project);
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

            RemoveTransientArtifacts(project, ArtifactKind.TransparentFrame, ArtifactKind.NormalizedFrame, ArtifactKind.Thumbnail);
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

            RemoveTransientArtifacts(project, ArtifactKind.NormalizedFrame, ArtifactKind.Thumbnail);
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

    public async Task<string> BuildSheetPreviewAsync(
        ProjectDocument project,
        ProjectWorkspacePaths workspace,
        CancellationToken cancellationToken)
    {
        var sequence = sequenceSelector.Select(project);
        if (sequence.Count == 0) throw new InvalidOperationException("No frames are available for sheet composition.");
        var layout = layoutCalculator.Calculate(sequence, project.Sheet);
        var frames = BuildSheetFrames(project, workspace, sequence);
        var output = Path.Combine(workspace.Analysis, $"sheet-preview-{Guid.NewGuid():N}.png");
        await sheetExporter.ExportAsync(output, layout, frames, cancellationToken).ConfigureAwait(false);
        return output;
    }

    public async Task<ExportPipelineResult> ExportAsync(
        ProjectDocument project,
        ProjectWorkspacePaths workspace,
        ExportPipelineRequest request,
        CancellationToken cancellationToken)
    {
        var safeName = MakeSafeFileName(request.ExportName);
        var destination = Path.GetFullPath(request.DestinationDirectory);
        Directory.CreateDirectory(destination);
        var externalSheet = Path.Combine(destination, safeName + ".png");
        var externalMetadata = Path.Combine(destination, safeName + ".json");
        var externalFramesDirectory = Path.Combine(destination, safeName + "_frames");
        EnsureExternalTargetsAvailable(externalSheet, externalMetadata, externalFramesDirectory, request.IncludeIndividualFrames);
        var externalFramesDirectoryExisted = Directory.Exists(externalFramesDirectory);

        var sequence = sequenceSelector.Select(project);
        if (sequence.Count == 0) throw new InvalidOperationException("No frames are available for export.");
        var layout = layoutCalculator.Calculate(sequence, project.Sheet);
        var frames = BuildSheetFrames(project, workspace, sequence);
        var exportId = Guid.NewGuid();
        var canonicalDirectory = Path.Combine(workspace.Exports, "history", exportId.ToString("N"));
        var canonicalSheet = Path.Combine(canonicalDirectory, safeName + ".png");
        var canonicalMetadata = Path.Combine(canonicalDirectory, safeName + ".json");
        var canonicalFramesDirectory = Path.Combine(canonicalDirectory, "frames");

        try
        {
            Directory.CreateDirectory(canonicalDirectory);
            await sheetExporter.ExportAsync(canonicalSheet, layout, frames, cancellationToken).ConfigureAwait(false);
            await metadataExporter.ExportAsync(
                canonicalMetadata,
                Path.GetFileName(canonicalSheet),
                layout,
                project.Extraction.Fps,
                project.Loop.Enabled,
                cancellationToken).ConfigureAwait(false);

            IReadOnlyList<string> canonicalFramePaths = [];
            if (request.IncludeIndividualFrames)
            {
                canonicalFramePaths = await individualFrameExporter.ExportAsync(
                    frames.Select(frame => frame.Path),
                    canonicalFramesDirectory,
                    safeName,
                    cancellationToken).ConfigureAwait(false);
            }

            await CopyFileAsync(canonicalSheet, externalSheet, cancellationToken).ConfigureAwait(false);
            await CopyFileAsync(canonicalMetadata, externalMetadata, cancellationToken).ConfigureAwait(false);
            var externalFramePaths = request.IncludeIndividualFrames
                ? await CopyFrameExportsAsync(canonicalFramePaths, externalFramesDirectory, cancellationToken).ConfigureAwait(false)
                : [];

            var sheetArtifact = await CreateArtifactAsync(
                canonicalSheet,
                ArtifactKind.SpriteSheet,
                "image/png",
                workspace,
                cancellationToken).ConfigureAwait(false);
            var metadataArtifact = await CreateArtifactAsync(
                canonicalMetadata,
                ArtifactKind.Metadata,
                "application/json",
                workspace,
                cancellationToken).ConfigureAwait(false);
            var frameArtifacts = new List<ArtifactRecord>(canonicalFramePaths.Count);
            foreach (var path in canonicalFramePaths)
            {
                frameArtifacts.Add(await CreateArtifactAsync(
                    path,
                    ArtifactKind.ExportedFrame,
                    "image/png",
                    workspace,
                    cancellationToken).ConfigureAwait(false));
            }

            var record = new ExportRecord(
                exportId,
                DateTimeOffset.UtcNow,
                request.IncludeIndividualFrames ? "png_json_frames" : "png_json",
                sheetArtifact.Id,
                metadataArtifact.Id,
                request.SettingsFingerprint);
            project.Artifacts.Add(sheetArtifact);
            project.Artifacts.Add(metadataArtifact);
            project.Artifacts.AddRange(frameArtifacts);
            project.Exports.Add(record);

            return new ExportPipelineResult(externalSheet, externalMetadata, externalFramePaths, record);
        }
        catch
        {
            TryDeleteDirectory(canonicalDirectory);
            TryDeleteFile(externalSheet);
            TryDeleteFile(externalMetadata);
            if (request.IncludeIndividualFrames && !externalFramesDirectoryExisted) TryDeleteDirectory(externalFramesDirectory);
            throw;
        }
    }

    public IReadOnlyList<FrameRecord> GetExportSequence(ProjectDocument project) => sequenceSelector.Select(project);

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

    private static void RemoveTransientArtifacts(ProjectDocument project, params ArtifactKind[] kinds)
    {
        var set = kinds.ToHashSet();
        project.Artifacts.RemoveAll(artifact => set.Contains(artifact.Kind));
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

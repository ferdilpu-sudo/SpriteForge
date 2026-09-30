using SpriteForge.Core.Enums;
using SpriteForge.Core.Models;

namespace SpriteForge.Application.Pipeline;

public sealed partial class SpritePipelineService
{
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
}

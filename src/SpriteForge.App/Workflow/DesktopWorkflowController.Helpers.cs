using SpriteForge.Core.Enums;
using SpriteForge.Core.Errors;
using SpriteForge.Core.Models;

namespace SpriteForge.App.Workflow;

internal sealed partial class DesktopWorkflowController
{
private string ComputeExportFingerprint(bool includeIndividualFrames)
    {
        var project = CurrentProject;
        var sequence = _services.Pipeline.GetExportSequence(project);
        var frames = sequence.Select(frame =>
        {
            var artifact = frame.Artifacts.Normalized is { } id
                ? project.Artifacts.FirstOrDefault(candidate => candidate.Id == id)
                : null;
            return new
            {
                frame.Id,
                frame.DurationMs,
                frame.Pivot,
                RasterSha256 = artifact?.Sha256 ?? string.Empty
            };
        }).ToArray();

        return _services.Fingerprints.Compute("export", new
        {
            project.Extraction.Fps,
            project.Loop,
            project.Sheet,
            frames,
            ExportIndividualFrames = includeIndividualFrames
        });
    }

    private void MarkDownstreamStale(PipelineStage changedStage)
    {
        foreach (var stage in _services.Invalidations.GetInvalidatedStages(changedStage))
        {
            var item = _viewModel.GetStage(stage);
            if (item.State is StageState.Complete or StageState.Skipped) item.State = StageState.Stale;
        }
    }

    private void AdoptSourceName(string sourcePath)
    {
        if (CurrentProject.Name != "Untitled Sprite") return;
        var name = Path.GetFileNameWithoutExtension(sourcePath).Trim();
        if (!string.IsNullOrWhiteSpace(name)) CurrentProject.Name = name;
    }

    private void ShowError(Exception exception)
    {
        _viewModel.JobStatus.Message = exception switch
        {
            SpriteForgeException spriteError => $"{spriteError.Code}: {spriteError.Message}",
            _ => exception.Message
        };
    }

    private async Task EnsureProjectAsync()
    {
        if (_project is null) await CreateNewProjectAsync();
    }

    private ProjectDocument CurrentProject => _project ?? throw new InvalidOperationException("No project is open.");
    private ProjectWorkspacePaths CurrentWorkspace => _workspace ?? throw new InvalidOperationException("No project workspace is open.");

    private static string MakeSafeExportName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars().ToHashSet();
        var chars = value.Trim().ToLowerInvariant().Select(character =>
            char.IsWhiteSpace(character) ? '_' : invalid.Contains(character) ? '_' : character);
        var result = string.Concat(chars).Trim('_');
        return string.IsNullOrWhiteSpace(result) ? "sprite" : result;
    }
}

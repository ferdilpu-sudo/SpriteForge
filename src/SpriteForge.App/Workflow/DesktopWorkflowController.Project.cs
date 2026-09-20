using SpriteForge.Core.Enums;
using SpriteForge.Core.Models;

namespace SpriteForge.App.Workflow;

internal sealed partial class DesktopWorkflowController
{
    public async Task InitializeAsync()
    {
        if (_project is not null) return;
        await CreateNewProjectAsync().ConfigureAwait(true);
    }

    public async Task CreateNewProjectAsync()
    {
        var project = _services.Projects.Create("Untitled Sprite");
        var baseDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SpriteForge",
            "Projects");
        var root = Path.Combine(baseDirectory, project.ProjectId.ToString("N"));
        var workspace = new ProjectWorkspacePaths(root);
        _services.Workspace.EnsureCreated(workspace);

        _project = project;
        _workspace = workspace;
        _projectFile = Path.Combine(root, "project.spriteforge.json");
        _viewModel.ExportDestination = workspace.Exports;
        await SaveAsync().ConfigureAwait(true);
        RefreshViewModel();
        _viewModel.JobStatus.Message = "New project ready";
    }

    public async Task OpenProjectAsync()
    {
        var projectFile = await _pickers.PickProjectAsync();
        if (string.IsNullOrWhiteSpace(projectFile)) return;
        try
        {
            var project = await _services.Projects.OpenAsync(projectFile).ConfigureAwait(true);
            var root = Path.GetDirectoryName(Path.GetFullPath(projectFile))
                ?? throw new InvalidOperationException("Project file has no parent directory.");
            var workspace = new ProjectWorkspacePaths(root);
            _services.Workspace.EnsureCreated(workspace);
            _project = project;
            _workspace = workspace;
            _projectFile = projectFile;
            _viewModel.ExportDestination = workspace.Exports;
            RefreshViewModel();
            _viewModel.JobStatus.Message = "Project opened";
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    public async Task SaveAsync()
    {
        if (_project is null || _projectFile is null) return;
        await _services.Projects.SaveAsync(_projectFile, _project).ConfigureAwait(true);
        _viewModel.ProjectPath = _projectFile;
    }

    public async Task ImportSourceAsync()
    {
        await EnsureProjectAsync();
        var sourcePath = await _pickers.PickSourceAsync();
        if (string.IsNullOrWhiteSpace(sourcePath)) return;
        try
        {
            var extension = Path.GetExtension(sourcePath).ToLowerInvariant();
            if (extension is ".mp4" or ".webm" or ".mov")
            {
                await _services.SourceImports.ImportVideoAsync(CurrentProject, CurrentWorkspace, sourcePath, CancellationToken.None);
                _viewModel.NavigateTo(PipelineStage.Frames);
            }
            else
            {
                await _services.SourceImports.ImportImageAsync(CurrentProject, CurrentWorkspace, sourcePath, CancellationToken.None);
                _viewModel.NavigateTo(PipelineStage.Animate);
            }

            AdoptSourceName(sourcePath);
            await SaveAsync();
            RefreshViewModel();
            _viewModel.JobStatus.Message = $"Imported {Path.GetFileName(sourcePath)}";
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    public async Task ImportFrameSequenceAsync()
    {
        await EnsureProjectAsync();
        var paths = await _pickers.PickFrameSequenceAsync();
        if (paths.Count == 0) return;
        try
        {
            await _services.SourceImports.ImportFrameSequenceAsync(CurrentProject, CurrentWorkspace, paths, CancellationToken.None);
            if (CurrentProject.Name == "Untitled Sprite") CurrentProject.Name = "Frame Sequence";
            await SaveAsync();
            RefreshViewModel();
            _viewModel.NavigateTo(PipelineStage.Cutout);
            _viewModel.JobStatus.Message = $"Imported {paths.Count} source frames";
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    public async Task ImportDroppedPathsAsync(IReadOnlyList<string> paths)
    {
        await EnsureProjectAsync();
        var files = paths.Where(path => !string.IsNullOrWhiteSpace(path)).ToArray();
        if (files.Length == 0) return;

        try
        {
            if (files.Length > 1)
            {
                var unsupported = files.FirstOrDefault(path => Path.GetExtension(path).ToLowerInvariant() is not (".png" or ".webp"));
                if (unsupported is not null)
                    throw new NotSupportedException("Multiple dropped files must be a PNG/WebP frame sequence.");
                await _services.SourceImports.ImportFrameSequenceAsync(CurrentProject, CurrentWorkspace, files, CancellationToken.None);
                if (CurrentProject.Name == "Untitled Sprite") CurrentProject.Name = "Frame Sequence";
                await SaveAsync();
                RefreshViewModel();
                _viewModel.NavigateTo(PipelineStage.Cutout);
                _viewModel.JobStatus.Message = $"Imported {files.Length} source frames";
                return;
            }

            var sourcePath = files[0];
            var extension = Path.GetExtension(sourcePath).ToLowerInvariant();
            if (extension is ".mp4" or ".webm" or ".mov")
            {
                await _services.SourceImports.ImportVideoAsync(CurrentProject, CurrentWorkspace, sourcePath, CancellationToken.None);
                _viewModel.NavigateTo(PipelineStage.Frames);
            }
            else if (extension is ".png" or ".jpg" or ".jpeg" or ".webp")
            {
                await _services.SourceImports.ImportImageAsync(CurrentProject, CurrentWorkspace, sourcePath, CancellationToken.None);
                _viewModel.NavigateTo(PipelineStage.Animate);
            }
            else
            {
                throw new NotSupportedException($"The '{extension}' file type is not supported.");
            }

            AdoptSourceName(sourcePath);
            await SaveAsync();
            RefreshViewModel();
            _viewModel.JobStatus.Message = $"Imported {Path.GetFileName(sourcePath)}";
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    public async Task ImportVideoInsteadAsync()
    {
        await EnsureProjectAsync();
        var path = await _pickers.PickVideoAsync();
        if (string.IsNullOrWhiteSpace(path)) return;
        try
        {
            await _services.SourceImports.ImportVideoAsync(CurrentProject, CurrentWorkspace, path, CancellationToken.None);
            AdoptSourceName(path);
            await SaveAsync();
            RefreshViewModel();
            _viewModel.NavigateTo(PipelineStage.Frames);
            _viewModel.JobStatus.Message = $"Imported {Path.GetFileName(path)}";
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }


}
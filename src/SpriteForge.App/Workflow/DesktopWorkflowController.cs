using SpriteForge.App.Composition;
using SpriteForge.App.Diagnostics;
using SpriteForge.Core.Models;
using SpriteForge.Presentation.Shell;

namespace SpriteForge.App.Workflow;

internal sealed partial class DesktopWorkflowController
{
    private readonly AppServices _services;
    private readonly ShellViewModel _viewModel;
    private readonly DesktopPickerService _pickers;
    private readonly SemaphoreSlim _uiOperationGate = new(1, 1);
    private CancellationTokenSource? _operationCts;
    private ProjectDocument? _project;
    private ProjectWorkspacePaths? _workspace;
    private string? _projectFile;

    public DesktopWorkflowController(
        Microsoft.UI.Xaml.Window window,
        AppServices services,
        ShellViewModel viewModel)
    {
        _services = services;
        _viewModel = viewModel;
        _pickers = new DesktopPickerService(window);
    }

    public ProjectDocument? Project => _project;
    public ProjectWorkspacePaths? Workspace => _workspace;

    public async Task ExecuteUiOperationAsync(string operationName, Func<Task> operation)
    {
        await _uiOperationGate.WaitAsync().ConfigureAwait(true);
        try
        {
            await operation().ConfigureAwait(true);
        }
        catch (OperationCanceledException)
        {
            _viewModel.JobStatus.Message = $"{operationName} cancelled";
        }
        catch (Exception ex)
        {
            StartupLog.Write($"Interactive operation '{operationName}' failed.", ex);
            ShowError(ex);
        }
        finally
        {
            _uiOperationGate.Release();
        }
    }
}

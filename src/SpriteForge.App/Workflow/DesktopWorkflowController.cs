using SpriteForge.App.Composition;
using SpriteForge.Core.Models;
using SpriteForge.Presentation.Shell;

namespace SpriteForge.App.Workflow;

internal sealed partial class DesktopWorkflowController
{
    private readonly AppServices _services;
    private readonly ShellViewModel _viewModel;
    private readonly DesktopPickerService _pickers;
    private CancellationTokenSource? _operationCts;
    private ProjectDocument? _project;
    private ProjectWorkspacePaths? _workspace;
    private string? _projectFile;

    public DesktopWorkflowController(Microsoft.UI.Xaml.Window window, AppServices services, ShellViewModel viewModel)
    {
        _services = services;
        _viewModel = viewModel;
        _pickers = new DesktopPickerService(window);
    }

    public ProjectDocument? Project => _project;
    public ProjectWorkspacePaths? Workspace => _workspace;

}

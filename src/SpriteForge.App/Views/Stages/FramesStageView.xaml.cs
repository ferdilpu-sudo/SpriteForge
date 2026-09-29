using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SpriteForge.App.Workflow;

namespace SpriteForge.App.Views.Stages;

public sealed partial class FramesStageView : UserControl
{
    private DesktopWorkflowController? _workflow;

    public FramesStageView() => InitializeComponent();
    internal FramesStageView(DesktopWorkflowController workflow) : this() => _workflow = workflow;

    private async void OnExtractClick(object sender, RoutedEventArgs e)
    {
        if (_workflow is null) return;
        await _workflow.ExecuteUiOperationAsync("Extract frames", _workflow.ExtractFramesAsync);
    }
}

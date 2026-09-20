using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SpriteForge.App.Workflow;

namespace SpriteForge.App.Views.Stages;

public sealed partial class CutoutStageView : UserControl
{
    private DesktopWorkflowController? _workflow;

    public CutoutStageView() => InitializeComponent();
    internal CutoutStageView(DesktopWorkflowController workflow) : this() => _workflow = workflow;

    private async void OnProcessAllClick(object sender, RoutedEventArgs e)
    {
        if (_workflow is not null) await _workflow.RemoveBackgroundsAsync();
    }
}

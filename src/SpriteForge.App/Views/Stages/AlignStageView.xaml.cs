using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SpriteForge.App.Workflow;

namespace SpriteForge.App.Views.Stages;

public sealed partial class AlignStageView : UserControl
{
    private DesktopWorkflowController? _workflow;

    public AlignStageView() => InitializeComponent();
    internal AlignStageView(DesktopWorkflowController workflow) : this() => _workflow = workflow;

    private async void OnApplyPivotClick(object sender, RoutedEventArgs e)
    {
        if (_workflow is not null) await _workflow.SetSelectedFramePivotAsync();
    }

    private async void OnApplyAllClick(object sender, RoutedEventArgs e)
    {
        if (_workflow is not null) await _workflow.NormalizeFramesAsync();
    }
}

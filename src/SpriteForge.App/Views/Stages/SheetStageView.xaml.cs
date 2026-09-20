using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SpriteForge.App.Workflow;

namespace SpriteForge.App.Views.Stages;

public sealed partial class SheetStageView : UserControl
{
    private DesktopWorkflowController? _workflow;

    public SheetStageView() => InitializeComponent();
    internal SheetStageView(DesktopWorkflowController workflow) : this() => _workflow = workflow;

    private async void OnBuildClick(object sender, RoutedEventArgs e)
    {
        if (_workflow is not null) await _workflow.BuildSheetAsync();
    }
}

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SpriteForge.App.Workflow;

namespace SpriteForge.App.Views.Stages;

public sealed partial class ExportStageView : UserControl
{
    private DesktopWorkflowController? _workflow;

    public ExportStageView() => InitializeComponent();
    internal ExportStageView(DesktopWorkflowController workflow) : this() => _workflow = workflow;

    private async void OnBrowseClick(object sender, RoutedEventArgs e)
    {
        if (_workflow is not null) await _workflow.ChooseExportDestinationAsync();
    }

    private async void OnExportClick(object sender, RoutedEventArgs e)
    {
        if (_workflow is not null) await _workflow.ExportAsync();
    }
}

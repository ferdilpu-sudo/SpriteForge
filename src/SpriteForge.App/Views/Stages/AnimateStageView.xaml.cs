using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SpriteForge.App.Workflow;

namespace SpriteForge.App.Views.Stages;

public sealed partial class AnimateStageView : UserControl
{
    private DesktopWorkflowController? _workflow;

    public AnimateStageView() => InitializeComponent();
    internal AnimateStageView(DesktopWorkflowController workflow) : this() => _workflow = workflow;

    private async void OnImportVideoClick(object sender, RoutedEventArgs e)
    {
        if (_workflow is not null) await _workflow.ImportVideoInsteadAsync();
    }
}

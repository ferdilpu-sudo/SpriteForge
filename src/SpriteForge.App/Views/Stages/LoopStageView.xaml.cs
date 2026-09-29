using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SpriteForge.App.Workflow;

namespace SpriteForge.App.Views.Stages;

public sealed partial class LoopStageView : UserControl
{
    private DesktopWorkflowController? _workflow;

    public LoopStageView() => InitializeComponent();
    internal LoopStageView(DesktopWorkflowController workflow) : this() => _workflow = workflow;

    private async void OnAnalyzeClick(object sender, RoutedEventArgs e)
    {
        if (_workflow is null) return;
        await _workflow.ExecuteUiOperationAsync("Analyze loop", _workflow.AnalyzeLoopAsync);
    }

    private async void OnApplyManualClick(object sender, RoutedEventArgs e)
    {
        if (_workflow is null) return;
        await _workflow.ExecuteUiOperationAsync("Apply loop range", _workflow.ApplyManualLoopAsync);
    }

    private async void OnUseSuggestionClick(object sender, RoutedEventArgs e)
    {
        if (_workflow is null) return;
        await _workflow.ExecuteUiOperationAsync("Apply loop suggestion", _workflow.UseSelectedLoopSuggestionAsync);
    }
}

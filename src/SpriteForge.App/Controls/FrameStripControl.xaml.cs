using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SpriteForge.App.Workflow;
using SpriteForge.Presentation.Frames;

namespace SpriteForge.App.Controls;

public sealed partial class FrameStripControl : UserControl
{
    internal DesktopWorkflowController? Workflow { get; set; }

    public FrameStripControl() => InitializeComponent();

    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        Workflow?.SelectFrame(FrameList.SelectedItem as FrameThumbnailViewModel);
    }

    private async void OnToggleClick(object sender, RoutedEventArgs e)
    {
        if (Workflow is not null) await Workflow.ToggleSelectedFrameAsync();
    }

    private async void OnMoveLeftClick(object sender, RoutedEventArgs e)
    {
        if (Workflow is not null) await Workflow.MoveSelectedFrameLeftAsync();
    }

    private async void OnMoveRightClick(object sender, RoutedEventArgs e)
    {
        if (Workflow is not null) await Workflow.MoveSelectedFrameRightAsync();
    }

    private async void OnDuplicateClick(object sender, RoutedEventArgs e)
    {
        if (Workflow is not null) await Workflow.DuplicateSelectedFrameAsync();
    }

    private async void OnRemoveClick(object sender, RoutedEventArgs e)
    {
        if (Workflow is not null) await Workflow.RemoveSelectedFrameAsync();
    }

    private async void OnResetClick(object sender, RoutedEventArgs e)
    {
        if (Workflow is not null) await Workflow.ResetFrameSequenceAsync();
    }

    private async void OnApplyDurationClick(object sender, RoutedEventArgs e)
    {
        if (Workflow is not null) await Workflow.SetSelectedFrameDurationAsync();
    }
}

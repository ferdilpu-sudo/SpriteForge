using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
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
        if (Workflow is null) return;
        await Workflow.ExecuteUiOperationAsync("Toggle frame", Workflow.ToggleSelectedFrameAsync);
    }

    private async void OnMoveLeftClick(object sender, RoutedEventArgs e)
    {
        if (Workflow is null) return;
        await Workflow.ExecuteUiOperationAsync("Move frame left", Workflow.MoveSelectedFrameLeftAsync);
    }

    private async void OnMoveRightClick(object sender, RoutedEventArgs e)
    {
        if (Workflow is null) return;
        await Workflow.ExecuteUiOperationAsync("Move frame right", Workflow.MoveSelectedFrameRightAsync);
    }

    private async void OnDuplicateClick(object sender, RoutedEventArgs e)
    {
        if (Workflow is null) return;
        await Workflow.ExecuteUiOperationAsync("Duplicate frame", Workflow.DuplicateSelectedFrameAsync);
    }

    private async void OnRemoveClick(object sender, RoutedEventArgs e)
    {
        if (Workflow is null) return;
        await Workflow.ExecuteUiOperationAsync("Remove frame", Workflow.RemoveSelectedFrameAsync);
    }

    private async void OnResetClick(object sender, RoutedEventArgs e)
    {
        if (Workflow is null) return;
        await Workflow.ExecuteUiOperationAsync("Reset frame sequence", Workflow.ResetFrameSequenceAsync);
    }

    private async void OnDeleteDisabledClick(object sender, RoutedEventArgs e)
    {
        if (Workflow is null) return;
        var count = Workflow.GetDisabledFrameCount();
        if (count == 0) return;

        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = $"Delete {count} disabled frames?",
            Content = "Unused frame files will be removed from this project. Re-extract the source video if you need them again.",
            PrimaryButtonText = "Delete",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close
        };

        if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;
        await Workflow.ExecuteUiOperationAsync(
            "Delete disabled frames",
            Workflow.DeleteDisabledFramesAsync);
    }

    private void OnFrameListPointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        var scrollViewer = FindScrollViewer(FrameList);
        if (scrollViewer is null || scrollViewer.ScrollableWidth <= 0) return;

        var delta = e.GetCurrentPoint(FrameList).Properties.MouseWheelDelta;
        var target = Math.Clamp(
            scrollViewer.HorizontalOffset - delta,
            0,
            scrollViewer.ScrollableWidth);
        scrollViewer.ChangeView(target, null, null, disableAnimation: true);
        e.Handled = true;
    }

    private static ScrollViewer? FindScrollViewer(DependencyObject root)
    {
        if (root is ScrollViewer viewer) return viewer;
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var index = 0; index < count; index++)
        {
            var result = FindScrollViewer(VisualTreeHelper.GetChild(root, index));
            if (result is not null) return result;
        }
        return null;
    }

    private async void OnApplyDurationClick(object sender, RoutedEventArgs e)
    {
        if (Workflow is null) return;
        await Workflow.ExecuteUiOperationAsync("Set frame duration", Workflow.SetSelectedFrameDurationAsync);
    }
}

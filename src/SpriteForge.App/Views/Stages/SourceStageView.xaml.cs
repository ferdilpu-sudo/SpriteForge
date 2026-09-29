using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SpriteForge.App.Workflow;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;

namespace SpriteForge.App.Views.Stages;

public sealed partial class SourceStageView : UserControl
{
    private DesktopWorkflowController? _workflow;

    public SourceStageView() => InitializeComponent();

    internal SourceStageView(DesktopWorkflowController workflow) : this() => _workflow = workflow;

    private async void OnChooseSourceClick(object sender, RoutedEventArgs e)
    {
        if (_workflow is null) return;
        await _workflow.ExecuteUiOperationAsync("Import source", _workflow.ImportSourceAsync);
    }

    private async void OnImportSequenceClick(object sender, RoutedEventArgs e)
    {
        if (_workflow is null) return;
        await _workflow.ExecuteUiOperationAsync("Import frame sequence", _workflow.ImportFrameSequenceAsync);
    }

    private void OnSourceDragOver(object sender, DragEventArgs e)
    {
        e.AcceptedOperation = DataPackageOperation.Copy;
        e.DragUIOverride.Caption = "Import into SpriteForge";
        e.DragUIOverride.IsCaptionVisible = true;
    }

    private async void OnSourceDrop(object sender, DragEventArgs e)
    {
        if (_workflow is null) return;

        await _workflow.ExecuteUiOperationAsync("Import dropped source", async () =>
        {
            if (!e.DataView.Contains(StandardDataFormats.StorageItems)) return;
            var items = await e.DataView.GetStorageItemsAsync();
            var paths = items
                .OfType<StorageFile>()
                .Select(file => file.Path)
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .ToArray();
            await _workflow.ImportDroppedPathsAsync(paths);
        });
    }
}

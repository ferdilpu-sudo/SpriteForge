using Microsoft.UI.Xaml;
using Microsoft.Windows.Storage.Pickers;

namespace SpriteForge.App.Workflow;

internal sealed class DesktopPickerService(Window window)
{
    public Task<string?> PickSourceAsync() =>
        PickSingleFileAsync([".png", ".jpg", ".jpeg", ".webp", ".mp4", ".webm", ".mov"]);

    public Task<string?> PickVideoAsync() =>
        PickSingleFileAsync([".mp4", ".webm", ".mov"]);

    public Task<string?> PickProjectAsync() =>
        PickSingleFileAsync([".json"]);

    public async Task<IReadOnlyList<string>> PickFrameSequenceAsync()
    {
        var picker = new FileOpenPicker(window.AppWindow.Id)
        {
            SuggestedStartLocation = PickerLocationId.PicturesLibrary
        };
        picker.FileTypeFilter.Add(".png");
        picker.FileTypeFilter.Add(".webp");

        var files = await picker.PickMultipleFilesAsync();
        return files
            .Select(file => file.Path)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .ToArray();
    }

    public async Task<string?> PickExportFolderAsync()
    {
        var picker = new FolderPicker(window.AppWindow.Id)
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary
        };
        var folder = await picker.PickSingleFolderAsync();
        return folder?.Path;
    }

    private async Task<string?> PickSingleFileAsync(IEnumerable<string> extensions)
    {
        var picker = new FileOpenPicker(window.AppWindow.Id)
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary
        };
        foreach (var extension in extensions) picker.FileTypeFilter.Add(extension);

        var file = await picker.PickSingleFileAsync();
        return file?.Path;
    }
}

using Microsoft.UI.Xaml;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace SpriteForge.App.Workflow;

internal sealed class DesktopPickerService(Window window)
{
    public Task<string?> PickSourceAsync() => PickSingleFileAsync([".png", ".jpg", ".jpeg", ".webp", ".mp4", ".webm", ".mov"]);

    public Task<string?> PickVideoAsync() => PickSingleFileAsync([".mp4", ".webm", ".mov"]);

    public Task<string?> PickProjectAsync() => PickSingleFileAsync([".json"]);

    public async Task<IReadOnlyList<string>> PickFrameSequenceAsync()
    {
        var picker = new FileOpenPicker { SuggestedStartLocation = PickerLocationId.PicturesLibrary };
        picker.FileTypeFilter.Add(".png");
        picker.FileTypeFilter.Add(".webp");
        Initialize(picker);
        var files = await picker.PickMultipleFilesAsync();
        return files.Select(file => file.Path).Where(path => !string.IsNullOrWhiteSpace(path)).ToArray();
    }

    public async Task<string?> PickExportFolderAsync()
    {
        var picker = new FolderPicker { SuggestedStartLocation = PickerLocationId.DocumentsLibrary };
        picker.FileTypeFilter.Add("*");
        Initialize(picker);
        var folder = await picker.PickSingleFolderAsync();
        return folder?.Path;
    }

    private async Task<string?> PickSingleFileAsync(IEnumerable<string> extensions)
    {
        var picker = new FileOpenPicker { SuggestedStartLocation = PickerLocationId.DocumentsLibrary };
        foreach (var extension in extensions) picker.FileTypeFilter.Add(extension);
        Initialize(picker);
        var file = await picker.PickSingleFileAsync();
        return file?.Path;
    }

    private void Initialize(object picker)
    {
        var hwnd = WindowNative.GetWindowHandle(window);
        InitializeWithWindow.Initialize(picker, hwnd);
    }
}

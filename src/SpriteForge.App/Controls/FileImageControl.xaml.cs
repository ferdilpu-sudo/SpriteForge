using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage;

namespace SpriteForge.App.Controls;

public sealed partial class FileImageControl : UserControl
{
    private long _loadVersion;
    public static readonly DependencyProperty ImagePathProperty = DependencyProperty.Register(
        nameof(ImagePath),
        typeof(string),
        typeof(FileImageControl),
        new PropertyMetadata(null, OnImagePathChanged));

    public FileImageControl() => InitializeComponent();

    public string? ImagePath
    {
        get => (string?)GetValue(ImagePathProperty);
        set => SetValue(ImagePathProperty, value);
    }

    private static void OnImagePathChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        if (dependencyObject is FileImageControl control)
            _ = control.LoadImageAsync(args.NewValue as string);
    }

    private async Task LoadImageAsync(string? path)
    {
        var loadVersion = Interlocked.Increment(ref _loadVersion);
        ImageElement.Source = null;
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return;

        try
        {
            var file = await StorageFile.GetFileFromPathAsync(path);
            using var stream = await file.OpenAsync(FileAccessMode.Read);
            var bitmap = new BitmapImage();
            await bitmap.SetSourceAsync(stream);
            if (loadVersion == Volatile.Read(ref _loadVersion)) ImageElement.Source = bitmap;
        }
        catch
        {
            if (loadVersion == Volatile.Read(ref _loadVersion)) ImageElement.Source = null;
        }
    }
}

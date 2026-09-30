using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

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

    private static void OnImagePathChanged(
        DependencyObject dependencyObject,
        DependencyPropertyChangedEventArgs args)
    {
        if (dependencyObject is FileImageControl control)
            _ = control.LoadImageAsync(args.NewValue as string);
    }

    private async Task LoadImageAsync(string? path)
    {
        var loadVersion = Interlocked.Increment(ref _loadVersion);
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            ImageElement.Source = null;
            return;
        }

        var bitmap = await PreviewImageCache.GetAsync(path);
        if (loadVersion != Volatile.Read(ref _loadVersion))
            return;

        ImageElement.Source = bitmap;
    }
}

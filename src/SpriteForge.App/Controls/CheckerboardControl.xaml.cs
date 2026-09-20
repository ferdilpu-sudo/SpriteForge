using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

namespace SpriteForge.App.Controls;

public sealed partial class CheckerboardControl : UserControl
{
    private const double TileSize = 18;

    public static readonly DependencyProperty ImagePathProperty = DependencyProperty.Register(
        nameof(ImagePath),
        typeof(string),
        typeof(CheckerboardControl),
        new PropertyMetadata(null, OnImagePathChanged));

    public static readonly DependencyProperty ZoomProperty = DependencyProperty.Register(
        nameof(Zoom),
        typeof(double),
        typeof(CheckerboardControl),
        new PropertyMetadata(1d, OnZoomChanged));

    public CheckerboardControl() => InitializeComponent();

    public string? ImagePath
    {
        get => (string?)GetValue(ImagePathProperty);
        set => SetValue(ImagePathProperty, value);
    }

    public double Zoom
    {
        get => (double)GetValue(ZoomProperty);
        set => SetValue(ZoomProperty, value);
    }

    private static void OnZoomChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        if (dependencyObject is not CheckerboardControl control) return;
        var zoom = Math.Clamp(args.NewValue is double value ? value : 1d, 0.25, 4d);
        control.PreviewScale.ScaleX = zoom;
        control.PreviewScale.ScaleY = zoom;
    }

    private static void OnImagePathChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        if (dependencyObject is not CheckerboardControl control) return;
        control.PreviewImage.ImagePath = args.NewValue as string;
        control.Placeholder.Visibility = string.IsNullOrWhiteSpace(args.NewValue as string)
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        Tiles.Children.Clear();
        var columns = (int)Math.Ceiling(e.NewSize.Width / TileSize);
        var rows = (int)Math.Ceiling(e.NewSize.Height / TileSize);
        var light = new SolidColorBrush(ColorHelper.FromArgb(255, 42, 42, 42));
        var dark = new SolidColorBrush(ColorHelper.FromArgb(255, 34, 34, 34));

        for (var row = 0; row < rows; row++)
        {
            for (var column = 0; column < columns; column++)
            {
                var rectangle = new Rectangle
                {
                    Width = TileSize,
                    Height = TileSize,
                    Fill = (row + column) % 2 == 0 ? light : dark
                };
                Canvas.SetLeft(rectangle, column * TileSize);
                Canvas.SetTop(rectangle, row * TileSize);
                Tiles.Children.Add(rectangle);
            }
        }
    }
}

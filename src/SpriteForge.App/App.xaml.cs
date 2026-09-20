using Microsoft.UI.Xaml;
using SpriteForge.App.Composition;

namespace SpriteForge.App;

public partial class App : Microsoft.UI.Xaml.Application
{
    private Window? _window;

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var services = AppComposition.Build();
        _window = new MainWindow(services);
        _window.Activate();
    }
}

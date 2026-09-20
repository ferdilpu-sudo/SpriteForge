using Microsoft.UI.Xaml;
using SpriteForge.App.Composition;
using SpriteForge.App.Diagnostics;

namespace SpriteForge.App;

public partial class App : Microsoft.UI.Xaml.Application
{
    private Window? _window;

    public App()
    {
        UnhandledException += OnUnhandledException;

        try
        {
            InitializeComponent();
            StartupLog.Write($"App initialized. BaseDirectory={AppContext.BaseDirectory}");
        }
        catch (Exception ex)
        {
            StartupLog.Write("App InitializeComponent failed.", ex);
            throw;
        }
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            StartupLog.Write("Application launch entered.");
            XamlStartupProbe.Run();
            var services = AppComposition.Build();
            _window = new MainWindow(services);
            _window.Activate();
            StartupLog.Write("Main window activated.");
        }
        catch (Exception ex)
        {
            StartupLog.Write("Application launch failed.", ex);
            throw;
        }
    }

    private static void OnUnhandledException(
        object sender,
        Microsoft.UI.Xaml.UnhandledExceptionEventArgs args)
    {
        StartupLog.Write("Unhandled WinUI exception.", args.Exception);
    }
}
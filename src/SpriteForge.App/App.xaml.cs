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
        AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

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

    private static void OnDomainUnhandledException(
        object? sender,
        System.UnhandledExceptionEventArgs args)
    {
        var exception = args.ExceptionObject as Exception
            ?? new InvalidOperationException($"Unhandled non-Exception object: {args.ExceptionObject}");
        StartupLog.Write(
            $"Unhandled AppDomain exception. IsTerminating={args.IsTerminating}.",
            exception);
    }

    private static void OnUnobservedTaskException(
        object? sender,
        UnobservedTaskExceptionEventArgs args)
    {
        StartupLog.Write("Unobserved task exception.", args.Exception);
    }
}

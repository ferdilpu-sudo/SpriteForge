using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SpriteForge.App.Composition;
using SpriteForge.App.Workflow;
using SpriteForge.Presentation.Shell;

namespace SpriteForge.App;

public sealed partial class MainWindow : Window
{
    private readonly AppServices _services;
    private readonly DesktopWorkflowController _workflow;
    private readonly DispatcherTimer _previewTimer = new();
    private bool _initialized;

    public MainWindow(AppServices services)
    {
        _services = services;
        InitializeComponent();
        ViewModel = new ShellViewModel();
        _workflow = new DesktopWorkflowController(this, services, ViewModel);
        FrameStrip.Workflow = _workflow;
        RootGrid.DataContext = ViewModel;
        StageList.SelectedItem = ViewModel.SelectedStage;
        ShowSelectedStage();
        ConfigurePreviewTimer();
        Title = "SpriteForge";
    }

    public ShellViewModel ViewModel { get; }

    private async void OnRootLoaded(object sender, RoutedEventArgs e)
    {
        if (_initialized) return;
        _initialized = true;
        await _workflow.InitializeAsync();
        StageList.SelectedItem = ViewModel.SelectedStage;
        ShowSelectedStage();
        await RunStartupDiagnosticsAsync();
    }

    private void OnStageSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (StageList.SelectedItem is PipelineStageItemViewModel selected)
        {
            ViewModel.SelectedStage = selected;
            ShowSelectedStage();
        }
    }

    private async void OnNewProjectClick(object sender, RoutedEventArgs e)
    {
        StopPreview();
        await _workflow.CreateNewProjectAsync();
        StageList.SelectedItem = ViewModel.SelectedStage;
        ShowSelectedStage();
    }

    private async void OnOpenProjectClick(object sender, RoutedEventArgs e)
    {
        StopPreview();
        await _workflow.OpenProjectAsync();
        StageList.SelectedItem = ViewModel.SelectedStage;
        ShowSelectedStage();
    }

    private async void OnSaveProjectClick(object sender, RoutedEventArgs e)
    {
        try
        {
            await _workflow.SaveAsync();
            ViewModel.JobStatus.Message = "Project saved";
        }
        catch (Exception ex)
        {
            ViewModel.JobStatus.Message = ex.Message;
        }
    }

    private async void OnDiagnosticsClick(object sender, RoutedEventArgs e)
    {
        ViewModel.JobStatus.Message = "Checking local dependencies…";
        var ffmpeg = await _services.FfmpegDiagnostics.CheckAsync();
        var background = await _services.BackgroundRemovalDiagnostics.CheckAsync();
        var workspace = _workflow.Workspace is null
            ? null
            : await _services.WorkspaceDiagnostics.CheckWriteAccessAsync(_workflow.Workspace.RootPath);
        var workspaceState = workspace is null ? "N/A" : workspace.Passed ? "OK" : "Read-only";
        ViewModel.JobStatus.Message =
            $"FFmpeg: {(ffmpeg.Passed ? "OK" : "Missing")} · Background: {(background.Passed ? "OK" : "Setup required")} · Workspace: {workspaceState}";
    }


    private async Task RunStartupDiagnosticsAsync()
    {
        var ffmpeg = await _services.FfmpegDiagnostics.CheckAsync();
        var background = await _services.BackgroundRemovalDiagnostics.CheckAsync();
        var workspace = _workflow.Workspace is null
            ? null
            : await _services.WorkspaceDiagnostics.CheckWriteAccessAsync(_workflow.Workspace.RootPath);

        var issues = new List<string>();
        if (!ffmpeg.Passed) issues.Add("FFmpeg missing");
        if (!background.Passed) issues.Add("background worker setup required");
        if (workspace is { Passed: false }) issues.Add("workspace is not writable");
        if (issues.Count > 0) ViewModel.JobStatus.Message = $"Local setup: {string.Join(" · ", issues)}";
    }

    private void OnPreviewPlayClick(object sender, RoutedEventArgs e)
    {
        if (_previewTimer.IsEnabled)
        {
            StopPreview();
            return;
        }

        var fps = Math.Clamp(ViewModel.PreviewFps, 0.25, 120);
        _previewTimer.Interval = TimeSpan.FromSeconds(1d / fps);
        _previewTimer.Start();
        PreviewPlayButton.Content = "❚❚";
    }

    private void OnCancelClick(object sender, RoutedEventArgs e) => _workflow.CancelCurrentOperation();

    private void ConfigurePreviewTimer()
    {
        _previewTimer.Interval = TimeSpan.FromMilliseconds(83);
        _previewTimer.Tick += (_, _) =>
        {
            if (!_workflow.AdvancePreview(ViewModel.LoopEnabled)) StopPreview();
        };
    }

    private void StopPreview()
    {
        _previewTimer.Stop();
        PreviewPlayButton.Content = "▶";
    }

    private void ShowSelectedStage()
    {
        var view = StageViewFactory.Create(ViewModel.SelectedStage.Stage, _workflow);
        view.DataContext = ViewModel;
        StageHost.Content = view;
    }
}

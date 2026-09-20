using CommunityToolkit.Mvvm.ComponentModel;

namespace SpriteForge.Presentation.Jobs;

public sealed partial class JobStatusViewModel : ObservableObject
{
    [ObservableProperty] private string message = "Ready";
    [ObservableProperty] private double progress;
    [ObservableProperty] private bool isRunning;
    [ObservableProperty] private bool canCancel;
}

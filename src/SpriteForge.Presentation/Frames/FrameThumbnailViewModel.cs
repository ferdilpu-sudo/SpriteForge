using CommunityToolkit.Mvvm.ComponentModel;

namespace SpriteForge.Presentation.Frames;

public sealed partial class FrameThumbnailViewModel : ObservableObject
{
    public FrameThumbnailViewModel(Guid frameId, int sourceIndex, int order, bool enabled, string? previewPath)
    {
        FrameId = frameId;
        SourceIndex = sourceIndex;
        Order = order;
        Enabled = enabled;
        PreviewPath = previewPath;
    }

    public Guid FrameId { get; }
    public int SourceIndex { get; }
    public int DisplayNumber => Order + 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayNumber))]
    private int order;

    [ObservableProperty]
    private bool enabled;

    [ObservableProperty]
    private string? previewPath;
}

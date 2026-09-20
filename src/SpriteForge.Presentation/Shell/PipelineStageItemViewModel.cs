using CommunityToolkit.Mvvm.ComponentModel;
using SpriteForge.Core.Enums;

namespace SpriteForge.Presentation.Shell;

public sealed partial class PipelineStageItemViewModel : ObservableObject
{
    public PipelineStageItemViewModel(int number, PipelineStage stage, string label, StageState state)
    {
        Number = number;
        Stage = stage;
        Label = label;
        State = state;
    }

    public int Number { get; }
    public PipelineStage Stage { get; }
    public string Label { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StateGlyph))]
    private StageState state;

    public string StateGlyph => State switch
    {
        StageState.Complete => "✓",
        StageState.Active => "●",
        StageState.Stale => "↻",
        StageState.Error => "!",
        StageState.Skipped => "–",
        _ => string.Empty
    };
}

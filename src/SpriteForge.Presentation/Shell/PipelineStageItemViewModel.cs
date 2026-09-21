using CommunityToolkit.Mvvm.ComponentModel;
using SpriteForge.Core.Enums;

namespace SpriteForge.Presentation.Shell;

public sealed partial class PipelineStageItemViewModel : ObservableObject
{
    public PipelineStageItemViewModel(
        int number,
        PipelineStage stage,
        string label,
        string neutralLabel,
        StageState state)
    {
        Number = number;
        Stage = stage;
        Label = label;
        NeutralLabel = neutralLabel;
        State = state;
    }

    public int Number { get; }
    public PipelineStage Stage { get; }
    public string Label { get; }
    public string NeutralLabel { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StateGlyph))]
    [NotifyPropertyChangedFor(nameof(StateLabel))]
    private StageState state;

    public string StateGlyph => State switch
    {
        StageState.Complete => "✓",
        StageState.Active => "●",
        StageState.Stale => "↻",
        StageState.Error => "!",
        StageState.Skipped => "–",
        _ => "○"
    };

    public string StateLabel => State switch
    {
        StageState.Complete => "Complete",
        StageState.Active => "Active",
        StageState.Stale => "Needs update",
        StageState.Error => "Error",
        StageState.Skipped => "Skipped",
        _ => NeutralLabel
    };
}

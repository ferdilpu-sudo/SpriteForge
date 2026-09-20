using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using SpriteForge.Core.Enums;
using SpriteForge.Presentation.Frames;
using SpriteForge.Presentation.Jobs;
using SpriteForge.Presentation.Looping;

namespace SpriteForge.Presentation.Shell;

public sealed partial class ShellViewModel : ObservableObject
{
    public ShellViewModel()
    {
        Stages = new ObservableCollection<PipelineStageItemViewModel>
        {
            new(1, PipelineStage.Source, "Source", StageState.Active),
            new(2, PipelineStage.Animate, "Animate", StageState.Neutral),
            new(3, PipelineStage.Frames, "Frames", StageState.Neutral),
            new(4, PipelineStage.Cutout, "Cutout", StageState.Neutral),
            new(5, PipelineStage.Align, "Align", StageState.Neutral),
            new(6, PipelineStage.Loop, "Loop", StageState.Neutral),
            new(7, PipelineStage.Sheet, "Sheet", StageState.Neutral),
            new(8, PipelineStage.Export, "Export", StageState.Neutral)
        };
        SelectedStage = Stages[0];
    }

    public ObservableCollection<PipelineStageItemViewModel> Stages { get; }
    public ObservableCollection<FrameThumbnailViewModel> Frames { get; } = [];
    public ObservableCollection<LoopCandidateViewModel> LoopCandidates { get; } = [];
    public JobStatusViewModel JobStatus { get; } = new();

    [ObservableProperty] private string projectName = "Untitled Sprite";
    [ObservableProperty] private string projectPath = string.Empty;
    [ObservableProperty] private PipelineStageItemViewModel selectedStage;
    [ObservableProperty] private string sourceSummary = "No source imported";
    [ObservableProperty] private string sourceKind = "None";
    [ObservableProperty] private string? previewImagePath;
    [ObservableProperty] private double previewFps = 12;
    [ObservableProperty] private double previewZoom = 1;
    [ObservableProperty] private bool loopEnabled = true;
    [ObservableProperty] private FrameThumbnailViewModel? selectedFrame;
    [ObservableProperty] private int currentFrameIndex;
    [ObservableProperty] private double selectedFrameDurationMs = 83.333;
    [ObservableProperty] private double selectedFramePivotX = 0.5;
    [ObservableProperty] private double selectedFramePivotY = 1;

    [ObservableProperty] private double extractionStartSeconds;
    [ObservableProperty] private double extractionEndSeconds = 3;
    [ObservableProperty] private bool extractionHasEndTime = true;

    [ObservableProperty] private bool backgroundRemovalEnabled = true;
    [ObservableProperty] private double alphaThreshold = 0.05;

    [ObservableProperty] private double canvasWidth = 512;
    [ObservableProperty] private double canvasHeight = 512;
    [ObservableProperty] private string fitMode = "contain";
    [ObservableProperty] private string anchor = "bottom_center";
    [ObservableProperty] private bool autoTrim = true;

    [ObservableProperty] private double loopStart = 1;
    [ObservableProperty] private double loopEnd = 1;
    [ObservableProperty] private LoopCandidateViewModel? selectedLoopCandidate;

    [ObservableProperty] private double sheetCellWidth = 512;
    [ObservableProperty] private double sheetCellHeight = 512;
    [ObservableProperty] private double sheetColumns;
    [ObservableProperty] private double sheetPadding;
    [ObservableProperty] private double sheetSpacing;
    [ObservableProperty] private bool powerOfTwoSheet;
    [ObservableProperty] private string sheetSummary = "No sheet built";

    [ObservableProperty] private string exportName = "sprite";
    [ObservableProperty] private string exportDestination = string.Empty;
    [ObservableProperty] private bool exportIndividualFrames;
    [ObservableProperty] private string exportSummary = "Nothing exported yet";

    public string FramePosition => Frames.Count == 0
        ? "No frames"
        : $"Frame {Math.Clamp(CurrentFrameIndex + 1, 1, Frames.Count)}/{Frames.Count}";

    public PipelineStageItemViewModel GetStage(PipelineStage stage) =>
        Stages.First(item => item.Stage == stage);

    public void SetStageState(PipelineStage stage, StageState state) => GetStage(stage).State = state;

    public void NavigateTo(PipelineStage stage)
    {
        SelectedStage = GetStage(stage);
    }

    public void ReplaceFrames(IEnumerable<FrameThumbnailViewModel> frames)
    {
        Frames.Clear();
        foreach (var frame in frames.OrderBy(frame => frame.Order)) Frames.Add(frame);
        CurrentFrameIndex = Frames.Count == 0 ? 0 : Math.Clamp(CurrentFrameIndex, 0, Frames.Count - 1);
        SelectedFrame = Frames.Count == 0 ? null : Frames[CurrentFrameIndex];
        OnPropertyChanged(nameof(FramePosition));
    }

    public void ReplaceLoopCandidates(IEnumerable<LoopCandidateViewModel> candidates)
    {
        LoopCandidates.Clear();
        foreach (var candidate in candidates) LoopCandidates.Add(candidate);
        SelectedLoopCandidate = LoopCandidates.FirstOrDefault();
    }

    partial void OnCurrentFrameIndexChanged(int value) => OnPropertyChanged(nameof(FramePosition));
}

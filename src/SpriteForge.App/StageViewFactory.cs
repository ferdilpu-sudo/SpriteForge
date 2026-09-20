using Microsoft.UI.Xaml.Controls;
using SpriteForge.App.Views.Stages;
using SpriteForge.App.Workflow;
using SpriteForge.Core.Enums;

namespace SpriteForge.App;

internal static class StageViewFactory
{
    public static UserControl Create(PipelineStage stage, DesktopWorkflowController workflow) => stage switch
    {
        PipelineStage.Source => new SourceStageView(workflow),
        PipelineStage.Animate => new AnimateStageView(workflow),
        PipelineStage.Frames => new FramesStageView(workflow),
        PipelineStage.Cutout => new CutoutStageView(workflow),
        PipelineStage.Align => new AlignStageView(workflow),
        PipelineStage.Loop => new LoopStageView(workflow),
        PipelineStage.Sheet => new SheetStageView(workflow),
        PipelineStage.Export => new ExportStageView(workflow),
        _ => throw new ArgumentOutOfRangeException(nameof(stage), stage, null)
    };
}

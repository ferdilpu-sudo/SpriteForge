using SpriteForge.Core.Enums;

namespace SpriteForge.Application.Pipeline;

public sealed class PipelineInvalidationService
{
    private static readonly IReadOnlyDictionary<PipelineStage, PipelineStage[]> Downstream =
        new Dictionary<PipelineStage, PipelineStage[]>
        {
            [PipelineStage.Source] = [PipelineStage.Animate, PipelineStage.Frames, PipelineStage.Cutout, PipelineStage.Align, PipelineStage.Loop, PipelineStage.Sheet, PipelineStage.Export],
            [PipelineStage.Animate] = [PipelineStage.Frames, PipelineStage.Cutout, PipelineStage.Align, PipelineStage.Loop, PipelineStage.Sheet, PipelineStage.Export],
            [PipelineStage.Frames] = [PipelineStage.Cutout, PipelineStage.Align, PipelineStage.Loop, PipelineStage.Sheet, PipelineStage.Export],
            [PipelineStage.Cutout] = [PipelineStage.Align, PipelineStage.Loop, PipelineStage.Sheet, PipelineStage.Export],
            [PipelineStage.Align] = [PipelineStage.Loop, PipelineStage.Sheet, PipelineStage.Export],
            [PipelineStage.Loop] = [PipelineStage.Sheet, PipelineStage.Export],
            [PipelineStage.Sheet] = [PipelineStage.Export],
            [PipelineStage.Export] = []
        };

    public IReadOnlyList<PipelineStage> GetInvalidatedStages(PipelineStage changedStage) => Downstream[changedStage];
}

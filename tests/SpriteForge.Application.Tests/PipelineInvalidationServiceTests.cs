using SpriteForge.Application.Pipeline;
using SpriteForge.Core.Enums;

namespace SpriteForge.Application.Tests;

public sealed class PipelineInvalidationServiceTests
{
    [Fact]
    public void FramesChange_InvalidatesOnlyDownstreamStages()
    {
        var result = new PipelineInvalidationService().GetInvalidatedStages(PipelineStage.Frames);

        Assert.Equal(
            [PipelineStage.Cutout, PipelineStage.Align, PipelineStage.Loop, PipelineStage.Sheet, PipelineStage.Export],
            result);
        Assert.DoesNotContain(PipelineStage.Source, result);
        Assert.DoesNotContain(PipelineStage.Animate, result);
    }
}

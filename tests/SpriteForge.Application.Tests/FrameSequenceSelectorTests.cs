using SpriteForge.Application.Frames;
using SpriteForge.Core.Models;

namespace SpriteForge.Application.Tests;

public sealed class FrameSequenceSelectorTests
{
    [Fact]
    public void Select_UsesEnabledFramesInOrder_WhenLoopIsDisabled()
    {
        var project = new ProjectDocument { Loop = new LoopSettings(false, null, null, false) };
        var first = Frame(0, order: 2);
        var disabled = Frame(1, order: 0, enabled: false);
        var second = Frame(2, order: 1);
        project.Frames.AddRange([first, disabled, second]);

        var result = new FrameSequenceSelector().Select(project);

        Assert.Equal(new[] { second.Id, first.Id }, result.Select(frame => frame.Id));
    }

    [Fact]
    public void Select_UsesExactInclusiveLoopRange()
    {
        var project = new ProjectDocument();
        var frames = Enumerable.Range(0, 5).Select(index => Frame(index, index)).ToArray();
        project.Frames.AddRange(frames);
        project.Loop = new LoopSettings(true, frames[1].Id, frames[3].Id, false);

        var result = new FrameSequenceSelector().Select(project);

        Assert.Equal(new[] { frames[1].Id, frames[2].Id, frames[3].Id }, result.Select(frame => frame.Id));
    }

    [Fact]
    public void Select_Throws_WhenLoopReferencesDisabledFrame()
    {
        var project = new ProjectDocument();
        var first = Frame(0, 0);
        var disabled = Frame(1, 1, enabled: false);
        project.Frames.AddRange([first, disabled]);
        project.Loop = new LoopSettings(true, first.Id, disabled.Id, false);

        var exception = Assert.Throws<InvalidOperationException>(() => new FrameSequenceSelector().Select(project));

        Assert.True(exception.Message.Contains("disabled or missing", StringComparison.OrdinalIgnoreCase));
    }

    private static FrameRecord Frame(int sourceIndex, int order, bool enabled = true) => new(
        Guid.NewGuid(), sourceIndex, order, enabled, 83.333,
        new FrameArtifactLinks(null, null, null, null),
        new FrameTransform(0, 0, 1),
        new FramePivot(0.5, 1));
}

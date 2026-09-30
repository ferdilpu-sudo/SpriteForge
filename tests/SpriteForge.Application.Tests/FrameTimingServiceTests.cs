using SpriteForge.Application.Frames;
using SpriteForge.Core.Models;

namespace SpriteForge.Application.Tests;

public sealed class FrameTimingServiceTests
{
    [Fact]
    public void PreserveEnabledTotalDuration_RescalesEnabledFramesOnly()
    {
        var project = new ProjectDocument();
        project.Frames.Add(Frame(0, true, 100));
        project.Frames.Add(Frame(1, true, 100));
        project.Frames.Add(Frame(2, false, 90));

        var service = new FrameTimingService();
        service.PreserveEnabledTotalDuration(project, 300);

        Assert.Equal(150d, project.Frames[0].DurationMs, precision: 6);
        Assert.Equal(150d, project.Frames[1].DurationMs, precision: 6);
        Assert.Equal(90d, project.Frames[2].DurationMs, precision: 6);
        Assert.Equal(300d, service.GetEnabledTotalDurationMs(project), precision: 6);
    }

    [Fact]
    public void PreserveEnabledTotalDuration_ThrowsWhenNoFrameIsEnabled()
    {
        var project = new ProjectDocument();
        project.Frames.Add(Frame(0, false, 100));

        Assert.Throws<InvalidOperationException>(() =>
            new FrameTimingService().PreserveEnabledTotalDuration(project, 100));
    }

    private static FrameRecord Frame(
        int index,
        bool enabled,
        double durationMs) =>
        new(
            Guid.NewGuid(),
            index,
            index,
            enabled,
            durationMs,
            new FrameArtifactLinks(null, null, null, null),
            new FrameTransform(0, 0, 1),
            new FramePivot(0.5, 1));
}

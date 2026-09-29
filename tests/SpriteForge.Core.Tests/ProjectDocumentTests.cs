using SpriteForge.Core.Models;

namespace SpriteForge.Core.Tests;

public sealed class ProjectDocumentTests
{
    [Fact]
    public void NewProject_UsesCurrentSchemaAndSafeDefaults()
    {
        var project = new ProjectDocument();

        Assert.Equal(ProjectDocument.CurrentSchemaVersion, project.SchemaVersion);
        Assert.Equal(12, project.Extraction.Fps);
        Assert.Equal("balanced", project.FrameOptimization.Mode);
        Assert.Equal(0.96, project.FrameOptimization.SimilarityThreshold, precision: 6);
        Assert.True(project.FrameOptimization.PreserveMotionPeaks);
        Assert.True(project.BackgroundRemoval.Enabled);
        Assert.Equal("contain", project.Normalization.Fit);
        Assert.True(project.Loop.Enabled);
    }
}

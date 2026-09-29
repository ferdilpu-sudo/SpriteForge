using SpriteForge.Application.Projects;
using SpriteForge.Core.Errors;
using SpriteForge.Core.Models;

namespace SpriteForge.Application.Tests;

public sealed class ProjectDocumentValidatorTests
{
    private readonly ProjectDocumentValidator _validator = new();

    [Fact]
    public void Validate_DefaultProject_Passes()
    {
        _validator.Validate(new ProjectDocument());
    }

    [Fact]
    public void Validate_NullNormalization_RejectsProject()
    {
        var project = new ProjectDocument
        {
            Normalization = null!
        };

        var exception = Assert.Throws<SpriteForgeException>(() => _validator.Validate(project));

        Assert.Equal("PROJECT_INVALID", exception.Code);
        Assert.Contains("Normalization settings", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_MissingFrameArtifactReference_RejectsProject()
    {
        var project = new ProjectDocument();
        project.Frames.Add(new FrameRecord(
            Guid.NewGuid(),
            0,
            0,
            true,
            83.333,
            new FrameArtifactLinks(Guid.NewGuid(), null, null, null),
            new FrameTransform(0, 0, 1),
            new FramePivot(0.5, 1)));

        var exception = Assert.Throws<SpriteForgeException>(() => _validator.Validate(project));

        Assert.Equal("PROJECT_INVALID", exception.Code);
        Assert.Contains("missing artifact", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_NonFiniteFrameDuration_RejectsProject()
    {
        var project = new ProjectDocument();
        var artifactId = Guid.NewGuid();
        project.Artifacts.Add(new ArtifactRecord(
            artifactId,
            SpriteForge.Core.Enums.ArtifactKind.ExtractedFrame,
            "cache/extracted/frame.png",
            "image/png",
            null,
            null,
            null,
            "abc123",
            DateTimeOffset.UtcNow));
        project.Frames.Add(new FrameRecord(
            Guid.NewGuid(),
            0,
            0,
            true,
            double.NaN,
            new FrameArtifactLinks(artifactId, null, null, null),
            new FrameTransform(0, 0, 1),
            new FramePivot(0.5, 1)));

        var exception = Assert.Throws<SpriteForgeException>(() => _validator.Validate(project));

        Assert.Equal("PROJECT_INVALID", exception.Code);
        Assert.Contains("duration", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}

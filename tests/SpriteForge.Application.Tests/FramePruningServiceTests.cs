using SpriteForge.Application.Frames;
using SpriteForge.Core.Enums;
using SpriteForge.Core.Models;

namespace SpriteForge.Application.Tests;

public sealed class FramePruningServiceTests
{
    [Fact]
    public void DeleteDisabledFrames_RemovesUnusedFiles_AndPreservesSharedArtifacts()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            "spriteforge-frame-pruning",
            Guid.NewGuid().ToString("N"));
        var workspace = new ProjectWorkspacePaths(root);
        Directory.CreateDirectory(workspace.Extracted);

        try
        {
            var shared = CreateArtifact(workspace, "shared.png");
            var unused = CreateArtifact(workspace, "unused.png");
            var project = new ProjectDocument();
            project.Artifacts.AddRange([shared, unused]);
            project.Frames.Add(Frame(0, true, shared.Id));
            project.Frames.Add(Frame(1, false, shared.Id));
            project.Frames.Add(Frame(2, false, unused.Id));
            project.Loop = project.Loop with
            {
                StartFrameId = project.Frames[0].Id,
                EndFrameId = project.Frames[0].Id,
                Recommended = true
            };

            var result = new FramePruningService().DeleteDisabledFrames(project, workspace);

            Assert.Equal(2, result.RemovedFrames);
            Assert.Single(project.Frames);
            Assert.Equal(0, project.Frames[0].Order);
            Assert.Contains(project.Artifacts, artifact => artifact.Id == shared.Id);
            Assert.DoesNotContain(project.Artifacts, artifact => artifact.Id == unused.Id);
            Assert.True(File.Exists(workspace.ResolveRelative(shared.RelativePath)));
            Assert.False(File.Exists(workspace.ResolveRelative(unused.RelativePath)));
            Assert.Null(project.Loop.StartFrameId);
            Assert.Null(project.Loop.EndFrameId);
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    private static ArtifactRecord CreateArtifact(
        ProjectWorkspacePaths workspace,
        string fileName)
    {
        var path = Path.Combine(workspace.Extracted, fileName);
        File.WriteAllBytes(path, [1, 2, 3]);
        return new ArtifactRecord(
            Guid.NewGuid(),
            ArtifactKind.ExtractedFrame,
            workspace.ToRelative(path),
            "image/png",
            null,
            null,
            null,
            "fixture",
            DateTimeOffset.UtcNow);
    }

    private static FrameRecord Frame(
        int index,
        bool enabled,
        Guid artifactId) =>
        new(
            Guid.NewGuid(),
            index,
            index,
            enabled,
            83.333,
            new FrameArtifactLinks(artifactId, null, null, null),
            new FrameTransform(0, 0, 1),
            new FramePivot(0.5, 1));
}

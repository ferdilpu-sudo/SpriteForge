using SpriteForge.Core.Models;

namespace SpriteForge.Core.Tests;

public sealed class ProjectWorkspacePathsTests
{
    [Fact]
    public void ResolveRelative_ReturnsPathInsideWorkspace()
    {
        var root = Path.Combine(Path.GetTempPath(), "spriteforge-workspace-tests", Guid.NewGuid().ToString("N"));
        var workspace = new ProjectWorkspacePaths(root);

        var result = workspace.ResolveRelative(Path.Combine("cache", "normalized", "frame.png"));

        Assert.Equal(
            Path.GetFullPath(Path.Combine(root, "cache", "normalized", "frame.png")),
            result);
    }

    [Fact]
    public void ResolveRelative_RejectsTraversalOutsideWorkspace()
    {
        var root = Path.Combine(Path.GetTempPath(), "spriteforge-workspace-tests", Guid.NewGuid().ToString("N"));
        var workspace = new ProjectWorkspacePaths(root);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            workspace.ResolveRelative(Path.Combine("..", "outside.png")));

        Assert.True(exception.Message.Contains("escapes", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ToRelative_RejectsPathOutsideWorkspace()
    {
        var parent = Path.Combine(Path.GetTempPath(), "spriteforge-workspace-tests", Guid.NewGuid().ToString("N"));
        var root = Path.Combine(parent, "project");
        var workspace = new ProjectWorkspacePaths(root);
        var outside = Path.Combine(parent, "outside.png");

        var exception = Assert.Throws<InvalidOperationException>(() => workspace.ToRelative(outside));

        Assert.True(exception.Message.Contains("escapes", StringComparison.OrdinalIgnoreCase));
    }
}

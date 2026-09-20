using SpriteForge.Core.Models;

namespace SpriteForge.Infrastructure.Workspace;

public sealed class WorkspaceInitializer
{
    public void EnsureCreated(ProjectWorkspacePaths workspace)
    {
        foreach (var path in new[]
        {
            workspace.RootPath,
            workspace.Source,
            workspace.GeneratedVideo,
            workspace.Extracted,
            workspace.Transparent,
            workspace.Normalized,
            workspace.Thumbnails,
            workspace.Analysis,
            workspace.Exports,
            workspace.Logs
        })
        {
            Directory.CreateDirectory(path);
        }
    }
}

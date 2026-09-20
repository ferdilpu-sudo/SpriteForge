using SpriteForge.Core.Models;

namespace SpriteForge.Core.Contracts;

public interface IProjectRepository
{
    Task<ProjectDocument> LoadAsync(string projectFile, CancellationToken cancellationToken);
    Task SaveAsync(string projectFile, ProjectDocument project, CancellationToken cancellationToken);
}

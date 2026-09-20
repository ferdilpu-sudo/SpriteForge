using SpriteForge.Core.Contracts;
using SpriteForge.Core.Models;

namespace SpriteForge.Application.Projects;

public sealed class ProjectService(IProjectRepository repository)
{
    public ProjectDocument Create(string name)
    {
        return new ProjectDocument { Name = string.IsNullOrWhiteSpace(name) ? "Untitled Sprite" : name.Trim() };
    }

    public Task<ProjectDocument> OpenAsync(string projectFile, CancellationToken cancellationToken = default) =>
        repository.LoadAsync(projectFile, cancellationToken);

    public async Task SaveAsync(string projectFile, ProjectDocument project, CancellationToken cancellationToken = default)
    {
        project.UpdatedAt = DateTimeOffset.UtcNow;
        await repository.SaveAsync(projectFile, project, cancellationToken).ConfigureAwait(false);
    }
}

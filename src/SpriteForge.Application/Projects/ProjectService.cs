using SpriteForge.Core.Contracts;
using SpriteForge.Core.Models;

namespace SpriteForge.Application.Projects;

public sealed class ProjectService(
    IProjectRepository repository,
    ProjectDocumentValidator? validator = null)
{
    private readonly ProjectDocumentValidator _validator = validator ?? new ProjectDocumentValidator();

    public ProjectDocument Create(string name)
    {
        return new ProjectDocument { Name = string.IsNullOrWhiteSpace(name) ? "Untitled Sprite" : name.Trim() };
    }

    public async Task<ProjectDocument> OpenAsync(
        string projectFile,
        CancellationToken cancellationToken = default)
    {
        var project = await repository.LoadAsync(projectFile, cancellationToken).ConfigureAwait(false);
        _validator.Validate(project);
        return project;
    }

    public async Task SaveAsync(
        string projectFile,
        ProjectDocument project,
        CancellationToken cancellationToken = default)
    {
        _validator.Validate(project);
        project.UpdatedAt = DateTimeOffset.UtcNow;
        await repository.SaveAsync(projectFile, project, cancellationToken).ConfigureAwait(false);
    }
}

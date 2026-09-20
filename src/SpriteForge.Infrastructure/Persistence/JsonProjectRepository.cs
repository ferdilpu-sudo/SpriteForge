using System.Text.Json;
using SpriteForge.Core.Contracts;
using SpriteForge.Core.Errors;
using SpriteForge.Core.Models;

namespace SpriteForge.Infrastructure.Persistence;

public sealed class JsonProjectRepository : IProjectRepository
{
    private readonly JsonSerializerOptions _json = ProjectJsonOptions.Create();

    public async Task<ProjectDocument> LoadAsync(string projectFile, CancellationToken cancellationToken)
    {
        if (!File.Exists(projectFile))
        {
            throw new FileNotFoundException("SpriteForge project file was not found.", projectFile);
        }

        await using var stream = File.OpenRead(projectFile);
        using var raw = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
        var version = ReadSchemaVersion(raw.RootElement);
        if (version > ProjectDocument.CurrentSchemaVersion)
        {
            throw new SpriteForgeException(
                "PROJECT_SCHEMA_NEWER",
                $"This project uses schema version {version}, but this build supports version {ProjectDocument.CurrentSchemaVersion}.",
                recoverable: false);
        }

        if (version < ProjectDocument.CurrentSchemaVersion)
        {
            throw new SpriteForgeException(
                "PROJECT_MIGRATION_REQUIRED",
                $"Project schema version {version} requires a migration before it can be opened.");
        }

        var project = raw.RootElement.Deserialize<ProjectDocument>(_json)
            ?? throw new SpriteForgeException("PROJECT_INVALID", "Project metadata could not be read.");
        ValidateArtifactPaths(projectFile, project);
        return project;
    }

    public async Task SaveAsync(string projectFile, ProjectDocument project, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(projectFile))
            ?? throw new ArgumentException("Project path must have a parent directory.", nameof(projectFile));
        Directory.CreateDirectory(directory);

        var tempFile = Path.Combine(directory, $".{Path.GetFileName(projectFile)}.{Guid.NewGuid():N}.tmp");
        try
        {
            await using (var stream = new FileStream(tempFile, FileMode.CreateNew, FileAccess.Write, FileShare.None, 64 * 1024, true))
            {
                await JsonSerializer.SerializeAsync(stream, project, _json, cancellationToken).ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }

            File.Move(tempFile, projectFile, overwrite: true);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }


    private static void ValidateArtifactPaths(string projectFile, ProjectDocument project)
    {
        var root = Path.GetDirectoryName(Path.GetFullPath(projectFile))
            ?? throw new SpriteForgeException("PROJECT_INVALID", "Project file has no parent directory.");
        var workspace = new ProjectWorkspacePaths(root);

        foreach (var artifact in project.Artifacts)
        {
            try
            {
                _ = workspace.ResolveRelative(artifact.RelativePath);
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
            {
                throw new SpriteForgeException(
                    "PROJECT_ARTIFACT_PATH_INVALID",
                    $"Artifact {artifact.Id} contains an invalid workspace path.",
                    recoverable: false,
                    inner: ex);
            }
        }
    }

    private static int ReadSchemaVersion(JsonElement root)
    {
        if (!root.TryGetProperty("schemaVersion", out var value) || !value.TryGetInt32(out var version))
        {
            throw new SpriteForgeException("PROJECT_SCHEMA_MISSING", "Project metadata is missing schemaVersion.");
        }

        return version;
    }
}

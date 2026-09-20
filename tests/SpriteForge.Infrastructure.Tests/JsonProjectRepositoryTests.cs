using SpriteForge.Core.Models;
using SpriteForge.Infrastructure.Persistence;

namespace SpriteForge.Infrastructure.Tests;

public sealed class JsonProjectRepositoryTests
{
    [Fact]
    public async Task SaveAndLoad_RoundTripsProjectMetadata()
    {
        var root = Path.Combine(Path.GetTempPath(), "spriteforge-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var file = Path.Combine(root, "project.spriteforge.json");

        try
        {
            var repository = new JsonProjectRepository();
            var expected = new ProjectDocument { Name = "Round Trip" };
            await repository.SaveAsync(file, expected, TestContext.Current.CancellationToken);

            var actual = await repository.LoadAsync(file, TestContext.Current.CancellationToken);

            Assert.Equal(expected.ProjectId, actual.ProjectId);
            Assert.Equal("Round Trip", actual.Name);
            Assert.Equal(ProjectDocument.CurrentSchemaVersion, actual.SchemaVersion);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
    [Fact]
    public async Task Load_RejectsArtifactPathTraversal()
    {
        var root = Path.Combine(Path.GetTempPath(), "spriteforge-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var file = Path.Combine(root, "project.spriteforge.json");

        try
        {
            var artifactId = Guid.NewGuid();
            var json = $$"""
            {
              "schemaVersion": 1,
              "projectId": "{{Guid.NewGuid()}}",
              "name": "Unsafe",
              "createdAt": "2026-09-20T00:00:00Z",
              "updatedAt": "2026-09-20T00:00:00Z",
              "artifacts": [
                {
                  "id": "{{artifactId}}",
                  "kind": "source_image",
                  "relativePath": "../outside.png",
                  "mimeType": "image/png",
                  "sha256": "deadbeef",
                  "createdAt": "2026-09-20T00:00:00Z"
                }
              ],
              "frames": [],
              "exports": []
            }
            """;
            await File.WriteAllTextAsync(file, json, TestContext.Current.CancellationToken);

            var exception = await Assert.ThrowsAsync<SpriteForge.Core.Errors.SpriteForgeException>(() =>
                new JsonProjectRepository().LoadAsync(file, TestContext.Current.CancellationToken));

            Assert.Equal("PROJECT_ARTIFACT_PATH_INVALID", exception.Code);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

}
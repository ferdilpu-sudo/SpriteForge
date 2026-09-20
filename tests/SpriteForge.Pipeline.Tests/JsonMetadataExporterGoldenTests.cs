using System.Text.Json.Nodes;
using SpriteForge.Core.Models;
using SpriteForge.Export.Metadata;

namespace SpriteForge.Pipeline.Tests;

public sealed class JsonMetadataExporterGoldenTests
{
    [Fact]
    public async Task ExportAsync_MatchesCanonicalGoldenMetadata()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var root = Path.Combine(Path.GetTempPath(), "spriteforge-metadata-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            var layout = new SheetLayout(
                68,
                34,
                2,
                1,
                [
                    new SheetCell(
                        0,
                        Guid.Parse("11111111-1111-1111-1111-111111111111"),
                        1,
                        1,
                        32,
                        32,
                        100,
                        new FramePivot(0.5, 1)),
                    new SheetCell(
                        1,
                        Guid.Parse("22222222-2222-2222-2222-222222222222"),
                        35,
                        1,
                        32,
                        32,
                        125,
                        new FramePivot(0.5, 0.75))
                ]);

            var outputPath = Path.Combine(root, "hero.json");
            await new JsonMetadataExporter().ExportAsync(
                outputPath,
                "hero.png",
                layout,
                defaultFps: 10,
                loop: true,
                cancellationToken);

            var expectedPath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "expected-metadata.json");
            var expected = JsonNode.Parse(await File.ReadAllTextAsync(expectedPath, cancellationToken));
            var actual = JsonNode.Parse(await File.ReadAllTextAsync(outputPath, cancellationToken));

            Assert.True(
                JsonNode.DeepEquals(expected, actual),
                $"Metadata output changed. Expected:{Environment.NewLine}{expected}{Environment.NewLine}Actual:{Environment.NewLine}{actual}");
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }
}

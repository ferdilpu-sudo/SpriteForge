using System.Text.Json;
using SpriteForge.Core.Contracts;
using SpriteForge.Core.Models;

namespace SpriteForge.Export.Metadata;

public sealed class JsonMetadataExporter : IMetadataExporter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public async Task ExportAsync(
        string outputPath,
        string imageFileName,
        SheetLayout layout,
        double defaultFps,
        bool loop,
        CancellationToken cancellationToken)
    {
        var payload = new
        {
            format = "spriteforge.sprite_sheet",
            version = 1,
            image = imageFileName,
            width = layout.Width,
            height = layout.Height,
            cellWidth = layout.Cells[0].Width,
            cellHeight = layout.Cells[0].Height,
            columns = layout.Columns,
            rows = layout.Rows,
            frameCount = layout.Cells.Count,
            loop,
            defaultFps,
            frames = layout.Cells.Select(cell => new
            {
                index = cell.Index,
                x = cell.X,
                y = cell.Y,
                width = cell.Width,
                height = cell.Height,
                durationMs = cell.DurationMs,
                pivot = new { x = cell.Pivot.X, y = cell.Pivot.Y }
            })
        };

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath))!);
        await using var stream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, 32 * 1024, true);
        await JsonSerializer.SerializeAsync(stream, payload, JsonOptions, cancellationToken).ConfigureAwait(false);
    }
}

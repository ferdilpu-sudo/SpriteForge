using SkiaSharp;
using SpriteForge.Core.Contracts;
using SpriteForge.Core.Models;

namespace SpriteForge.Export.Sheets;

public sealed class PngSpriteSheetExporter : ISpriteSheetExporter
{
    public Task ExportAsync(
        string outputPath,
        SheetLayout layout,
        IReadOnlyList<SheetFrame> frames,
        CancellationToken cancellationToken) =>
        Task.Run(() => ExportCore(outputPath, layout, frames, cancellationToken), cancellationToken);

    private static void ExportCore(
        string outputPath,
        SheetLayout layout,
        IReadOnlyList<SheetFrame> frames,
        CancellationToken cancellationToken)
    {
        var frameById = frames.ToDictionary(frame => frame.FrameId);
        var imageInfo = new SKImageInfo(layout.Width, layout.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var sheet = new SKBitmap(imageInfo);
        using var canvas = new SKCanvas(sheet);
        canvas.Clear(SKColors.Transparent);

        foreach (var cell in layout.Cells)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!frameById.TryGetValue(cell.FrameId, out var source))
                throw new InvalidOperationException($"No raster frame exists for {cell.FrameId}.");

            using var image = SKBitmap.Decode(source.Path)
                ?? throw new InvalidDataException($"Unable to decode normalized frame '{source.Path}'.");
            if (image.Width != cell.Width || image.Height != cell.Height)
                throw new InvalidOperationException($"Frame {cell.FrameId} does not match the configured cell size.");

            canvas.DrawBitmap(image, cell.X, cell.Y);
        }

        canvas.Flush();
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath))!);
        using var encodedImage = SKImage.FromBitmap(sheet);
        using var encoded = encodedImage.Encode(SKEncodedImageFormat.Png, 100)
            ?? throw new InvalidOperationException("SkiaSharp failed to encode the sprite sheet.");
        using var stream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);
        encoded.SaveTo(stream);
    }
}

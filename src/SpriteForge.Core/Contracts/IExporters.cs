using SpriteForge.Core.Models;

namespace SpriteForge.Core.Contracts;

public interface ISpriteSheetExporter
{
    Task ExportAsync(
        string outputPath,
        SheetLayout layout,
        IReadOnlyList<SheetFrame> frames,
        CancellationToken cancellationToken);
}

public interface IMetadataExporter
{
    Task ExportAsync(
        string outputPath,
        string imageFileName,
        SheetLayout layout,
        double defaultFps,
        bool loop,
        CancellationToken cancellationToken);
}

public interface IIndividualFrameExporter
{
    Task<IReadOnlyList<string>> ExportAsync(
        IEnumerable<string> orderedFrames,
        string destinationDirectory,
        string baseName,
        CancellationToken cancellationToken);
}

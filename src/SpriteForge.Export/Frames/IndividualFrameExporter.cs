using SpriteForge.Core.Contracts;

namespace SpriteForge.Export.Frames;

public sealed class IndividualFrameExporter : IIndividualFrameExporter
{
    public async Task<IReadOnlyList<string>> ExportAsync(
        IEnumerable<string> orderedFrames,
        string destinationDirectory,
        string baseName,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(destinationDirectory);
        var outputs = new List<string>();
        var index = 0;
        foreach (var frame in orderedFrames)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var output = Path.Combine(destinationDirectory, $"{baseName}_{index:D4}.png");
            if (File.Exists(output)) throw new IOException($"Export target already exists: {output}");

            await using var source = new FileStream(frame, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024, true);
            await using var target = new FileStream(output, FileMode.CreateNew, FileAccess.Write, FileShare.None, 64 * 1024, true);
            await source.CopyToAsync(target, cancellationToken).ConfigureAwait(false);
            outputs.Add(output);
            index++;
        }
        return outputs;
    }
}

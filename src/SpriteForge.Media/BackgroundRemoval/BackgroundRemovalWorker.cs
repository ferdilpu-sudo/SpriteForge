using SpriteForge.Core.Contracts;
using SpriteForge.Core.Errors;
using SpriteForge.Core.Models;

namespace SpriteForge.Media.BackgroundRemoval;

public sealed class BackgroundRemovalWorker(
    IExternalProcessRunner processRunner,
    string pythonExecutable,
    string workerScript) : IBackgroundRemovalService
{
    public async Task<ProcessedFrame> RemoveAsync(
        string inputPath,
        string outputPath,
        BackgroundRemovalOptions options,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(inputPath)) throw new FileNotFoundException("Input frame was not found.", inputPath);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath))!);

        var tempPath = outputPath + $".{Guid.NewGuid():N}.tmp.png";
        var args = new[]
        {
            workerScript,
            "--input", inputPath,
            "--output", tempPath,
            "--alpha-threshold", options.AlphaThreshold.ToString(System.Globalization.CultureInfo.InvariantCulture)
        };

        try
        {
            var result = await processRunner.RunAsync(pythonExecutable, args, null, cancellationToken).ConfigureAwait(false);
            if (!result.Succeeded || !File.Exists(tempPath))
            {
                throw new SpriteForgeException(
                    "BACKGROUND_REMOVAL_FAILED",
                    "Local background removal failed.",
                    inner: new InvalidOperationException(result.StandardError.Trim()));
            }

            File.Move(tempPath, outputPath, overwrite: true);
            return new ProcessedFrame(inputPath, outputPath);
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }
}

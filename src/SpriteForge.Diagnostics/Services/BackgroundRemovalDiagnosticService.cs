using SpriteForge.Core.Contracts;
using SpriteForge.Diagnostics.Models;

namespace SpriteForge.Diagnostics.Services;

public sealed class BackgroundRemovalDiagnosticService(
    IExternalProcessRunner processRunner,
    string pythonPath,
    string workerScriptPath)
{
    public async Task<DiagnosticCheck> CheckAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(workerScriptPath))
            return new DiagnosticCheck("background_worker", false, "Background-removal worker script is missing.", workerScriptPath);
        if (!File.Exists(pythonPath))
            return new DiagnosticCheck("background_worker", false, "Background-removal Python environment is not installed.", pythonPath);

        try
        {
            var result = await processRunner.RunAsync(
                pythonPath,
                ["-c", "import rembg; print('rembg ok')"],
                null,
                cancellationToken).ConfigureAwait(false);
            return result.Succeeded
                ? new DiagnosticCheck("background_worker", true, "Background removal is available.", result.StandardOutput.Trim())
                : new DiagnosticCheck("background_worker", false, "Background removal dependency check failed.", result.StandardError.Trim());
        }
        catch (Exception ex)
        {
            return new DiagnosticCheck("background_worker", false, "Background removal dependency check failed.", ex.Message);
        }
    }
}

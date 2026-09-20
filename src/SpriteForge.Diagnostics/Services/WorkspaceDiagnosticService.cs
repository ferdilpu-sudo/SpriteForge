using SpriteForge.Diagnostics.Models;

namespace SpriteForge.Diagnostics.Services;

public sealed class WorkspaceDiagnosticService
{
    public async Task<DiagnosticCheck> CheckWriteAccessAsync(string directory, CancellationToken cancellationToken = default)
    {
        try
        {
            Directory.CreateDirectory(directory);
            var probe = Path.Combine(directory, $".spriteforge-write-probe-{Guid.NewGuid():N}.tmp");
            await File.WriteAllTextAsync(probe, "ok", cancellationToken).ConfigureAwait(false);
            File.Delete(probe);
            return new DiagnosticCheck("workspace_write", true, "Workspace is writable.");
        }
        catch (Exception ex)
        {
            return new DiagnosticCheck("workspace_write", false, "Workspace is not writable.", ex.Message);
        }
    }
}

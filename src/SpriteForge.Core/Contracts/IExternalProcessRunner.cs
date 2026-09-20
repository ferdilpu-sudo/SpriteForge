using SpriteForge.Core.Models;

namespace SpriteForge.Core.Contracts;

public interface IExternalProcessRunner
{
    Task<ExternalProcessResult> RunAsync(
        string executable,
        IEnumerable<string> arguments,
        string? workingDirectory,
        CancellationToken cancellationToken);
}

namespace SpriteForge.Core.Models;

public sealed record ExternalProcessResult(int ExitCode, string StandardOutput, string StandardError)
{
    public bool Succeeded => ExitCode == 0;
}

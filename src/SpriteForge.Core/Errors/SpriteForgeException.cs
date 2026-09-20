namespace SpriteForge.Core.Errors;

public sealed class SpriteForgeException : Exception
{
    public SpriteForgeException(string code, string message, bool recoverable = true, Exception? inner = null)
        : base(message, inner)
    {
        Code = code;
        Recoverable = recoverable;
    }

    public string Code { get; }
    public bool Recoverable { get; }
}

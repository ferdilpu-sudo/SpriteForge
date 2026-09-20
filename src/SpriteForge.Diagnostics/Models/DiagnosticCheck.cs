namespace SpriteForge.Diagnostics.Models;

public sealed record DiagnosticCheck(string Id, bool Passed, string Summary, string? Detail = null);

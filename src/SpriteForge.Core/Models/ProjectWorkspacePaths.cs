namespace SpriteForge.Core.Models;

public sealed record ProjectWorkspacePaths(string RootPath)
{
    public string Source => Path.Combine(RootPath, "source");
    public string GeneratedVideo => Path.Combine(RootPath, "generated", "video");
    public string Extracted => Path.Combine(RootPath, "cache", "extracted");
    public string Transparent => Path.Combine(RootPath, "cache", "transparent");
    public string Normalized => Path.Combine(RootPath, "cache", "normalized");
    public string Thumbnails => Path.Combine(RootPath, "cache", "thumbnails");
    public string Analysis => Path.Combine(RootPath, "cache", "analysis");
    public string Exports => Path.Combine(RootPath, "exports");
    public string Logs => Path.Combine(RootPath, "logs");

    public string ResolveRelative(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            throw new ArgumentException("Artifact path cannot be empty.", nameof(relativePath));
        if (Path.IsPathRooted(relativePath))
            throw new InvalidOperationException("Project artifact paths must be relative to the project workspace.");

        var fullPath = Path.GetFullPath(Path.Combine(GetFullRoot(), relativePath));
        EnsureInsideWorkspace(fullPath);
        return fullPath;
    }

    public string ToRelative(string path)
    {
        var fullPath = Path.GetFullPath(path);
        EnsureInsideWorkspace(fullPath);
        return Path.GetRelativePath(GetFullRoot(), fullPath);
    }

    private string GetFullRoot() => Path.TrimEndingDirectorySeparator(Path.GetFullPath(RootPath));

    private void EnsureInsideWorkspace(string fullPath)
    {
        var relative = Path.GetRelativePath(GetFullRoot(), fullPath);
        if (Path.IsPathRooted(relative) ||
            string.Equals(relative, "..", StringComparison.Ordinal) ||
            relative.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal) ||
            relative.StartsWith($"..{Path.AltDirectorySeparatorChar}", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The artifact path escapes the project workspace.");
        }
    }
}

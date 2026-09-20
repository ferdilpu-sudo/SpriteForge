using System.Xml.Linq;

namespace SpriteForge.Architecture.Tests;

public sealed class DependencyRuleTests
{
    [Fact]
    public void Core_HasNoProjectOrPackageDependencies()
    {
        var root = FindRepositoryRoot();
        var projectFile = Path.Combine(root, "src", "SpriteForge.Core", "SpriteForge.Core.csproj");
        var document = XDocument.Load(projectFile);

        Assert.Empty(document.Descendants("ProjectReference"));
        Assert.Empty(document.Descendants("PackageReference"));
    }

    [Fact]
    public void Presentation_DoesNotReferenceInfrastructureOrMedia()
    {
        var root = FindRepositoryRoot();
        var projectFile = Path.Combine(root, "src", "SpriteForge.Presentation", "SpriteForge.Presentation.csproj");
        var references = XDocument.Load(projectFile)
            .Descendants("ProjectReference")
            .Select(element => (string?)element.Attribute("Include") ?? string.Empty)
            .ToArray();

        Assert.DoesNotContain(references, value => value.Contains("Infrastructure", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(references, value => value.Contains("Media", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("SpriteForge.Media")]
    [InlineData("SpriteForge.Export")]
    [InlineData("SpriteForge.Diagnostics")]
    [InlineData("SpriteForge.Providers")]
    [InlineData("SpriteForge.Infrastructure")]
    public void AdapterProjects_DoNotReferenceEachOther(string projectName)
    {
        var root = FindRepositoryRoot();
        var projectFile = Path.Combine(root, "src", projectName, $"{projectName}.csproj");
        var references = XDocument.Load(projectFile)
            .Descendants("ProjectReference")
            .Select(element => Path.GetFileNameWithoutExtension((string?)element.Attribute("Include") ?? string.Empty))
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToArray();

        Assert.All(references, reference => Assert.Equal("SpriteForge.Core", reference));
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Directory.Packages.props")))
        {
            directory = directory.Parent;
        }
        return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root was not found.");
    }
}

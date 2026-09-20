using SpriteForge.Application.Fingerprints;

namespace SpriteForge.Application.Tests;

public sealed class PipelineFingerprintServiceTests
{
    [Fact]
    public void Compute_IsDeterministic_ForEquivalentInputs()
    {
        var service = new PipelineFingerprintService();
        var first = service.Compute("export", new { fps = 12d, frames = new[] { 1, 2, 3 } });
        var second = service.Compute("export", new { fps = 12d, frames = new[] { 1, 2, 3 } });

        Assert.Equal(first, second);
        Assert.Equal(64, first.Length);
    }

    [Fact]
    public void Compute_Changes_WhenInputChanges()
    {
        var service = new PipelineFingerprintService();
        var first = service.Compute("export", new { fps = 12d });
        var second = service.Compute("export", new { fps = 24d });

        Assert.NotEqual(first, second);
    }
}

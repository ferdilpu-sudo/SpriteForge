using SpriteForge.Infrastructure.Processes;
using SpriteForge.Media.BackgroundRemoval;

namespace SpriteForge.Pipeline.Tests;

public sealed class BackgroundRemovalWorkerIntegrationTests
{
    [Fact]
    public async Task RemoveAsync_InvokesWorkerAndCommitsCompletedOutput()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var root = Path.Combine(Path.GetTempPath(), "spriteforge-worker-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            var input = Path.Combine(root, "input.png");
            var output = Path.Combine(root, "output.png");
            await File.WriteAllBytesAsync(
                input,
                [137, 80, 78, 71, 13, 10, 26, 10, 1, 2, 3, 4],
                cancellationToken);

            var workerScript = Path.Combine(AppContext.BaseDirectory, "Fixtures", "copy-worker.py");
            var worker = new BackgroundRemovalWorker(
                new ProcessRunner(),
                "python",
                workerScript);

            var result = await worker.RemoveAsync(
                input,
                output,
                new SpriteForge.Core.Models.BackgroundRemovalOptions(0.25),
                cancellationToken);

            Assert.Equal(input, result.InputPath);
            Assert.Equal(output, result.OutputPath);
            Assert.True(File.Exists(output));
            Assert.Equal(
                await File.ReadAllBytesAsync(input, cancellationToken),
                await File.ReadAllBytesAsync(output, cancellationToken));
            Assert.Empty(Directory.EnumerateFiles(root, "*.tmp.png"));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }
}

using System.Security.Cryptography;
using SpriteForge.Application.Imports;
using SpriteForge.Core.Contracts;
using SpriteForge.Core.Models;

namespace SpriteForge.Application.Tests;

public sealed class SourceImportServiceTests
{
    [Fact]
    public async Task ImportFrameSequence_NaturalSortsFrames_AndCopiesSourcesIntoWorkspace()
    {
        var root = Path.Combine(Path.GetTempPath(), "spriteforge-import-tests", Guid.NewGuid().ToString("N"));
        var input = Path.Combine(root, "input");
        var workspaceRoot = Path.Combine(root, "project");
        Directory.CreateDirectory(input);
        Directory.CreateDirectory(workspaceRoot);

        try
        {
            var frame10 = Path.Combine(input, "walk10.png");
            var frame2 = Path.Combine(input, "walk2.png");
            await File.WriteAllBytesAsync(frame10, [10]);
            await File.WriteAllBytesAsync(frame2, [2]);

            var project = new ProjectDocument();
            var workspace = new ProjectWorkspacePaths(workspaceRoot);
            Directory.CreateDirectory(workspace.Source);
            var service = new SourceImportService(new TestHashService(), new AcceptAllValidator());

            var artifacts = await service.ImportFrameSequenceAsync(
                project,
                workspace,
                [frame10, frame2],
                CancellationToken.None);

            Assert.Equal(2, artifacts.Count);
            Assert.Equal(2, project.Frames.Count);
            Assert.Equal(0, project.Frames[0].SourceIndex);
            Assert.Equal(new byte[] { 2 }, await File.ReadAllBytesAsync(Path.Combine(workspace.RootPath, artifacts[0].RelativePath)));
            Assert.All(artifacts, artifact => Assert.True(artifact.RelativePath.StartsWith("source", StringComparison.OrdinalIgnoreCase)));
            Assert.Equal("frame_sequence", project.Source?.Kind);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ImportVideo_FailureDoesNotDiscardExistingProjectState()
    {
        var root = Path.Combine(Path.GetTempPath(), "spriteforge-import-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var project = new ProjectDocument { Source = new ProjectSource("image", Guid.NewGuid()) };
            project.Frames.Add(new FrameRecord(
                Guid.NewGuid(), 0, 0, true, 83.333,
                new FrameArtifactLinks(null, null, null, null),
                new FrameTransform(0, 0, 1),
                new FramePivot(0.5, 1)));
            var originalSource = project.Source;
            var originalFrameId = project.Frames[0].Id;
            var service = new SourceImportService(new TestHashService(), new AcceptAllValidator());

            await Assert.ThrowsAsync<FileNotFoundException>(() => service.ImportVideoAsync(
                project,
                new ProjectWorkspacePaths(root),
                Path.Combine(root, "missing.mp4"),
                CancellationToken.None));

            Assert.Equal(originalSource, project.Source);
            Assert.Equal(originalFrameId, project.Frames.Single().Id);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ImportImage_ValidatorFailurePreservesExistingProjectState()
    {
        var root = Path.Combine(Path.GetTempPath(), "spriteforge-import-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var sourcePath = Path.Combine(root, "corrupt.png");
            await File.WriteAllBytesAsync(sourcePath, new byte[] { 1, 2, 3 });
            var existingSource = new ProjectSource("video", Guid.NewGuid());
            var project = new ProjectDocument { Source = existingSource };
            var service = new SourceImportService(new TestHashService(), new RejectingValidator());

            await Assert.ThrowsAsync<InvalidDataException>(() => service.ImportImageAsync(
                project,
                new ProjectWorkspacePaths(Path.Combine(root, "project")),
                sourcePath,
                CancellationToken.None));

            Assert.Equal(existingSource, project.Source);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    private sealed class RejectingValidator : ISourceAssetValidator
    {
        public Task ValidateImageAsync(string path, CancellationToken cancellationToken) =>
            Task.FromException(new InvalidDataException("corrupt"));
        public Task ValidateVideoAsync(string path, CancellationToken cancellationToken) =>
            Task.FromException(new InvalidDataException("corrupt"));
    }

    private sealed class AcceptAllValidator : ISourceAssetValidator
    {
        public Task ValidateImageAsync(string path, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task ValidateVideoAsync(string path, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class TestHashService : IFileHashService
    {
        public async Task<string> ComputeSha256Async(string path, CancellationToken cancellationToken)
        {
            await using var stream = File.OpenRead(path);
            return Convert.ToHexString(await SHA256.HashDataAsync(stream, cancellationToken)).ToLowerInvariant();
        }
    }
}

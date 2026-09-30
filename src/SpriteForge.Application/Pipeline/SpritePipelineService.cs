using SpriteForge.Application.Artifacts;
using SpriteForge.Application.Frames;
using SpriteForge.Application.Sheets;
using SpriteForge.Core.Contracts;

namespace SpriteForge.Application.Pipeline;

public sealed partial class SpritePipelineService
{
    private readonly IFrameExtractor frameExtractor;
    private readonly IFrameOptimizer frameOptimizer;
    private readonly IBackgroundRemovalService backgroundRemoval;
    private readonly IFrameNormalizer frameNormalizer;
    private readonly ILoopAnalyzer loopAnalyzer;
    private readonly ISpriteSheetExporter sheetExporter;
    private readonly IMetadataExporter metadataExporter;
    private readonly IIndividualFrameExporter individualFrameExporter;
    private readonly IFileHashService hashService;
    private readonly SheetLayoutCalculator layoutCalculator;
    private readonly FrameSequenceSelector sequenceSelector;
    private readonly ArtifactFileCleanupService _artifactCleanup;

    public SpritePipelineService(
        IFrameExtractor frameExtractor,
        IFrameOptimizer frameOptimizer,
        IBackgroundRemovalService backgroundRemoval,
        IFrameNormalizer frameNormalizer,
        ILoopAnalyzer loopAnalyzer,
        ISpriteSheetExporter sheetExporter,
        IMetadataExporter metadataExporter,
        IIndividualFrameExporter individualFrameExporter,
        IFileHashService hashService,
        SheetLayoutCalculator layoutCalculator,
        FrameSequenceSelector sequenceSelector,
        ArtifactFileCleanupService? artifactCleanup = null)
    {
        this.frameExtractor = frameExtractor;
        this.frameOptimizer = frameOptimizer;
        this.backgroundRemoval = backgroundRemoval;
        this.frameNormalizer = frameNormalizer;
        this.loopAnalyzer = loopAnalyzer;
        this.sheetExporter = sheetExporter;
        this.metadataExporter = metadataExporter;
        this.individualFrameExporter = individualFrameExporter;
        this.hashService = hashService;
        this.layoutCalculator = layoutCalculator;
        this.sequenceSelector = sequenceSelector;
        _artifactCleanup = artifactCleanup ?? new ArtifactFileCleanupService();
    }

    public IReadOnlyList<FrameRecord> GetExportSequence(ProjectDocument project) => sequenceSelector.Select(project);
}

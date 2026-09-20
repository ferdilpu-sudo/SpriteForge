namespace SpriteForge.Core.Models;

public sealed record ProjectSource(string Kind, Guid ArtifactId);

public sealed record ExportRecord(
    Guid Id,
    DateTimeOffset CreatedAt,
    string Format,
    Guid SheetArtifactId,
    Guid MetadataArtifactId,
    string SettingsFingerprint);

public sealed class ProjectDocument
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; init; } = CurrentSchemaVersion;
    public Guid ProjectId { get; init; } = Guid.NewGuid();
    public string Name { get; set; } = "Untitled Sprite";
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public ProjectSource? Source { get; set; }
    public GenerationRecord? Generation { get; set; }
    public ExtractionSettings Extraction { get; set; } = new(12, 0, null);
    public BackgroundRemovalSettings BackgroundRemoval { get; set; } = new(true, "local_default", 0.05);
    public NormalizationSettings Normalization { get; set; } = new(512, 512, "contain", "bottom_center", true);
    public LoopSettings Loop { get; set; } = new(true, null, null, false);
    public SheetSettings Sheet { get; set; } = new(null, 512, 512, 0, 0, false);
    public List<FrameRecord> Frames { get; init; } = [];
    public List<ArtifactRecord> Artifacts { get; init; } = [];
    public List<ExportRecord> Exports { get; init; } = [];
}

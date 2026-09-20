# SpriteForge — Architecture

## 1. Architectural Direction
Windows-first desktop application using a layered, provider-agnostic architecture. UI must not know FFmpeg command syntax, model internals, provider HTTP details, or storage implementation.

Recommended stack:
- .NET 10 / C#
- WinUI 3
- MVVM
- FFmpeg as an external local process
- Local background-removal worker behind an adapter (Python/ONNX implementation is replaceable)
- SQLite or JSON-backed project persistence behind repositories; choose one implementation without leaking it into domain code

## 2. Solution Layout

```text
SpriteForge.sln
src/
  SpriteForge.App/              # WinUI views, navigation, composition root
  SpriteForge.Presentation/     # ViewModels, UI state, commands
  SpriteForge.Core/             # Domain models, contracts, validation
  SpriteForge.Application/      # Use cases / orchestration
  SpriteForge.Infrastructure/   # filesystem, process runner, persistence
  SpriteForge.Media/            # FFmpeg, frame/image processing adapters
  SpriteForge.Providers/        # image-to-video provider adapters
  SpriteForge.Export/           # sheet + metadata exporters
  SpriteForge.Diagnostics/      # health checks and logs
workers/
  background-removal/           # optional isolated local worker
scripts/
tests/
  SpriteForge.Core.Tests/
  SpriteForge.Application.Tests/
  SpriteForge.Infrastructure.Tests/
  SpriteForge.Architecture.Tests/
```

## 3. Dependency Rule

```text
App → Presentation → Application → Core
                         ↑
Infrastructure ──────────┤
Media ───────────────────┤
Providers ───────────────┤
Export ──────────────────┘
```

`Core` depends on no UI, provider SDK, FFmpeg implementation, database library, or OS-specific view code.

## 4. Major Components

### Project Service
Owns project lifecycle, autosave, dirty state, and pipeline configuration.

### Pipeline Orchestrator
Executes stages as jobs:
1. acquire animation source
2. extract frames
3. remove background
4. normalize
5. analyze/select loop
6. compose sheet
7. export

Each stage receives typed input and produces typed output/artifact references.

### Media Process Service
Contract example:

```csharp
public interface IFrameExtractor
{
    Task<FrameExtractionResult> ExtractAsync(
        FrameExtractionRequest request,
        IProgress<PipelineProgress>? progress,
        CancellationToken cancellationToken);
}
```

FFmpeg implementation belongs in `SpriteForge.Media`.

### Background Removal

```csharp
public interface IBackgroundRemovalService
{
    Task<ProcessedFrame> RemoveAsync(
        SourceFrame frame,
        BackgroundRemovalOptions options,
        CancellationToken cancellationToken);
}
```

The first implementation may invoke an isolated local worker. Core never references Python, rembg, U²-Net, or ONNX directly.

### Video Generation Provider

```csharp
public interface IImageToVideoProvider
{
    string ProviderId { get; }
    Task<GenerationJob> SubmitAsync(...);
    Task<GenerationJobStatus> GetStatusAsync(...);
    Task<VideoArtifact> DownloadAsync(...);
}
```

Provider-specific credentials/configuration stay outside project documents.

### Loop Analyzer
Calculates similarity/difference between frames near candidate boundaries and returns recommendations, not irreversible edits.

```text
Frames → feature/difference calculation → candidate ranges → ranked seam candidates
```

V1 can use image-difference metrics; future versions can replace the implementation without changing project schema.

### Sprite Composer
Pure application/domain operation where practical:
- calculate rows/columns
- calculate cell rectangles
- calculate sheet dimensions
- assign frame indexes
- hand rendering to an image backend

### Exporters

```csharp
public interface ISpriteExporter
{
    string FormatId { get; }
    Task<ExportResult> ExportAsync(...);
}
```

JSON metadata is a separate exporter from raster sheet generation.

## 5. Pipeline State

```text
Idle
 ↓
SourceReady
 ↓
VideoReady
 ↓
FramesExtracted
 ↓
BackgroundProcessed
 ↓
FramesNormalized
 ↓
LoopConfigured
 ↓
SheetBuilt
 ↓
Exported
```

Stages may be skipped where valid. Example: importing transparent PNG frames can enter at `FramesExtracted`/`BackgroundProcessed` equivalent.

Changing upstream settings invalidates downstream derived artifacts. Example: changing extraction FPS invalidates extracted frames, transparency results, normalization, loop analysis, and built sheets, but does not delete the source video.

## 6. Job Model
Every expensive operation is represented as a job with:
- id
- stage
- status
- progress 0..1 where measurable
- started/completed timestamps
- cancellation token
- error code/message
- input fingerprint
- output artifact references

No long-running work on the UI thread. Humanity has suffered enough frozen Windows apps.

## 7. File Workspace

```text
<ProjectFolder>/
  project.spriteforge.json
  source/
  generated/
    video/
  cache/
    extracted/
    transparent/
    normalized/
    thumbnails/
    analysis/
  exports/
  logs/
```

`cache/` is disposable. `source/` and project metadata are not.

## 8. Artifact Identity
Artifacts use stable IDs independent of filenames. Project metadata references IDs and relative paths. External absolute paths may be supported as linked assets, but the UI must indicate missing links.

## 9. Persistence
- Project schema is versioned.
- Atomic save: write temporary file then replace.
- Migrations are explicit.
- Unknown future fields should be tolerated where possible.
- Secrets never enter project persistence.

## 10. Error Model
Use typed errors such as:
- `FFMPEG_NOT_FOUND`
- `SOURCE_UNREADABLE`
- `FRAME_EXTRACTION_FAILED`
- `BACKGROUND_MODEL_UNAVAILABLE`
- `PROVIDER_AUTH_FAILED`
- `PROVIDER_GENERATION_FAILED`
- `EXPORT_PATH_DENIED`
- `INSUFFICIENT_DISK_SPACE`

User-facing messages translate these into useful actions. Raw stderr belongs in diagnostics/logs.

## 11. Security Boundaries
- External processes receive minimal arguments and controlled paths.
- Never build shell command strings from unsanitized user text.
- Use process argument lists.
- Credentials stored using Windows-protected secret storage or provider-specific secure mechanism.
- Do not log tokens, cookies, authorization headers, or full sensitive provider responses.

## 12. Testing
- Core unit tests for layout, frame ordering, invalidation, loop selection logic.
- Contract tests for provider interfaces.
- Integration tests for FFmpeg using tiny fixtures.
- Golden-file tests for metadata export.
- Architecture tests enforcing project references/dependency direction.
- UI smoke tests for create/import/process/export flow.

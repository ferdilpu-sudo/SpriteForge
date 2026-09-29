# SpriteForge — Data Schema

## 1. Conventions

- JSON UTF-8.
- `schemaVersion` uses integer major versions in V1.
- IDs are UUID strings unless otherwise noted.
- Dates are ISO 8601 UTC.
- Coordinates/dimensions are pixels.
- Internal frame indexes are zero-based.

## 2. Project Document

```json
{
  "schemaVersion": 1,
  "projectId": "uuid",
  "name": "Ancient Tree Idle",
  "createdAt": "2026-09-29T12:00:00Z",
  "updatedAt": "2026-09-29T12:20:00Z",
  "source": {
    "kind": "video",
    "artifactId": "uuid"
  },
  "generation": null,
  "extraction": {
    "fps": 12.0,
    "startSeconds": 0.0,
    "endSeconds": 3.0
  },
  "frameOptimization": {
    "mode": "balanced",
    "similarityThreshold": 0.96,
    "preserveMotionPeaks": true
  },
  "backgroundRemoval": {
    "enabled": true,
    "processorId": "local_default",
    "alphaThreshold": 0.05
  },
  "normalization": {
    "canvasWidth": 512,
    "canvasHeight": 512,
    "fit": "contain",
    "anchor": "bottom_center",
    "autoTrim": true
  },
  "loop": {
    "enabled": true,
    "startFrameId": "uuid",
    "endFrameId": "uuid",
    "recommended": false
  },
  "sheet": {
    "columns": 4,
    "cellWidth": 512,
    "cellHeight": 512,
    "padding": 0,
    "spacing": 0,
    "powerOfTwo": false
  },
  "frames": [],
  "artifacts": [],
  "exports": []
}
```

### Frame optimization modes

- `raw`: no automatic frame reduction.
- `compact`: target up to 7 optimized frames.
- `balanced`: target up to 10 optimized frames.
- `smooth`: target up to 16 optimized frames.

`similarityThreshold` is normalized to `[0,1]`. Optimized modes can disable redundant candidate frames while retaining them in project metadata until the user explicitly deletes disabled frames.

## 3. Artifact

```json
{
  "id": "uuid",
  "kind": "source_image",
  "relativePath": "source/tree.png",
  "mimeType": "image/png",
  "width": 1024,
  "height": 1024,
  "durationSeconds": null,
  "sha256": "hex-string",
  "createdAt": "2026-09-29T12:00:00Z"
}
```

Artifact kinds include source images/videos, generated video, extracted/transparent/normalized/thumbnail frames, sprite sheets, metadata, and exported frames.

## 4. Frame

```json
{
  "id": "uuid",
  "sourceIndex": 0,
  "order": 0,
  "enabled": true,
  "durationMs": 83.333,
  "artifacts": {
    "extracted": "artifact-uuid",
    "transparent": "artifact-uuid",
    "normalized": "artifact-uuid",
    "thumbnail": "artifact-uuid"
  },
  "transform": {
    "offsetX": 0,
    "offsetY": 0,
    "scale": 1.0
  },
  "pivot": {
    "x": 0.5,
    "y": 1.0
  }
}
```

`pivot.x/y` are normalized coordinates in `[0,1]`.

## 5. Sprite Sheet Metadata Export

```json
{
  "format": "spriteforge.sprite_sheet",
  "version": 1,
  "image": "tree_idle.png",
  "width": 2048,
  "height": 1536,
  "cellWidth": 512,
  "cellHeight": 512,
  "columns": 4,
  "rows": 3,
  "frameCount": 12,
  "loop": true,
  "defaultFps": 12.0,
  "frames": [
    {
      "index": 0,
      "x": 0,
      "y": 0,
      "width": 512,
      "height": 512,
      "durationMs": 83.333,
      "pivot": { "x": 0.5, "y": 1.0 }
    }
  ]
}
```

## 6. Export Record

```json
{
  "id": "uuid",
  "createdAt": "2026-09-29T12:20:00Z",
  "format": "png_json",
  "sheetArtifactId": "uuid",
  "metadataArtifactId": "uuid",
  "settingsFingerprint": "sha256-hex"
}
```

## 7. Job Record

```json
{
  "id": "uuid",
  "stage": "extract_frames",
  "status": "completed",
  "progress": 1.0,
  "startedAt": "2026-09-29T12:05:00Z",
  "completedAt": "2026-09-29T12:05:04Z",
  "inputFingerprint": "sha256-hex",
  "outputs": ["artifact-uuid"],
  "error": null
}
```

## 8. Invalidation Rules

| Changed setting | Invalidate |
|---|---|
| Source image/video | all derived stages |
| Generation settings | generated video onward |
| Extraction FPS/range | extracted frames onward |
| Frame optimization settings | extracted frame selection onward |
| Background removal | transparent frames onward |
| Normalization | normalized frames onward |
| Frame enable/order | loop selection + sheet/export |
| Loop range | sheet/export |
| Sheet layout | sheet/export only |
| Export destination | no processing artifacts |

## 9. Safety Constraints

- Candidate extraction is capped at 2,000 frames.
- Sprite sheet dimensions are capped at 16,384 px per side.
- Sprite sheet raw RGBA allocation estimate is capped at 512 MiB.
- Project artifact paths must stay inside the project workspace.

## 10. Migration Policy

- Load schema version before deserializing project state.
- Reject unsupported newer major versions clearly.
- Older major versions require an explicit migration.
- Future migrations must back up metadata before destructive changes.
- Migration must never modify original source assets.

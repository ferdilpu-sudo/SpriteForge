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
  "createdAt": "2026-09-19T13:00:00Z",
  "updatedAt": "2026-09-19T13:20:00Z",
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
  "createdAt": "2026-09-19T13:00:00Z"
}
```

Allowed `kind` values initially:
- `source_image`
- `source_video`
- `generated_video`
- `extracted_frame`
- `transparent_frame`
- `normalized_frame`
- `thumbnail`
- `sprite_sheet`
- `metadata`
- `exported_frame`

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

## 5. Image-to-Video Generation

```json
{
  "providerId": "provider_name",
  "providerJobId": "external-id",
  "prompt": "Ancient tree gently swaying in a light breeze, locked camera",
  "status": "completed",
  "submittedAt": "2026-09-19T13:01:00Z",
  "completedAt": "2026-09-19T13:02:30Z",
  "outputArtifactId": "uuid",
  "settings": {
    "durationSeconds": 3.0,
    "aspectRatio": "1:1"
  }
}
```

`settings` is provider-neutral where possible. Provider-only transient parameters should not infect the core schema unless they are required to reproduce the job.

Generation statuses:
- `idle`
- `queued`
- `generating`
- `downloading`
- `completed`
- `failed`
- `cancelled`

## 6. Loop Analysis

```json
{
  "analysisVersion": 1,
  "createdAt": "2026-09-19T13:10:00Z",
  "candidates": [
    {
      "startFrameId": "uuid",
      "endFrameId": "uuid",
      "seamScore": 0.041,
      "frameCount": 24
    }
  ]
}
```

Lower `seamScore` means a closer visual boundary for the V1 analyzer. The score is implementation-specific and MUST NOT be presented as a universal quality percentage.

## 7. Sprite Sheet Metadata Export

Recommended generic export:

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
    },
    {
      "index": 1,
      "x": 512,
      "y": 0,
      "width": 512,
      "height": 512,
      "durationMs": 83.333,
      "pivot": { "x": 0.5, "y": 1.0 }
    }
  ]
}
```

## 8. Export Record

```json
{
  "id": "uuid",
  "createdAt": "2026-09-19T13:20:00Z",
  "format": "png_json",
  "sheetArtifactId": "uuid",
  "metadataArtifactId": "uuid",
  "settingsFingerprint": "sha256-hex"
}
```

## 9. Job Record

```json
{
  "id": "uuid",
  "stage": "extract_frames",
  "status": "completed",
  "progress": 1.0,
  "startedAt": "2026-09-19T13:05:00Z",
  "completedAt": "2026-09-19T13:05:04Z",
  "inputFingerprint": "sha256-hex",
  "outputs": ["artifact-uuid"],
  "error": null
}
```

Job statuses:
- `pending`
- `running`
- `completed`
- `failed`
- `cancelled`

## 10. Error Object

```json
{
  "code": "FRAME_EXTRACTION_FAILED",
  "message": "Frame extraction failed.",
  "technicalDetail": "Sanitized diagnostic detail",
  "recoverable": true
}
```

## 11. Invalidation Rules

| Changed setting | Invalidate |
|---|---|
| Source image/video | all derived stages |
| Generation settings | generated video onward |
| Extraction FPS/range | extracted frames onward |
| Background removal | transparent frames onward |
| Normalization | normalized frames onward |
| Frame enable/order | loop selection + sheet/export |
| Loop range | sheet/export |
| Sheet layout | sheet/export only |
| Export destination | no processing artifacts |

## 12. Migration Policy
- Load schema version.
- Reject unsupported newer major versions with a clear read-only/recovery path where possible.
- Apply ordered migrations for older versions.
- Backup project metadata before migration.
- Migration MUST NOT modify original source assets.

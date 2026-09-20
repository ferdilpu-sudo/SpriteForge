# SpriteForge — Product Requirements Document

## 1. Product Summary
SpriteForge is a Windows-first desktop application that converts a source image or image-to-video animation into clean, loopable, transparent sprite sheets for games, web experiences, stickers, UI animation, and other 2D pipelines.

Core pipeline:

`Source Image → Image-to-Video → Extract Frames → Remove Background → Normalize Frames → Optimize Loop → Build Sprite Sheet → Preview → Export`

The application should automate the repetitive technical work while keeping the user in control of frames, timing, transparency, layout, and export.

## 2. Goals
- Turn a single AI-generated or user-provided image into an animated sprite workflow.
- Accept an existing video so AI generation is optional.
- Automate FFmpeg frame extraction.
- Remove backgrounds locally when possible.
- Normalize frame dimensions, crop, pivot, and alignment.
- Detect/select a smooth looping segment.
- Build PNG spritesheets and animation metadata.
- Provide instant animation preview before export.
- Keep local processing usable without an LLM.
- Make provider integrations replaceable rather than hard-coded.

## 3. Non-Goals — V1
- Full 2D animation editor.
- Skeletal animation/rigging.
- 3D animation.
- Full video editor.
- Training custom image/video models.
- Game-engine replacement.
- Cloud account/session manager.

## 4. Target Users
- Indie game developers.
- AI-assisted game asset creators.
- Web developers needing lightweight animated assets.
- Content creators making looping transparent animations.
- Non-technical creators who do not want to manually operate FFmpeg and image-processing tools.

## 5. Primary User Flows

### A. Image → AI animation → Sprite
1. Import PNG/JPG/WebP.
2. Enter animation prompt.
3. Select an available image-to-video provider.
4. Generate/import returned MP4/WebM.
5. Extract candidate frames.
6. Remove background.
7. Normalize frame geometry.
8. Optimize/select loop.
9. Preview animation.
10. Export spritesheet + metadata.

### B. Existing video → Sprite
1. Import MP4/WebM/MOV.
2. Choose time range and target FPS.
3. Extract frames.
4. Process transparency and alignment.
5. Preview/select frames.
6. Export.

### C. Existing frame sequence → Sprite
1. Import ordered PNG/WebP frames.
2. Normalize size/alignment.
3. Reorder/remove frames.
4. Preview.
5. Export.

## 6. Functional Requirements

### FR-01 Project
- Create/open/save project.
- Autosave non-destructive project state.
- Store references to source assets and generated artifacts.

### FR-02 Import
- PNG, JPG/JPEG, WebP source image.
- MP4, WebM, MOV video when supported by local FFmpeg.
- Multi-select PNG/WebP frame sequence.
- Validate unreadable/corrupt input.

### FR-03 Image-to-Video
- Provider abstraction.
- Prompt field and provider-specific options.
- Generation states: idle, queued, generating, downloading, completed, failed, cancelled.
- Generation must be optional; imported video can bypass this stage.

### FR-04 Frame Extraction
- FFmpeg-backed extraction.
- Configurable FPS.
- Optional start/end time.
- Preserve source order.
- Deterministic frame naming.

### FR-05 Transparency
- Local background-removal adapter.
- Preserve alpha channel.
- Optional alpha threshold/refinement.
- User can bypass removal for already-transparent frames.

### FR-06 Frame Normalization
- Canvas width/height.
- Fit modes: contain, cover, original.
- Anchor/pivot.
- Auto-trim transparent margins.
- Consistent placement across frames.
- Never destructively modify original assets.

### FR-07 Loop Optimization
- Compare candidate boundary frames.
- Recommend a start/end range with low visual discontinuity.
- User can override the recommendation.
- Preview must clearly expose the loop seam.

### FR-08 Frame Editor
- Thumbnail strip/grid.
- Enable/disable frames.
- Reorder frames.
- Duplicate/delete processed-frame references.
- Set per-frame duration where supported.
- Reset to extracted sequence.

### FR-09 Sprite Sheet Builder
- Automatic grid sizing.
- Manual column count.
- Optional fixed cell dimensions.
- Padding and spacing.
- Power-of-two output option.
- Transparent background.
- Deterministic frame indexing: left-to-right, top-to-bottom.

### FR-10 Preview
- Play/pause.
- FPS/speed control.
- Loop toggle.
- Checkerboard transparency preview.
- Zoom without modifying source.

### FR-11 Export
Required V1:
- PNG spritesheet.
- Individual PNG frames.
- JSON metadata.

Optional/next:
- GIF.
- APNG.
- WebP animation.
- Engine presets such as Godot, Unity, Phaser, PixiJS.

### FR-12 Diagnostics
- Detect FFmpeg availability/version.
- Validate write access to workspace/export path.
- Surface failed pipeline stage and actionable error.
- Keep processing logs per job.

## 7. Quality Requirements
- UI must remain responsive during processing.
- Cancellation must be supported for long-running jobs where technically possible.
- A failed stage must not destroy completed prior stages.
- Re-running one stage should invalidate only dependent downstream artifacts.
- Generated outputs should be reproducible from saved project settings where providers are deterministic enough.

## 8. Performance Targets
- Open project UI: target < 2 seconds excluding exceptionally large assets.
- Thumbnail virtualization for large frame sets.
- Do not load all full-resolution frames into memory simultaneously.
- Process frame sequences incrementally/streamed where practical.

## 9. Privacy
- Local assets remain local unless the user explicitly invokes an external AI provider.
- UI must identify which stage sends data externally.
- Secrets/API tokens must never be stored in project files or logs.

## 10. V1 Acceptance Criteria
V1 is complete when a user can import a short video, extract frames, create transparent normalized frames, preview a seamless-enough loop, export a PNG spritesheet and JSON metadata, close the app, reopen the project, and reproduce/export the same configured result without manually invoking command-line tools.

## 11. Future Roadmap
- Integrated image generation.
- More image-to-video providers.
- Batch asset processing.
- Automatic motion classification.
- Palette reduction/pixel-art mode.
- Texture atlas packing.
- Engine-specific metadata exporters.
- Switchly Studio handoff/integration without coupling SpriteForge core to Switchly.

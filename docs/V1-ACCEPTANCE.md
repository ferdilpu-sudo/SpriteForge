# SpriteForge V1 Acceptance

## Status

The automated V1 acceptance gate is green on Windows CI.

The automated gate proves the local processing pipeline can:

1. generate and validate a short video fixture;
2. import the video non-destructively into a project workspace;
3. extract ordered PNG frames through the production FFmpeg adapter;
4. execute the Cutout stage through the background-removal contract;
5. produce alpha-bearing frame artifacts;
6. normalize frames through the production SkiaSharp normalizer;
7. analyze and select a loop range;
8. compose a sprite-sheet preview;
9. export a PNG spritesheet, JSON metadata, and individual PNG frames;
10. save the project document;
11. reopen the project through the production JSON repository;
12. export the reopened project again;
13. verify the first and second spritesheet/metadata outputs are byte-identical by SHA-256.

The background-removal process adapter is also tested independently with a deterministic worker fixture. This verifies process invocation, argument passing, temporary-output commit behavior, and cleanup without requiring a model download in CI.

## Automated gate

Run locally from a Windows PowerShell terminal:

```powershell
.\scripts\acceptance-v1.ps1
```

To also create/update the local `rembg` environment first:

```powershell
.\scripts\acceptance-v1.ps1 -SetupWorker
```

The script runs prerequisite checks, restore, Release x64 build, and the complete automated test suite.

GitHub Actions additionally:

- installs .NET 10;
- installs Python 3.12;
- syntax-checks `workers/background-removal/worker.py`;
- installs pinned FFmpeg 9.0.1;
- executes Core, Application, Infrastructure, Architecture, and Pipeline integration tests.

## Manual desktop acceptance

The following checks intentionally remain manual because they depend on interactive WinUI behavior or the real local ML model runtime.

### 1. Startup and diagnostics

- Launch `SpriteForge.App` on Windows x64.
- Confirm the main shell opens without an unhandled exception.
- Open Diagnostics.
- Confirm workspace write access and FFmpeg are reported healthy.
- After worker setup, confirm background-removal diagnostics are healthy.

### 2. Real media workflow

Use a short real MP4/WebM/MOV clip.

- Import the video through the Source stage.
- Confirm the original file is not modified.
- Extract frames at a known FPS.
- Run Cutout with the real `rembg` worker.
- Inspect several frames for preserved alpha and acceptable foreground edges.
- Run Align/Normalize.
- Confirm anchor, pivot, canvas size, and auto-trim changes appear in preview.
- Run Loop analysis and inspect the recommended seam.
- Override the loop range manually and confirm preview updates.
- Build the sheet.
- Export PNG spritesheet, JSON metadata, and individual PNG frames.

### 3. Project reopen and reproducibility

- Save the project.
- Close SpriteForge completely.
- Reopen the same `project.spriteforge.json`.
- Confirm source, frame order, durations, loop range, pivots, and stage state are restored.
- Export again with unchanged settings.
- Confirm visual output and metadata match the previous export.

The automated acceptance suite already performs hash equality for its deterministic fixture. The manual check confirms the same contract through the desktop UI and real worker environment.

### 4. Responsiveness and cancellation

During Extract, Cutout, and Normalize:

- confirm the window remains responsive;
- confirm inline progress updates;
- cancel an operation;
- confirm previously completed stage artifacts remain valid;
- confirm the cancelled stage does not leave partial committed artifacts.

### 5. UI smoke checks

- Source file picker works.
- Source drag-and-drop works.
- Pipeline navigation uses the expected eight stages.
- Frame strip can enable/disable, reorder, duplicate, delete, and reset frames.
- Per-frame duration editing persists.
- Preview play/pause, loop toggle, speed/FPS behavior, checkerboard, and zoom work.
- Export destination validation refuses silent overwrite.

## Release boundary

A V1 release candidate should not be labelled desktop-accepted until all manual checks above pass on a Windows machine with the real `rembg` worker.

External image-to-video provider integration is not part of this V1 acceptance boundary.

# SpriteForge — Implementation Readiness Audit

## Verdict

SpriteForge is technically ready for a **public beta** once the release-hardening branch is merged and its CI is green.

The core processing pipeline is no longer the release risk. Remaining stable-1.0 work is primarily distribution, signing, dependency onboarding, and product/legal packaging.

## Public-beta-ready areas

- Windows-first WinUI 3 desktop shell.
- Non-destructive video/image/frame-sequence import.
- Deterministic FFmpeg extraction.
- Local background removal through an isolated `rembg` worker.
- Automatic frame optimization with Raw/Compact/Balanced/Smooth modes.
- Smooth optimized preview playback with cached preview frames.
- Optional destructive cleanup of disabled frame files.
- SkiaSharp normalization and pivot-aware sheet composition.
- Loop analysis plus manual loop control.
- PNG sprite sheet, JSON metadata, and optional frame export.
- Atomic project persistence and workspace path containment.
- Cancellation, diagnostics, per-job logs, and stage invalidation.
- Core/Application/Infrastructure/Architecture/Pipeline test projects.
- Windows CI and self-contained x64 packaging.

## Release hardening

The public beta hardening layer adds:

- separate runtime prerequisite checks that do not require the .NET SDK;
- 2,000-frame extraction safety cap;
- 1,024 px preview decode cap before images enter the preview cache;
- 512 MiB raw RGBA sheet-allocation guard;
- explicit application version metadata;
- versioned package names and SHA-256 checksum files;
- tag-driven GitHub Release automation;
- SECURITY, CHANGELOG, release, and third-party notice documents.

## Current distribution boundary

The application package is self-contained for .NET and Windows App SDK, but beta users still need:

- FFmpeg on PATH;
- Python 3.11+ for Cutout;
- a one-time local `rembg` worker setup.

That is acceptable for a technical public beta, but not the intended stable-1.0 onboarding experience.

## Stable 1.0 remaining work

1. Decide and document the project license.
2. Add final application branding/icon assets.
3. Decide installer format and code-signing strategy.
4. Reduce or eliminate manual FFmpeg/Python setup.
5. Add cache/artifact garbage collection and retention controls.
6. Improve crash recovery and end-user diagnostics.
7. Complete accessibility and keyboard-navigation audit.
8. Define update delivery/rollback behavior.

## Engineering note

The following files are already large enough that future features should be split by responsibility rather than appended:

- `SpritePipelineService.cs`;
- `DesktopWorkflowController.Pipeline.cs`;
- `DesktopWorkflowController.State.cs`;
- `SkiaFrameOptimizer.cs`.

Do not add another broad feature to these files without extracting a focused service/module first.

## Validation

The stable frame-optimization baseline on `main` passed local QA and Windows CI. The release-hardening branch must pass the same build/test/package gate before merge.

See `docs/V1-ACCEPTANCE.md` and `docs/RELEASE.md`.

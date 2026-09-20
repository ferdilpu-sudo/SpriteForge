# SpriteForge

Windows-first desktop pipeline for turning images, videos, or frame sequences into transparent, loopable sprite sheets and JSON metadata.

## V1 pipeline

`Source → Animate (optional) → Frames → Cutout → Align → Loop → Sheet → Export`

AI generation is optional. A user can import a video and complete the local sprite workflow without configuring an external provider.

## Stack

- .NET 10 / C#
- WinUI 3 via Windows App SDK 2.5.1
- CommunityToolkit.Mvvm 8.4.2
- FFmpeg for frame extraction
- SkiaSharp 4.151.2 for normalization, loop image scoring, and sheet composition
- `rembg[cpu]` 2.0.84 in an isolated local Python worker
- Versioned JSON project persistence

## Repository structure

```text
src/
  SpriteForge.App/              WinUI shell and stage views
  SpriteForge.Presentation/     ViewModels and UI state
  SpriteForge.Core/             Domain models and contracts
  SpriteForge.Application/      Use cases, invalidation, layout, orchestration
  SpriteForge.Infrastructure/   JSON persistence, process runner, workspace, hashing
  SpriteForge.Media/            FFmpeg, cutout adapter, normalization, loop analyzer
  SpriteForge.Providers/        Replaceable provider adapters
  SpriteForge.Export/           PNG sheet, frame, and JSON exporters
  SpriteForge.Diagnostics/      Dependency/workspace checks
workers/
  background-removal/           Isolated rembg worker
tests/
  SpriteForge.Pipeline.Tests/     FFmpeg integration + golden export contracts
docs/
scripts/
```

## Prerequisites

1. Windows 10 1809+ or Windows 11.
2. Visual Studio 2026 with WinUI application development workload.
3. .NET 10 SDK.
4. FFmpeg available on `PATH` or configured by the application.
5. Python 3.11+ for the default background-removal worker.

Run the local prerequisite probe:

```powershell
.\scripts\verify-prereqs.ps1
```

Set up background removal:

```powershell
.\scripts\setup-worker.ps1
```

## Build

```powershell
dotnet restore SpriteForge.sln
dotnet build SpriteForge.sln -c Debug
dotnet test SpriteForge.sln -c Debug
```

Open `SpriteForge.sln` in Visual Studio and run `SpriteForge.App` on x64.

## Source-safety contract

- Imported originals are never edited in place.
- Derived processing lives under `cache/`, `generated/`, or `exports/`.
- Project metadata uses atomic writes.
- Export does not overwrite existing files without explicit confirmation.
- Provider secrets never belong in project JSON or logs.

## Current implementation status

Implemented V1 foundation:
- desktop shell matching the eight-stage pipeline with inline progress/cancellation;
- typed project/frame/artifact/job models and versioned JSON persistence;
- non-destructive image, video, and PNG/WebP frame-sequence import with decode validation;
- deterministic FFmpeg extraction using staged artifact replacement;
- local background-removal worker adapter;
- SkiaSharp normalization with nine anchors, per-frame pivots, and alpha-preserving composition;
- frame enable/disable, reorder, duplicate, delete, reset, and per-frame duration editing;
- image-difference loop recommendations plus manual loop selection and preview playback;
- deterministic sprite-sheet layout with a 16,384 px per-side safety limit and no silent downscale;
- PNG spritesheet, JSON metadata, and optional individual PNG export;
- immutable canonical export history and export fingerprints based on settings, frame order, timing, pivots, and normalized raster hashes;
- project artifact path-containment validation and atomic metadata writes;
- physical artifact-aware stage state so missing cache/export files are not reported as complete;
- FFmpeg/rembg/workspace diagnostics, per-job logs, and architecture/unit test projects.

Provider-specific image-to-video generation remains intentionally unimplemented until a provider is selected. The local V1 workflow does not depend on it.

### Validation status

The repository now has a Windows CI release gate in `.github/workflows/windows-ci.yml`. On September 20, 2026, the `main` branch completed all of the following successfully on `windows-latest` with .NET 10:

- solution restore;
- Release x64 build, including WinUI/XAML compilation;
- Core tests;
- Application tests;
- Infrastructure tests;
- Architecture tests;
- Pipeline integration tests covering real FFmpeg extraction and golden JSON metadata output.

CI installs a pinned FFmpeg 9.0.1 dependency before integration testing. Static XML/XAML/project checks, event-handler wiring checks, project-reference boundary checks, Python worker syntax validation, and FFmpeg extraction smoke testing were also performed during implementation.

For local verification:

```powershell
.\scripts\verify-prereqs.ps1
.\scripts\setup-worker.ps1
dotnet restore SpriteForge.sln
dotnet build SpriteForge.sln -c Release -p:Platform=x64
dotnet test SpriteForge.sln -c Release
```

The remaining release gate is application launch plus the end-to-end V1 acceptance flow with real media and the local background-removal worker.

See `docs/IMPLEMENTATION-READINESS.md` for readiness decisions and the supplied product documents in `docs/` for the source-of-truth requirements.

## V1 acceptance

The automated Windows acceptance gate now covers import, real FFmpeg extraction, Cutout contract execution, normalization, loop analysis, sprite-sheet/metadata/frame export, project save/reopen, and deterministic re-export hash comparison.

Run the local gate with:

```powershell
.\scripts\acceptance-v1.ps1
```

The real `rembg` model runtime and interactive WinUI behavior remain desktop-manual checks. See `docs/V1-ACCEPTANCE.md` for the release checklist.

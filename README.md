<p align="center">
  <img src="assets/branding/SpriteForge-app-icon-1024.png" alt="SpriteForge" width="128" />
</p>

# SpriteForge

SpriteForge is a Windows-first desktop pipeline for turning videos, images, or frame sequences into optimized sprite sheets and JSON animation metadata.

Current release line: **0.9.0-beta.1 public beta candidate**.

## Pipeline

`Source → Animate (optional) → Frames → Cutout → Align → Loop → Sheet → Export`

AI generation is optional. Existing videos and frame sequences can complete the local workflow without an external provider.

## Public beta runtime

The packaged Windows build is self-contained for .NET and Windows App SDK. End users do **not** need the .NET SDK.

External runtime prerequisites:

- Windows 10 1809+ or Windows 11.
- FFmpeg **and FFprobe** available on `PATH`.
- Python 3.11+ only when using Cutout/background removal.
- The local `rembg` worker, installed once with the included setup script.

From an extracted beta package:

```powershell
.\scripts\setup-worker.ps1
.\scripts\verify-runtime-prereqs.ps1 -Strict -RequireWorker
.\scripts\desktop-smoke.ps1
```

The first `rembg` use can download its model data.

## Developer setup

Development additionally requires:

- Visual Studio with WinUI application development workload.
- .NET 10 SDK.

Verify the development machine:

```powershell
.\scripts\verify-prereqs.ps1 -Strict
```

Set up the local background-removal worker:

```powershell
.\scripts\setup-worker.ps1
```

Build and test:

```powershell
dotnet restore SpriteForge.sln --configfile NuGet.config
dotnet build SpriteForge.sln -c Release -p:Platform=x64
dotnet test SpriteForge.sln -c Release
```

Run the app:

```powershell
dotnet run --project .\src\SpriteForge.App\SpriteForge.App.csproj -c Debug -p:Platform=x64
```

## Frame optimization

The Frames stage can automatically reduce redundant extracted frames while preserving the active motion range.

Modes:

- **Raw**: keep every candidate frame.
- **Compact**: up to 7 keyframes.
- **Balanced**: up to 10 keyframes, default.
- **Smooth**: up to 16 keyframes.

The optimizer uses localized RGB motion, preserves subtle continuous movement and strong motion peaks, fills temporal gaps inside the active motion range, and normalizes optimized playback timing.

Disabled frames can be reviewed before using **Delete disabled frames…** to remove unused frame files from the project.

## Safety limits

Public beta guardrails prevent unusually large jobs from consuming unreasonable resources:

- candidate extraction limit: **2,000 frames**;
- video extraction preflight rejects work above a **6 GiB decoded RGBA pixel budget**;
- a single decoded RGBA video frame is capped at **256 MiB**;
- extraction with a known range is rejected before FFmpeg starts when the estimate is over the limit;
- unknown-duration extraction is capped with a sentinel frame and fails clearly if the limit is exceeded;
- preview images are decoded to a maximum dimension of **1,024 px** before entering the preview cache;
- sprite sheets remain limited to **16,384 px per side**;
- a single sheet is rejected when its estimated raw RGBA bitmap exceeds **512 MiB**.

These are safety limits, not recommended asset sizes. Typical game sprites should be much smaller.

## Export

V1 exports:

- PNG sprite sheet;
- JSON metadata;
- optional individual PNG frames.

Export metadata includes frame rectangles, duration, pivots, loop state, and default FPS.

## Source safety

- Imported originals are never edited in place.
- Derived processing stays under the project workspace.
- Project metadata uses atomic writes.
- Artifact paths are constrained to the project workspace.
- Export refuses silent overwrite.
- Provider secrets do not belong in project JSON or logs.

## CI and release

Windows CI on `main` runs:

- Release x64 build;
- warnings-as-errors build gate;
- Core/Application/Infrastructure/Architecture tests;
- FFmpeg pipeline integration tests;
- self-contained beta packaging.

Version tags matching `v*` trigger the release workflow, which rebuilds/tests, creates a versioned ZIP plus SHA-256 checksum, and publishes a GitHub Release.

See:

- `docs/V1-ACCEPTANCE.md` for QA;
- `docs/RELEASE.md` for release steps;
- `CHANGELOG.md` for user-visible changes;
- `THIRD-PARTY-NOTICES.md` for direct runtime dependencies.

## Repository structure

```text
src/        application projects
workers/    isolated local background-removal worker
tests/      unit, architecture, and pipeline integration tests
docs/       product, schema, acceptance, and release documentation
scripts/    setup, verification, acceptance, packaging, and smoke scripts
```

Provider-specific image-to-video generation remains intentionally outside the V1 local workflow.

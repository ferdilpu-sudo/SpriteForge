# SpriteForge V1 Acceptance

## Status

The V1 local pipeline and frame-optimization baseline have passed desktop QA on Windows x64.

The automated Windows gate covers:

1. solution restore and Release x64 build;
2. warnings-as-errors compilation;
3. Core, Application, Infrastructure, and Architecture tests;
4. real FFmpeg frame extraction;
5. background-removal adapter integration with a deterministic worker;
6. normalization and sheet layout;
7. frame optimization behavior and timing;
8. sprite-sheet/JSON/individual-frame export;
9. project save and reopen;
10. deterministic repeated export checks.

The real `rembg` model and interactive WinUI behavior remain manual desktop checks.

## Automated gate

Run:

```powershell
.\scripts\acceptance-v1.ps1
```

To create/update the local Cutout environment first:

```powershell
.\scripts\acceptance-v1.ps1 -SetupWorker
```

For a packaged build, runtime verification is separate from developer verification:

```powershell
.\scripts\verify-runtime-prereqs.ps1 -Strict -RequireWorker
.\scripts\desktop-smoke.ps1
```

The packaged runtime must not require the .NET SDK.

## Manual desktop acceptance

### 1. Startup and diagnostics

- Launch the x64 packaged build.
- Confirm the shell opens without an unhandled exception.
- Confirm FFmpeg/FFprobe and workspace diagnostics are healthy.
- After worker setup, confirm Cutout diagnostics are healthy.

### 2. Real media workflow

Use at least one short real MP4/WebM/MOV clip.

- Import video.
- Confirm the original file is unchanged.
- Extract frames at a known FPS.
- Confirm Balanced frame optimization selects the active motion range.
- Play enabled frames and confirm no flicker, blank-frame glitch, or long static pause.
- Review disabled frames.
- Test horizontal frame-strip scrolling with the mouse wheel.
- Use **Delete disabled frames…** and confirm only selected keyframes remain.
- Run Cutout using the real worker.
- Inspect alpha quality.
- Run Align/Normalize and confirm placement is stable.
- Run Loop analysis and inspect the seam.
- Build a sheet.
- Export PNG + JSON + optional individual frames.

### 3. Frame optimization modes

Verify:

- Raw keeps every candidate frame.
- Compact caps optimized output around 7 frames.
- Balanced caps optimized output around 10 frames.
- Smooth caps optimized output around 16 frames.
- Optimized playback timing remains visually smooth.
- Strong motion peaks remain represented.
- Long static pre-roll/post-roll does not become a long playback pause.
- A subtle idle/sway clip keeps multiple meaningful frames instead of collapsing to one.
- Disabling, duplicating, or removing frames preserves the total enabled animation duration.

### 4. Resource guardrails

- A known extraction range above 2,000 estimated frames must fail before FFmpeg starts.
- A high-resolution extraction above the 6 GiB decoded-pixel budget must fail before FFmpeg starts.
- A source whose single decoded RGBA frame exceeds 256 MiB must fail before extraction.
- An unknown-duration extraction must stop safely if it crosses the 2,000-frame sentinel.
- Large preview images must remain responsive because preview decoding is capped.
- A sheet above the 512 MiB raw RGBA estimate must fail with an actionable message.
- A sheet above 16,384 px per side must fail.

### 5. Project reopen and reproducibility

- Save the project.
- Close SpriteForge completely.
- Reopen the same `project.spriteforge.json`.
- Confirm source, enabled frames, order, duration, optimizer settings, loop range, and pivots restore correctly.
- Export again without changing settings.
- Confirm the result matches the previous export visually and structurally.

### 6. Responsiveness and cancellation

During Extract, Cutout, and Normalize:

- window remains responsive;
- inline progress updates;
- Cancel stops the active job;
- previously completed stages survive;
- partial output from the cancelled stage is not committed.

### 7. Export safety

- Existing export targets are not silently overwritten.
- Metadata references the generated PNG correctly.
- Per-frame duration and pivot values are present.
- SHA-256 checksum file is generated for packaged public builds.

## Public beta boundary

A public beta can be published when:

- this checklist passes on Windows x64;
- `main` CI is green;
- a versioned package and checksum are produced;
- third-party notices are included;
- release notes are present.

Stable 1.0 additionally requires installer/signing decisions, end-user dependency onboarding improvements, and the owner-selected project license.

# SpriteForge — Implementation Readiness Audit

## Verdict
The supplied plan is implementation-ready for V1 with two explicit implementation decisions:

1. **Project persistence:** JSON (`project.spriteforge.json`) is the V1 source of truth. The supplied schema, workspace layout, migration policy, and reproducibility requirements already center on a versioned JSON document. SQLite remains a future option behind the repository contract if project scale requires indexed queries.
2. **Background removal:** V1 uses an isolated local Python worker with `rembg`, invoked through an adapter. The domain does not reference Python/rembg and can replace the implementation later.

## Ready areas
- Product scope and V1 non-goals are explicit.
- Pipeline stages and invalidation direction are explicit.
- Source-safety rules are release-blocking and implementable.
- UI information architecture is specific enough to implement without inventing a second navigation system.
- Project and export schema are sufficiently concrete for V1.
- Error/privacy boundaries are clear.
- Testing expectations are concrete.

## Remaining non-blocking choices
- No external image-to-video provider is selected. This does not block V1 because imported video and frame sequences are first-class and AI generation is explicitly optional.
- Loop score thresholds are intentionally not defined. V1 exposes ranked seam suggestions without presenting the raw score as a universal quality percentage.
- The plan does not mandate a texture-size number. V1 therefore defines an explicit **16,384 px maximum per sprite-sheet side** and fails with an actionable error rather than silently downscaling.

## Implemented V1 boundary
The repository now includes:

- WinUI 3 shell for the eight-stage pipeline;
- create/open/save/autosave project sessions;
- non-destructive image, video, and PNG/WebP frame-sequence import;
- source decode validation before replacing valid project state;
- deterministic FFmpeg extraction with staging before artifact replacement;
- local background removal through an isolated `rembg` worker;
- SkiaSharp normalization with contain/cover/original fit, auto-trim, nine anchors, and per-frame pivot metadata;
- frame enable/disable, reorder, duplicate, delete, reset, and per-frame duration editing;
- loop seam analysis, recommendations, manual boundaries, and preview playback;
- deterministic sheet layout with explicit maximum dimensions and no silent downscale;
- PNG spritesheet, JSON metadata, and optional individual PNG export;
- canonical immutable export history plus settings/frame fingerprinting;
- relative artifact path containment checks when loading projects;
- per-job logging, cancellation, FFmpeg/rembg/workspace diagnostics;
- stage-state checks that distinguish missing physical artifacts from valid cached/exported artifacts;
- Core/Application/Infrastructure/Architecture test projects;
- pipeline integration tests for real FFmpeg frame extraction and golden JSON metadata output.

## Validation boundary
Windows CI is now part of the repository. On September 20, 2026, GitHub Actions on `windows-latest` with .NET 10 successfully completed:

- `dotnet restore SpriteForge.sln`;
- Release x64 solution build, including WinUI/XAML compilation;
- Core tests;
- Application tests;
- Infrastructure tests;
- Architecture tests;
- pipeline integration tests using FFmpeg 9.0.1;
- golden JSON metadata export verification.

The integration pass also caught and fixed obsolete FFmpeg `-vsync` usage by migrating frame extraction to `-fps_mode passthrough`.

The implementation pass also completed:

- XAML, project files, props, and manifest-style XML parsing;
- XAML event-handler existence checks;
- coarse C# delimiter/static structure checks;
- project-reference architecture boundary checks;
- path-safety pattern checks;
- Python worker `py_compile`;
- FFmpeg presence and extraction smoke testing.

The remaining release validation is runtime application launch and the end-to-end V1 workflow with real media, FFmpeg, and the local `rembg` worker on a Windows desktop environment.
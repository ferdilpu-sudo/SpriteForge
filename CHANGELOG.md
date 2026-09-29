# Changelog

All notable user-visible changes to SpriteForge are documented here.

## [0.9.0-beta.1] - 2026-09-29

### Added

- Raw, Compact, Balanced, and Smooth frame-optimization modes.
- Localized motion analysis and motion-peak preservation.
- Temporal coverage filling for smoother optimized playback.
- Preview frame preloading/cache.
- **Delete disabled frames…** project cleanup action.
- Mouse-wheel horizontal scrolling for the keyframe strip.
- 2,000-frame extraction safety guard.
- Preview decode-size guard.
- Sprite-sheet memory-allocation guard.
- Versioned public-beta packaging with SHA-256 checksum.
- Tag-driven GitHub Release workflow.

### Changed

- Optimized playback timing is normalized across the active motion range.
- Cutout and downstream processing only operate on enabled frames.
- Packaged runtime checks no longer require the .NET SDK.
- Release builds treat compiler warnings as errors in CI.

### Fixed

- Incorrect keyframe selection caused by static video tails.
- Long static pauses during enabled-frame playback.
- Blank-frame/flicker glitch while preview images loaded.
- Horizontal frame-strip scroll blocker.
- Nullable-flow build warnings in project validation.

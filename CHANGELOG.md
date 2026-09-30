# Changelog

All notable user-visible changes to SpriteForge are documented here.

## [Unreleased]

### Added

- FFprobe extraction preflight with decoded-pixel and per-frame memory budgets.
- Automatic cleanup for superseded source and transient cache artifacts.

### Changed

- Loop analysis now caches sampled frames and bounds edge sampling for large Raw sequences.
- Manual enable/disable, duplicate, and remove operations preserve total animation duration.
- Untitled startup sessions remain ephemeral until the first import or explicit save.
- Large pipeline and optimizer classes are split by responsibility.

### Fixed

- Subtle continuous RGB motion no longer collapses to a single optimized frame.
- Failed preview decodes no longer leave stale preview images/cache entries.
- Late export failures roll back already-copied individual frames.


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

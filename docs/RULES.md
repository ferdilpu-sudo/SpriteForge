# SpriteForge — Engineering & Product Rules

These rules are normative. `MUST` and `MUST NOT` are release-blocking unless explicitly amended.

## 1. Source Safety
1. Original imported assets MUST NOT be modified in place.
2. Derived artifacts MUST live in generated/cache/export locations.
3. Deleting cache MUST NOT make the original project definition unrecoverable.
4. Destructive actions MUST require explicit user intent.

## 2. Architecture
1. `Core` MUST NOT reference WinUI, FFmpeg wrappers, provider SDKs, Python, databases, or OS UI APIs.
2. UI MUST NOT execute FFmpeg directly.
3. Provider implementations MUST sit behind interfaces.
4. Export formats MUST be extensible without modifying unrelated UI/domain logic.
5. Processing stages MUST be independently testable.
6. Cross-layer shortcuts are forbidden even when they make a demo work five minutes sooner. Five minutes is how architectural debt reproduces.

## 3. Pipeline
1. Every long-running stage MUST support progress when measurable.
2. Long-running work MUST NOT block the UI thread.
3. Cancellation MUST be propagated where the underlying operation supports cancellation.
4. A stage failure MUST preserve valid upstream artifacts.
5. Upstream changes MUST invalidate dependent downstream results.
6. Skipped stages MUST be explicitly represented, not silently faked as successful processing.
7. Re-processing MUST create/replace derived artifacts only after successful completion.

## 4. Frame Integrity
1. Frame order MUST be deterministic.
2. Frame index `0` is the first logical animation frame internally.
3. UI may display frame numbering starting at `1`, but serialization MUST document indexing semantics.
4. All frames used in one spritesheet MUST have a consistent output cell size.
5. Alpha MUST be preserved after transparency processing.
6. Disabled frames MUST NOT appear in final exports.
7. User frame ordering overrides automatic ordering after manual edits.

## 5. Looping
1. Loop analysis is a recommendation, never an irreversible decision.
2. Users MUST be able to override start/end frames.
3. Preview MUST loop using the exact selected export sequence.
4. Automatic optimization MUST NOT silently interpolate/generated frames in V1.
5. If interpolation is introduced later, generated/interpolated frames MUST be distinguishable in metadata.

## 6. Sprite Sheet Layout
1. Default order: left-to-right, then top-to-bottom.
2. Cell coordinates MUST be deterministic.
3. Padding and spacing MUST be represented separately.
4. Transparent unused cells MUST remain transparent.
5. Metadata MUST correspond exactly to the exported raster sheet.
6. Sheet generation MUST fail clearly rather than silently downscale due to texture-size limits.

## 7. AI/External Providers
1. AI generation MUST remain optional.
2. Local sprite processing MUST work without an LLM.
3. Users MUST know when an asset will leave the machine.
4. API keys/tokens MUST NOT be stored in project files.
5. Provider-specific IDs MAY be stored; credentials MUST NOT.
6. Provider failure MUST NOT corrupt local project state.
7. Provider adapters MUST be replaceable.

## 8. Files & Paths
1. Project-internal artifact paths SHOULD be relative to the project root.
2. Filenames MUST be filesystem-safe.
3. Temporary files MUST be uniquely named.
4. Atomic writes MUST be used for project metadata.
5. Export MUST NOT overwrite existing user files without explicit overwrite confirmation/configuration.

## 9. UI/UX
1. Primary workflow order MUST follow the actual pipeline.
2. The UI MUST expose current stage, state, and errors.
3. Advanced settings SHOULD be progressive disclosure, not a wall of controls.
4. Preview changes MUST NOT mutate source assets.
5. Keyboard navigation and tooltips are required for icon-only controls.
6. Empty, loading, error, and success states MUST be intentionally designed.
7. No modal dialog for routine progress.
8. A user should be able to import a video and reach first sprite export without configuring an AI provider.

## 10. Performance
1. Do not retain all full-resolution frames in UI memory when thumbnails suffice.
2. Thumbnail lists MUST virtualize when frame count is large.
3. Background work SHOULD use bounded concurrency.
4. Disk-space checks SHOULD occur before large extraction/export jobs.
5. Cached artifacts SHOULD use input/settings fingerprints to avoid unnecessary recomputation.

## 11. Logging
1. Logs MUST include timestamp, job ID, stage, severity, and error code when applicable.
2. Logs MUST NOT contain secrets.
3. Raw external-process output MAY be logged after secret filtering.
4. User-facing errors MUST be understandable without reading logs.

## 12. Schema
1. All persisted root documents MUST include `schemaVersion`.
2. Breaking schema changes require migration.
3. Enum values are lowercase snake_case unless an external format requires otherwise.
4. IDs are stable strings/UUIDs and MUST NOT depend on filenames.
5. Dates use ISO 8601 UTC in persistence.
6. Numeric dimensions are integer pixels unless otherwise stated.
7. FPS is a positive decimal number.

## 13. Definition of Done
A feature is not done until:
- happy path works;
- cancellation/failure behavior is defined;
- persistence impact is handled;
- relevant tests pass;
- no architecture contract is violated;
- UI has loading/error/empty state where applicable;
- documentation/schema is updated if behavior is persisted or externally observable.

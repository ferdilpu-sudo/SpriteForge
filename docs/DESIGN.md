# SpriteForge — Product & UI Design

## 1. Design Principle
SpriteForge should feel like a focused production tool, not a node editor and not a miniature video editor. The user should always understand three things: what asset is selected, which pipeline stage they are in, and what will be exported.

## 2. Desktop Shell
Recommended minimum window: 1180 × 720.

```text
┌──────────────────────────────────────────────────────────────────────┐
│ SpriteForge   Project: Ancient Tree                     _  □  ×      │
├──────────────┬───────────────────────────────────┬───────────────────┤
│ PIPELINE     │                                   │ INSPECTOR         │
│              │                                   │                   │
│ 1 Source   ✓ │          MAIN PREVIEW             │ Stage settings    │
│ 2 Animate  ✓ │                                   │ for current       │
│ 3 Frames   ✓ │       checkerboard canvas         │ selection         │
│ 4 Cutout   ✓ │                                   │                   │
│ 5 Align    ✓ │                                   │                   │
│ 6 Loop     ● │                                   │                   │
│ 7 Sheet      │                                   │                   │
│ 8 Export     │                                   │                   │
│              ├───────────────────────────────────┤                   │
│              │ [01][02][03][04][05][06][07]... │                   │
├──────────────┴───────────────────────────────────┴───────────────────┤
│ ▶  12 FPS   Loop ✓   Frame 7/24          Job status / diagnostics   │
└──────────────────────────────────────────────────────────────────────┘
```

## 3. Navigation
Use a single left pipeline rail. Do not create separate overlapping navigation systems.

Stages:
1. Source
2. Animate
3. Frames
4. Cutout
5. Align
6. Loop
7. Sheet
8. Export

States:
- neutral = not started
- active = current
- complete = valid output exists
- stale = output exists but upstream changes invalidate it
- error = stage failed
- skipped = intentionally bypassed

Do not communicate state by color alone. Use icon + text/tooltip.

## 4. Source Screen
Primary drop zone:

```text
Drop an image, video, or frame sequence here

[ Choose file ]

PNG · JPG · WebP · MP4 · WebM · MOV
```

After import show preview plus source facts. Avoid dumping codec trivia unless Diagnostics is opened.

## 5. Animate Screen
If source is an image:

```text
Animation prompt
┌──────────────────────────────────────────────┐
│ Ancient tree gently swaying in a light      │
│ breeze. Locked camera. No zoom.              │
└──────────────────────────────────────────────┘

Provider       [ Select provider ▾ ]
Duration       [ 3 sec ▾ ]

[ Generate animation ]     [ Import video instead ]
```

If no provider is configured, importing video remains first-class. Never trap the user behind account configuration.

## 6. Frames Screen
Main preview + extraction controls:
- start/end range
- target FPS
- estimated frame count
- Extract/Re-extract

Timeline thumbnails support multi-select. Re-extraction warns only about dependent derived work, not original files.

## 7. Cutout Screen
Use checkerboard canvas.

Inspector:
- Background removal: on/off
- Processor
- Alpha threshold
- Edge refinement when supported
- Reprocess selected / all

Provide before/after toggle. Hair/foliage edges make background removal imperfect, because pixels apparently enjoy philosophical ambiguity.

## 8. Align Screen
Purpose: stop objects from jittering around their cells.

Controls:
- Canvas preset/custom dimensions
- Fit: Contain / Cover / Original
- Anchor: 3×3 selector
- Auto-trim
- Pivot overlay
- Apply to selected/all

Preview can onion-skin previous/next frame as an optional visualization.

## 9. Loop Screen
This is a key differentiator.

```text
Loop Preview
[◀──────────── selected range ────────────▶]

Start frame [ 3 ]
End frame   [ 26 ]

Suggested seams
○ 3 → 26   score 0.041
○ 5 → 28   score 0.057
○ 1 → 24   score 0.063

[ Use selected suggestion ]
```

Do not label scores as percentages. Show `Best match`, `Good match`, etc. only if thresholds are empirically defined later. For V1, raw score can live in advanced details while the recommendation is simply marked `Suggested`.

Playback controls:
- play/pause
- loop
- speed
- seam flash/debug toggle

## 10. Sheet Screen
Two-panel composition preview.

Inspector:
- Cell: 512 × 512
- Columns: Auto / numeric
- Rows: calculated
- Padding
- Spacing
- Power-of-two canvas
- Frame order

Preview must render actual sheet placement and unused transparent cells.

## 11. Export Screen
V1:

```text
Export name      [ tree_idle            ]
Destination      [ C:\...\Exports       ] [Browse]

☑ Sprite sheet PNG
☑ Metadata JSON
☐ Individual PNG frames

Summary
12 frames · 4 × 3 grid · 2048 × 1536 · 12 FPS

                         [ Export ]
```

After success, show artifact cards with `Open folder` and `Copy path`. Do not throw a modal confetti parade for successfully writing a PNG.

## 12. Frame Strip
- Horizontal virtualized strip below preview.
- Thumbnail + display number.
- Disabled frames visually muted and crossed/marked.
- Context actions: enable/disable, set as loop start/end, remove from sequence.
- Drag reorder only when manual ordering mode is active.

## 13. Progress
Long-running processing uses an inline job panel/status area:

```text
Removing backgrounds
Frame 18 of 32                       56%
████████████████░░░░░░░░░░░░
[ Cancel ]
```

Do not block the whole app when users can safely inspect other project areas.

## 14. Empty/Error States
Errors state:
- what failed
- which stage
- what remains safe
- primary recovery action
- optional `View diagnostics`

Example:

```text
FFmpeg was not found.
SpriteForge needs FFmpeg to extract frames from video.

[ Locate FFmpeg ]   [ Open diagnostics ]
```

## 15. Visual Language
- Windows-native, compact creator-tool density.
- Neutral surfaces with checkerboard where transparency matters.
- Rounded corners should follow platform tokens, not excessive card nesting.
- Icons require tooltips.
- Keep the main preview visually dominant.
- Avoid gradients/decorative illustrations in production workspace.

## 16. Accessibility
- Full keyboard traversal.
- Visible focus states.
- Text labels for critical icon actions.
- State not encoded by color alone.
- Respect Windows text scaling where practical.
- Playback animation can be paused.

## 17. Responsive Desktop Behavior
At narrower widths:
- Inspector collapses into a right flyout/panel.
- Pipeline rail may collapse to icons with tooltips.
- Main preview retains priority.
- Never collapse the frame strip into an unusable postage stamp collection.

## 18. First-Run Experience
First launch performs diagnostics quietly and shows actionable setup only for missing required dependencies. AI provider setup is optional and should not appear as a mandatory onboarding wall.

First successful workflow target:
`Import video → Extract → Cutout → Align → Loop → Sheet → Export`

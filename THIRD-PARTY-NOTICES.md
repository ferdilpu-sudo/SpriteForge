# Third-Party Notices

SpriteForge uses or integrates with the following direct runtime components.

This file is a practical notice summary, not a replacement for each upstream project's license text.

## Microsoft Windows App SDK 2.5.1

- Project: Microsoft Windows App SDK
- License: MIT
- Role: WinUI 3 / Windows application runtime
- Source: https://github.com/microsoft/WindowsAppSDK

## CommunityToolkit.Mvvm 8.4.2

- Project: .NET Community Toolkit
- License: MIT
- Role: MVVM helpers used by the presentation layer
- Source: https://github.com/CommunityToolkit/dotnet

## SkiaSharp 4.151.2

- Project: SkiaSharp
- License: MIT
- Role: image decoding, normalization, analysis, and sprite-sheet rendering
- Source: https://github.com/mono/SkiaSharp

## rembg 2.0.84

- Project: rembg
- License: MIT
- Role: optional local background-removal worker
- Source: https://github.com/danielgatis/rembg

SpriteForge's beta ZIP contains the worker integration script, not a preinstalled rembg environment. `setup-worker.ps1` installs rembg and its Python dependencies into the user's local SpriteForge worker environment.

## FFmpeg

- Project: FFmpeg
- Role: external prerequisite for video validation and frame extraction
- Source: https://ffmpeg.org/

SpriteForge does not currently bundle FFmpeg in the beta ZIP. FFmpeg is primarily LGPL 2.1+ by default, while builds configured with GPL components are governed by GPL terms. The license of the user's installed FFmpeg build depends on how that build was configured.

## Transitive dependencies

NuGet and Python packages can bring transitive dependencies with their own notices and license terms. Release maintainers should review the exact dependency graph for each shipped version before stable 1.0 distribution.

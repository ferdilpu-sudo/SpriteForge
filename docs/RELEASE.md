# SpriteForge Release Process

## Current release line

Public beta candidate: `0.9.0-beta.1`.

## Required gate

From a clean checkout:

```powershell
.\scripts\verify-prereqs.ps1 -Strict
dotnet restore SpriteForge.sln --configfile NuGet.config
dotnet build SpriteForge.sln -c Release -p:Platform=x64 -warnaserror
dotnet test SpriteForge.sln -c Release
.\scripts\package-rc.ps1 -Version 0.9.0-beta.1 -Channel beta
```

Then complete `docs\V1-ACCEPTANCE.md` with real media and the real Cutout worker.

## Package outputs

The packaging script produces:

```text
artifacts/
  SpriteForge-0.9.0-beta.1-win-x64/
  SpriteForge-0.9.0-beta.1-win-x64.zip
  SpriteForge-0.9.0-beta.1-win-x64.zip.sha256
```

The ZIP contains runtime setup/smoke scripts, build metadata, changelog, acceptance notes, and third-party notices.

## GitHub release

Release workflow is tag-driven.

After the release commit on `main` is green:

```powershell
git switch main
git pull origin main
git tag v0.9.0-beta.1
git push origin v0.9.0-beta.1
```

The `Release` GitHub Actions workflow rebuilds/tests the tag and publishes the versioned ZIP plus checksum.

Do not reuse a version tag. Fix forward with a new beta version.

## Main branch protection

Configure this once in GitHub repository settings:

- require a pull request before merging;
- require the Windows CI status check;
- require branches to be up to date before merge;
- block force pushes;
- block branch deletion.

The current connector cannot configure repository branch protection, so this is an explicit repository-owner action.

## Public beta release checklist

- [ ] Main CI green.
- [ ] Manual V1 acceptance green.
- [ ] Version matches app metadata, package, changelog, and tag.
- [ ] SHA-256 checksum generated.
- [ ] Third-party notices included.
- [ ] Release notes reviewed.
- [ ] Project license selected by the repository owner.
- [ ] Public download tested on a clean Windows machine.

## Stable 1.0 additions

Stable 1.0 additionally needs:

- final application icon/branding;
- installer decision;
- code-signing strategy;
- simpler FFmpeg/Python/rembg onboarding;
- update/rollback strategy;
- accessibility review.

# Security Policy

## Supported versions

Security fixes are currently targeted at the latest SpriteForge public beta line.

| Version | Supported |
|---|---|
| 0.9.x beta | Yes |
| Older development snapshots | No |

## Reporting a vulnerability

Prefer GitHub private vulnerability reporting for this repository when it is available.

If private reporting is unavailable, open a minimal GitHub issue asking for a private reporting channel. Do not include exploit code, sensitive local paths, credentials, private media, or reproduction data that would make exploitation easier.

Useful information includes:

- affected SpriteForge version/commit;
- Windows version;
- whether the issue affects imported local files, project metadata, external processes, or export paths;
- a minimal reproduction using non-sensitive test assets;
- expected and observed behavior.

## Security boundaries

SpriteForge is designed so that:

- imported originals are copied into a project workspace and are not edited in place;
- project artifact paths must remain inside that workspace;
- FFmpeg and Python worker arguments are passed without shell execution;
- provider secrets must not be stored in project JSON or job logs;
- export refuses silent overwrite.

The public beta still executes locally installed FFmpeg and Python/rembg components. Users should obtain those runtimes from sources they trust.

# Customize with your own LLM

## Get an independent copy

1. Click **Fork** on GitHub. No approval from the author is needed.
2. Clone your fork, or use **Code > Download ZIP** if you do not need Git yet.
3. Follow `docs/PLAY.md` to get the complete runtime/development dependencies.
   `Get-OfflineDAoC.ps1` creates `playable` beside this checkout by default.
4. Open the source checkout in your LLM coding tool. Give it this starting prompt:

> This is my fork of Offline DAoC, an AI-developed single-player DAoC server with
> autonomous gamebots and companion bots. Read AGENTS.md, docs/DEVELOPMENT.md and
> the relevant component instructions first. My editable source is in source/;
> the complete installation is in playable/ (or the folder I specify). Resolve all
> paths locally, not from historical author paths. Inspect the current implementation
> before making changes. Do not start the server or replace runtime files without
> asking me. Protect my accounts, bot roster, inventory, real loot/coins, and saves.
> Build and test separately, make only the changes I request, and report exactly
> what changed and what was verified. My requested customization is: [describe it].

## Where to work

| Change | Starting point |
|---|---|
| Camlann full-PvP conversion (plan only) | `docs/CAMLANN.md` |
| Autonomous bot goals, events, travel | `source/server/GameServer/bots/autonomous` |
| Companion bots and class AI | `source/server/GameServer/bots` |
| Commands, combat, pets, spells | `source/server/GameServer` |
| Launcher and dashboards | `source/tools/OfflineDaoc.Launcher` |
| Progress import | `source/tools/OfflineDaoc.ProgressImport` |
| Navigation generation | `source/development-tools` and the release's `tools/NavmeshBuilder` |
| Active meshes | `playable/runtime/server/navmesh` |
| World definitions and local saves | `playable/runtime/data/opendaoc.sqlite3.db` — never commit after playing |
| Texture tool source | `tools/asset-tool` |
| Ready-to-run texture tool | `playable/OFFLINE DAOC ASSET TOOL` |
| Native raid UI builder | `tools/build-client-raid.py` and `source/server/tools` |

The complete release also includes source for people who downloaded without Git.
When using a fork, edit the fork's `source/` as the canonical copy and deliberately
deploy tested outputs to your separate `playable/runtime/`. Do not edit two copies
and assume they are synchronized.

## Build and test

See docs/DEVELOPMENT.md. The release's `tools/dotnet/dotnet.exe` is a bundled SDK;
`tools/nuget-feed` is the offline dependency cache. Build output is not automatically
installed. Back up saves, stop the application, and install only the intended files.

For native raid customization, use `tools/build-client-raid.py --distribution
<complete-installation> --output <new-staging-folder>`. It uses bundled dependencies
and a hash-verified baseline instead of the author's absolute paths. No automatic
deployment occurs. Read the historical scripts before using them: many were
one-time engineering/migration helpers, not reusable install commands.

## Share your changes

Commit source changes to your fork, not your runtime/save folder. Review staged
files before pushing. Keep existing licenses and attribution. You may propose a
pull request to the original project, but it will not merge itself. Your fork is
yours to customize and does not alter anyone else's local game.

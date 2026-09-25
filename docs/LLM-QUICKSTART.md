# Customize with your own LLM

The current public baseline is **v0.31**. It is an AI-created customization of
the upstream server, directed and tested by the repository owner. The experimental
Sluaghbinder is an optional **v0.31b** overlay; the immutable public v0.3 and
v0.31 baselines remain available without it. Do not infer private saves or copy
runtime files from the author's local folders. The optional class source and
static-overlay tooling are included in this repository so a fork can audit or
modify them with an LLM.

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

For a v0.31 fork, keep the ordinary release and source versioned together. Do not
commit runtime saves, accounts, logs, or private backups. If you want the old
baseline, branch from the `v0.3` tag instead of deleting or rewriting v0.31.

For the optional class, use the `v0.31b` playable release and the
`release/v0.31b-sluaghbinder` branch for its latest maintenance source. The
original `v0.31b` Git tag records the first publication and is not rewritten
for later pet updates; the release's explicit updated-source ZIP and that
branch carry the matching current source. The patcher is copy-first:
it accepts only a clean v0.3/v0.31 installation, validates hashes and the DB
schema, writes a rollback backup, and leaves the selected base untouched. The
public launcher label is 0.31b; the private local 0.4 label is not part of this
repository.

## Where to work

| Change | Starting point |
|---|---|
| Autonomous bot goals, events, travel | `source/server/GameServer/bots/autonomous` |
| Companion bots and class AI | `source/server/GameServer/bots` |
| Commands, combat, pets, spells | `source/server/GameServer` |
| Launcher and dashboards | `source/tools/OfflineDaoc.Launcher` |
| Progress import | `source/tools/OfflineDaoc.ProgressImport` |
| Navigation generation | `source/development-tools` and the release's `tools/NavmeshBuilder` |
| Active meshes | `playable/runtime/server/navmesh` |
| World definitions and local saves | `playable/runtime/data/opendaoc.sqlite3.db` — never commit after playing |
| Texture tool source | `tools/asset-tool` |
| Sluaghbinder private pet texture handoff | [`docs/LLM-SLUAGHBINDER-PET-TEXTURES.md`](LLM-SLUAGHBINDER-PET-TEXTURES.md) |
| Ready-to-run texture tool | `playable/OFFLINE DAOC ASSET TOOL` |
| Native raid UI builder | `tools/build-client-raid.py` and `source/server/tools` |
| Bounty quests, journal progress, and rewards | `source/server/GameServer/scripts/quests/Bounty` and `source/server/Tests/UnitTests/UT_Bounty*.cs` |
| Bounty map marker and UI patch | `source/server/GameServer/quests/QuestsMgr/BountyMapMarkers.cs`, `source/server/tools/patch_bounty_map_client.py`, and the optional release's two quest-journal XML payloads |

The complete release also includes source for people who downloaded without Git.
When using a fork, edit the fork's `source/` as the canonical copy and deliberately
deploy tested outputs to your separate `playable/runtime/`. Do not edit two copies
and assume they are synchronized.

The Bounty Masters are script-spawned at startup; do not copy a played
database into a fork to reproduce them. Their normal target pool reads the
installed world spawns, and completed progress uses the standard quest
journal rows. The red marker is a client-specific visual patch, not a
teleport destination. It is visible only on the map for the target's zone
or dungeon. Use the matching Release client payload and preserve the
native patch's SHA-256 guard; never run the patch on an arbitrary game.dll.

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

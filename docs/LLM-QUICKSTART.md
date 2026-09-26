# Customize with your own LLM

The current releases are **v0.32 Darkness Falls Beta** for the normal game and
**v0.32b Darkness Falls Beta** with optional Sluaghbinder. They are AI-developed
customizations of the upstream server, directed by the repository owner. The
owner has not completed a long live Darkness Falls bot test. Darkness Falls
raid AI is not implemented; Legion, the hardest level-70+ encounters,
unreachable flying targets, and unverified content are excluded from ordinary
bot goals. The v0.3, v0.31, and v0.31b tags/releases remain legacy choices. Do
not infer private saves or copy runtime files from the author's local folders.

## Get an independent copy

1. Click **Fork** on GitHub. No approval from the author is needed.
2. Clone your fork, or use **Code > Download ZIP** if you do not need Git yet.
3. Choose [normal v0.32](https://github.com/shadowofze/OfflineDAoC/tree/release/v0.32-darkness-falls)
   or [optional v0.32b](https://github.com/shadowofze/OfflineDAoC/tree/release/v0.32b-sluaghbinder-darkness-falls)
   as the source branch. Follow `docs/PLAY.md` for the matching complete
   runtime/development download. Source ZIPs and **Code > Download ZIP** do
   not contain the full playable client, world data, and bundled dependencies.
4. Open the source checkout in your LLM coding tool. Give it this starting prompt:

> This is my fork of Offline DAoC, an AI-developed single-player DAoC server with
> autonomous gamebots and companion bots. Read AGENTS.md, docs/DEVELOPMENT.md and
> the relevant component instructions first. My editable source is in source/;
> the complete installation is in the folder I specify. Resolve all
> paths locally, not from historical author paths. Inspect the current implementation
> before making changes. Do not start the server or replace runtime files without
> asking me. Protect my accounts, bot roster, inventory, real loot/coins, and saves.
> Build and test separately, make only the changes I request, and report exactly
> what changed and what was verified. My requested customization is: [describe it].

Keep the source branch and playable release at the same version. For normal
Darkness Falls work, start from `release/v0.32-darkness-falls` and its v0.32
release. It should have no Sluaghbinder class, quests, or optional client assets.
For Sluaghbinder, use `release/v0.32b-sluaghbinder-darkness-falls` and the
v0.32b playable release. The optional source includes the class and patch
tooling. Do not mix a v0.32b source build into a normal v0.32 install. The
v0.32b patcher works on a verified v0.32 base, makes a new copy, and leaves the
base intact. Do not commit runtime saves, accounts, logs, or private backups.

For legacy work, use the matching v0.3/v0.31/v0.31b tag or maintained branch.
The old [v0.31b branch](https://github.com/shadowofze/OfflineDAoC/tree/release/v0.31b-sluaghbinder)
and [release](https://github.com/shadowofze/OfflineDAoC/releases/tag/v0.31b)
remain available; do not rewrite their history or assume their patch applies
to v0.32.

The [Sluaghbinder pet-texture guide](https://github.com/shadowofze/OfflineDAoC/blob/release/v0.31b-sluaghbinder/docs/LLM-SLUAGHBINDER-PET-TEXTURES.md)
explains the private models and old-client archives. The patcher is copy-first:
the current optional patch should validate the v0.32 base and DB schema, write
a rollback backup, and leave the selected base untouched. Verify those guards
in the exact patch source before changing them. The public launcher labels are
0.32 and 0.32b; a private local label is not a release version.

## Where to work

| Change | Starting point |
|---|---|
| Autonomous bot goals, events, travel | `source/server/GameServer/bots/autonomous` |
| Companion bots and class AI | `source/server/GameServer/bots` |
| Commands, combat, pets, spells | `source/server/GameServer` |
| Launcher and dashboards | `source/tools/OfflineDaoc.Launcher` |
| Progress import | `source/tools/OfflineDaoc.ProgressImport` |
| Navigation generation | `source/development-tools` and the release's `tools/NavmeshBuilder` |
| Active meshes | `<playable-folder>/runtime/server/navmesh` |
| World definitions and local saves | `<playable-folder>/runtime/data/opendaoc.sqlite3.db` — never commit after playing |
| Texture tool source | `tools/asset-tool` |
| Ready-to-run texture tool | `<playable-folder>/OFFLINE DAOC ASSET TOOL` |
| Native raid UI builder | `tools/build-client-raid.py` and `source/server/tools` |

The complete release also includes source for people who downloaded without Git.
When using a fork, edit the fork's `source/` as the canonical copy and deliberately
deploy tested outputs to your separate playable folder. Do not edit two copies
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

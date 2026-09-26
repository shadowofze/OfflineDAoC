# Offline DAoC — single-player DAoC with bots

**AI-developed customizations, directed and tested by a human player. Open-source server and customization code.**

Offline DAoC is a local, single-player Dark Age of Camelot setup with autonomous
gamebots, recruitable companion bots, raids, realm events, navigation, a launcher,
and development tools. It builds on Dawn of Light/OpenDAoC and other existing
projects; this is not a claim that AI created the original game or all upstream code.
It is a community project, not an official DAoC product or a product for sale.

## Fork it and make it yours

You do **not** need to ask permission to fork this public repository. Click **Fork**
to make a copy under your own GitHub account, then give your preferred LLM the
checkout and `AGENTS.md`. Changes in your fork do not change this repository or
the author's local installation. Pull requests are proposals, not automatic updates.
Follow the included component licenses when modifying or redistributing code.

## Play / download

- **Players:** [Download and play instructions](docs/PLAY.md).
- **Everyday commands:** [Quick commands and bot-generation shortcuts](docs/QUICK-COMMANDS.md).
- **Developers and LLM users:** [Fork and customize instructions](docs/LLM-QUICKSTART.md).
- **Changelog:** [Full version history and Darkness Falls Beta notes](CHANGELOG.md).
- **Current release notes:** [v0.32 normal](docs/RELEASE-0.32.md) and
  [v0.32b with Sluaghbinder](docs/RELEASE-0.32B.md).

## Video demos

These are linked previews, not large files stored in the repository. Click a
thumbnail to watch on YouTube:

[![Offline DAoC V3.1b — Optional Hibernian Sluaghbinder Class Expansion](https://i.ytimg.com/vi/EowrCcjigBY/hqdefault.jpg)](https://www.youtube.com/watch?v=EowrCcjigBY)

**[Offline DAoC V3.1b — Optional Hibernian Sluaghbinder Class Expansion](https://www.youtube.com/watch?v=EowrCcjigBY)**
Short introduction to the optional class.

[![Offline DAoC v0.3 — Introduction to Raids & Realm Events](https://i.ytimg.com/vi/zmh7YkajRx0/hqdefault.jpg)](https://www.youtube.com/watch?v=zmh7YkajRx0)

**[Offline DAoC v0.3 — Introduction to Raids & Realm Events](https://www.youtube.com/watch?v=zmh7YkajRx0)**
Dragon raid demonstration for the normal v0.3 feature set.

The current downloads are [v0.32 Darkness Falls Beta](https://github.com/shadowofze/OfflineDAoC/releases/tag/v0.32)
for normal play and [v0.32b Darkness Falls Beta](https://github.com/shadowofze/OfflineDAoC/releases/tag/v0.32b)
for the optional Sluaghbinder class. They contain all shared fixes through v0.31,
including Bounty Masters, Bard and companion song repairs, and bot route fixes.
**Beta** means the owner has not yet completed a long live bot test in Darkness
Falls. Darkness Falls raid AI is not implemented; Legion, the hardest
level-70+ encounters, and unreachable flying targets are excluded from
ordinary bot goals. [Read the implementation and limits](docs/RELEASE-0.32.md).

Use the release's download helper and `Get-OfflineDAoC.ps1` for the complete
playable game. **Code > Download ZIP** contains editable source, not the full game.
The [v0.3](https://github.com/shadowofze/OfflineDAoC/releases/tag/v0.3),
[v0.31](https://github.com/shadowofze/OfflineDAoC/releases/tag/v0.31), and
[v0.31b](https://github.com/shadowofze/OfflineDAoC/releases/tag/v0.31b)
releases stay available as legacy versions.

The intended supported target is a compatible **64-bit Windows PC**. The launcher
uses Windows Forms and the legacy game client has Windows/graphics prerequisites;
“any PC” does not mean native macOS/Linux or every CPU/driver combination.

System requirements: CPUs without AVX2 support will not work. 16 GB RAM is the
recommended minimum; 8 GB may work but is untested.

The copy-first download keeps rollback folders and verified download parts.
Budget roughly **40 GB free for v0.32** or **55 GB for v0.32b**, with extra
headroom for your saves and future updates. Existing installations are not
deleted automatically.

The current complete download includes clean world data, navigation meshes, the
runnable components, source, and offline development dependencies. Accounts, characters,
inventories, saved bot profiles and personal settings from the author's game are
not included. Each installation creates its own local account and saves.

### Pick the play path that fits you

You do **not** need an LLM, Git, or programming knowledge to play. Use one of
these two clearly separate paths:

| What you want | What to download | What happens |
| --- | --- | --- |
| Normal Classic + Shrouded Isles with Darkness Falls | The **v0.32 Darkness Falls Beta** release's `DOWNLOAD-AND-PLAY-v0.32.cmd` and `Get-OfflineDAoC.ps1` | Builds a fresh normal v0.32 game from the preserved v0.31 base. No Sluaghbinder class is installed. |
| The same game **plus** optional Hibernian Sluaghbinder | The **v0.32b Darkness Falls Beta** release's `DOWNLOAD-AND-PLAY-v0.32b.cmd` and `Get-OfflineDAoC.ps1` | Builds a fresh v0.32 base, then applies the optional class patch into a separate v0.32b copy. |

Download both files from the same release into one new folder, then double-click
the helper. The download verifies its parts and refuses to overwrite an existing
installation. Keep the earlier version's folder as a rollback path. See the
[step-by-step play guide](docs/PLAY.md) before transferring an existing save.

For either path, open the new playable folder, read `READ ME FIRST.txt`, start the
launcher, click **START SERVER**, wait for **RUNNING**, then click **ENTER
REALM**. The launcher creates a local offline account automatically; no online
account or LLM is required. Choose v0.32b only when you want a Hibernian
Acolyte and the Sluaghbinder trainer and quests in-game.

Both new versions retain repeatable Bounty Masters in Cotswold,
Mularn, and Mag Mell. Take one hunt, follow its journal kill count, and return
for a reward. The red target marker appears on the local map after you enter
the assigned zone or dungeon.

## Darkness Falls Beta in v0.32 and v0.32b

Characters and autonomous bots from Albion, Midgard, and Hibernia can enter
Darkness Falls in this offline setup. Bots have staged, floor-aware routes for
ordinary dungeon grinding, their own realm exits, and opposing-realm fights
near the shared center. Players can use the existing seal vendors. The routes
account for one-way drops and ledges; a bot should not try to walk back up a
drop. These are staged intended behaviors, not a claim of live bot success.
Darkness Falls raid AI is not implemented. Legion, the hardest level-70+
encounters, unreachable flying targets, and unverified routes are excluded
from ordinary bot goals. Other Classic/SI raid features shown in the v0.3
demo are separate.

## Optional Sluaghbinder expansion (v0.32b)

Sluaghbinder is an optional Hibernian player class. Normal v0.32 does not include
its class, quests, or client assets. For the current optional version, use the
[v0.32b release](https://github.com/shadowofze/OfflineDAoC/releases/tag/v0.32b)
and its `DOWNLOAD-AND-PLAY-v0.32b.cmd` helper. The optional installer verifies
the v0.32 base and patch, makes a new sibling copy, and places a rollback
command in that copy. Older [v0.31b instructions](docs/RELEASE-0.31B.md) remain
available for legacy installations.

The expansion includes the Sluaghbinder character path (Acolyte through level
5 promotion), its three core lines and three trainable paths, dedicated player,
companion, and autonomous gamebot behavior, Muirenn in Tir na Nog, and the
five chained epic quests that unlock the Epic Spells service summons. It does
not export or import the author's accounts, characters, bot roster, settings,
or saves. If you do not want the class, use normal v0.32.

## Customize with your own LLM

- `source/server`: current server, bot AI, combat, spells, groups, sieges, economy,
  routes, world-goal resources, tests, and historical engineering scripts.
- `source/tools`: current launcher, progress importer, archive utilities and tests.
- `source/development-tools`: navigation builder and matching native pathing source.
- `source/server/tools`: native raid UI / bot-map patch builders and tests, in
  addition to server diagnostics and migration utilities.
- `tools/asset-tool`: texture-tool source, profiles and tests.
- [Normal v0.32 source](https://github.com/shadowofze/OfflineDAoC/tree/release/v0.32-darkness-falls) contains Darkness Falls without Sluaghbinder. [Optional v0.32b source](https://github.com/shadowofze/OfflineDAoC/tree/release/v0.32b-sluaghbinder-darkness-falls) contains both.
- [LLM guide to the optional Sluaghbinder pet textures](https://github.com/shadowofze/OfflineDAoC/blob/release/v0.32b-sluaghbinder-darkness-falls/docs/LLM-SLUAGHBINDER-PET-TEXTURES.md): private NIF/DDS registrations, legacy MPK rules, visual testing, and rollback. The older v0.31b source remains on its [legacy branch](https://github.com/shadowofze/OfflineDAoC/tree/release/v0.31b-sluaghbinder).
- `source/reference`: additional launcher/portal source snapshots. These are
  reference material, not substitutes for the current launcher.
- `docs/DEVELOPMENT.md`: build, safety, portability, and dependency notes.

No gameplay features have intentionally been removed for sharing. However, the
original game executable is a binary dependency: the material found here includes
customization/patch source, not a complete source tree for the original DAoC client.
The server's license does not relicense third-party client assets or dependencies.
Their existing rights and notices remain applicable; see `THIRD_PARTY.md`.

## Privacy and defaults

The public baseline uses fresh saves, normal-player access, 1x XP, no pre-created
bot roster, and default keep/relic ownership. Settings can be changed locally.
Keep your own save database, credentials and logs out of commits. `.gitignore`
is a safety net, not a substitute for reviewing `git diff --cached` before pushing.

This repository is a clean baseline, not the author's old Git history or backups.
The customizations in this repository were produced by AI coding agents under
the author's direction, then reviewed and tested on the author's offline setup.
That statement does not relicense upstream OpenDAoC or third-party client assets.
AI-generated code can contain bugs: review changes, test a disposable copy, and
back up saves before installing a build. No zero-regression guarantee is implied.

## Current version scope

v0.32 is the normal Darkness Falls Beta and contains no Sluaghbinder class,
quests, or patch. v0.32b adds that optional class to the same current feature
set. Their source and playable downloads are versioned together. The v0.3,
v0.31, and v0.31b tags and releases remain separate legacy downloads.

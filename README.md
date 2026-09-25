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
- **Changelog:** [Version history and the v0.31 scope](CHANGELOG.md).

Use the [v0.31 release](https://github.com/shadowofze/OfflineDAoC/releases/tag/v0.31)
for the complete playable download. **Code > Download ZIP** contains the editable
source; it is not the complete game download. The release's small download helper
fetches, verifies and extracts the large parts automatically. The original
[v0.3 release](https://github.com/shadowofze/OfflineDAoC/releases/tag/v0.3) remains
available and is never replaced; use it when you want the unmodified v0.3 baseline.

The intended supported target is a compatible **64-bit Windows PC**. The launcher
uses Windows Forms and the legacy game client has Windows/graphics prerequisites;
“any PC” does not mean native macOS/Linux or every CPU/driver combination.

System requirements: CPUs without AVX2 support will not work. 16 GB RAM is the
recommended minimum; 8 GB may work but is untested.

The v0.31 release includes clean world data, current navigation meshes, the
runnable components, source, and offline development dependencies. Accounts, characters,
inventories, saved bot profiles and personal settings from the author's game are
not included. Each installation creates its own local account and saves.

The maintained v0.31 update also includes repeatable realm bounties for all
three factions. Visit the Bounty Master in Mag Mell, Cotswold Village, or
Mularn; the quest journal tracks progress. See [how bounties and the local
map marker work](docs/PLAY.md#optional-repeatable-bounties-within-normal-v031-play).
The latest `OfflineDAoC-v0.31-bard-beach-rats-update.zip` also corrects Bard
bot PvE mez use and the low-level Shannon Estuary beach-rat camp; the helper
selects it automatically through the release manifest.

## Customize with your own LLM

- `source/server`: current server, bot AI, combat, spells, groups, sieges, economy,
  routes, world-goal resources, tests, and historical engineering scripts.
- `source/tools`: current launcher, progress importer, archive utilities and tests.
- `source/development-tools`: navigation builder and matching native pathing source.
- `source/server/tools`: native raid UI / bot-map patch builders and tests, in
  addition to server diagnostics and migration utilities.
- `tools/asset-tool`: texture-tool source, profiles and tests.
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

## v0.31 scope

v0.31 is the normal Classic/Shrouded Isles maintenance update. It does **not**
include the optional Sluaghbinder class, its quests, or its patch. That expansion
is available only through the separate [v0.31b release](https://github.com/shadowofze/OfflineDAoC/releases/tag/v0.31b).
The normal source and playable download are versioned together, while v0.3
stays downloadable as a separate immutable release.

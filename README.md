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
- **This fork's changes:** [Changelog](CHANGELOG.md). The launcher pin
  (`DisplayVersion`) is this fork's version. The playable runtime is still the
  upstream v0.3 download.

Use the [v0.3 release](https://github.com/shadowofze/OfflineDAoC/releases/tag/v0.3)
for the complete playable download. **Code > Download ZIP** contains the editable
source; it is not the complete game download. The release's two small download
helpers fetch, verify and extract the large parts automatically.

The intended supported target is a compatible **64-bit Windows PC**. The launcher
uses Windows Forms and the legacy game client has Windows/graphics prerequisites;
“any PC” does not mean native macOS/Linux or every CPU/driver combination.

System requirements: CPUs without AVX2 support will not work. 16 GB RAM is the
recommended minimum; 8 GB may work but is untested.

The release includes clean world data, current navigation meshes, the runnable
components, source, and offline development dependencies. Accounts, characters,
inventories, saved bot profiles and personal settings from the author's game are
not included. Each installation creates its own local account and saves.

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
AI-generated code can contain bugs: review changes, test a disposable copy, and
back up saves before installing a build. No zero-regression guarantee is implied.

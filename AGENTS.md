# Offline DAoC: start here

This project contains AI-developed customizations of existing DAoC server projects.
Read README.md, CHANGELOG.md, docs/DEVELOPMENT.md, and the relevant component's
AGENTS.md before editing.

## Changelog and versioning

This fork uses MAJOR.MINOR.PATCH. The launcher pin `DisplayVersion` and the
latest dated heading in CHANGELOG.md must match. The upstream playable
download stays GitHub v0.3; do not rewrite Get-OfflineDAoC.ps1 or the PLAY.md
download steps when bumping this fork.

Every completed change set must:

- Add a dated version heading in CHANGELOG.md with Added / Changed / Fixed /
  Removed bullets. Keep the Unreleased section empty between tasks; do not
  pile work there and forget to bump.
- Bump once per finished task, using the highest applicable level:
  - PATCH (0.3.1 → 0.3.2): docs, agent rules, tests-only, comments, or
    tooling that does not change gameplay.
  - MINOR (0.3.1 → 0.4.0): new or changed gameplay or launcher behavior that
    still loads the existing save.
  - MAJOR (0.4.0 → 1.0.0): save/schema incompatibility, required progress
    import, a native client patch, or a new playable package. Also use 1.0.0
    if the owner explicitly declares a stable fork release.
- Keep these in lockstep with the changelog heading:
  - `source/tools/OfflineDaoc.Launcher/MainForm.cs` (`DisplayVersion`)
  - `source/tools/OfflineDaoc.Launcher.Tests/LauncherPresentationTests.cs`
  - `ALL SERVER COMMANDS.txt` header
- Never put live save data, credentials, or hashes of personal databases in
  the changelog.

Do not jump to a bare `0.4` label. Three-part versions keep this fork
distinct from the original author's private 0.4 launcher.

## Safety defaults

- Resolve paths from this checkout, never from the original author's Windows username.
- Distinguish real players, companion bots, and autonomous gamebots before changing AI.
- Preserve saves, real inventories/loot/coins, equipment upgrades, realm exchange,
  and existing travel behavior unless the owner explicitly asks to change them.
- Historical scripts in source/server/tools may contain absolute deployment paths
  and destructive migration operations. Read and parameterize them before use.
  Do not execute them just because they are present. They are not the bootstrap.
- Never start a game server or deploy over a running installation without permission.
- Build/test in a separate output tree. Never publish a live SQLite database,
  account.txt, bot profiles, credentials, logs, dumps, or previous Git history.
- Keep native client patch hash guards. Never apply a binary patch to an unverified
  client build. Texture atlases are not mesh/skeleton replacements.
- Do not globally rebuild/replace navigation to fix one local route without evidence.
- Report offline/static checks separately from real-client gameplay verification.
- The user may customize their fork's rules. These are safety defaults, not a ban
  on intentional gameplay changes requested by the fork owner.

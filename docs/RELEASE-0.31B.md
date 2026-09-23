# Offline DAoC v0.31b — optional Sluaghbinder expansion

`v0.31b` is an optional overlay on the normal v0.3/v0.31 playable release. It
does not replace either baseline and is not required for ordinary Classic +
Shrouded Isles play.

## If you only want to play

Use `DOWNLOAD AND PLAY v0.31b.cmd` from the repository copy, or download the
latest `Sluaghbinder-v0.31b-pet-refresh-patch.zip` release asset and run
`INSTALL SLAUGHBINDER PATCH.cmd`. Select your clean v0.3 or v0.31 folder when asked. The installer
creates a new sibling folder ending in `-Sluaghbinder-v0.31b`; it never writes
back to the folder you selected. Start the new copy with its normal
`START OFFLINE DAOC.cmd`. If the optional class is not wanted, use the ordinary
v0.3/v0.31 downloader instead.

The patch keeps the selected copy's local account, characters, inventory,
settings, and bots. It writes a consistent pre-patch database backup and a
`ROLLBACK SLAUGHBINDER PATCH.cmd` command in the new copy. Stop the server and
game before rollback. The rollback restores only that patched copy; it does not
remove or alter the original.

Sluaghbinder is Hibernian and uses Celt or Firbolg. New characters begin as
Acolytes and are promoted by Muirenn at level 5. Core spells arrive with level;
the three optional paths are trained. The five epic quests are consecutive and
unlock the Epic Spells page and its stationary, player-only skeletal services.
Quest objectives and locations are intentionally discovered in-game; this file
does not spoil their clues.

The included launcher also carries the v0.31 exchange-layout repair: Eilwen's
two Sentinel Exchange Guards are placed beside her in Tir na Nog. The repair is
limited to those two Hibernian guard rows and does not move Eilwen or any other
broker.

The companion `/spawn` path now commits each Sluaghbinder to one advanced
build at creation. Bulwark companions start with blunt and shield, Bane
companions start with a scythe, and Covenant companions randomly choose either
weapon plan while retaining their pet-focused spell and buff line. The normal
Sluaghbinder spell/style filtering is build-specific, so a companion receives
only its chosen path's taunts, scythe/life-steal/rot tools, or pet enhancements.
Persistent autonomous Sluaghbinders retain their existing deterministic build
choice; this repair only makes the starter and reconciliation equipment honor
that choice.

The latest optional v0.31b patch also makes Zombie Defender 33% larger with
its own rusty-plate appearance while keeping its prior shield. Dullahan is
50% larger with a private dark armored appearance, a chain morningstar and
existing dark weapon effect, and no offhand shield. Zombie Priest carries a
dagger rather than mace and buckler. Stock monsters keep their appearances.
Covenant companion and autonomous bots wait for an active one-minute pet
heal-over-time to finish before recasting; direct heals and other builds are
unchanged. The copy-first installer merges the private client assets into the
new copy and backs them up for rollback.

The maintained optional-class source is on `release/v0.31b-sluaghbinder` and
in the v0.31b release's explicit updated-source ZIP. The original v0.31b Git
tag remains an initial-publication record. LLM developers can follow the
[private pet-texture guide](https://github.com/shadowofze/OfflineDAoC/blob/release/v0.31b-sluaghbinder/docs/LLM-SLUAGHBINDER-PET-TEXTURES.md).

## Maintenance fixes included in the current 0.31b source

The optional source carries the same narrow camp, PvE recovery, Reaver Flexible
equipment, and stale service-assignment fixes as normal v0.31. They do not alter
Sluaghbinder rules, player-only skeletal services, realm-event groups, or the
preserved v0.3 baseline.

## If you want to develop with an LLM

Fork the repository and give the fork and `AGENTS.md` to the LLM. The complete
Sluaghbinder source is under `source/server`, the client-build-identifying
patch helpers are under `source/server/tools`, and the static overlay builder
is under `tools/sluaghbinder/build_overlay.py`. The patch executable source is
`source/tools/OfflineDaoc.SluaghbinderPatch`. Build the server and launcher in
the normal Release configuration, then regenerate the static overlay from a
disposable database. Never commit a runtime database, account file, logs,
backups, or private bot records.

The public launcher in this tag displays **0.31b**. The private 0.4 launcher is
not used or published here.

# Offline DAoC v0.31b — optional Sluaghbinder expansion

`v0.31b` is an optional overlay on the normal v0.3/v0.31 playable release. It
does not replace either baseline and is not required for ordinary Classic +
Shrouded Isles play.

## If you only want to play

Use `DOWNLOAD AND PLAY v0.31b.cmd` from the repository copy, or download the
`Sluaghbinder-v0.31b-patch.zip` release asset and run `INSTALL SLAUGHBINDER
PATCH.cmd`. Select your clean v0.3 or v0.31 folder when asked. The installer
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

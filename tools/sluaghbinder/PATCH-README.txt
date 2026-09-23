Offline DAoC v0.31b — optional Sluaghbinder expansion

PLAYER QUICK START

1. Close the launcher, server, and game for the base installation.
2. Run INSTALL SLAUGHBINDER PATCH.cmd.
3. Select a clean v0.3 or v0.31 Offline DAoC folder.
4. Wait for the new sibling folder ending in -Sluaghbinder-v0.31b.
5. Open that new folder and run START OFFLINE DAOC.cmd.

The installer copies your local progress into the new folder but never changes
the folder you selected. It verifies the class/server/client payload and runs a
SQLite integrity check. If you do not want Sluaghbinder, keep using your normal
v0.3 or v0.31 folder.

This update includes private Zombie Defender and Dullahan model/texture entries,
the Zombie Priest dagger and Dullahan equipment changes, and the Covenant pet
heal-over-time upkeep fix. Client catalogs and skin archives are merged into
the new copy; stock monster appearances are not replaced. The optional patch
does not affect the normal v0.3 or v0.31 releases.

ROLLBACK

The new folder contains ROLLBACK SLAUGHBINDER PATCH.cmd. Stop the server and
game, run it, and it restores that copy's original files and database. It does
not delete or alter the original base folder. Backups stay under
runtime\.sluaghbinder-backup.

For developers editing the private pet textures, read
LLM-SLUAGHBINDER-PET-TEXTURES.md in this package or docs/LLM-SLUAGHBINDER-PET-TEXTURES.md
in the GitHub source. The old client needs correctly bound NIF/DDS catalog rows
and case-insensitively ordered MPK entries; a PNG alone is not a usable mesh.

The public launcher label is 0.31b. The author's private local 0.4 build is not
part of this patch. Quest details are intentionally left to the in-game clues.

# Sluaghbinder data overlay

The optional class is applied to a copy of an existing v0.3/v0.31 installation.
`build_overlay.py` reads a clean, disposable test database in read-only mode and
exports only class 63's static rows. It never exports accounts, characters,
inventory, bots, settings, logs, or backups.

The generated `overlay.json` is consumed by `Install-Sluaghbinder.ps1`. The
installer backs up the target database, validates the schema before writing,
removes only rows owned by the feature, inserts the overlay, runs SQLite
`quick_check`, and writes a rollback manifest. The patch is copy-first: the
selected installation is never overwritten unless the user explicitly chooses
an output folder that does not already exist.

The shared Bounty Masters are script-spawned by the updated server. They do
not require a separate static database overlay or any player's quest data.
The current patch payload includes the corresponding client map renderer and
quest-journal XML files; rollback restores the copied client's prior files.

The current optional overlay also exports only named Sluaghbinder
`NPCEquipment` templates. `client-assets-v0.31b` contains two private NIFs
and two private DDS atlases. The patch executable merges their catalog rows
and texture entries into the copied client's own MPAK archives, checks the
legacy case-insensitive skin-entry order, and preserves unrelated stock rows.
The installer backs up those archives and records whether each private NIF
already existed so rollback can restore or remove the exact paths safely.
See `docs/LLM-SLUAGHBINDER-PET-TEXTURES.md` for the model/skin chains and
manual texture-editing workflow.

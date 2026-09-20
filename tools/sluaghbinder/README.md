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

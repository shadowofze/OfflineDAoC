# v0.31b verification

This optional release was checked in the public GitHub publication checkout;
the private local 0.4 installation and the isolated test database were not
modified.

- Release server source build: 0 errors.
- Full server suite: **1,893 passed, 0 failed** (environment-only probes remain
  skipped exactly as in the normal release).
- Launcher suite: **96 passed, 0 failed**, with the public launcher label
  **0.31b**.
- Sluaghbinder patch executable: Release build succeeded with 0 errors.
- Disposable copy-first installer test: static overlay applied, payload hashes
  verified, SQLite `quick_check` returned `ok`, and the original base folder was
  unchanged.
- Extracted-release-asset test: the same installer worked from a freshly
  extracted `Sluaghbinder-v0.31b-patch.zip`.
- Rollback test: the generated rollback command restored the patched copy's
  pre-patch files and database backup; no original folder was touched.

The public asset is a small binary/source overlay, not a copy of a player's
runtime. It contains no account, character, inventory, bot, settings, log, or
backup data. The game client payload is build-identified and hash-checked by the
patcher; an unknown/private 0.4 base is refused.

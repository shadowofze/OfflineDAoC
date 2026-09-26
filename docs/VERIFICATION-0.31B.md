# v0.31b verification

## 2026-09-26 Bard companion/gamebot hotfix

- The refreshed v0.31b Release server build succeeded and the full server
  suite passed: **1,981 passed, 0 failed**. Focused Bard tests cover solo,
  grouped, and null-group combat paths.
- The optional patch ZIP, `Sluaghbinder-v0.31b-bard-guard-patch.zip`, is
  **15,205,764 bytes** with SHA-256
  `0DBFFF1DB00D517D38545235817FF3C7094E2FCB28F8E44EDB188CEE1CC59A6E`.
  All 31 entries passed the archive CRC check and have the expected patch root.
- The exact ZIP installed successfully into a disposable copy of the refreshed
  normal v0.31 release. Its three server DLL locations match the new optional
  payload (SHA-256 `1AB2F99B2B4FC8DEDEF23AA1D9A5CD5C54AB983A8BC5AD5D58502A103B48F91D`)
  and the static Sluaghbinder database overlay was applied. The normal base
  installation remained unchanged.
- Rollback on that disposable installed copy exited successfully: all 16 file
  receipts matched their original hashes, both private NIF files were removed,
  and the database returned byte-for-byte to its pre-patch SHA-256
  `F837C08A663BDE2A0E710EB5337F5187779B1039BFAD4F968CB9BA3923B81733`.
- This is a copy-first installer/rollback test, **not** a live-client visual or
  long-running gamebot test. Existing release tags and assets were not changed.

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
- Pet overlay check: template `60170003` (Sturdy Zombie) now uses model `467`
  with no visible weapon, and template `60170004` (Zombie Magician) uses
  Murkman model `446` while retaining its staff template. Their spell, stat,
  and damage fields are unchanged.

## 2026-09-22 optional pet refresh

- Current v0.31b server Release build: **0 errors**; full server suite:
  **1,895 passed, 0 failed**. The Covenant one-minute pet HoT guard has
  focused tests for active/expired effects, direct heals, and other specs.
- Static overlay contains 10 whitelisted tables and 12 Sluaghbinder-owned
  `NPCEquipment` rows; it contains no account, character, inventory, bot,
  setting, or save tables. Defender template `60170005` uses private model
  `2494` and size `67`; Dullahan template `60170007` uses model `2495`,
  size `75`, and no shield slot; Priest template `60170006` has only its dagger.
- The four packaged private source assets match the isolated build's
  two NIFs and two DDS entries byte-for-byte. The client-asset merger on a
  disposable clean public v0.3 client preserved stock catalog rows and every
  original skin entry, validated archive CRCs and both offset tables, and gave
  byte-identical results on a second application.
- Full copy-first installation from a clean public v0.3 distribution passed:
  14 file receipts matched their installed hashes, the database passed
  `quick_check`, and the original base retained its recorded hashes.
  Rollback of that disposable patched copy restored the database and 12
  original files byte-for-byte and removed the two newly added private NIFs.
- The public v0.31 update ZIP contains no `runtime/data` or client-app files,
  so it uses the same v0.3 database and client assets for this merge; its
  differing launcher/server files are replaced by the explicit v0.31b payload.
- The final patch ZIP's entries have the expected root, four private assets,
  and a clean ZIP CRC check. These are static/integration checks; the exact
  public ZIP has **not** been visually tested in a live game client here.

The disposable rollback test had to bypass the script's global running-process
guard in its test shell because unrelated game/server processes were open.
The distributed rollback script retains the guard and requires the game to be
closed. Neither that test nor this release preparation changed the separate
original game or the current isolated class-test installation.

The public asset is a small binary/source overlay, not a copy of a player's
runtime. It contains no account, character, inventory, bot, settings, log, or
backup data. The game client payload is build-identified and hash-checked by the
patcher; an unknown/private 0.4 base is refused.

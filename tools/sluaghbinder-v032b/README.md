# Sluaghbinder v0.32b patch source

This is the separate optional Sluaghbinder package for the normal **v0.32
Darkness Falls Beta** game. It makes a new copy of a verified v0.32 installation
in a sibling folder and leaves that base untouched. If any preparation or
patch step fails, the installer discards only the new incomplete copy after
checking its exact location; it never removes the selected base. The legacy `tools/sluaghbinder` directory is
for v0.31b and its earlier bases; do not mix its manifest, payload, installer,
or rollback script with this one.

The patch combines the compiled v0.32b server/launcher files in
`payload-v0.32b`, the class-only `overlay.json`, and four private files in
`client-assets-v0.32b`. It merges the private NIF/DDS catalog registrations
into the copied client. It does not include a complete world database or the
Shannon Estuary SQL patch: the normal v0.32 base already has the public world
changes, including the public level-69 Darkness Falls boss. The class overlay
contains only named Sluaghbinder static rows, not accounts or characters.

## Package integration

1. Build the matching optional v0.32b server and launcher separately, then
   stage only the changed runtime files under `payload-v0.32b` with their
   installation-relative paths. Include the public launcher label **0.32b**.
2. Build `source/tools/OfflineDaoc.SluaghbinderPatch` for win-x64 and stage its
   executable plus its required runtime files under `patcher/`. Its database
   mode is called **without** `--world-patch`.
3. Provide a clean final normal v0.32 installation as the base to
   `Seal-PatchManifest.ps1`. The sealing script records the exact launcher and
   server hashes and each payload file's size/hash. The checked-in manifest
   deliberately has placeholders; the installer refuses it until sealed.
4. Build the patch ZIP with root `OfflineDAoC-Sluaghbinder-v0.32b-patch/` and
   include this folder's installer, rollback script, manifest, overlay,
   assets, `payload-v0.32b`, `patcher`, and player README. Do not include an
   account file, world/save database, logs, or personal progress.
5. Test install and rollback on a complete disposable v0.32 copy. Check the
   v0.32 base still exists unchanged, the optional copy has label 0.32b and
   the class rows/assets, and rollback restores that copy's prepatch state.

The standalone class overlay generator (`build_overlay.py`) accepts a
disposable class-test database in read-only mode. Do not generate the public
world database from that class-test copy. The v0.32 world data, including the
boss level, remains authoritative.

Darkness Falls raid AI is not implemented; Legion, hardest level-70+
encounters, unreachable flying targets, and unverified routes are excluded
from ordinary autonomous bot goals. A long live Darkness Falls bot test has
not yet been completed.

# Release verification

This is the GitHub v0.31 distribution of the normal maintenance functionality, not
a rollback to an older gameplay version. The author's local launcher remains
labeled 0.4. The separate v0.3 tag/release is preserved.

## Checks performed for this sharing copy

- Current server source restored and rebuilt: zero build errors. Existing compiler
  warnings were retained rather than changing gameplay for publication.
- 1,893 normal server tests passed. Explicit installed-environment probes are not
  presented as having run in that ordinary test total.
- 96 launcher tests passed with the public launcher label set to 0.31.
- 23 texture-tool tests passed using temporary edit/rollback copies.
- Native 40/80 raid client rebuilt from the included baseline and customization
  source, matching the installed client SHA-256 exactly.
- Four offline downloader cases passed: valid package, corrupt download rejection,
  traversal-path rejection, and refusal to overwrite an existing installation.
- 31,850-file runtime baseline checked against the current installation, with
  only the documented launcher/configuration/profile differences permitted.
- All 99 current navigation meshes retained byte-for-byte.
- Clean seed database integrity checked; account, character, saved bot, inventory,
  economy-history and other progress tables checked empty before packaging.
- No author account.txt or old Git history is included; source reviewed for
  credentials and personal-save files before publishing.
- A separate relocated zero-bot copy started with its bundled runtime, generated
  and preserved fresh local credentials, and automatically created a normal-player
  account through the real game client. The owner confirmed realm selection.
  The test server then shut down normally; its saves are not part of the release.

## v0.31 maintenance checks

- Player-owned summoned-pet spell scaling resolves the real player owner, including
  nested pet ownership; companion-only Bonedancer upkeep remains scoped to temporary
  `/spawn` companions.
- Audited large-dragonfly, boobrie-hatchling, feccan, huldu-outcast and green-serpent
  camps use live anchors, prove a reachable target, and only use measured recovery
  pockets after normal recovery fails.
- Effect state transitions no longer hold an effect lock while taking the owner's
  effect-list lock, preventing the verified worker deadlock/freeze path.
- Focused pet/effect and route-policy tests were run for the v0.31 source changes;
  the exact test command/results are recorded with the release notes.

The v0.31 update asset is sealed after source/build checks and is checked by
SHA-256 before it is applied. `download-manifest.json` and `SHA256SUMS.txt` on the
release provide the patch hash and byte count; the updater also rechecks the
preserved v0.3 seed before applying anything.

## Intended sharing differences

Fresh local account bootstrap; clean progress and default settings; GitHub launcher
label 0.31; an isolated client preference profile. Existing/imported credentials are
preserved. Game saves are local to the extracted installation. The legacy client
still uses its isolated Windows AppData profile for display preferences.

## Limits

Verification on one Windows PC is not proof of every CPU, graphics driver or
operating system. Ordinary tests and file hashes do not constitute an overnight
gameplay soak test of every encounter. The code and tools are shared so defects
can be inspected and fixed, not as a claim that AI-generated software is bug-free.

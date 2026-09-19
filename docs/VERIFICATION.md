# Release verification

This document records the GitHub v0.3 package, not a rollback to an older
gameplay version. This fork's launcher pin is `DisplayVersion` in
`source/tools/OfflineDaoc.Launcher/MainForm.cs` (see CHANGELOG.md), not the
0.3 label below. The original author's private launcher remains labeled 0.4.

## Checks performed for this sharing copy

- Current server source restored and rebuilt: zero build errors. Existing compiler
  warnings were retained rather than changing gameplay for publication.
- 1,886 normal server tests passed. Explicit installed-environment probes are not
  presented as having run in that ordinary test total.
- 96 launcher tests passed with the public launcher label set to 0.3.
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

The release archive is sealed only after the separate startup test and then every
archive entry is checked by CRC and SHA-256. `PACKAGE FILE HASHES.json` inside the
download and `download-manifest.json` on the release provide verification metadata.

## Intended sharing differences

Fresh local account bootstrap; clean progress and default settings; GitHub launcher
label 0.3; an isolated client preference profile. Existing/imported credentials are
preserved. Game saves are local to the extracted installation. The legacy client
still uses its isolated Windows AppData profile for display preferences.

## Limits

Verification on one Windows PC is not proof of every CPU, graphics driver or
operating system. Ordinary tests and file hashes do not constitute an overnight
gameplay soak test of every encounter. The code and tools are shared so defects
can be inspected and fixed, not as a claim that AI-generated software is bug-free.

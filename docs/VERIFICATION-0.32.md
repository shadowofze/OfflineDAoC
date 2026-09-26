# v0.32 Darkness Falls Beta verification

This page records what can be claimed for the **normal** v0.32 release. Keep
package checks, source tests, and actual client gameplay separate. Do not
reuse v0.31 test counts as v0.32 results.

## Implementation evidence

- The server contains Darkness Falls entrance and realm-access checks, a
  shared-combat-dungeon policy for region 249, local opposing-realm PvP
  eligibility, and autonomous dungeon route and camp selection.
- The source has focused Darkness Falls policy tests for entrance authority,
  region edges, local PvP, and timed PvE assignments. These tests exercise
  policy decisions; they do not prove that bots finish a live route.
- The normal release must be checked for absence of Sluaghbinder class code,
  quests, patch tool, and optional client assets before the source or playable
  package is published.

## Release checks

- Isolated normal Release server build: successful, zero errors.
- Full normal server suite: **2,043/2,043** passed. Focused bot-combat
  regression subset: **57/57** passed.
- Normal launcher tests: **97/97** passed. The launcher displays 0.32.
- The staged public world database has no accounts, characters, inventory,
  or bot profiles. Its Darkness Falls rows, exit and seal-vendor settings,
  normal beach-rat camp, and 85 excluded raid NPCs passed the release-data
  verifier. This is a database check, not a live bot result.
- The normal server DLL's SHA-256 is
  `9BB22CFC53BA21A8B94CF858DDF8708B57DD44C3005A6911324796B7C742B090`.
  The same DLL hash is in all three staged runtime server locations.
- The update ZIP passed a full entry CRC check. Its manifest byte count and
  SHA-256 match the generated archive, and it contains no Sluaghbinder paths.
- An independent v0.31 playable copy accepted the extracted v0.32 update;
  its clean world verifier and new DLL hashes passed after the overlay. The
  v0.31 base and the original local installation were not modified.

Compare the published update ZIP and source ZIP against the release's
`SHA256SUMS.txt` and `download-manifest.json` before installing. The helper's
retry/path guards were syntax-checked and exercised in isolation; a complete
network downloader run and relocated in-client launch were **not** performed.
These checks do **not** establish a long live autonomous Darkness Falls run.

## Live gameplay limits

The owner has not yet completed a long live bot test in Darkness Falls.
Darkness Falls raid AI is not implemented. Legion, the hardest level-70+
encounters, unreachable flying targets, and unverified routes/monsters are
excluded from ordinary autonomous bot goals.
Static navigation checks, policy tests, build success, and file hashes cannot
substitute for a live all-realm dungeon run. Please report reproducible bot
failures with the version, realm, location, and relevant log details; never
publish an account file or progress database to do so.

For historical verification records, see [v0.31](VERIFICATION.md) and
[v0.31b](VERIFICATION-0.31B.md). Their counts apply only to those builds.

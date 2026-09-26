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
- Full normal server suite after the beta maintenance fixes: **2,051/2,051**
  passed. The earlier focused bot-combat regression subset: **57/57** passed.
- Normal launcher tests: **97/97** passed. The launcher displays 0.32.
- The staged public world database has no accounts, characters, inventory,
  or bot profiles. Its Darkness Falls rows, exit and seal-vendor settings,
  normal beach-rat camp, and 85 excluded raid NPCs passed the release-data
  verifier. This is a database check, not a live bot result.
- The normal server DLL's SHA-256 is
  `73C36868E4565B4D12D48D2FEB69FDC5E7A21A5D5856760CF8B0C50F8F82F171`.
  The same DLL hash is in all three staged runtime server locations.
- The 2026-09-26 hotfix update ZIP passed a full 3,739-entry CRC check; its
  entries match every staged file path and byte count. The source ZIP passed
  a full 4,129-entry CRC check and its changed files match the checkout.
  The manifest byte count and SHA-256 match the update archive. Neither ZIP
  contains Sluaghbinder class code or optional assets.
- A separate clean v0.32 base accepted the hotfix staging overlay and all
  three installed server DLL copies matched the tested Release DLL hash.
  The original local installation and earlier release assets were not modified.

Compare the published update ZIP and source ZIP against the release's
`SHA256SUMS.txt` and `download-manifest.json` before installing. The helper's
retry/path guards were syntax-checked and exercised in isolation for the
earlier release; a complete network downloader run and relocated in-client
launch for this hotfix were **not** performed.
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

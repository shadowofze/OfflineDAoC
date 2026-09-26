# v0.32b Darkness Falls Beta verification

This page is for the optional Sluaghbinder source and package. The normal
v0.32 verification record is [separate](VERIFICATION-0.32.md).

## Isolated source checks

- The optional Release server and launcher builds completed with zero errors.
- The isolated optional server test run passed **2,047/2,047** tests.
- The optional launcher tests passed **96/96**; its displayed version is 0.32b.
- A focused check covered **24 non-Darkness Falls dungeon route regions**;
  it passed. That protects the unrelated route catalog against the new
  Darkness Falls policy in the tested source.

## Disposable package install and rollback

- The staged optional patch was sealed against a complete, clean normal
  v0.32 installation. Its manifest records exact launcher/server base hashes
  and **74 optional payload files** (the install-tested 69 plus this
  verification page and four developer-helper files). The client asset builder produced the
  five expected private model/archive files without modifying the base.
- A copy-first install into a new sibling folder succeeded. All **69/69**
  runtime/source files present at that smoke test matched the then-sealed
  manifest; the final five added files are documentation and developer helpers,
  not gameplay runtime files. The copied database passed
  SQLite `quick_check`, contained the Sluaghbinder trainer and **89** class
  spells, and retained the normal Darkness Falls High Lord Oro level **69**.
  The clean base had zero accounts, characters, and inventory rows.
- A rollback of that disposable optional copy succeeded. All **74/74**
  recorded file replacements, including five generated client assets,
  returned to their original state or were removed if newly added. The
  database and three changed client archives matched the untouched normal
  base afterward. The original normal launcher and server hashes remained
  unchanged.
- The Windows PowerShell 5.1 installer, manifest sealer, and rollback were
  checked with extended paths so deeply nested Desktop folders work.

The staged optional launcher DLL SHA-256 is
`49011B42074292CFCE2084827CFF2429525F2FFD449FF017C32C929136216812`;
the optional server DLL SHA-256 is
`E5E3C775203F11FA5768380B2B39489DBB8A57F6BE9C1604654D67CD3A2E1AE4`.
The release manifest and `SHA256SUMS.txt` identify the final uploaded ZIPs.
These package checks do **not** establish a successful in-client launch or a
long live autonomous Darkness Falls run.

## Live gameplay limits

The owner has not yet completed a long live bot test in Darkness Falls.
Darkness Falls raid AI is not implemented. Legion, the hardest level-70+
encounters, unreachable flying targets, and unverified content are excluded
from ordinary autonomous bot goals.
The optional Sluaghbinder build shares those limits. The class features
described in [v0.31b verification](VERIFICATION-0.31B.md) are historical
results for that older build and do not prove this new package.

# v0.32b Darkness Falls Beta verification

This page is for the optional Sluaghbinder source and package. The normal
v0.32 verification record is [separate](VERIFICATION-0.32.md).

## Isolated source checks

- The optional Release server and launcher builds completed with zero errors.
- After the September 26 Bard, High Lord Oro, and ordinary Darkness Falls
  bounty maintenance fixes, the isolated optional server test run passed
  **2,055/2,055** tests. Focused Bard/DF policy tests passed **41/41**;
  focused bounty tests passed **7/7**. The Release build completed with
  zero errors.
- The optional launcher tests passed **96/96**; its displayed version is 0.32b.
- A focused check covered **24 non-Darkness Falls dungeon route regions**;
  it passed. That protects the unrelated route catalog against the new
  Darkness Falls policy in the tested source.

## Disposable package install and rollback

- The September 26 patch was sealed against a complete, clean normal v0.32
  installation with the new Bard/Oro/Bounty fixes. Its manifest records the
  exact normal launcher/server hashes and **74 optional payload files**.
- The exact patch ZIP installed into a new sibling copy. All **79/79** file
  receipts matched after installation; all **80** checked base fingerprints
  (including the database and game/documentation files) remained unchanged.
  The optional database passed SQLite `quick_check` and contained nine class
  specializations, seven pet templates, 39 styles, and 89 class spells. The
  private Dullahan and Zombie Defender NIFs were present. The normal base
  retained its original launcher and server hashes.
- Rollback of that disposable optional copy restored **79/79** receipts with
  no hash mismatches, removed all **18** patch-created files, and restored
  the database byte-for-byte to its original SHA-256. An earlier exploratory
  run overlapped a documentation update to the disposable normal copy; the
  final clean run above had no such overlap or mismatch.
- The Windows PowerShell 5.1 installer, manifest sealer, and rollback were
  checked with extended paths so deeply nested Desktop folders work.

The staged optional launcher DLL SHA-256 is
`49011B42074292CFCE2084827CFF2429525F2FFD449FF017C32C929136216812`;
the optional server DLL SHA-256 is
`BBE31AA4409FD8C44C17ECEB7DDCDCC8D1EB5D449BE6774859AD860F7A83D3D7`.
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

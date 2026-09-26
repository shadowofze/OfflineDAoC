# v0.32b Darkness Falls Beta verification

This page is for the optional Sluaghbinder source and package. The normal
v0.32 verification record is [separate](VERIFICATION-0.32.md).

## Isolated source checks

- The isolated optional server test run passed **2,047/2,047** tests.
- A focused check covered **24 non-Darkness Falls dungeon route regions**;
  it passed. That protects the unrelated route catalog against the new
  Darkness Falls policy in the tested source.

These are source and route checks. They are not evidence that the final
download was installed and played, nor that every Sluaghbinder or Darkness
Falls encounter is correct. Final package hashes, clean world-data checks,
installer/rollback results, and a relocated first launch should be added
only after they have been performed on the exact uploaded assets.

## Live gameplay limits

The owner has not yet completed a long live bot test in Darkness Falls.
Darkness Falls raid AI is not implemented. Legion, the hardest level-70+
encounters, unreachable flying targets, and unverified content are excluded
from ordinary autonomous bot goals.
The optional Sluaghbinder build shares those limits. The class features
described in [v0.31b verification](VERIFICATION-0.31B.md) are historical
results for that older build and do not prove this new package.

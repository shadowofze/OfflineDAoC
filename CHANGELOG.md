# Offline DAoC changelog

## v0.31 — normal maintenance update

- Fixed level-based scaling for player-owned summoned pets and nested pet owners.
- Corrected companion/Zealot spell power and Ally/Compatriot healing and buff scaling.
- Repaired the audited Isle of Glass large-dragonfly camp with live spawn anchors,
  reachability checks and bounded recovery for the confirmed collision pocket.
- Applied the same narrow route protection to boobrie hatchlings, feccan, huldu
  outcasts and green serpents.
- Fixed temporary `/spawn` Bonedancer commander and sub-pet upkeep. Persistent
  autonomous gamebot scheduling remains unchanged.
- Fixed the effect-processing lock order that could freeze the world loop.
- Updated the public launcher label to 0.31.
- This release deliberately contains no Sluaghbinder class, quests or patch.

The v0.31 playable updater uses the unchanged v0.3 release as its clean seed and
applies a small SHA-256-verified update in a new folder. The v0.3 tag and release
remain available for anyone who wants that original public baseline.

The repository and these customizations were made with AI coding agents under the
owner's direction and testing. Upstream OpenDAoC and third-party client/data
licenses remain applicable.

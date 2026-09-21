# Offline DAoC v0.31 — maintenance update

This is the normal Classic/Shrouded Isles release. It contains no Sluaghbinder
class, Sluaghbinder quest, or Sluaghbinder patch. That experimental work remains
private until it is intentionally published in a later, separate update.

## What changed

- Moved Eilwen's two Sentinel Exchange Guards to flanking positions beside her
  in Tir na Nog. The v0.31 launcher applies this narrow repair automatically to
  the v0.31 database before startup; Eilwen and other realm-exchange brokers
  are unchanged.
- Fixed level-based spell scaling for player-owned summoned pets, including pets
  whose owner is reached through another controlled pet. Companion/Zealot spell
  power and Ally/Compatriot healing/buff values now use the correct owner level.
- Repaired the audited large-dragonfly camp in Isle of Glass. Bots use live spawn
  clusters, verify that a target is reachable before committing to the pull, and
  have bounded recovery points for the confirmed collision pocket.
- Applied the same narrow route checks to boobrie hatchlings, feccan, huldu
  outcasts and green serpents. Ordinary camps, difficulty, navmeshes and unrelated
  routes are unchanged.
- Made temporary `/spawn` Bonedancers treat their commander and learned sub-pet as
  required equipment. At level 15 and above, upkeep can replace a level-1 commander
  and summon the learned sub-pet before follow/rest scheduling postpones it.
  Persistent autonomous gamebots keep their existing scheduling.
- Fixed the effect-processing lock order that could deadlock three workers and
  leave the world loop frozen while background database work continued.
- Source-empty camps now use small live spawn clusters and an actual nearby
  creature as the route anchor. The bot verifies that anchor before pulling,
  preventing stale averaged points from sending it into an empty pocket.
- PvE meetups can continue with a viable tank, healer, and attacker core when a
  member cannot reach the rendezvous. The missing bot releases and uses its
  normal return route instead of disbanding the whole party. RvR and realm
  event groups are not changed.
- Reaver Flexible weapons are accepted when the saved Flexible specialization
  is present, generated Flexible loot is recognized, and repeated inventory
  warnings are rate-limited. Completed training and item-service assignments
  close as soon as their requested work is actually complete.

## Validation

The normal local build reported zero release-build errors. The full server suite
passed 1,893/1,893 tests and the launcher suite passed 96/96 tests; the focused
pet/effect and route-policy checks are included in those totals. The public source
includes the narrow policy tests and the playable update is hash-verified against a
clean, non-personal v0.3 seed. Static checks and tests are not a promise that every
PC or encounter is bug-free; test a disposable copy before importing real progress.

## Version and data safety

- The public `v0.3` tag and release remain available unchanged.
- `v0.31` is a new tag/release with matching source and a small, hash-verified
  playable update layered on the preserved v0.3 seed. The one-click helper performs
  that layering into a new folder; it never edits an existing install.
- No accounts, characters, inventories, saved bot profiles, logs, or private settings
  are included. A fresh local account is created on first entry to a new install.
- The customizations in this repository were made by AI coding agents under the
  owner's direction and testing. Upstream OpenDAoC code and third-party client/data
  assets retain their original licenses and notices.

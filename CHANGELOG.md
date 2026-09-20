# Offline DAoC changelog

## 2026-09-20 — Sluaghbinder companion build plans

- Fixed Hibernian `/spawn` Sluaghbinder companions always receiving the
  default blunt-and-shield loadout.  Each companion now rolls one advanced
  path once at creation and keeps it: Dullahan's Bulwark uses one-handed
  blunt and shield, Abhartach's Bane starts with a scythe, and Sluagh
  Covenant randomly chooses between those two weapon plans.
- The selected path remains the companion's build for its learned abilities:
  Covenant pet buffs, Bane scythe styles/life-steal/extra rot effects, and
  Bulwark taunts and protection tools are filtered into that bot's spell/style
  catalog.  Player-only skeletal service spells remain excluded.
- Persistent autonomous gamebots keep their existing deterministic
  specialization choice; their inventory reconciliation now recognizes the
  same scythe plan without requiring a real loot scythe first.
- No other class, player, PvE group, loot table, save, or gamebot behavior was
  changed.

## 2026-09-20 — Hibernian exchange guard layout

- Moved Eilwen's two Sentinel Exchange Guards to flanking positions beside her
  in Tir na Nog. Eilwen herself and every other exchange broker are unchanged.
- v0.31 and the optional v0.31b launcher apply this narrow, idempotent repair to
  an existing release database before the server starts; older v0.3 remains
  available unchanged.

## 2026-09-20 — bot shield-style damage fix

- Fixed companion bots and autonomous gamebots using a shield as the damage
  weapon when they selected a shield style. They still require and validate the
  equipped shield, but the swing now uses the active main-hand weapon so it
  deals normal damage instead of producing a misleading 0-damage hit.
- No player attack behavior, style data, shield permissions, saves, or loot
  tables were changed.

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

## v0.31b — optional Sluaghbinder expansion

- Added the optional Hibernian Sluaghbinder class and its Acolyte level-5
  promotion path. The original v0.3 and v0.31 baselines remain available
  unchanged.
- Added the three automatic core lines, three trainable paths, role-specific
  pets, styles, armor/weapon permissions, spell scaling, companion support, and
  a dedicated autonomous gamebot brain.
- Added Muirenn's Tir na Nog trainer, the chained level 10/20/30/40/50 epic
  quests, and the player-only Epic Spells skeletal quality-of-life summons.
- Added a hash-verified, copy-first patcher. It refuses unknown bases, keeps a
  database backup, writes a patch marker, and installs a rollback command in
  the new copy. No accounts, bot data, inventories, settings, logs, or saves
  are included in the public patch asset.
- The public optional launcher label is **0.31b**. The author's private local
  build remains **0.4** and is not part of this repository or release.

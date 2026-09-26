# Offline DAoC changelog

## 2026-09-26 — v0.31b Bard maintenance correction

- Corrected a solo-Bard gamebot guard in the maintained v0.31b source:
  a Bard with no group can no longer treat that missing group as a valid
  party add-mez target. Actual grouped add control and PvP mez remain
  available; Bard healing, songs, and other classes are unchanged.
- This is a focused fix for the refreshed optional v0.31b patch. Older
  immutable release assets and tags remain historical snapshots.

## 2026-09-25 — Bard combat and Shannon camp repair (v0.31 / v0.31b)

- Bard companions and autonomous gamebots no longer repeatedly mesmerize
  their ordinary PvE kill target. They continue normal combat and keep their
  existing secondary-healer role. A grouped Bard may mez a separate,
  full-health add attacking the party, with a bounded retry delay.
- Stationary companion Bards, Skalds, and Minstrels complete normal buffs
  before song twisting takes the cast slot. A moving player's companion Bard
  favors speed/endurance; companion Skalds and Minstrels favor speed/health
  regeneration. Autonomous gamebot song scheduling is unchanged.
- The Shannon Estuary beach-rat camp now has its original rat plus ten spaced
  level-1/2 rats. The optional patch applies the same guarded, idempotent
  correction to its copied database inside the existing transaction; its
  pre-patch database backup and rollback remain intact.
- The Sluaghbinder class, pets, quests, and art stay confined to v0.31b.
  Normal v0.31 receives only the shared Bard and world-camp repairs.

## 2026-09-25 — repeatable Bounty Master hunts (v0.31 / v0.31b)

- Added a Bounty Master in Mag Mell, Cotswold Village, and Mularn. Each offers
  one repeatable, realm-appropriate hunt at a time. Levels 1–49 draw a
  yellow-con monster and ask for 5–50 kills; level 50 draws a named dungeon
  boss or dragon. The quest journal tracks kills and the return objective.
- Normal completion grants two XP bulbs measured at the *assigned* level,
  multiplied by the server XP rate, plus 1–3 class-appropriate equipment
  pieces. A level-50 boss hunt instead pays 100 gold and 1–3 high-utility
  class items. Completed hunts can be taken again without a limit.
- Rerolling is unlimited, but halves the normal XP reward until that bounty
  is completed. Refreshing a bounty after outleveling its assigned level is
  free and clears the reroll penalty for the new contract. Normal kill credit
  uses the monster's name in the assigned zone, not its exact spawn level.
- The active bounty has one red map marker. Its **BOUNTY MAP** journal button
  opens the current map; the dot appears only while the character is inside
  the target's zone or dungeon. `/bountylocation` refreshes the marker and
  explains where to look. Turning in or replacing the bounty removes its
  previous marker.
- Journal text is bounded to the legacy client's 255-byte quest-packet limit,
  including on relog. The Bounty Master and map UI are shared by both public
  paths; Sluaghbinder itself remains exclusive to the optional v0.31b patch.
- Added `/stables`, `/stables classic`, `/stables si`, and `/stables <page>` to
  list the character's realm-specific Classic and Shrouded Isles stable-ticket
  connections. These commands display routes only; they do not travel.

## 2026-09-23 — shared bot progression and route repair (v0.31 / v0.31b)

- Savages now keep native instant buffs from interrupting their melee loop and
  verify a reachable target inside their assigned camp cell before committing
  to a pull. Other classes and cast-time spells are unchanged.
- PvE parties recruit nearby available members, and a viable reduced roster
  can fight after unreachable meetup members are released. Nearby resurrection
  resumes local recovery; each corpse gets its own bounded resurrection wait.
- Reduced parties can hand a dungeon corridor blocker to their puller in
  Koalinth, Vendo, Keltoi, and Tepok. We found no separate broken dungeon
  geometry, so no dungeon spawn or navmesh data was changed.
- Small parties skip one repeatedly unproductive Salisbury spirit camp; other
  spirit camps remain available. Solo bots rest fully after death, temporarily
  avoid the failed camp/target after repeated no-XP deaths, and back off capped
  retries instead of immediately repeating a lethal loop.
- Mularn's audited low stable landing is raised to the walkable surface after
  arrival. Stable planning checks the first boarding leg and an in-range
  boarding point, including the Pheuloc hotspot, without changing tickets or
  horse routes.
- These class-neutral fixes are shared with normal v0.31. The normal package
  still has no Sluaghbinder code or assets; the optional class remains confined
  to v0.31b.

## 2026-09-22 — v0.31b optional pet appearance and Covenant upkeep

- Zombie Defender is 33% larger and has its own rusty-plate zombie texture and
  private model. Its prior shield and combat rules remain unchanged. The stock
  Decaying Marshman appearance is not replaced.
- Dullahan is 50% larger and has its own dark, battle-worn armored texture and
  private model. It uses a chain morningstar with the existing dark weapon
  effect, no offhand shield, and the corresponding shield/block adjustment.
  The stock Headless Corpse appearance is not replaced.
- Zombie Priest now carries only a dagger instead of a mace and buckler.
- Covenant companion and autonomous Sluaghbinder bots allow an active
  one-minute pet heal-over-time to finish before recasting it. Direct heals,
  other Sluaghbinder paths, and other classes retain their existing behavior.
- The optional patch now carries the pet equipment rows and private client
  assets with copy-first installation and rollback. Normal v0.3 and v0.31
  releases remain independent and unchanged.
- Added an [LLM pet-texture workflow](docs/LLM-SLUAGHBINDER-PET-TEXTURES.md)
  covering the private NIF/DDS catalog chains, legacy MPK ordering, validation,
  and safe visual testing.

## 2026-09-21 — v0.31b Sluaghbinder pet model polish

- Moved the former Zombie Magician model to the level-7 Sturdy Zombie.
- Changed the level-12 Zombie Magician to use the existing Murkman model.
- The Sturdy Zombie remains unarmed; the Zombie Magician keeps its staff,
  spells, stats, and damage unchanged.
- This is an optional v0.31b visual-only patch change. The normal v0.3/v0.31
  releases are unchanged.

## 2026-09-21 — v0.31 / v0.31b maintenance fixes

- Source-empty PvE camps now use small live spawn clusters and a real nearby
  creature as the route anchor. Bots verify that anchor before pulling, so a
  stale average point cannot send them into an empty or unreachable pocket.
- PvE groups can continue with a viable tank, healer, and attacker core when a
  member cannot reach the meetup. The missing bot is released to rejoin on its
  normal route instead of disbanding the whole party. Realm-event and RvR
  groups are unchanged.
- Reaver Flexible weapons now pass the configured-proficiency check while a
  saved build finishes loading, and generated Flexible loot is classified as a
  usable weapon. Repeated full-inventory/service warnings are rate-limited.
- Completed training, exchange, repair-kit, sale, and purchase assignments now
  close promptly instead of holding a stale service route until its lease ends.
- The v0.31 and v0.31b release assets were rebuilt, hash-verified, and published;
  the v0.3 baseline, player data, bot data, saves, settings, and the running
  launcher/server are untouched.

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

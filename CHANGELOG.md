# Offline DAoC changelog

## 2026-09-26 — v0.32 Darkness Falls Beta maintenance

- Fixed a Darkness Falls goal-certification issue that could prevent all
  gamebots from choosing dungeon camps after a restart. High Lord Oro's
  original NPC template randomly rolls level 65–70 and saves the result;
  the bot safety check now accepts that exact range for Oro alone. Every
  other raid exclusion, spawn identity, location, and navigation check
  remains strict. His level, encounter, and normal loot are unchanged.
- Solo Bard gamebots no longer treat a missing group as permission to
  perform group-only PvE add mez. Actual grouped add control and PvP mez
  remain available.
- Ordinary level-1–49 Bounty Master hunts can now draw from certified,
  floor-reachable Darkness Falls camps for all three realms. Home-realm
  dungeon targets remain available; level-50 epic contracts are unchanged.
  Unverified or raid Darkness Falls spawns cannot be selected or give credit.
- These are focused source fixes for the normal v0.32 beta, not a claim that
  a long live Darkness Falls bot run has passed. Darkness Falls raids and
  the hardest-content bot goals remain unimplemented.

## 2026-09-25 — v0.32 Darkness Falls Beta (normal release)

- Added the Darkness Falls region to the normal Classic + Shrouded Isles
  release. Albion, Midgard, and Hibernia can enter in the offline setup;
  the shared center permits opposing-realm bot PvP under server rules.
- Autonomous bots have staged, floor-aware paths for ordinary dungeon
  grinding. Entrance and exit checks preserve actual region edges; routes
  treat ledges and descents as one-way and return through each bot's own
  realm exit. Corridor threats, party staging, recovery, and failed-camp
  fallback use the shared bot systems. Players can use the existing seal
  vendors; automated bot purchases are not claimed.
- Darkness Falls raid AI is **not implemented**. Legion, the hardest
  level-70+ encounters, unreachable flying targets, and unverified content
  are excluded from ordinary bot goals. The owner has not
  completed a long live Darkness Falls bot test, so both 0.32 releases are
  labeled **Darkness Falls Beta** until that testing is done.
- This is the fully current normal build: it also contains the earlier
  Bounty Master, Bard, companion-song, Shannon beach-rat, and shared bot
  progression/navigation repairs listed below. It contains **no**
  Sluaghbinder class, quests, or optional client assets.
- Shared combat and training fixes carried into both 0.32 editions correct
  shield-style damage calculation, trainer examine behavior, and `/train`
  names containing apostrophes. No class-specific training was added to the
  normal edition.
- The [v0.32 release notes](docs/RELEASE-0.32.md),
  [verification record](docs/VERIFICATION-0.32.md), and
  [player guide](docs/PLAY.md) explain implementation, limits, download,
  and rollback. The new downloader layers over v0.31 into a separate
  folder and keeps that legacy base intact.

## 2026-09-25 — v0.32b Darkness Falls Beta (optional Sluaghbinder)

- Includes every current v0.32 shared and Darkness Falls change, then adds
  the optional Hibernian Sluaghbinder class, quests, pets, class-specific
  companion/autonomous bot support, and private client visuals. These class
  features were introduced in v0.31b and remain optional.
- The v0.32b patch makes a new copy from a verified v0.32 normal base, so
  the normal installation stays available. The optional copy has a rollback
  command. [v0.32b release notes](docs/RELEASE-0.32B.md) and
  [verification record](docs/VERIFICATION-0.32B.md) cover its separate
  source and package.
- v0.3, v0.31, and v0.31b remain unchanged as legacy tags and releases.

## 2026-09-25 — Bard combat, companion songs, and Shannon beach rats (v0.31 / v0.31b)

- Solo Bard gamebots engage PvE targets instead of repeatedly casting mez.
  Grouped Bards reserve PvE mez for a fresh extra monster already attacking
  their party, never the party's selected kill target; failed attempts have a
  short retry limit. PvP mez and Bard's secondary-healing role are unchanged.
- Bard, Skald, and Minstrel `/spawn` companions finish ordinary group buffs
  while stationary before resuming songs. When their player leader moves,
  travel songs take priority: Bard uses speed and endurance rather than power;
  Skald and Minstrel use their available speed/health songs. Autonomous
  gamebot song behavior is unchanged.
- Filled out the low-level Shannon Estuary beach-rat camp to eleven spaced
  level-1/2 rats. The v0.31 downloader and optional v0.31b installer apply a
  guarded, repeatable world-data patch without copying anyone's save database.
  The preserved v0.3 release and Sluaghbinder-free normal v0.31 path remain
  separate.

## 2026-09-25 — repeatable Bounty Masters (v0.31 / v0.31b)

- Bounty Masters in Cotswold, Mularn, and Mag Mell now offer one repeatable
  monster hunt at a time. Levels 1–49 get a yellow-con species and a kill count
  that grows with level; the journal tracks progress and turn-in.
- A completed normal bounty grants class-appropriate equipment and two XP bulbs
  measured at the assigned level, scaled by the server's XP rate. Unlimited
  rerolls change the target but reduce XP to one bulb until completion. Refreshing
  an outleveled contract is free; an old contract's XP value does not grow with
  the character. Level-50 contracts instead hunt major bosses for gold and
  high-quality class gear, with no XP reward.
- A red bounty marker appears on the target's local zone or dungeon map once
  you enter that area. The journal's BOUNTY MAP button opens your current map;
  the Bounty Master and `/bountylocation` explain how to find the marker.
- Added `/stables`, `/stables classic`, `/stables si`, and `/stables <page>` to
  list your realm's actual Classic and Shrouded Isles stable-ticket routes.
  These commands are informational; they do not move your character.
- Fixed a too-long journal entry that could block a character with an active
  bounty from logging in. Ordinary bounty kills now count every matching
  monster name in the assigned zone, regardless of its variable spawn level;
  level-50 named bosses still require the exact target.
- The bounty system is shared by the normal v0.31 and optional v0.31b paths.
  The v0.3 release remains unchanged, and v0.31 still does not contain the
  Sluaghbinder class. Source tests and package checks are separate from an
  in-client gameplay verification.

## 2026-09-23 — shared bot progression and route repair (v0.31 / v0.31b)

- Savages now keep native instant buffs from interrupting melee and verify a
  reachable target within their assigned camp. Failed path checks are briefly
  cached instead of retried every AI tick.
- PvE parties recruit nearby available bots and can fight as a viable reduced
  roster after meetup no-shows. Nearby resurrection resumes local recovery;
  each corpse has its own bounded wait timer.
- Reduced parties can clear post-entry dungeon corridor blockers. Koalinth,
  Vendo, Keltoi, and Tepok were audited without speculative navmesh changes.
- Small parties skip one unproductive Salisbury spirit cell. Solo bots rest
  fully after death and use temporary failed-camp avoidance and capped no-XP
  retry delays rather than repeating lethal loops immediately.
- Mularn's audited low stable landing is corrected after a confirmed ride;
  first-leg and boarding-range checks address repeated horse-route failures.
- These class-neutral fixes are in both downloads. The normal v0.31 release
  remains free of Sluaghbinder class code, quests, trainer and client assets.
  A post-fix overnight live run has not yet been observed.

## 2026-09-22 — optional v0.31b pet and Covenant refresh

- Zombie Defender is 33% larger, retains its prior shield, and has a private
  rusty-plate texture/model that does not replace the stock monster.
- Dullahan is 50% larger, has a private dark armored texture/model, uses a
  chain morningstar with an existing dark effect, and has no offhand shield.
- Zombie Priest carries a dagger instead of mace and buckler.
- Covenant Sluaghbinder companions and autonomous bots now wait for an active
  one-minute pet heal-over-time to finish before recasting. Direct heals and
  other builds/classes are unchanged.
- The v0.31b installer merges private client entries and pet equipment into a
  separate game copy and rolls them back there; normal v0.3/v0.31 downloads
  are unchanged. The [LLM pet-texture guide](https://github.com/shadowofze/OfflineDAoC/blob/release/v0.31b-sluaghbinder/docs/LLM-SLUAGHBINDER-PET-TEXTURES.md)
  documents the old-client NIF/DDS/MPK workflow.

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

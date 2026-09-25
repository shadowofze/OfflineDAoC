# Offline DAoC changelog

## 2026-09-25 — v0.31 repeatable realm bounties

- Added Bounty Masters in Mag Mell, Cotswold Village, and Mularn. Each player
  may take one repeatable hunt at a time. Levels 1-49 receive a yellow-con
  monster in their own realm; the required kills rise from 5 to 50 with level.
  Any monster of the assigned name in the assigned zone counts, even when
  individual spawns have different levels.
- The native quest journal saves and displays kill progress. The red bounty
  marker appears on the local map only after entering the target's zone or
  dungeon; BOUNTY MAP opens the current map, and `/bountylocation` refreshes
  the marker. The accepted quest remains intact across logout.
- A completed normal bounty pays 1-3 class-appropriate equipment pieces and
  two XP bulbs at the *assigned quest level*, multiplied by the server XP rate.
  Unlimited target rerolls lower XP to one bulb until completion; replacing an
  outleveled bounty has no reroll penalty. Level-50 contracts instead target
  major bosses and pay 100 gold plus 1-3 high-quality class items, with no XP.
- Added `/stables` to display the character's own realm's actual Classic and
  Shrouded Isles ticket connections. This is information only; it does not
  change travel or teleport the player.
- Bounty journal text is bounded to the 1.127 client's 255-byte quest-packet
  limit. The normal client's red-dot hook is SHA-256 guarded and separate from
  the optional Sluaghbinder client. The preserved v0.3 release is unchanged.

## 2026-09-23 — v0.31 shared bot progression and route repair

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
- These shared fixes are published in both the normal v0.31 and optional
  v0.31b branches. The normal release still contains no optional class code,
  quests, trainer, or client assets.

## 2026-09-21 — v0.31 maintenance fixes

- Source-empty PvE camps now use small live spawn clusters and a real nearby
  creature as the route anchor, with reachability checked before a pull.
- PvE groups continue with a viable tank, healer, and attacker core when an
  unreachable meetup member is released. The missing bot uses its normal
  return route instead of disbanding the party; RvR groups are unchanged.
- Reaver Flexible equipment is recognized during saved-build loading and
  repeated inventory/service warnings are rate-limited.
- Completed training, exchange, repair-kit, sale, and purchase work closes its
  service assignment promptly instead of leaving a stale route behind.

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

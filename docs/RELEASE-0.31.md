# Offline DAoC v0.31 — maintenance update

This is the normal Classic/Shrouded Isles release. It contains no Sluaghbinder
class, Sluaghbinder quest, or Sluaghbinder patch. Players who want that optional
expansion can choose its separate v0.31b release instead.

## Latest Bard and world-camp repair

Companion and autonomous Bard bots no longer repeatedly mez their ordinary PvE
kill target. They keep fighting and can still heal when needed; grouped Bards
can reserve mez for a separate, full-health add attacking the party. Companion
Bards, Skalds, and Minstrels finish ordinary buffs before stationary song
twisting. Traveling companion Bards favor speed/endurance; Skalds and
Minstrels favor speed/health regeneration. Autonomous song
scheduling and other classes are unchanged.

The Shannon Estuary beach-rat camp now has eleven spaced level-1/2 rats in
place of one incorrectly high-level rat. The latest playable update applies
this narrow, repeat-safe world change inside the new installation it creates;
it does not ship or overwrite anyone's saved accounts, characters, or bots.
The v0.3 release and earlier v0.31 update ZIPs remain available unchanged.

## What changed

### Repeatable realm bounties

Find the Bounty Master near the road in Mag Mell (Hibernia), Cotswold Village
(Albion), or Mularn (Midgard). Accept one hunt, track its kills in the quest
journal, then return to the same realm's master for the reward. Levels 1-49
receive a random same-level/yellow-con monster; every matching monster in its
assigned zone counts even if nearby spawns vary in level. Required kills rise
from 5 to 50 as the character levels. Level-50 hunts are major-boss contracts;
their specific targets are left for players to discover in-game.

A normal completion grants 1-3 class-appropriate items and two XP bulbs at
the bounty's *assigned level*, multiplied by the server XP rate. Rerolling is
unlimited but cuts that contract's XP to one bulb until it is completed. If you
outlevel a contract, you may replace it without that penalty. Level-50 hunts
grant 100 gold and 1-3 exceptional class items instead of XP. Only one bounty
can be active at a time.

The journal shows kill progress and a BOUNTY MAP button. Enter the target's
zone or dungeon first: only then does its red marker appear on your local map.
The button opens your current map; `/bountylocation` refreshes the marker and
explains where it is visible. `/stables` lists your realm's real Classic and
Shrouded Isles stable-ticket connections; it is informational only.

### September 23 shared bot fix

- Savages verify reachable camp targets and no longer let native instant buffs
  stall melee. This does not change other classes' spell scheduling.
- PvE groups recruit available nearby bots, continue with a viable reduced
  party after meetup no-shows, and avoid repeated town regroup trips after a
  nearby resurrection. Corpse waits are tracked per member.
- Reduced parties can clear post-entry dungeon blockers rather than waiting
  for all eight original members. Koalinth, Vendo, Keltoi, and Tepok were
  audited; no unproven navmesh or spawn relocation was made.
- Small parties avoid the confirmed unproductive Salisbury spirit cell. Solo
  bots recover fully and use bounded failed-camp and no-XP death backoff.
- An audited Mularn stable arrival is lifted to the walkable surface only
  after travel; first-leg and boarding-range checks prevent repeat boarding
  failures without changing stable tickets or routes.

The maintained branch and latest release manifest point to the current
playable update. Older v0.31 assets remain available as historical snapshots,
and the v0.3 release is unchanged. These fixes need a restarted server to take
effect; the published package has not yet had a live overnight rerun.

The latest playable delta is named
`OfflineDAoC-v0.31-bard-beach-rats-update.zip` and the matching editable
checkout is `OfflineDAoC-v0.31-bard-beach-rats-source.zip`. The one-click helper
uses the release manifest rather than relying on the older asset names.

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

The September 23 normal source built with zero errors and passed 1,909/1,909
server tests. The previous launcher-only suite passed 96/96; the launcher was
not changed by this server-only repair. Static checks and tests are not a
promise that every PC or encounter is bug-free; no post-fix overnight live run
has been observed yet. Test a disposable copy before importing real progress.

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

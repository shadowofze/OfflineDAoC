# Camlann conversion

This is the implementation plan for replacing Offline DAoC's three-realm RvR
world with a **single Camlann/Mordred-style full-PvP world**. It is a plan, not
a completed change. Do not start this work, start the game server, or deploy
over a running install unless the owner asks.

Read `AGENTS.md`, `docs/DEVELOPMENT.md`, and `source/server/AGENTS.md` before
editing. Distinguish the real player, companion bots, and autonomous gamebots
in every hostility and AI change.

## Target: Camlann 1.65, Old Frontiers, pre-ToA

Camlann was GOA's European full-PvP shard (Mordred was the US one). It opened
in early December 2002 alongside Shrouded Isles and ran SI-only until Trials of
Atlantis reached Europe on **27 February 2004**. New Frontiers came later and
players blame its relic situation (one guild holding many relics) and cheating
fallout for the population collapse.

The fondly remembered era is the SI-era Old Frontiers shard, mid-to-late 2003:

- 8v8 and small crews roaming the OF frontier loops (Emain, Hadrian's, Odin's)
- ganking and "PvP leveling" around the portal keeps and levelling zones
- tight cross-realm guilds (Requiem, Fear, Horde, Public Enemy, etc.)
- guild-owned keeps and relics

Its launch weeks (about patch 1.60) had worse class balance (enchanters and
spiritmasters dominated the charts), and later `/level` shortcuts were
disliked. **This fork targets 1.65 rules on the existing Old Frontiers
world**, which is what the world data, `classic165` spawn catalogs, frontier
transport, and 99 navmeshes already model. No ToA, no New Frontiers, no
`/level` shortcut, no Master Levels or Champion Levels.

Sources: Camelot Herald PvP server addendum and PvP FAQ (fandom mirror),
FreddysHouse "Save Mordred" and "Camlann XML" (Dec 2002) threads, the War Legend
guild history, and Uthgard "PvP server like Camlann" threads. There is no
official "best version." The era choice comes from community memory plus
fit with this codebase.

### Ruleset reference (Mordred/Camlann)

| Rule | Value |
|---|---|
| Hostility | Anyone not in your group, guild, or battlegroup |
| Grouping, guilds, chat, trade | Open across realms |
| Safe regions | Camelot, Jordheim, Tir na Nog, housing, the three no-PvP newbie dungeons |
| `/safety` | Under level 10, on by default, protects in home zones, `/safety off` is permanent |
| Immunity | Short timers after zoning, login, bind release, and same-region teleport |
| Player kills | XP **and** realm points; no item loot |
| PvP death | Constitution loss (bought back at healers) |
| Keeps | Claimed by guilds; unclaimed until taken |
| Relics | Picked only from unclaimed keeps, carried to the guild's own claimed keep, bonus for that guild only |

## Owner decisions (2026-09-19)

These are settled. Do not reopen them without the owner.

1. **Era:** 1.65 Old Frontiers, pre-ToA (above).
2. **Player guild:** the player founds their own guild. No 8-player founding
   requirement. Companions and recruited bots count toward claim size. The
   player's guild holds keeps and relics exactly like a bot crew.
3. **Relics:** faithful and **uncapped**. Relics mount in the carrying guild's
   own claimed keep, and the bonus applies to that guild only. A guild may hold
   all six. This is intentional, even though relic stacking hurt live PvP.
4. **Save:** a one-time launcher-driven world reset (Tier 0). The runtime
   download stays v0.3.
5. **Companions:** `/spawn` may create companions from **any realm**.
6. **Grey targets:** autonomous bots **rarely** start fights with targets that
   con grey to them (tunable, opportunistic, or only when threatened).
7. **Portal keeps:** Castle Sauvage, Svasud Faste, and Druim Ligen become
   **neutral safe hubs**, like the capitals.
8. **Kill reward:** XP + RP for player-shaped kills, con loss on PvP death, no
   coin or item drop.

## Contract

- **One mode.** No `if (Normal)` leftover world. `GameType` is PvP for the
  shipped server.
- **New world.** Existing characters, inventories, coins, bot rosters, keep
  ownership, relic state, Realm Exchange listings, and realm-event records are
  discarded. The launcher performs this reset once, with a backup.
- **Keep the maps.** Current world spawn data and the 99 navigation meshes stay
  unless a specific Camlann route is proven broken. Do not globally rebuild nav.
- **Realm is identity, not team.** Characters still have a realm (race, class,
  capital, starter zone, armor/weapon types). Realm no longer means ally.
- **Companions never attack their leader.** That is Camlann grouping, not a
  second PvP mode.
- **Versioning.** Planning edits are PATCH. Implementing the conversion is
  **MAJOR** (new world/save). Do not bump MAJOR until a tier that actually
  changes the running game is finished. Completing the conversion is `1.0.0`
  unless the owner picks another MAJOR label. Internal slices before that can
  stay on `0.x` MINOR bumps only if they do not ship a playable half-state;
  otherwise hold them on a branch.
- **Upstream download.** GitHub v0.3 remains the playable runtime package.
  Do not rewrite `Get-OfflineDAoC.ps1` or the `docs/PLAY.md` download steps
  when bumping this fork.

## How to use the tiers

Work **one tier at a time**. Finish the gate before starting the next. Each
tier should:

1. Change only the listed systems.
2. Add or rewrite tests for the new hostility/ownership model. Do not keep
   tests that encode Alb-vs-Mid-vs-Hib as the definition of "enemy." About 21
   test files construct `NormalServerRules` today and about 53 reference
   concrete realms. Budget for that.
3. Build and run the ordinary server and launcher tests in a **separate output
   tree**. Report those separately from any later real-client check.
4. Leave Normal server rules in the tree only if unused DOL code still
   references the enum. Do not keep a playable Normal path, launcher toggle, or
   bot brain for it.

Do not implement a dual-mode flag "for later." That is the work this plan
exists to avoid.

## Non-goals

- Dual Normal/PvP operation
- Preserving or importing current characters
- Public multiplayer or internet exposure of the local server
- Native client patches beyond the existing raid/bot-map builders
- Replacing texture atlases, meshes, or skeletons
- New Frontiers, ToA zones/items/Master Levels, `/level` shortcuts
- Recreating GOA infrastructure, language clusters, or live Camlann population
- Executing historical scripts in `source/server/tools` just because they exist

## Current code (the thing being replaced)

| Area | Today | Camlann target |
|---|---|---|
| Server type | `GST_Normal` (`GameType` default `"Normal"` in `GameServerConfiguration.cs`) | `GST_PvP` only |
| Hostility | Other realm = enemy | Anyone outside group/guild/battlegroup = enemy |
| `GameBot` | `GameNPC` + `IGamePlayer`; hostile only by realm | Treated as a **player** by PvP rules, packets, keeps, relics |
| Bot guild | `GameBot.Guild` is an in-memory property only; not persisted, no rank, not in `GuildMgr` rosters | Real guild membership with rank, persisted |
| Companions | `CompanionPvpEngagement` assists vs other-realm bots only; same realm required to group | Any realm; assist vs any legal target |
| Gamebots | Three realm armies, realm staging, realm keep events | Mixed-race crews (guilds) |
| Home worlds | `AutonomousRealmBoundary` keeps bots in their own lands | Open travel |
| Frontiers | **Old Frontiers** inside regions 1/100/200 (Emain, Hadrian's, Odin's) | Same zones, open PvP |
| Portal keeps | Realm staging (Castle Sauvage, Svasud Faste, Druim Ligen) | Neutral safe hubs |
| Keeps | Realm ownership; launcher resets to `OriginalRealm` | Guild claim; unclaimed until taken |
| Relics | 6 realm pads in relic temples; realm-wide bonus (`RelicMgr`) | Guild carry/mount in own keep; guild-only bonus |
| Battlegrounds | Present | Unreachable |

**The world is Old Frontiers, not New Frontiers.** Region 163 is not the
frontier here. The OF frontier zones live in the same regions as the home
realms (1, 100, 200), so region-level checks such as
`PvPServerRules.m_unsafeRegions = { 163 }` do nothing useful. Safety and
frontier rules must be **zone-level** (`Zone.IsRvR` or an explicit OF zone
list).

### Fork code missing from `PvPServerRules`

`NormalServerRules.IsAllowedToAttack` carries fork-specific guards that
`PvPServerRules` lacks. Port them before flipping the type:

- `BotPvpCrowdControl.Protected(attacker, defender)`
- Stable-master route immunity (`GameBot.IsOnStableMasterRoute`)

### DOL/Atlas `GST_PvP` branches to audit

OpenDAoC's PvP code is Atlas-flavoured, not Camlann. Each branch below needs
a keep/change/remove decision in the tier that touches it:

| File | Current PvP behavior | Camlann action |
|---|---|---|
| `packets/Server/PacketLib1124.cs` (~L300–335) | Sends every realm NPC and own pet as "same guild" so the client treats it as friendly | **Critical.** Hostile `GameBot`s must *not* get the dummy-guild trick; allied bots (group/guild) must. Do not delete this hack; extend it |
| `gameutils/Group.cs` (~L146, L274) | Re-sends pet guild IDs on group join/leave | Extend to grouped `GameBot`s and their pets |
| `gameutils/Guild.cs` `DummyGuild` | Creates a DB guild for the hack | Keep; exclude it from rosters, claim, relic, and bot logic |
| `keeps/KeepManager.IsEnemy` | Guild-based, `GamePlayer` only | Accept `IGamePlayer`/`GameBot` |
| `keeps/AbstractGameKeep.CheckForClaim` | Requires `player.Realm == keep.Realm`, `GamePlayer`, rank | Drop realm check; accept bot claimers |
| `keeps/Gameobjects/Guards/Lord.cs`, `AbstractGameKeep`, `MissionMaster` | "Lords are mobs farmed for seals" respawn timers for realm-None keeps | Replace with Camlann keep-lord respawn/claim behavior |
| `Managers/RandomObjectGeneration/AtlasROGManager.cs` | Generates bounty points on PvP | Remove (not 1.65 Camlann) |
| `keeps/Managers/Player Manager.cs` | "The forces of {empty} have defeated..." | Broadcast the guild name |
| `gameobjects/GamePlayer.cs` death / examine | PvP death type, guild-based examine | Keep; verify con loss path |
| `DoorRequestHandler.cs`, `HouseTemplateMgr.cs`, `DFEnterJumpPoint.cs` | Already realm-open | Keep |
| `who.cs`, `assist.cs`, `PlayerEnterExit.cs`, `AtlasOF_Volley.cs` | Normal/PvP branches | Make PvP the only path |

### Client presentation (real-client risk)

`GameBot` reaches the client as an **NPC**. With PvP color handling `1`, NPCs
show their con color, not player red. The client's friend/enemy logic for NPCs
depends on the realm byte from `GetLivingRealm` and the guild-ID trick above.
Getting "this same-realm bot is hostile, this other-realm companion is
friendly" right on screen is a Tier 1 **spike**. It needs a real-client check
before building on it. If the client cannot target or attack a same-realm NPC,
per-viewer realm spoofing in `GetLivingRealm` (hostile bot → a realm different
from the viewer) is the likely fix. Verify; do not assume.

---

## Tier 0 — Project bootstrap and world reset

**Goal:** The repo and an existing v0.3 install agree that Camlann is the only
world, before AI or keeps change.

### Steps

1. Default `EGameServerType.GST_PvP`:
   - `source/server/GameServer/GameServerConfiguration.cs` (load default and
     constructor)
   - `source/server/CoreServer/config/serverconfig.example.xml`
   - whatever the launcher/setup writes into the playable `serverconfig.xml`
2. **World marker.** Add a small row (for example in `offline_local_options`,
   which the launcher already creates) such as `WorldModel=Camlann-1`. The
   server refuses to start without it and logs why.
3. **Launcher one-time reset.** On first launch of a Camlann build against a DB
   without the marker, the launcher:
   - requires the server to be stopped (same guard as `KeepRelicReset`)
   - shows what will be discarded and asks for confirmation
   - copies the whole DB to a timestamped backup beside it (local only, never
     published)
   - in one transaction, clears characters, their inventories, bot profiles and
     settings, `offline_world_bots`, Realm Exchange listings, realm-event
     records, guilds (except `DummyGuild`), and the account's characters, while
     keeping the local account itself
   - sets every keep unclaimed with `Realm=0` and `ClaimedGuildName=''`, and
     homes relics to their temple pads
   - writes the marker
   Item templates, world spawns, mob data, and navmeshes are untouched.
   Unit-test it on a synthetic SQLite fixture, never on a real save.
4. `docs/PLAY.md` progress-import section: the importer must refuse
   Normal → Camlann imports. Do not write a character progress importer.
5. Launcher copy that says "return keeps to original realms" is replaced in
   Tier 5. Until then, hide that panel in Camlann builds rather than ship a
   home-realm restore.
6. Keep player-facing text from promising Camlann until Tier 1 lands.

### Tests and gate

- Launcher: missing or `Normal` `GameType` is rewritten to PvP or fails
  closed.
- Reset: fixture DB ends with empty progress tables, a local account, unclaimed
  keeps, homed relics, and the marker; a second run does nothing; running with
  the server up fails.
- Server: refuses a DB without the marker.
- **Gate:** An existing v0.3 folder can be converted once, with a backup, and
  no supported path loads a Normal save.

---

## Tier 1 — PvP ruleset and GameBot-as-player

**Goal:** Camlann attack/heal/group/guild/safe-zone rules apply to the human
**and** every `GameBot`. The client shows friend/enemy correctly.

### Why first

Every later AI change calls `GameServer.ServerRules.IsAllowedToAttack`. Today
`PvPServerRules` would make every bot a "friendly NPC": the "friendly NPCs
can't attack friendly players" block matches `GameBot` (a `GameNPC` with a
realm), and `IsSameRealm` returns true for player → realm NPC. The packet
layer also marks all realm NPCs as guildmates.

### Steps

1. Keep `PvPServerRules` as the live rules class. Port the fork guards listed
   above (`BotPvpCrowdControl`, stable route).
2. Add one helper, e.g. `PvpCombatant.Resolve(living)`, that returns the
   player-shaped owner: a `GamePlayer`, a `GameBot`, or the owner of a
   controlled pet (use `GetLivingOwner`, not `GetPlayerOwner`, which returns
   null for bot-owned pets). Use it in:
   - `IsAllowedToAttack` (group, guild, battlegroup, duel, safe region,
     `/safety`)
   - `IsSameRealm` (heals and buffs follow "friendly," not realm)
   - `AbstractServerRules.IsAllowedToAttack` immunity checks
   - `GetLivingRealm` / packet guild-ID trick (see Client presentation)
3. **Allied** means same group, same guild (not `DummyGuild`), or same
   battlegroup. Everyone else player-shaped is hostile outside safe areas.
4. **Companion immunity:** a temporary companion (`IsTemporaryGroupHelper`)
   can never attack its leader, the leader's group, or their pets, even after
   the group dissolves. Autonomous gamebots are not companions.
5. **Safe areas:**
   - Regions `10`, `101`, `201` (capitals), `2`, `102`, `202` (housing),
     `21`, `129`, `221` (no-PvP newbie dungeons).
   - The three portal keeps as **areas** (radius around each keep, owner
     decision 7). Their guards become `PEACE` or non-aggressive.
   - Replace `m_unsafeRegions = { 163 }` with zone-level OF frontier detection
     for `/safety`.
6. **Immunity for bots.** Bot victims get the same post-release and zone
   immunity as a human, so crews cannot farm a respawning bot. Store it on the
   bot (the `IsInvulnerableToAttack` equivalent).
7. **Kill rewards (decision 8).** Split "player-shaped kill" from "PvE mob
   kill" in `AbstractServerRules.OnNpcKilled`: autonomous bot victims already
   route to `AutonomousBotRealmPointRewards`. Add player-kill XP for human and
   bot killers, and con loss on PvP death for bot victims if bots track con.
   Remove the `credited.Realm == killedBot.Realm` exclusion in favor of
   "not allied." No grey-con rewards.
8. **Client spike** (see Client presentation). Prove in a real client that a
   same-realm hostile bot is targetable and attackable, and that an
   other-realm companion is friendly (heals, buffs, `/assist`). Record the
   finding in this file before moving on.
9. Move unit tests off `new NormalServerRules()` wherever they assert
   hostility (`EpicTestServerScope`, `UT_PlayerLedPullCoordinator`,
   `UT_RealmExchangeBotDecisions`, `UT_ContinuousBotRoutes`, and similar).
10. Make the PvP branches in `assist.cs`, `who.cs`, `PlayerEnterExit.cs` the
    only branches. Keep the `PacketLib1124` / `Group.cs` pet hack (extended).

### Tests and gate

- Human vs autonomous bot: allowed outside safe areas, blocked in Camelot and
  at Castle Sauvage.
- Human vs own companion (any realm): never allowed.
- Companion vs autonomous bot: allowed if the leader could attack that bot.
- Two grouped humans/bots: not allowed. Same-guild bots of different realms:
  not allowed. Same-realm strangers, no guild: **allowed**.
- Bot-owned pet resolves to its bot owner.
- `/safety` sub-10 is protected in a home zone and not in an OF frontier zone.
- **Gate:** `IsAllowedToAttack` and `IsSameRealm` match Camlann for
  player/companion/gamebot/pet combinations, **and** the client spike passed.

---

## Tier 2 — Neutral home worlds and cross-realm companions

**Goal:** Any realm can use any capital, merchant, trainer-free service, chat,
and travel. Companions can be any realm. Battlegrounds are not part of play.

### Steps

1. Confirm `PvPServerRules.IsAllowedToGroup / JoinGuild / Trade / Understand`
   return true. Keep them that way.
2. City guards and `PEACE` NPCs stay unattackable. Keep guards follow
   Tier 5.
3. **Cross-realm companions (decision 5):**
   - `/spawn` picker lists all three realms' classes and races.
   - Remove same-realm requirements in `BotGroupInvite` (`player.Realm !=
     bot.Realm`), `GameBot` leader matching (~L113, ~L2354),
     `PlayerLedPullCoordinator` (~L184), and `BotBrain` heal/buff/carrier
     filters (~L4111, ~L4150), plus `BotGroupPetBuffTargets`. Use "allied"
     instead.
   - Equipment and weapon choice stays by the bot's **own** realm
     (`BotEquipment`, `BotRangedCombat`, `GameBot` armor selection).
4. Stop realm-locking bot movement:
   - `AutonomousRealmBoundary`
   - `AutonomousWorldBotController.ProtectedRealm`
   - `AutonomousTownIdleRouting`, `AutonomousWorldBotCapitalRouting` filters
   - `AutonomousFrontierTransport` (necklace/portal pairs are keyed by realm;
     let any realm use any portal-keep teleporter)
5. Realm Exchange: use the **local** broker (`BotBrain` ~L1505 matches
   `candidate.Realm == bot.Realm`). Keep real items and coin.
6. Darkness Falls: `DFEnterJumpPoint` is already open when not Normal.
   `AutonomousDarknessFallsPolicy` must stop assuming one owning realm.
7. Housing is already realm-open on PvP. Keep.
8. Battlegrounds: teleporters, frontier stones, and bot travel must not pick
   BG regions. Set `bg_zones_open` false.
9. `BotManager` name lookup (~L274) is realm-scoped; make it realm-agnostic or
   explicitly allow duplicate names across realms.

### Tests and gate

- An Albion character (or bot) can path into Jordheim and use a merchant.
- An Albion player can `/spawn` a Midgard healer that heals and buffs them.
- Realm Exchange list/buy works from a foreign capital.
- No autonomous goal selects a battleground region.
- **Gate:** Open travel and city services work; mixed-realm groups function.

---

## Tier 3 — Hostility rewrite

**Goal:** Every "is this an enemy?" check uses the Tier 1 helper, not
`actor.Realm != target.Realm`.

### Call sites

About 45 files under `bots/` compare realms. Classify each as
**hostility** (rewrite), **identity** (keep: gear, race, starter zone,
siege kit), or **ownership** (Tier 5). Known hostility sites:

- `CompanionPvpEngagement.Enemy`
- `BotBrain` ~L376 (`realTarget.Realm != Body.Realm`)
- `BotRvrAmbush.IsEnemyCombatant`
- `BotPvpCrowdControl` (target selection and realm claim sharing)
- `AutonomousRvrTargetPolicy.IsEligible`
- `AutonomousDungeonPolicy.CanEngageLocalOpponent`
- `BotSiegeRuntime` target checks (~L106)
- `AutonomousWorldBotController.FrontierThreat` / `AutonomousFrontierThreatPolicy`
- `AutonomousDefensivePull`, `AutonomousThreatAwarePathing`
- `AutonomousBotRealmPointRewards` (~L80)
- `BotReleaseBindPoints.IsEnemyBindPosition`
- Companion defensive scan and `/pull` target validation

Keep `ServerRules.IsAllowedToAttack` as the last word. Do not invent a
second hostility matrix.

### Grey-target policy (decision 6)

Add one tunable property (e.g. `camlann_bot_grey_engage_chance`, default low)
in the autonomous target policy:

- Prefer targets that con blue or higher to the bot.
- Grey targets: engage only if the grey target attacked the bot or its crew,
  or on a low-probability opportunistic roll.
- Never apply this filter to companions defending their leader.

### Companion behavior

- Aggressive: assist what the leader attacks, if legal.
- Defensive: hold near the leader; engage nearby legal threats.
- Never acquire the leader, other companions, grouped gamebots, or guildmates.
- A same-realm autonomous bot in the open world **is** a legal threat.

### Tests and gate

- Same-realm stranger: enemy. Mixed-realm group member: friend.
- Companion raid of 40/80: no friendly fire.
- Stealth ambush can pick a same-realm target.
- Grey policy: a level-50 bot does not start on a level-5 in a starter zone
  unless the level-5 hit it (or the tunable forces it).
- **Gate:** No remaining bot *combat* filter uses realm inequality as the
  definition of enemy (enforced by a test that greps or reflects the known
  sites).

---

## Tier 4 — Crews instead of realm armies

**Goal:** The autonomous population is mixed-race **crews** (real guilds), not
three realm factions.

### Guild membership for bots (prerequisite)

`GameBot.Guild` is a bare property today. Make crew membership real:

1. Crews are real `DbGuild` rows created through `GuildMgr` (not
   `DummyGuild`).
2. Persist membership and rank on the bot record (`offline_world_bots` gets
   `GuildId` and `GuildRank` columns, added at schema load; fresh save only).
3. `Guild.HasRank`, rosters, `/gc` listings, and guild chat must accept bot
   members, or the claim/relic code must use a bot-aware rank check. Choose
   one and test it.
4. The **player's guild (decision 2):** `/gc form` without the 8-player rule
   (the `GUILD_NUM` check is already commented out; keep it off). The player can
   `/gc invite` autonomous bots and companions; invited bots accept based on
   crew AI (level fit, not already in a crew).

### What to stop

- `AutonomousRealmLoginBalancer` filling Alb/Mid/Hib quotas
- `AutonomousRvrStaging` (portal keeps as army spawns; they are safe hubs now)
- `AutonomousRvrEventLayer` attackerRealm vs defenderRealm keep wars
- `AutonomousRvrDirector` / `AutonomousRvrPlanningView` / `AutonomousRvrDashboard`
  ally/enemy counts by realm
- `AutonomousRvrRally` / `RealmWarbandSupport` as realm-wide PvP rallies
- `RealmRaidMuster` as a realm PvP rally (realm PvE raids stay; Tier 7)
- Chat/banter (`RealmEventBanter`, `AutonomousChatKnowledge`,
  `RealmEventNotices`) that treats realms as sides

### What to add

1. **Crew identity.** Each autonomous bot belongs to one crew guild with mixed
   realms and a generated guild name.
2. **Spawn/login.** The population slider still sets the gamebot count.
   Balance **crews**, not realms. Mix of crew sizes: gank pairs, 8-man roams,
   and keep groups.
3. **Goals.** Replace "defend our realm frontier" with:
   - grind / hunt in dangerous zones
   - roam OF frontier loops for fights (the Camlann "8v8 in Hib loop" feel)
   - gank unallied targets (grey policy applies)
   - visit mixed cities
   - contest a keep or relic as a crew (Tier 5)
4. **Identity generator.** Names and races stay realm-correct for the
   character. Guild names are shared across realms.
5. **Town idle.** Mixed-realm crowds in capitals and portal-keep hubs.
6. **Siege kits.** `AutonomousSiegePolicy` merchant lists stay per-realm. A
   Hibernian buys the kit that matches **their character realm**.

### Tests and gate

- A guild roster can contain all three realms and survives a server restart.
- Login balancer does not force 1:1:1 realm counts as teams.
- No event layer starts a battle because `attacker.Realm != keep.Realm` alone.
- The player can form a guild alone and invite a bot.
- **Gate:** Dumping the live population shows crews, not three armies.

---

## Tier 5 — Guild keeps and relics

**Goal:** Old Frontiers warfare matches Camlann: guilds own keeps and relics;
relic bonuses apply to that guild only; stacking is uncapped (decision 3).

### Keeps

1. Fresh save: all frontier keeps unclaimed (`Realm=0`,
   `ClaimedGuildName=''`). Portal keeps are excluded from claiming (safe hubs).
2. Unclaimed keeps: set `pvp_unclaimed_keeps_enemy` **true**, so unclaimed
   keep guards defend against everyone and a crew must fight in.
3. Replace the Atlas "lords are seal mobs" respawn logic with normal keep-lord
   respawn so a taken keep stays taken until someone else kills the lord.
4. `CheckForClaim`: drop `player.Realm != this.Realm`; accept a `GameBot`
   claimer (bot-aware rank); count grouped `GameBot`s toward `claim_num` (8,
   towers 4). The player's companions count.
5. `guilds_claim_limit`: raise it above 1 so a guild can hold a keep plus
   relic keeps. Pick a default (for example 3) in this tier and document it in
   the changelog.
6. `PvPServerRules.ResetKeep` must accept a `GameBot` killer and a bot-owned
   pet killer, not only `GamePlayer`. Its "leader realm" display realm is
   cosmetic.
7. On claim, run `ChangeGuild` for lord and guards for bot-led takes.
8. Capture broadcast names the guild (`Player Manager.cs`).
9. Bot keep AI (`AutonomousRvrKeepPolicy`, `AutonomousKeepApproachNavigation`,
   `AutonomousSiegeJobs`, `AutonomousSiegeOwnership`, `BotSiegeRuntime`,
   `RvrKeepRoute`) targets unallied-guild or unclaimed keeps, not "other
   realm" keeps.

### Relics (faithful, uncapped)

The world has six `GameRelicPad`s in the realm relic temples, keyed by emblem
(`OriginalRealm + 10 * type`). Camlann needs relics to mount in the
**carrying guild's claimed keep**:

1. **Temple pads become shrines.** They are the relics' home and start
   location. Their guards must be killed before pickup.
2. **Keep relic pad.** Add a guild-keep mount point: one dynamic pad per
   claimed keep, spawned at a fixed offset from the lord (per keep, data-driven
   so no world spawn edits are needed). Persist the mounted keep in the `Relic`
   row (for example a `KeepID` column, added at schema load; fresh save only).
3. Rules:
   - Pick up only from an **unclaimed** keep or its temple shrine (kill the lord
     or the shrine guards first).
   - The carrier's guild must already own a keep.
   - Mount only in your own guild's keep, and only after it has been claimed
     for a delay (live used two hours; pick an offline value and document it).
   - Cannot take your own guild's mounted relic.
   - Dropped or abandoned relics return to their temple shrine after
     `Relic_Return_Time` (20 min default).
   - Stealthed carriers are blocked, and "already carrying" applies to bots.
4. `RelicMgr` bonus: count relics mounted in keeps owned by the target's
   guild. Remove "own realm relic required" logic. **No cap**, so a guild may
   hold all six.
5. Broadcasts name the guild.
6. Bot relic AI: crews that own a keep may plan relic raids; carriers get
   escorts.

### Launcher reset panel

Rewrite `KeepRelicReset` / `KeepRelicResetPanel` so they no longer run
`SET Realm=OriginalRealm`:

- clear guild claims (`Realm=0`, `ClaimedGuildName=''`)
- home all relics to their temple shrines and clear `KeepID`
- leave guilds and characters alone
- update `KeepRelicResetTests`

### Tests and gate

- An unclaimed keep can be contested by two crews; a crew is friendly to its
  own claimed guards.
- A bot killer can reset and claim a keep; companions count toward
  `claim_num`.
- A relic cannot be mounted by a guild without a keep, or before the claim
  delay.
- The relic bonus reaches guild members and no one else (not same-realm
  strangers).
- One guild can hold all six relics.
- **Gate:** A crew (and the player's guild) can take a keep, move a relic to
  it, and only that guild receives the bonus.

---

## Tier 6 — Full PvP consequences

**Goal:** Original Camlann lethality, with the owner's grey-target mercy.

### Steps

1. `pvp_death_con_loss` true; the healer con buyback works.
2. Player-shaped kills grant XP + RP (Tier 1 step 7). No loot, no coin drop.
3. Immunity timers (`pvp` properties `Timer_Killed_By_Player`,
   `Timer_Killed_By_Mob`, `Timer_Region_Changed`, `Timer_PvP_Teleport`)
   apply to humans and bots. Bots must not camp immune targets or bind points
   in safe hubs.
4. `/safety` below 10: on by default, off is permanent (`safety.cs`), protects
   only outside OF frontier zones. Bots never exploit safety past level 10.
5. Starter zones (Cotswold, Mularn, Mag Mell, and so on) are **not** safe. The
   grey policy (decision 6) is the only bot restraint there.
6. No battleground leveling track and no `/level` shortcut.
7. Realm points and ranks from PvP kills are personal stats and realm-ability
   currency. They never buff a whole realm.
8. Remove Atlas bounty-point generation (`AtlasROGManager`) on PvP.

### Playability mercy that stays

- Capitals, housing, newbie PvE dungeons, portal-keep hubs
- Group/guild/battlegroup immunity
- Companion loyalty
- Sub-10 `/safety`
- Grey-target restraint

Do not add more switches unless the owner asks after playtesting.

### Tests and gate

- Con loss on PvP death is on.
- A flagged sub-10 is protected in Cotswold and not in Emain.
- An autonomous level-50 bot *can* legally attack a same-realm level-5 in a
  starter zone, and by default mostly doesn't.
- **Gate:** A fresh character leaving a capital is at risk. Cities and hubs
  remain sanctuaries.

---

## Tier 7 — PvE, economy, and meshes stay

**Goal:** Grinding, dungeons, raids, loot, and Realm Exchange still work;
they just happen in a PvP world.

### Steps

1. Do not strip `/grind`, `/spawn`, `/raid 40/80`, dungeon routes, or dragon /
   epic PvE directors. Hostile players may interrupt them.
2. `AutonomousRealmRaid` / `RealmRaid*` (Golestandt, Caer Sidi, etc.) are PvE.
   Recruit by class, level, and crew, not "Albion raids as a faction." Mixed
   crews may raid together. The raid dungeon itself stays where it is.
3. Loot, crafting, equipment upgrades, and coin stay real. A fresh save means
   empty inventories, not deleted item templates. `allow_cross_realm_items`
   stays false (1.65 rule: realm gear stays realm gear).
4. Realm Exchange stays a real-item market in the three capitals, usable by
   anyone.
5. Leave all current navmeshes in place. If a mixed-realm city path or portal
   route fails, fix **that** route with evidence.
6. Do not change native client hash guards or raid UI patches unless a PvP UI
   bug is proven.

### Tests and gate

- Existing PvE bot tests pass under `PvPServerRules`.
- A mixed-realm companion grind group kills mobs and not each other.
- Realm Exchange tests use local-broker rules.
- **Gate:** A player can level in PvE with companions while remaining
  attackable in the open world.

---

## Tier 8 — Population tuning

**Goal:** The shard *feels* like 2003 Camlann: crews, frontier roams, ganks,
keep fights. Not an empty ruleset, and not a 200-bot starter-zone camp.

### Tune, in order

1. Crew size mix (pairs, 8-man roams, keep groups).
2. Time split: grind / city / roam / hunt / keep and relic war.
3. How often high-level crews pass through levelling zones (grey policy rate).
4. Keep and relic contest frequency; how long relics stay put.
5. Active Population slider: still a count, now of Camlann actors.

Do this **after** Tiers 1–7 are honest.

### Gate

Owner playtest with a real client (Tier 9). Offline tests cannot certify feel.
Change numbers; do not reintroduce realm teams to "balance" the frontier.

---

## Tier 9 — Ship one mode

**Goal:** Everything the player can see agrees this is Camlann, and a real
client has run the main loops.

### Product text

- Launcher name/help, the reset prompt, keep/relic panel, Active Population
  copy, and the per-realm "+ Lv.1 / + Lv.50" buttons (now "add to crews")
- `docs/PLAY.md`, `docs/QUICK-COMMANDS.md`, `docs/LLM-QUICKSTART.md`
- `ALL SERVER COMMANDS.txt` for PvP commands (`/safety`, `/gc form`, etc.)
- Changelog MAJOR with Added/Changed/Removed. Removed: Normal RvR as the world
  model, home-realm keep reset, realm-as-team bot war, Normal save import.

### Verification (split the report)

**Offline / static**

- Server and launcher tests in a separate output tree
- Reset fixture: no leftover characters or realm-owned keeps; marker set
- Config `GameType` PvP
- Navmeshes unchanged unless a listed local fix exists

**Real client** (owner permission to start the server)

1. Convert a copy of an existing v0.3 folder; the backup exists; a fresh
   account is created.
2. The capital and a portal-keep hub are safe. Merchants, chat, and grouping
   with foreign-realm bots work.
3. Leave town: a same-realm gamebot shows as hostile and can attack the player.
4. `/spawn` a companion from another realm: it shows as friendly, heals and
   fights that bot, and never the player.
5. `/safety` behavior under level 10.
6. Open-world gank, frontier roam, and a grind mob pack all function.
7. `/gc form`, claim a keep with companions, and move a relic; the bonus is
   guild-only.
8. Realm Exchange in a foreign capital.
9. A PvE dungeon or raid still runs.
10. Stop and restart the server: the Camlann save loads and crews persist; an
    unconverted Normal DB is refused.

Do not present unit-test totals as that client pass.

---

## Suggested landing slices

Land **in order** as internal checkpoints toward `1.0.0`. Do not ship a
playable Normal world between slices.

| Slice | Tiers | Playable? |
|---|---|---|
| A | 0–1 | Conversion + rules + client spike. Bots may still act realm-ish. |
| B | 2–3 | Dangerous open world; cities and mixed groups work. |
| C | 4–5 | Crews, guild keeps, relics. Camlann structurally. |
| D | 6–8 | Full lethality and feel. |
| E | 9 | Documented, verified, MAJOR shipped. |

## File map (starting points)

| Work | Start here |
|---|---|
| Server type | `GameServerConfiguration.cs`, `serverconfig.example.xml`, `OfflineDaoc.Setup` |
| World reset | new launcher reset beside `KeepRelicReset.cs`; `offline_local_options` marker |
| PvP rules | `serverrules/PvPServerRules.cs`, `AbstractServerRules.cs`, `NormalServerRules.cs` (fork guards to port) |
| Client friend/enemy | `packets/Server/PacketLib1124.cs`, `gameutils/Group.cs`, `gameutils/Guild.cs`, `GetLivingRealm` |
| Bot as player | `bots/GameBot.cs`, `bots/IGamePlayer.cs` |
| Companions | `bots/CompanionPvpEngagement.cs`, `BotGroupInvite.cs`, `PlayerLedPullCoordinator.cs`, `BotBrain.cs` |
| Hostility | `bots/BotRvrAmbush.cs`, `BotPvpCrowdControl.cs`, `autonomous/AutonomousRvrTargetPolicy.cs` |
| Realm walls | `autonomous/AutonomousRealmBoundary.cs`, `AutonomousWorldBotController*.cs`, `AutonomousFrontierTransport.cs` |
| Armies / events | `autonomous/AutonomousRvrEventLayer*.cs`, `AutonomousRealmLoginBalancer.cs`, `AutonomousRvrStaging.cs` |
| Bot guilds | `autonomous/AutonomousBotEconomy.cs` (`offline_world_bots`), `gameutils/Guild.cs`, `GuildMgr` |
| Keeps | `keeps/KeepManager.cs`, `AbstractGameKeep.cs`, `keeps/Gameobjects/Guards/Lord.cs`, `keeps/Managers/Player Manager.cs` |
| Relics | `keeps/Managers/RelicMgr.cs`, `keeps/Relics/GameRelic.cs`, `GameRelicPad.cs` |
| Launcher reset | `source/tools/OfflineDaoc.Launcher/KeepRelicReset*.cs` |
| Exchange | `bots/BotBrain.cs` (exchange), `RealmExchangeBroker` |
| Tests | `source/server/Tests/UnitTests`, launcher tests |

## Safety reminders

- Resolve paths from this checkout.
- Build/test in a separate output tree. Never publish a live SQLite database,
  its backups, `account.txt`, bot profiles, or credentials.
- Do not start the server or overwrite a running install without permission.
- Historical scripts in `source/server/tools` are not the bootstrap.
- Owner-requested Camlann replacement is an intentional gameplay change; that
  overrides the default "preserve current RvR travel/saves" rule.

# Camlann conversion

This is the implementation plan for replacing Offline DAoC's three-realm RvR
world with a **single Camlann/Mordred-style full-PvP world**. It is a plan, not
a completed change. Do not start this work, start the game server, or deploy
over a running install unless the owner asks.

Camlann was the European GOA full-PvP shard. Mordred was the US equivalent.
This fork will not recreate 2003 GOA infrastructure. It will recreate that
**ruleset and bot society** as the only playable mode.

Read `AGENTS.md`, `docs/DEVELOPMENT.md`, and `source/server/AGENTS.md` before
editing. Distinguish the real player, companion bots, and autonomous gamebots
in every hostility and AI change.

## Contract

- **One mode.** No `if (Normal)` leftover world. `GameType` is PvP for the
  shipped server.
- **New world.** Existing characters, inventories, coins, bot rosters, keep
  ownership, relic state, Realm Exchange listings, and realm-event records are
  discarded. Fresh local account bootstrap still creates a new account.
- **Keep the maps.** Current world spawn data and the 99 navigation meshes stay
  unless a specific Camlann route is proven broken. Do not globally rebuild nav.
- **Realm is identity, not team.** Characters still have a realm (race, class,
  capital, starter zone). Realm no longer means ally.
- **Companions never attack their leader.** That is Camlann grouping, not a
  second PvP mode.
- **Versioning.** This planning document is a PATCH. Implementing the
  conversion is a **MAJOR** (new playable world / save). Do not bump MAJOR until
  a tier that actually changes the running game is finished. Completing the
  conversion is `1.0.0` unless the owner picks another MAJOR label.
- **Upstream download.** GitHub v0.3 remains the playable runtime package.
  Do not rewrite `Get-OfflineDAoC.ps1` or the `docs/PLAY.md` download steps
  when bumping this fork.

## How to use the tiers

Work **one tier at a time**. Finish the gate before starting the next. Each
tier should:

1. Change only the listed systems.
2. Add or rewrite tests for the new hostility/ownership model. Do not keep
   tests that encode Alb-vs-Mid-vs-Hib as the definition of "enemy."
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
- Recreating GOA-era patches, language clusters, or live Camlann population
- Executing historical scripts in `source/server/tools` just because they exist

## Current code (the thing being replaced)

| Area | Today | Camlann target |
|---|---|---|
| Server type | `GST_Normal` (`GameType` Normal) | `GST_PvP` only |
| Hostility | Other realm = enemy | Anyone outside group/guild = enemy |
| `GameBot` | `GameNPC` + `IGamePlayer`; Normal rules treat other-realm bots as enemies | Must be treated as a **player** in PvP rules |
| Companions | `CompanionPvpEngagement` assists vs other realm only | Assist vs any non-allied player-bot |
| Gamebots | Three realm armies, realm staging, realm keep events | Mixed-race crews / guilds |
| Home worlds | `AutonomousRealmBoundary` keeps bots in their own lands | Open travel; cities are mixed and mostly safe |
| Keeps | Realm ownership; launcher resets to `OriginalRealm` | Guild claim; unclaimed until taken |
| Relics | Realm pickup and realm-wide bonus (`RelicMgr`) | Guild carry/place; bonus for that guild only |
| Battlegrounds | Present in Normal | Unused / unreachable |
| Frontiers | New Frontiers RvR | Open PvP; keeps and relics still exist |

DOL already has `PvPServerRules` (`source/server/GameServer/serverrules/PvPServerRules.cs`).
It was written for human `GamePlayer`s. This fork's world is mostly `GameBot`s.
Flipping `GameType` without Tier 1 will likely make autonomous bots **friendly
NPCs**.

---

## Tier 0 — Project bootstrap

**Goal:** The repo and a fresh install agree that Camlann is the only world,
before AI or keeps change.

### Steps

1. Treat the next gameplay landing as a new save. Do not write a character
   progress importer for Normal → Camlann.
2. Default `EGameServerType.GST_PvP`:
   - `source/server/GameServer/GameServerConfiguration.cs` (load + constructor)
   - `source/server/CoreServer/config/serverconfig.example.xml`
   - whatever the launcher/setup writes into the playable `serverconfig`
3. Seed a **clean** world database: empty accounts/characters/bots/inventories,
   default unclaimed keeps, relics on home shrines, empty Realm Exchange.
   Never commit a database after playing.
4. Rename launcher copy that still describes "return keeps to original realms"
   as Ywain behavior. Leave the actual reset rewrite for Tier 5; do not ship a
   Camlann server that still offers a home-realm keep restore.
5. Point `docs/PLAY.md`, `docs/QUICK-COMMANDS.md`, and the launcher help at
   "full PvP / Camlann" only after a later tier makes that true. In this tier,
   keep player-facing text from promising Camlann until rules actually flip.
6. Add a short pointer in component docs so later agents read this file before
   editing hostility, keeps, or autonomous AI.

### Tests and gate

- Launcher/setup tests: missing or `Normal` `GameType` fails closed, or the
  shipped config is PvP.
- Clean-seed checks already used for releases still pass (empty progress
  tables).
- **Gate:** A new install's server config is PvP, and there is no supported
  path that loads a Normal save. Bot brains may still be realm-vs-realm; that
  is Tier 3–4.

---

## Tier 1 — PvP ruleset and GameBot-as-player

**Goal:** Stock Camlann attack/heal/group/guild/safe-zone rules apply to the
human **and** to every `GameBot`. Companions in the player's group are allies.

### Why first

Every later AI change calls `GameServer.ServerRules.IsAllowedToAttack`. If that
still treats bots as friendly NPCs, later tiers cannot be tested.

### Steps

1. Keep `PvPServerRules` as the live rules class
   (`[ServerRules(EGameServerType.GST_PvP)]`).
2. Introduce a single helper, used by PvP rules, of the form "this living is a
   PvP combatant" (`GamePlayer` or `GameBot` that is not a mundane mob). Use it
   for:
   - `IsAllowedToAttack`
   - `IsSameRealm` (PvP "same realm" already means "friendly player" for two
     humans; bots must join that path)
   - group, guild, battlegroup immunity
   - safe-region and `/safety` checks
3. Resolve controlled pets to their owner (already in `PvPServerRules`). Make
   sure `BotBrain` / companion pets do not skip that path.
4. **Companion immunity:** a temporary companion (`IsTemporaryGroupHelper`,
   not autonomous) cannot attack its leader, group members, or their pets.
   Autonomous gamebots are not companions.
5. Safe regions stay as in `PvPServerRules.m_safeRegions` (Camelot, Jordheim,
   Tir na Nog, housing, listed PvE tombs). New Frontiers (`163`) stays unsafe.
6. Color handling stays PvP byte `1` (all other PCs red). Bots must show as
   player-colored, not NPC-green.
7. `OnPlayerKilled` / immunity / con-loss paths must run for bot victims and
   bot killers where Camlann would award or penalize a human. If a kill of a
   `GameBot` currently goes through NPC XP only, split "autonomous PvP kill"
   from "PVE mob kill."
8. Move unit tests off `new NormalServerRules()` wherever they assert
   hostility. `EpicTestServerScope`, `UT_PlayerLedPullCoordinator`,
   `UT_RealmExchangeBotDecisions`, `UT_ContinuousBotRoutes`, and similar
   fixtures should construct `PvPServerRules` (or a thin test subclass).
9. Delete or stop calling playable Normal-only branches in
   `assist.cs`, `who.cs`, `PlayerEnterExit.cs`, `PacketLib1124.cs` group-hack
   comments, once PvP is the only type. Leave DOL enum values.

### Tests and gate

- Human vs autonomous bot: allowed outside safe zones, blocked in Camelot.
- Human vs own companion: never allowed.
- Companion vs autonomous bot: allowed if the leader could attack that bot.
- Two grouped humans/bots: not allowed.
- Same-guild autonomous bots: not allowed (even if different realms).
- Same-realm strangers, no guild: **allowed**.
- Mixed-realm group: allowed to group; not allowed to attack each other.
- **Gate:** `IsAllowedToAttack` matches Camlann for player/companion/gamebot
  triples. No bot AI rewrite required yet.

---

## Tier 2 — Neutral home worlds

**Goal:** Any realm can use any capital, merchant, guard (city), chat, and
travel. Battlegrounds are not part of play.

### Steps

1. Confirm `PvPServerRules.IsAllowedToGroup / JoinGuild / Trade / Understand`
   already return true. Keep them that way.
2. City guards and Peace-flag NPCs remain unattackable (already in PvP rules).
   Keep frontier keep guards on the keep/guild rules (Tier 5).
3. Stop realm-locking bot movement:
   - `AutonomousRealmBoundary`
   - `AutonomousWorldBotController.ProtectedRealm`
   - town-idle filters that skip other capitals
   Gamebots may idle in Camelot, Jordheim, or Tir regardless of their realm.
4. Realm Exchange: a bot or player uses the **local** broker, not "the broker
   of my realm." `BotBrain.TryHandleAutonomousRealmExchange` currently matches
   `candidate.Realm == bot.Realm`. Change to "broker in this city / this
   region." Keep real items and coin. Empty market on the fresh save.
5. Darkness Falls: `DFEnterJumpPoint.CanRealmEnter` already returns true when
   `ServerType != GST_Normal`. Do not reintroduce keep-count DF ownership as a
   realm gate. Bot DF policy
   (`AutonomousDarknessFallsPolicy`) must stop assuming one owning realm.
6. Housing already allows all realms on PvP (`HouseTemplateMgr`). Keep that.
7. Battlegrounds: do not send the player or gamebots there. Teleporters,
   frontier stones, and bot travel must not pick BG regions. Leaving the BG
   tables in the world DB is fine if nothing routes into them.
8. `/who`, login messages, and assist already have PvP cases. Make those the
   only cases.

### Tests and gate

- Albion character (or bot) can path into Jordheim and use a merchant.
- Realm Exchange list/buy works from a foreign capital.
- DF entry does not require realm keep score.
- No autonomous goal selects a battleground region.
- **Gate:** Open travel and city services work. Bots may still *prefer* their
  own starter zone; they must not be *forbidden* from others.

---

## Tier 3 — Hostility rewrite

**Goal:** Every "is this an enemy?" check uses group/guild, not `actor.Realm !=
target.Realm`.

### Primary call sites

Replace realm inequality in:

- `CompanionPvpEngagement.Enemy` (currently other-realm `GameBot` only)
- `BotRvrAmbush.IsEnemyCombatant`
- `AutonomousRvrTargetPolicy.IsEligible`
- `AutonomousDungeonPolicy.CanEngageLocalOpponent`
- `BotSiegeRuntime` enemy keep/player checks
- `AutonomousWorldBotController` frontier threat / NPC filters that assume
  other-realm
- `AutonomousBotRealmPointRewards` (do not award for same-guild kills)
- `BotReleaseBindPoints.IsEnemyBindPosition` if it uses realm
- Companion defensive scan / `/pull` target validation

Keep using `ServerRules.IsAllowedToAttack` as the last word. Do not invent a
second hostility matrix.

### Companion behavior

- Aggressive: assist what the leader attacks, if legal.
- Defensive: hold near the leader; engage nearby legal threats.
- Never acquire the leader, other companions, or grouped gamebots.
- Same-realm autonomous bot in the open world **is** a legal threat.

### Steps

1. Add a shared predicate, e.g. `CamlannHostility.IsEnemy(a, b)`, wrapping
   server rules plus "not in same group/guild."
2. Rewrite `CompanionPvpEngagement` tests in `UT_PlayerLedPullCoordinator` so
   a same-realm autonomous bot is an enemy and a grouped mixed-realm bot is
   not.
3. Rewrite `UT_AutonomousRvrEventLayer` / target-policy tests that currently
   assert Alb vs Alb is ineligible and Alb vs Mid is eligible.
4. Leave keep-take events and three-army directors for Tier 4–5. If those
   directors still spawn Alb-vs-Mid battles, they will look wrong until then;
   do not add a Normal fallback.

### Tests and gate

- Same-realm stranger: enemy.
- Mixed-realm group member: friend.
- Companion raid of 40/80: no friendly fire.
- Stealth ambush can pick a same-realm target.
- **Gate:** No remaining bot combat filter uses `realm != realm` as the
  definition of enemy. Keep *ownership* may still be realm-flavored until
  Tier 5.

---

## Tier 4 — Crews instead of realm armies

**Goal:** Autonomous population is mixed-race **crews** (guilds), not three
realm factions.

### What to stop

- `AutonomousRealmLoginBalancer` filling Alb/Mid/Hib quotas
- `AutonomousRvrStaging` (Castle Sauvage / realm gates as army spawns)
- `AutonomousRvrEventLayer` attackerRealm vs defenderRealm keep wars
- `AutonomousRvrDirector` / `AutonomousRvrPlanningView` ally/enemy counts by
  realm
- `RealmRaidMuster` as a realm-wide PvP rally (realm PvE raids stay in Tier 7)
- Chat/banter that announces "Midgard is taking keep X" as the world model

### What to add

1. **Crew identity.** Each autonomous bot belongs to a guild (or a lightweight
   crew record that is a real `Guild` so keep claims work in Tier 5). Mix
   Alb/Mid/Hib members in one crew.
2. **Spawn/login.** Population slider still sets how many gamebots exist.
   Balance **crews**, not realms. A crew can be small (gank pair) or larger
   (keep group).
3. **Goals.** Replace "defend our realm frontier" with:
   - grind / hunt in dangerous zones
   - gank unallied player-shaped targets
   - travel mixed cities
   - contest a keep as a crew (wired in Tier 5)
4. **Identity generator.** Names/races stay realm-correct for the character.
   Guild name is shared across realms.
5. **Town idle.** Mixed-realm crowds in capitals. No "this is an Albion-only
   square."
6. **Siege kits.** `AutonomousSiegePolicy` merchant lists are still per-realm
   item templates (ram kits differ). A Hibernian in a mixed crew buys the kit
   that matches **their character realm**, not the crew's fictional realm.

### Tests and gate

- A guild roster can contain all three realms.
- Login balancer does not force 1:1:1 realm counts as teams.
- No event layer starts a battle because `attacker.Realm != keep.Realm` alone.
- **Gate:** Dumping the live population shows crews, not three armies. Keep
  takes may still be incomplete until Tier 5.

---

## Tier 5 — Guild keeps and relics

**Goal:** Frontiers warfare matches Camlann/Mordred: guilds own keeps and
relics; relic bonuses apply to that guild only.

### Keeps

Live DOL already has pieces:

- `KeepManager.IsEnemy` on PvP uses `keep.Guild` vs `target.Guild`
- `AbstractGameKeep.CheckForClaim / Claim` is guild-based
- `PvPServerRules.ResetKeep` assigns the killer group's leader realm as the
  keep's display realm (cosmetic realm of the taker, not a team)

Required work:

1. Fresh save: all frontier keeps unclaimed (`ClaimedGuildName` empty). Do not
   seed them as Alb/Mid/Hib owned.
2. Unclaimed keep hostility: use `PVP_UNCLAIMED_KEEPS_ENEMY` deliberately.
   Camlann-style: unclaimed keeps are takeable; guards should not behave like
   a full enemy realm army unless that is the chosen rule. Document the choice
   in the changelog when implemented.
3. Claiming: crew guild must have claim rank; honor `GUILDS_CLAIM_LIMIT`.
4. `RelicGameKeep` currently says relic keeps cannot be claimed. Camlann
   placed relics in **claimed ordinary keeps**, not realm relic forts. Keep
   relic forts as shrines/start points if needed; do not require realm relic
   keeps as the only valid pads.
5. Rewrite launcher `KeepRelicReset` /
   `KeepRelicResetPanel`. It must **not** `SET Realm=OriginalRealm`. New
   behavior: clear guild claims, return relics to shrines, leave politics
   empty. Update `KeepRelicResetTests`.
6. Bot keep AI (`AutonomousRvrKeepPolicy`, keep approach, siege) attacks
   unallied-guild keeps, not "other realm" keeps.
7. Lord/guard guild refresh on claim (`ChangeGuild`) must run for bot-led
   takes. `ResetKeep` must accept a `GameBot` killer, not only `GamePlayer`.

### Relics

Replace realm logic in `RelicMgr` and `GameRelic`:

| Live Normal | Camlann target |
|---|---|
| Pickup if you own your realm's relic of that type | Pickup from **unclaimed** keep/shrine; carrying guild must already own a keep |
| Cannot take your own realm's mounted relic | Cannot take your **guild's** mounted relic |
| Bonus if realm owns original + enemy relic | Bonus only for members of the guild that mounted it |
| Broadcast "Midgard captured…" | Broadcast the **guild** name |
| `RelicGameKeep` unclaimable | Ordinary claimed keep is the destination pad |

Classic PvP details to implement unless playtesting proves them unusable
offline:

1. Kill shrine NPC guards (or keep lord) before pickup.
2. Relics can only be picked from an unclaimed keep; claiming without pickup
   returns the relic to the shrine.
3. Place a relic only after the keep has been claimed for a delay (live used
   two hours; a shorter offline delay is acceptable if documented).
4. Abandoned relics return to the shrine with guards.

`GameBot` already implements `IGamePlayer`; pickup uses that. Make sure
inventory, stealth block, and "already carrying" apply to bots.

### Tests and gate

- Unclaimed keep: two rival crews can contest it; same crew is friendly to
  its claimed guards.
- Relic bonus does not apply to same-realm strangers in another guild.
- Launcher reset clears claims and homes relics without restoring realm
  ownership as teams.
- Bot killer can claim/reset a keep through the PvP path.
- **Gate:** A crew can take a keep, move a relic, and only that crew receives
  the bonus.

---

## Tier 6 — Full PvP consequences

**Goal:** Original Camlann lethality, not a tamed flag.

### Steps

1. Keep `pvp_death_con_loss` true.
2. Player-shaped kills grant XP/coin per PvP rules; dying costs constitution
   (and cash as implemented).
3. Immunity timers: killed by player, killed by mob, region change, teleport
   (`ServerProperties` `pvp` group). Bots must not ignore them to machine-gun
   corpses in cities.
4. `/safety` for levels below 10, off is permanent (`safety.cs`). Unsafe in
   New Frontiers even with the flag. Autonomous bots never use safety as a
   gank shield past the intended level.
5. Starter zones (Mularn, Cotswold, Lough Derg, and equivalents) are **not**
   in `m_safeRegions`. Crews may gank there. That is the point of full
   Camlann.
6. No battleground leveling track.
7. Realm points / titles from PvP kills stay if they still make sense as
   personal stats. They must not buff an entire realm.

### Playability (not a second mode)

The only mercy that stays:

- City interiors listed in `m_safeRegions`
- Group/guild immunity
- Companion loyalty
- New-character `/safety` until 10, if they leave it on

Do not add a "bots won't attack below 20" switch unless the owner asks after
playtesting.

### Tests and gate

- Con loss on PvP death is on.
- Safety flag blocks attack in Cotswold for a flagged sub-10; does not block
  in region 163.
- Autonomous bot can legally attack a same-realm level 5 in a starter zone.
- **Gate:** A fresh character leaving a capital is at risk. Cities remain
  sanctuaries.

---

## Tier 7 — PvE, economy, and meshes stay

**Goal:** Grinding, dungeons, raids, loot, and Realm Exchange still work;
they just happen in a PvP world.

### Steps

1. Do not strip `/grind`, `/spawn`, `/raid 40/80`, dungeon routes, or dragon
   / epic PvE directors. Those are PvE content. Hostile players may interrupt
   them.
2. `AutonomousRealmRaid` (Golestandt, Caer Sidi, etc.) is PvE. Keep it as
   "bots of appropriate **class/level** go to this dungeon," not "Albion
   raids as a faction." Mixed crews may run PvE together.
3. Loot, crafting, equipment upgrades, and coin stay real. Fresh save means
   empty inventories, not deleted item templates.
4. Realm Exchange stays a real-item market. Brokers remain in the three
   capitals; anyone may use them (Tier 2). No migration of old listings.
5. Leave all current navmeshes in place. If a mixed-realm city path fails,
   fix **that** route with evidence. Do not rebuild the mesh set.
6. Do not change native client hash guards or raid UI patches unless a PvP
   UI bug is proven.

### Tests and gate

- Existing PvE bot tests still pass under `PvPServerRules`.
- Companion grind group still kills mobs and does not kill each other.
- Realm Exchange unit tests use local-broker rules, not realm-matching.
- **Gate:** A player can level in PvE with companions while remaining
  attackable in the open world.

---

## Tier 8 — Population tuning

**Goal:** The shard *feels* like Camlann: crews, ganks, keep fights — not an
empty ruleset and not a 200-bot starter-zone camp that makes the game
unplayable.

### Tune, in order

1. Crew size mix (pairs vs keep groups).
2. Time split: grind / city / hunt player-shaped targets / keep war.
3. How often high-level crews visit starter zones.
4. Relic and keep contest frequency.
5. Active Population slider: still a count, now of Camlann actors.

Do this **after** Tiers 1–7 are honest. Tuning a realm-army brain will not
produce Camlann.

### Gate

Owner playtest with a real client (see Tier 9). Offline tests cannot certify
feel. Change numbers; do not reintroduce realm teams to "balance" Frontiers.

---

## Tier 9 — Ship one mode

**Goal:** Everything the player can see agrees this is Camlann, and a real
client has run the main loops.

### Product text

- Launcher name/help, keep/relic panel, Active Population copy
- `docs/PLAY.md`, `docs/QUICK-COMMANDS.md`, `docs/LLM-QUICKSTART.md`
- `ALL SERVER COMMANDS.txt` only as needed for PvP commands (`/safety`, etc.)
- Changelog MAJOR with Added/Changed/Removed. Removed: Normal RvR as the
  world model, home-realm keep reset, realm-as-team bot war.

### Verification (split the report)

**Offline / static**

- Server and launcher tests in a separate output tree
- Clean seed: no leftover Normal characters or realm-owned keep teams
- Config `GameType` PvP
- Navmeshes unchanged unless a listed local fix exists

**Real client** (owner permission to start the server)

1. Fresh account, any realm.
2. Capital is safe; merchants/guards/chat/group with foreign-realm bots work.
3. Leave town: a same-realm gamebot can attack the player.
4. `/spawn` companions: they fight that bot and never the player.
5. `/safety` behavior if under 10.
6. Open-world gank and a grind mob pack both function.
7. A crew takes a keep; relic bonus is guild-only.
8. Realm Exchange in a foreign capital.
9. PvE dungeon or raid still runs.
10. Server stop; fresh save still loads; old Normal save is not supported.

Do not present unit-test totals as that client pass.

---

## Suggested landing slices

If a single MAJOR dump is too large, land **in order** as internal checkpoints
still on the way to `1.0.0`. Do not ship a playable Normal world between
slices.

| Slice | Tiers | Playable? |
|---|---|---|
| A | 0–1 | Not yet. Rules are PvP; bots may still be dumb/realm-ish. |
| B | 2–3 | Dangerous open world; cities work; armies may still look like realms. |
| C | 4–5 | Crews and guild Frontiers. This is Camlann structurally. |
| D | 6–8 | Full lethality and feel. |
| E | 9 | Documented, verified, MAJOR shipped. |

## File map (starting points)

| Work | Start here |
|---|---|
| Server type | `GameServerConfiguration.cs`, `serverconfig.example.xml` |
| PvP rules | `serverrules/PvPServerRules.cs`, `AbstractServerRules.cs` |
| Bot as player | `bots/GameBot.cs`, PvP rules helpers |
| Companion PvP | `bots/CompanionPvpEngagement.cs`, `BotBrain.cs` |
| Hostility | `bots/BotRvrAmbush.cs`, `autonomous/AutonomousRvrTargetPolicy.cs` |
| Realm walls | `autonomous/AutonomousRealmBoundary.cs`, `AutonomousWorldBotController.cs` |
| Armies / events | `autonomous/AutonomousRvrEventLayer*.cs`, `AutonomousRealmLoginBalancer.cs` |
| Keeps | `keeps/KeepManager.cs`, `AbstractGameKeep.cs` |
| Relics | `keeps/Managers/RelicMgr.cs`, `keeps/Relics/GameRelic.cs` |
| Launcher reset | `source/tools/OfflineDaoc.Launcher/KeepRelicReset*.cs` |
| Exchange | `bots/BotBrain.cs` (exchange), `RealmExchangeBroker` |
| Tests | `source/server/Tests/UnitTests`, launcher keep-reset tests |

## Safety reminders

- Resolve paths from this checkout.
- Build/test in a separate output tree. Never publish a live SQLite database,
  `account.txt`, bot profiles, or credentials.
- Do not start the server or overwrite a running install without permission.
- Historical scripts in `source/server/tools` are not the bootstrap.
- Owner-requested Camlann replacement is an intentional gameplay change;
  that overrides the default "preserve current RvR travel/saves" rule.

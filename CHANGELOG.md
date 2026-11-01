# Changelog

The newest version is first. For the full detail of every earlier update, see
[docs/history/CHANGELOG-0.3-to-0.32b.md](docs/history/CHANGELOG-0.3-to-0.32b.md).

> **On hold.** 0.37 / 0.37b / 0.37c is the last release of Offline DAoC, and the project won't receive any more
> updates. The owner has moved on to a separate True 1.65 version. The source changes made after 0.37c, listed
> below, are the last ones.

## Changes since 0.37c (source only, no release)

0.37 / 0.37b / 0.37c is the latest and last release. Everything below is in the source only.

**Going forward only the new client is supported.** The original DAoC client is no longer supported. The
Sluaghbinder stays in the "b" editions, as before.

**MAJOR: bot armor now works like a player's**
- Until now the armor a bot (gamebot or companion) wore did nothing when it was hit. Its defense was a monster's: a
  flat number from its level, and the game never picked which piece of armor was struck. So a bot's armor value,
  absorb, quality and condition, the armor-type resists against slash, crush and thrust, the armor's to-hit bonus
  and its procs never counted. At level 50 every bot defended better than any player: about 1.2 times a player in
  plate, and about 3 times a caster in cloth.
- Bots now defend exactly like players. The piece that is hit is chosen with the player odds, its armor value is
  capped by level and armor type, quality and condition count, absorb comes from the armor type, armor buffs have
  the player caps, and the worn piece's to-hit bonus and procs work. `/targetstats` on a bot shows its torso armor.
- This changes the game a lot. Well-geared bots are a little softer than before at high level, and bots with little
  or no armor are much softer (a bot caster in cloth takes 2 to 3 times the melee damage it used to), so gear matters
  for them as it does for players.

**Bots and raids**
- Hibernian bots no longer get stuck at the door of their portal keep in Odin's Gate. Once out of the keep's
  inner door, the route they planned led back in to the keep yard and then out again, over and over, until they
  gave up and went home (47 bots and 292 give-ups in one test run). A planned route no longer starts by going back
  through a door the bot has just come out of when that leads no nearer its goal.
- Bots no longer fight things a player can't target. In Galladoria the Corrupt Primal of Fire leaves short-lived
  patches of fire behind it (they can't be targeted, resist nearly everything and burn out in six seconds), and
  nearly the whole raid by the Earthmenders was swinging at them instead of the Earthmenders and the primal; the
  Fire primal was given up at 99%.
- When only a handful of a raid fight its current target while many stand around it, up to a third of those already
  busy with something else now switch to it as well. Before, only idle raiders were called over until the target had
  gone untouched for three minutes, which never happens while a few keep chipping at it (Galladoria's Magical
  Earthmender: 2 of 207 raiders on it).
- The patches of fire Galladoria's Corrupt Primal of Fire leaves behind hit for less: 120 heat damage in a 300 radius
  every 2 seconds (was 220 in a 450 radius).
- Siege armies' healers go out to their dead. A fallen attacker now waits for a raise while one of its army's healers
  is within 5,000 (it was 2,000; with nobody that close it released after 20 seconds and walked back from its portal
  keep, about 185,000 units at Dun Dagda), healers out of the fight walk out to the dead on the field, and the realm's
  other RvR healers at the battle raise them too. At Dun Dagda most attackers fall on the slopes 3,000 to 4,500 below
  the keep, out of reach of the healers at the gate.
- Albion bots no longer get sent home to Camelot from beside their portal keep in Odin's Gate. A walled-in floor just
  south of the keep joins nothing; bots that ended up on it now step back into the keep yard and go on.
- Bots can now finish Caer Sidi: a bot raid killed the four horsemen and Apocalypse for the first time. The hidden
  controller that starts the horsemen's challenge only woke up for players, so it never noticed the raid waiting at
  its trigger (over a hundred bots stood there for half an hour with nothing happening). Hidden encounter controllers
  now always run, whoever is near.
- Galladoria's Olcasgean wakes for a bot raid once all four primals are dead. He slept on as a statue, because only his
  own brain starts the timer that wakes him, and bots never woke a monster that couldn't attack them. Raid bots now
  wake any scripted encounter boss near them, as a player would.
- A relic or keep army stays together on the march. When its leader stopped leading, the next one was simply the first
  marcher on the list, wherever it was; a straggler 64,000 units behind could take over, and the front of the army
  walked back to it (at Dun Dagda the siege then opened with 62 of 170 attackers there). The new leader is the marcher
  with most of the army around it. In the next test the army marched in 22 minutes and opened the siege with 161 at
  the keep.
- A siege army's healers stay behind the line. Those that can raise the dead no longer pick fights of their own in the
  melee: they step back from an enemy close to them and heal and raise their army from behind, and fight back only
  when they are hit. They used to fight in the front with everyone else and were always under attack or dead when
  their army fell, so no fallen attacker was ever raised at Dun Dagda.
- A group member that dies three times on its way to the group's meetup point counts as a no-show at once, and
  the group goes on without it, instead of waiting the full fifteen minutes (a low-level group was seen dying
  about 70 times near its meetup during one wait and then being dropped anyway). (From the
  chris4aker/OfflineDAoC-SocialExpansion fork.)
- Bots and companions use their area spells on two or more monsters (GitHub issue #17). Point blank spells (centred on
  the caster) were never cast at all, because of a range check that could not pass. Now:
  - Thanes, Reavers, Clerics and Shamans use their instant point blank spells when two or more monsters already in the
    fight are around them.
  - Wizards, Theurgists, Eldritches, Enchanters, Mentalists, Spiritmasters, Runemasters and Bonedancers cast theirs
    where they stand, or walk a short way in (up to 700 units) to reach two or more while their group holds the
    monsters and nothing is hitting them. A caster that is hit or interrupted waits 15 seconds, and two failures end
    it for that fight, so it never walks in and out in a loop.
  - An area nuke is chosen over a single nuke when its total over the monsters it hits is bigger.
  - No area spell is cast when it would also hit a mesmerized monster (waking it) or one not in the fight yet (pulling
    it).
  - Ground-targeted area spells land on the target; bots never set a ground target, so these always failed.
- Every class with a mesmerize or stun now controls adds, not only Bards: a monster attacking the group that nobody
  is killing gets mesmerized (an area mez only where it cannot reach the monster being killed), and a stun protects
  someone an add is hitting while they fight something else. The instant mez and stun with a ten-minute reuse are kept
  for three or more adds. Healers, Clerics and Druids in a group never did this, because their support turn cancelled
  any harmful spell; they now do, after heals, and keep a quarter of their power for healing.
- Augmentation Healers cast Celerity while a melee groupmate is fighting in melee without it. The buff choice had no
  place for it, so it was never cast.
- Cabalist, Enchanter and Spiritmaster bots and companions use their pet focus damage shield again. It had been
  turned off because a bot shielding its pet stood still for good: every later spell was queued behind the shield's
  channel. Now a bot's next spell ends the channel, as a player's does, and the shield is used as a player uses it:
  only while a monster is hitting the pet (the pet tanking, as when soloing), in a fight that will last, with nothing
  hitting the caster and power to save. It is dropped two seconds after nobody hits the pet any more (and so when the
  fight ends), when the caster has to move or is hit, and for heals or a mez, and the bot carries on.
- A monster's area spell that recasts itself from each target it lands on (Lydsyg's disease in Iarnvidiur's Lair)
  could fill the server's memory against a large raid until it stopped. A monster can no longer ask for the same
  instant spell on the same target within a second, queue it twice, or hold more than 16 casts. (From the
  chris4aker/OfflineDAoC-LivingServer fork.)
- Reaver bots no longer stand in place. Starting a pulsing spell ends the others, so a Reaver's three auras (damage,
  armor and melee curse) cancelled each other every turn; a Reaver now runs one aura at a time, and ends it when it
  would hit a mesmerized monster or one not in the fight, or three seconds after nothing in the fight is in reach.
- Bots leave mesmerized and rooted monsters alone while something else is fighting them: a mesmerized monster, or one
  held by a root that damage breaks, is not chosen as a target and a bot's pet is moved off it; area spells will not
  reach one either. When every monster left is held, the bots break one and carry on. Single-target fights are as
  before.
- Bot raids no longer stall on a scripted boss that is immune outside its tether. On Albion's way to Summoner's Hall,
  Lieutenant Meade in the Hall of the Corrupt takes no damage once pulled away from his spot, and the raid fought him
  until its four hours ran out; bots now stop and lead such a boss home, as they already did for the epic bosses.
- A raid rally no longer gives up while its bots are still arriving: someone arriving with less than ten minutes left
  gives the rally ten more minutes, up to thirty minutes past its deadline. The 200 needed to start are unchanged.
- Bots spread over a zone's camps instead of crowding a few (84 level 19 to 23 bots shared four brown bear and moor
  wolf camps in Cornwall while other camps stood empty). A camp now counts as crowded when its bots outnumber half its
  live monsters (two bots always fit); bots choose among the uncrowded camps while any is left, and only then the
  least crowded ones. No monster was added or moved.
- Tank bots look after their group the way players do. Protect sits on the group's healer (or on the squishy member
  being hit), Guard covers the squishiest member a monster is hitting in melee while the tank has a shield up, and
  Intercept takes the blows for one below half health next to the tank, on its one-minute reuse. Taunting is as before.
- Siege weapons can be summoned in the battlegrounds as well as the Frontiers, and bots attack an enemy realm's keep
  doors, so battleground keeps can be taken.
- Siege results say how a siege ended. Sieges that ended any other way than captured, defended or a failed rally were
  all logged as "Ended (unconfirmed)"; they now show "Timed out", "Ended: keep no longer a target" or the reason given.
- A Necromancer bot's servant no longer stands still forever on a command that never finished. Out of combat, a
  servant command still waiting after ten seconds is dropped and the servant carries on.
- Bots can list up to three items on the Realm Exchange at a time (was one).
- Siege musters, marches, repels and the four-hour cap now run on the live clock. The first time check overflowed and
  stopped every later one from running. (Found by the stefanrows/OfflineDAoC fork; from the
  chris4aker/OfflineDAoC-LivingServer fork.)
- Long runs no longer slow down from effects that never end: an effect that ended before it started, an ending effect
  its owner had already dropped, and effects on deleted monsters or pets (a deleted Sluaghbinder pet's Cairn Boneplate
  kept pulsing) are now removed. (From the rambodolphin13/OfflineDAoC and chris4aker/OfflineDAoC-LivingServer forks.)
- The server no longer leaks memory on pathfinding (about 250 MB an hour on the fork's runs): route queries are now
  shared per zone instead of kept forever for every thread, and freed when a zone unloads. Bot logins no longer leak
  a system handle each (about 1,500 an hour). (From the chris4aker/OfflineDAoC-LivingServer fork.)
- No more lag spikes near medallion merchants ([issue #25](https://github.com/shadowofze/OfflineDAoC/issues/25)). A
  bot far from its merchant ran up to 18 failing route searches every 15 seconds; it now searches only within 2,000
  units of the merchant, walks toward it from farther away, and waits 30 seconds after a failed search. (From the
  chris4aker/OfflineDAoC-LivingServer fork.)
- Torst no longer disappears when he summons his eddies (the summon threw an error and the server removed him), and
  a bot letting go of a siege weapon no longer throws an error in the siege timer. (From the
  chris4aker/OfflineDAoC-LivingServer fork.)
- Bots no longer buy Darkness Falls seal gear with coin, and spend their seals instead. A bot shopping at the nearest
  merchant could pick a seal merchant and pay the seal price in copper (15 to 51 copper for seal gear), while the
  seals it looted sat unused in its bags. Bots now only buy with coin from merchants that sell for coin (never seal,
  Atlantean glass, aurulite or bounty point merchants). In Darkness Falls a bot that isn't grouped walks to its own
  realm's seal merchant and pays in seals for an upgrade it can afford, and wears it; while a dearer upgrade is still
  ahead it saves for it, and with nothing left to save for it buys the most valuable seal item it can and sells it or
  posts it on the Realm Exchange like any other loot. Applies to every server (normal mode).

- A dead bot no longer shows as two dead bodies. When a bot released, the server moved its body to the bind point
  before raising it: every client was told to leave the old body as a corpse, and the bot was re-created dead at the
  bind point too (seen since 0.1, in both clients). The body now goes without a trace and the raised bot appears
  standing. A dead bot also stays where it fell: nothing but its own release moves it (a companion killed outside an
  inn showed up dead next to the innkeeper; any other attempt is now refused and logged with the code that tried it).
- Bots and companions use what they learn at once. Training a companion's specs or a bot's level-up added the new
  spells to its skills but not to the spell list it casts from, so they waited until the bot was created again (a
  Hunter trained to Beastcraft 5 never used its new buffs until dismissed and summoned again). Styles and abilities
  already came at once.
- Bards, Minstrels, Skalds and Paladins hold one song or chant like a player instead of twisting through all of them:
  speed while travelling, endurance (then power, then health) in a fight, and whatever the group is short of when
  standing around. A held song lasts at least 10 seconds and is never recast while it pulses; Paladins hold endurance.
  A performer without speed or endurance yet uses another song it knows until then.
- Relics: a relic a bot had dropped could no longer be picked up by anyone, with no message. The broken hand-off is
  cleared, the relic drops properly and a bot whose bags lost it gets it back. Every refusal now says why (someone is
  already carrying it, too far away, how many of the 8 needed are at the pad), the Get key picks a relic up like a
  right-click, bots carrying a relic never stealth (they dropped it within seconds), and a /raid or group companion
  carrying one captures it at its realm's shrine.
- Keep doors no longer repair themselves while enemies are within 3,000. A broken relic temple door crossed the 15%
  line on its 30-minute repair tick and shut behind a raid fighting the guards inside (relic temples have no lord, so
  the keep counted as out of combat 5 minutes after the last door hit). Same 5% per 30 minutes otherwise.
- Legion's throw is an encounter throw like the dragons': companions stay down fighting instead of being dragged up
  to the purgatory platform, whose only way down is a zone point bots can't use; helpers already fighting near a
  player's landing spot stay in that fight.
- A companion released for good no longer makes the server retry saving it every second. Its record was deleted while
  its save stayed queued, and each failed save also held up the other bots saved with it.
- The Sluaghbinder follows the edition switch (`classes/enable_sluaghbinder`) everywhere, as in every release
  before. With it on (the "b" editions) the class is fully playable: character creation, gamebots, `/spawn`
  companions, its trainer, wisp, epic quest line and quest monsters. With it off, none of these are offered, and
  `tools/claude-version/remove_sluaghbinder_npcs.py` (run once with the server stopped) also removes its trainer and
  wisp; the script refuses to run on a world that has the class switched on. Old characters load either way.
- Solo gamebots pick up the coin and items of their own kills again. The check for who may pick up a dropped bag
  compared the owner by reference instead of by value, so a bot never counted as the owner of its own drops.
- Bots buy the best equipment upgrade they can afford. They chose the best upgrade on a merchant's list first and,
  when it cost more than they had, bought nothing, although cheaper upgrades were on the same list.
- Relic campaigns: an army out to take a relic first takes the frontier keeps whose sentinels guard that relic keep.
  Every keep a realm holds stands a 3x-health sentinel at its relic temple, and an army that marched straight in was
  wiped by them (in one test 125 of 173 deaths at a relic keep were guards, 63 of them sentinels). The relic siege
  follows once at most two sentinels are left.
- High-end raid encounters (epic named monsters, the raid boss, a dungeon's final bosses): a player in the fight gets
  one item in four straight into the bag and 25% of the coin; the bots share the rest. Other kills are unchanged.
- Gamebots are drawn at a player's height (short, average or tall) instead of a random size that could make them
  far taller than any player.
- Bot relic raids can take a relic home. Before, a relic raid could never end in a capture:
  - After the gates fall, the army goes up onto the enemy relic shrine (in four of the six relic keeps the relic sits
    about 650 units from the keep centre and 830 higher, so the bots never saw it and the pad never held the 8 it
    needs). A relic is lifted as soon as there is a way to it, not after every door of the keep is broken.
  - The healthiest raider on the pad carries it and runs home: no fighting or looting on the way unless stunned or
    cornered, and it opens its own keep's gates to reach its shrine (it used to stand outside them until the stuck
    check made it drop the relic, over and over). The escort stays with it until the capture.
  - Sieging bots with nothing else to fight hit the nearest gate by hand, as players did, instead of waiting for rams.
    Ranged bots step in to a sentinel (relic temple guards take no damage from beyond 800).
  - A relic campaign is kept, and retried, until its relic siege really starts; it ends after six hours without a
    keep taken (not six hours from the start) or three steps in a row with no progress. Raids in progress survive a
    server restart and are planned again about five minutes later; a relic being carried at a restart goes back to
    the pad it was taken from.
  - A relic that goes back to its pad by itself ends its carrier event (the leftover event blocked every new siege
    for every realm for up to four hours), only living sentinels count, every refused pickup is logged with the
    reason, and the frontier porters no longer teleport a player carrying a relic (bots already could not).
- Gamebots face the monster they are hitting. A bot that walked to its own spot (closing in, a flank, behind for a
  style) and stopped kept facing the way it walked while it swung; it now turns to its target.

**World**
- Bring A Friend reaches as far as on Dawn of Light, and only monsters in sight come. Friends came from up to 1,500
  units for a full group (450 plus 150 for each other member), through walls and from the next room; in Cardova's
  first room they came from other rooms and hallways. Now friends come from within 2/5 of the pulled monster's aggro
  range (200 units for most monsters), in a dungeon from 250 units for one attacker plus 150 for each other (up to
  1,000) with the nearest first, and only if they can see the pulled monster. How many come still depends on the size
  of the group. A monster that came as a friend, or one already in that fight, brings no friends of its own for two
  minutes, so a pull brings one round of adds instead of a chain across the room.
- Monsters with no faction bring friends again: their own kind. Bring A Friend only counted monsters of the same or an
  allied faction, and two thirds of the aggressive monsters in the game have no faction, so they never brought anyone,
  whatever the size of the group (Midgard's frore liches, windswept wraiths and chillsome wights, for example). A
  monster with no faction now brings others of the same name that have no faction either, the way wraiths brought
  wraiths; monsters with a faction work as before. The server setting baf_same_name_without_faction turns it off.
- The Huntsmen and Woodsmen of Campacorentin Forest patrol the roads in pairs again instead of standing still. Period
  sightings show a Huntsman and a Woodsman together all along the forest's main roads; each Huntsman now walks one
  stretch of road up and down with a Woodsman at his side, along the same road the horse routes take.
- Helmets that showed nothing on the head now look like a normal helm. Fifteen helmet looks have no head model in the
  game client at all, so the client drew nothing when one was worn; the commonest was the Albion cloth cap (most random
  Albion cloth caps rolled it), along with crowns, circlets, a leather cap and the Dragonsworn full helms. Every such
  helmet now uses its realm's usual helm for its armour type (Albion cloth caps the plain Albion cloth helm), random
  helmets no longer roll them, and the ones already in the game were changed.
- A door the server has no record of opens again when you use it. Such a door is opened by a stand-in that the
  server makes on the spot, but the stand-in started out already open, so opening it changed nothing and the
  door stayed shut. The server log now also names any door it doesn't know (DOOR_NOT_IN_DATABASE), so missing
  ones can be found and added.
- NPCs no longer flicker twice after every teleport. The server resent everything in sight twice after a teleport or
  region change; it now resends once, and only what it sent while your client was still loading.
- More Albion helmets that showed no face now show one: the chain coif family (11 looks, for example the alloy mail
  coif) and the leather helm family (16 looks, for example the cured siluric leather helm, with the cloth helm and
  chain helm stand-ins). Only how they are drawn changes.
- Ten Minstrel songs show their own icons. The speed songs (Motivational Chant to Motivational Anthem) carried the
  Skald's Song of Travel icons and the power songs (Song of Power to Song of Empowering) the Bard's Rhyme icons.
- Ordinary bounties no longer send you after named monsters.
- A second resurrection offer no longer cancels the first. Accepting it said "No one is currently trying to resurrect
  you" (a Druid gamebot cast ten rezzes with no result), and an offer that ran out was later taken as a decline. Now
  one offer is open at a time, and only the one you are looking at can be accepted or declined. (From the
  chris4aker/OfflineDAoC-LivingServer fork.)
- Clicking PLAY twice no longer drops you at "Entering the world": a second world entry while the first is still
  loading is ignored. (From the chris4aker/OfflineDAoC-LivingServer fork.)
- "You must wait 0 seconds" now shows the real wait, rounded up to whole seconds. (From the
  chris4aker/OfflineDAoC-LivingServer fork.)
- Quest names match whatever their case, so a quest whose name is written differently in two places still starts and
  finishes. (From the lometur/HearthDAoC fork.)
- Trials of Atlantis regions are switched off as the server settings always meant. disabled_expansions is written in
  the client's expansion numbers (Trials of Atlantis 3, Catacombs 4...), but the region check compared it with the
  Regions table's own numbers (Trials of Atlantis 2), so the Trials of Atlantis regions, and the Atlas event regions
  filed under it, stayed loaded; only Catacombs and Darkness Rising were off. Nothing reachable is lost: no zone line
  leads there and no player or bot was in them. Applies to every server (normal mode).
- Necromancers can be created again. The disabled_classes setting held the Disciple, the class a new Necromancer
  starts as, so the server refused new Necromancers (any race; Britons were the ones reported) for normal accounts,
  while GM accounts, which skip that check, could still make one. Necromancer is a Shrouded Isles class. This is a
  setting in the game database: new release databases carry the fix, and an existing install can run
  tools/claude-version/enable_necromancer_creation.py once with the server stopped (it backs up the database first).
  Applies to every server (normal mode).
- The classes and races the server settings switch off are refused on the original client too. Ranges in the
  disabled lists (Catacombs classes "58-62", races "16-21": Half Ogre, Frostalf, Shar and the Minotaurs) were never
  read as ranges at character creation or by trainers, so the original client could still make them while the new
  client already hid them. Characters that already exist are unchanged. Applies to every server (normal mode).
- Salvaging a jewellery-type (magical) item no longer turns it into a cloth item. Its type was changed for the
  salvage calculation and never changed back, so an item whose salvage was cancelled stayed cloth. Applies to every
  server (normal mode).
- 68 classic quest rewards now carry real 1.65 stats. When the classic quest rewards were added, 53 got made-up stats
  because no period page was found, and others lost bonuses the item builder couldn't read. Fixed:
  - 20 made-up rewards have a real Allakhazam page under another spelling and now use it: level, armour or damage and
    speed, and bonuses (for example Gurz, Avalon's Defender Boots, Devastation, the Viraniel's belts).
  - 23 more made-up rewards take the bonuses of a real sibling (another piece of the same set, or the same reward
    made for another class), with the item's own weapon skill (for example Kiss of Death, now Thrust +5, Parry +4;
    the Stoneheart weapons from the Troll's weapons).
  - 2 made-up belts that were stronger than any real reward of their level were brought down to the period range.
  - 23 rewards get back real 1.65 bonuses that were lost: Smiting, Soulrending, Arboreal Path, Light and Mana skills,
    and the staves' elemental, Body, Mind and Spirit focus levels.
  - Later bonus forms were turned back into the 1.65 ones (Power Pool % into Power, All Archery into the realm's bow
    skill); Catacombs class bonuses were not added.
  - Existing installs: run tools/claude-version/fix_quest_reward_stats.py once with the server stopped (it backs up
    the database and only changes rows that still hold the old values). Applies to every server (normal mode).

- Arrows and bolts have item information on a merchant, in your bags and in a trade: the damage, range and accuracy
  modifiers the game applies (for example "Range: Clout (-15%)"), the damage type, how many come in a pack and how
  many stack (the window was empty).
- The launcher no longer crashes when its server log can't be rotated (another launcher window held the file): the
  crash took the server and the game down with it.
- Monster loot tables roll as they were written. A monster with two loot-table rows read its own table once per row
  and carried the list over, so its sure drops (100%) dropped twice and every other item was rolled three times,
  while the second row's own table was never used. Each row now reads its own table once.
- Night and day monsters (ghosts, night spawns, faerie frogs...) saved while asleep no longer keep their asleep state
  (peaceful, can't be targeted, no name): about 220 of them could never be fought again.
- Tasks (/task): /task asks the targeted townsperson or guard for a task, or shows your current one (/task abort to
  give it up); the Quest Guide is /questguide. Kill tasks name a random common monster of the guard's zone (every
  kill task named the same one) and point the way from the guard; delivery tasks go to a townsperson of your own
  realm, far away when possible (it was any NPC of any realm); craft tasks no longer crash without recipes nearby;
  the 5-minute wait after refusing works; tasks stop after level 19 and a failed task still counts. A kill task is
  worth about three same-level kills; a delivery never more than a kill task.
- No error at server stop from the zone bonus handler.
- Inventory information no longer shows the "Atlas ROG" salvage marker.

**Companions and commands**
- /spawn specs opens a menu like /spawn that lists every class of your realm with its builds, such as
  [Healer (Mending)] [Healer (Augmentation)] [Healer (Pacification)]; click one to add that companion. /spawn healer
  augmentation does the same from a macro, and /spawn healer still picks a build at random. Each build is named after
  the spec line it really trains highest; melee builds are named by their weapon setup (One Handed and Shield, Two
  Handed, Celtic Dual, Hand to Hand, Left Axe).
- Companions with Stealth hide when you stealth and come out when you do, or when they are drawn into a fight. They
  follow the same rules you do (not in combat or casting, not too close to an enemy), a Minstrel stops its songs first,
  and while hidden they cast nothing but heals.
- /tc always takes you to your capital's Realm Exchange. /transfercorpse also claimed /tc, so the server reported an
  error at every start and the two could swap. /transfercorpse keeps its full name. (From the lometur/HearthDAoC
  fork; Nikse77 and chris4aker found the same clash.)
- /spawn Clerics, Druids and Healers stand and heal during a fight instead of walking back to their place in the
  formation between heals, follow you again three seconds after it, or at once if you get more than 1,500 units away,
  and no longer walk away from you to heal or buff someone. (From the Nikse77/OfflineDAoC fork, branch mod-0.37.)
- Companions' pets follow their own owner's target. When you sent your pet in, every companion's pet switched to your
  pet's target; now your pet attacking starts the fight for companions not yet in one, as a damage spell does, and a
  companion already fighting keeps its own target, and so does its pet.
- Companions and their pets leave your crowd control alone. Your mesmerize, stun or root no longer sends the group at
  that monster: they join the fight on another monster that is fighting the group, and the one you are holding is
  left alone even before the spell lands.
- Spell casters' builds are named after the class's own spell line, for example Sorcerer Telekinesis, Disorientation
  and Domination, Theurgist Abrasion, Refrigeration and Vapormancy, Cleric Rebirth, Guardian Angel and Terrible Hammer.
  The trainer's name (Matter Magic and so on) still works when typed.
- /spawn specs now offers builds for the classes that had only one. Their existing variants are listed by the line
  they train highest (Infiltrator Critical Strike, Dual Wield, Slash, Thrust; Nightshade Critical Strike, Celtic Dual,
  Blades, Piercing; Ranger Pathfinding, Recurve Bow, Celtic Dual; Berserker Left Axe or a weapon; and the same for
  Minstrel, Scout and Valewalker), and four builds were added: Death Servant Necromancer, Soulrending Reaver (flail
  and shield), Regrowth Bard and Spear Hunter. Existing builds and the bots already playing them are unchanged.
- Companions are put away while their player rides a stable route (hidden, untouchable, idle) and come back beside
  the player at the end of the route or when the player jumps off early. The check looked for a personal horse,
  which a stable route never sets, so companions ran (and died) behind the horse.
- Druids and Minstrels (solo too) send their pet into the fight while they heal or sing.
- Gamebots and companions sit down when they rest and stand up to move, cast or fight, as players do (the server now
  tells the client; only the look changes, resting itself works as before).

**The original client**
- The character creation screen no longer shows the Atlas freeshard's "Class / Race Combination:
  https://planner.atlasfreeshard.com" line across its top; that strip is now the same plain stone as the other
  pregame screens. Only the creation screen's picture in `pregame/pregame.mpk` changed; the original is kept in
  `runtime/client-opendaoc/app/backups/no-atlas-planner-text-20261030/` (tool: `source/tools/remove_atlas_planner_line.py`).

**The new client**
- A monster or NPC that dies stops attacking at once. It sometimes went on swinging for a second or two over its
  death, when a blow of its own arrived just after the killing blow; the original client never animates a dead body.
- The new client keeps a log of the spells it sees cast and land around you (spell_log.txt in its save folder, the
  last few hundred lines), to attach to a bug report when a spell's animation or effect doesn't show.
- A spell at a target you can't see (behind a wall or a hill) is refused at once with "You can't see your target
  from here!", as in the original client. It used to cast in full and only then fail with that message.
- Ghostly characters (Spiritmaster spirits, ghost NPCs) keep their hair from every angle. Part of it vanished
  depending on where you looked from, so a ghostly dwarf's braided head looked bald from the front.
- Robes hang as long robes, as in the original client. They were drawn on the vest body with sleeves and trousers
  showing under them (a troll Bonedancer's robe looked like a striped tunic). Now a robe uses the long robe body,
  with the arms and legs (and a Highlander's kilt) hidden and shorter boots and gloves, and every other armour piece
  takes the body, boot or glove shape its item names (its extension), where everything used to fall back to the
  first shape.
- Helms and hats wear their own material. The Norse Cloth Cap that many cloth casters wear showed a sheet of
  chain and scales; it is padded leather again, as are 23 other helms that had the same wrong texture (Celtic cloth,
  reinforced and scale caps, the tiaras, some guard helms).
- Snow wolves (savage winterwolves and the like) fall over when they die. They stood on in their idle pose,
  because they had been built with the animations of the wolf puppy pets, which look the same but have no death.
  Every monster now plays its own animation set, which also gives the Labyrinth's living Undead Minotaur its own
  moves (it had the dead one's), and Fomorians and Red Caps with and without weapons their own.
- A buff overridden by a stronger one of its kind (and an immunity) shows tinted red on your effects bar, as in
  the original client. Both used to look active.
- Weapons you aren't using stay on you, as in the original client: a two-hander rides on your back while you
  fight with one hand and a shield, and a one-hander hangs at your belt while you use your two-hander or bow.
  Shields, bows, staves and polearms you aren't using are put away out of sight.
- Sheathed weapons hang the way the original client hangs them: a two-hander's hilt comes up over your left shoulder
  with the blade down your back under the cloak, and one-handers hang head down at the belt. They used to hang out
  across the cloak.
- A cloak covers the spiked shoulder pieces of some chain chests (on Kobolds most of all), which came through the
  cloak's inner edges when standing still.
- A Necromancer's necroservant no longer shows up carrying a two-handed sword. A creature with no gear of its own
  could show the weapons of whatever had been in its place before (an abomination's sword after switching pets).
- Shift + right-click on a quickbar slot empties it, as in the original client. Dragging it off the bar still works.
- Helmets show on every race that can wear them: Briton women, Half Ogres, Frostalfs and Shar had no helmets in
  many sets, and some full helms (the Albion full plate helm) showed on Briton men only.
- Paladin chants look like the original client: the Refreshment chants' golden chalice appears over each healed
  group member again (it had been left out of the effect), and chant pulses no longer make the paladin wave their
  arms. The blessing halo and the Mjolnir and Gungnir effects got their missing parts back too.
- The Necromancer's abomination (and other glowing demon knights) has its glowing eyes in its helmet again, not on
  its chest armour.
- Weapon enchantment glows stay on the blade instead of trailing far behind a swinging sword.
- Names no longer peek around building walls when the character behind the wall is hidden.
- Four campfires that floated above the ground (one in Camelot Hills, among others) sit on the ground.
- The Crafted Keeper's arms no longer stretch into long thin strands when it moves. Its model file keeps its bones
  with the arms held straight out, while the body is fitted to them with the arms down; the client now fits the body
  the way the original client does. Some other monsters move more cleanly for the same reason, among them the stone
  and wolf dragons, the Dracolich, Soulkeeper, harpies, slugs, beetles, crabs, scorpions and eels.
- Avalonian, Elf and Sylvan men's feet no longer go through their cloak when they run. Their skeletons give two
  bones the same name, and the right side of the cloak followed the wrong one, so that panel never swung with the
  run. The client now picks the same bone the original client does. Dark Elves, Half Orcs, Xaphans, mammoths and some
  other monsters with the same quirk move more like the original too.
- Snowdonia Fortress's metal gates (portcullises) open when the levers are pulled. Gates that slide instead of
  swinging never moved; the other border keep gates, Caer Diogel's bars and two stone halls' portcullises open too.
- The target window's FACE, FOLLOW and STICK buttons work when playing on the server (they answered "No such
  command"), and pressing the same one again on the same target turns it off, as in the original client. (From the
  chris4aker/OfflineDAoC-SocialExpansion fork.)
- /target <name> and /assist fill the target window. The server picked the target, but the client never showed it,
  so a following /invite found no target. (From the chris4aker/OfflineDAoC-SocialExpansion fork.)
- Abilities such as Sprint, Stealth and Quickcast can be dragged onto the quickbar, as the skills sheet says.
  (From the chris4aker/OfflineDAoC-SocialExpansion fork.)
- Stealth works. Stealth is a specialization, not an ability, and the new client had no way to use it: it was
  missing from the Abilities window, could not go on the quickbar, and /stealth answered "unrecognized command"
  (issue #16). Stealth now has its own row in the Abilities window to click or drag onto the quickbar, and /stealth
  and /hide work. While hidden you, and stealthed group members you can still see, are drawn as a dark green
  ghostly form; enemies who can't detect a stealther still don't see it at all. The Stealth buff also shows its own
  icon instead of another spell's picture.
- The new client no longer freezes after a teleport. Right after /tc (or any region change, or a neighbouring zone
  loading in), it could lock up for good and show "Not Responding": the background zone loaders were touching the
  graphics card while it was drawing. They now leave those steps to the game's main thread, a little at a time, so
  nothing stutters, and region changes load a bit faster than before.
- The new client no longer gets stuck on the loading screen at login, or locks up when you spawn companions in a
  busy place. Part of the teleport fix above failed for every NPC, armour, weapon and helm model, which brought the
  same lock-up back whenever many of them loaded at once.
- The frame rate no longer drops for good after a long ride or a busy session. The models of characters, armour,
  weapons and spell effects you had passed stayed on the graphics card; the client now lets go of what nothing on
  screen uses, keeps one copy of a texture that many models share (one weapon texture was loaded once per weapon),
  and clears out sooner when the card is nearly full. Nothing is drawn at lower quality.
- No more lines along zone borders: the ground's colour map no longer bleeds the far side of a zone into its edge,
  and the corners where zones meet at the edge of a region are no longer torn open.
- Castle and keep walls block you again (Caer Ulfwych, the keeps, relic keeps, round towers, Caer Diogel and others):
  only part of their walls had collision.
- The Darkness Falls entrance in Avalon Marsh has its pillars, platform and purple flames instead of a plain white
  shape.
- Inconnu women have their hair under a circlet (and their own hair styles and hooded cloak), and Half Ogre men
  their own cloaks and hair: they had been given Briton and Highlander hair.
- Grindylows, grasping ghouls, waterlogged corpses and the other crawling legless zombies are visible again.
- Casting hands flicker as in the original client instead of holding a solid glowing orb (weapon glows were
  affected the same way).
- The fog on Avalon Isle and its neighbours is the light haze of the original client (it closed in at a few dozen
  metres); every zone now takes its region's fog, as the original does.
- Banners on far-off towers no longer seem to float in the sky.
- No more empty gaps in the combat window when songs pulse.
- Server lag no longer snaps you back (rubberbanding): when the server pulls you back to a spot you just left, you
  carry on where you are, as the original client lets you.
- You can't run while stunned, mesmerized or rooted (the server pulled you back each time), and a GM who starts the
  game hidden shows the stealth look.
- Every window an NPC opens (stable routes, Realm Exchange, emblems, quest guide, innkeeper) closes when you walk
  away, like the merchant and trainer windows.
- The X on the trainer's skill preview closes the trainer window.
- After the game is closed by force, logging in again keeps trying until the server lets the old session go (about
  a minute), instead of stopping at "already logged in".
- A double-click on PLAY enters the world once. (From the chris4aker/OfflineDAoC-LivingServer fork.)
- The merchant window has SELL and APPRAISE buttons next to BUY, as in the original client: right-click an item in
  your backpack, then SELL, or APPRAISE to hear what the merchant would pay. Dragging an item onto the window still
  sells it.
- Closing the new client while characters around you are still loading no longer crashes it on the way out.
- The new client no longer freezes for good at the loading screen or soon after logging in. It happened on most
  logins in testing: loading the spell effects, armour and models of the people around you in the background could
  take every slot the game engine keeps for small background jobs while waiting on the game itself, and the engine
  then waited on one of those slots forever. That background loading now runs on its own threads, and if the game
  ever stops responding for 8 seconds it writes what it was waiting on to its log.
- Logging in with a dyed weapon or helm no longer fills the log with "material is null" errors (and the occasional
  crash that could follow): the dyed copy of its look is now made once and shared.
- Crafting works in the new client. /craft (or a tradeskill row in the skills window) opens the tradeskill window:
  pick a trade, a product and its material tier, see what it needs and how much of it you carry, and MAKE it; the
  original's progress bar shows while you work, and MAKE again cancels. Your tradeskills and their levels are listed
  in the skills window.
- The item information window has SALVAGE and REPAIR buttons for items in your backpack, as in the original client.
- A house vault's window is titled HOUSE VAULT; only an Account Vault Keeper's page says ACCOUNT VAULT.
- Dead companion bots and gamebots can be targeted again (click the body, or the group window), so you can
  resurrect them. Monster bodies still can't be targeted.
- Right-clicking a relic picks it up in the new client (it did nothing; only targeting it and pressing Get Item
  worked). Right-clicking loot on the ground picks it up too; a gravestone is just targeted, for /pray.
- The window title says Offline DAoC Rebuilt v0.38.
- BUY, SELL and APPRAISE work even when you have targeted something else since opening a merchant (your companion,
  another NPC). The server only deals with your target and ignored the purchase without a word; the merchant whose
  window is open is now targeted again first.
- The merchant window works like the original client's. A QUANTITY row (1 to 100) buys that many packs per BUY and
  goes back to 1 afterwards unless KEEP is on. The list scrolls: a page holds up to 30 goods and only the first 20
  could be reached (the arrow merchants have more). Each row shows how many one purchase gives ("x20") for arrows,
  bolts, poisons and thrown weapons.
- Right-clicking arrows or bolts at a merchant shows what they are, as in the original client: how many, the damage
  type, the weight, and their damage, range and accuracy grades. Poisons show their pack size and level.
- A small AGGRESSIVE / DEFENSIVE button in the group window's corner switches your companions' stance in one click
  (the same as typing /aggressive or /defensive).
- A bot or companion riding a stable horse sits on it even when the server's riding message arrives just before the
  horse or the rider appears (it was dropped and the rider walked beside its horse).
- The new client logs every death of a gamebot or companion it sees (death_log.txt in its save folder: the server's
  last positions and where the body is drawn), to compare with the server's log when a body shows up in the wrong place.
- Bots on a stable master's horse stay in the saddle. They kept dropping off and teleporting along behind the horse:
  the client checked the bot's position against where it was drawing the horse, which runs a little behind its own
  updates at that speed, so every bend counted as a lost ride. The bot's position is now checked against the horse's
  own position from the server, and only a bot truly apart from its horse for 3 seconds gets off.
- Bots and companions sit down to rest, sitting until they get up to move on or fight (with the server's resting
  signal; they used to rest standing).
- A monster, bot or companion standing in a fight turns to face what it is hitting, as the original client turned
  fighters itself (with the server's own turn; bots that stepped to a flank kept facing the way they walked in).
- Loot bags, dropped gear and other things the server lays on the ground no longer float after you cross a zone border
  (most often on a stable horse). At a border everything moves with the new zone, but those objects stayed where they
  were and hung in the air or sank into the ground somewhere else.
- A Necromancer's shade (or anyone in another form, such as a Vendo) shows its spell glows on the body you see. They
  were drawn on the hidden normal body, so the hands never glowed.

**Credits**
- Thanks to chris4aker's Living Server fork
  ([chris4aker/OfflineDAoC-LivingServer](https://github.com/chris4aker/OfflineDAoC-LivingServer), formerly
  OfflineDAoC-SocialExpansion) for the fix for groups waiting on members that keep dying on the way to their meetup,
  the boss recast guard, and three new-client fixes: the FACE / FOLLOW / STICK buttons, the server's target changes
  (/target, /assist) and abilities on the quickbar.
- Thanks to the [Nikse77/OfflineDAoC](https://github.com/Nikse77/OfflineDAoC) fork (branch mod-0.37) for the /spawn
  healer fixes.
- Thanks to lometur's [HearthDAoC](https://github.com/lometur/HearthDAoC) fork (Thomas Farral) for the /tc fix and
  the quest names that match whatever their case; Nikse77 and chris4aker found the same /tc clash.
- Thanks to chris4aker's Living Server fork (release v1.1-beta) for the memory and handle leak fixes, the issue #25
  merchant lag fix, the resurrection offer fixes, the world-entry guard and the new client's one-click PLAY, and the
  Torst, wait seconds and siege weapon fixes.
- Thanks to [stefanrows/OfflineDAoC](https://github.com/stefanrows/OfflineDAoC) for finding the siege timer overflow.
- Thanks to [rambodolphin13/OfflineDAoC](https://github.com/rambodolphin13/OfflineDAoC) for the effect leak fix.

## 0.37 / 0.37b / 0.37c — 2026-10-18

The first version that comes as an update to 0.36: the overview is in [docs/RELEASE-0.37.md](docs/RELEASE-0.37.md);
every change is listed below.

**Updating and installing**
- One downloader zip per edition, with one question: **1 = Update my game** (0.36, 0.36b or 0.36c) downloads and
  applies only the update; **2 = New install** installs the complete 0.36 game and updates it.
- The update finds the edition and the new client by itself, checks that every file it replaces is as installed,
  backs up everything it replaces (and the database) in `patch-backups`, updates the world data without touching
  saves (a world row is changed only if it is still as released), and puts everything back if anything fails.
- If game files it would replace were changed, it lists them, changes nothing, and offers a fresh 0.37 in a new
  folder with your progress brought over by the progress transfer tool.
- The 0.37c downloader also offers to add the new client to a game that doesn't have it yet.
- The launcher shows VERSION 0.37, 0.37b or 0.37c; the progress transfer tool imports 0.36 / 0.36b saves too.

**Guilds**
- You can form a guild on your own. Stand near a Guild Registrar (Brit in Jordheim, Lord Christopher in Camelot,
  Filidh Fadywn in Tir na Nog) and type /gc form <guild name>; it costs 1 gold. Before, forming a guild needed a
  group of other players to accept, so on your own server it could never be done.
- Gamebots can join your guild. Target one of your realm's gamebots (or name it) and use /gc invite; it joins at
  once. Guild gamebots appear in the guild and alliance social window, show the guild name under their name to
  their own realm, wear the guild emblem on their cloak and shield, and chat in guild chat (and answer you there).
  Enemy players still see only their realm rank title under the name, but they can see the emblem.
- The emblem stays on through every gear upgrade, changes at once when the guild changes its emblem, and
  disappears when the gamebot is removed (/gc remove <name>), when the guild disbands, or when the gamebot is
  removed from the launcher's roster (which also takes it out of the guild).
- /spawn companions never join a guild (they are temporary), but while they are with you they show your guild
  name and emblem, and drop them when you leave the guild.

**Bots and raids**
- 22 weapons, shields and pieces of armour that were drawn as a bag now look like what they are: among them Luso's
  Claws (which a level 47 Savage companion carried in its left hand), Morvalt Bladed Ericril, Master Smed's Eye
  Remover, the Savage's Stoneheart Claw quest reward, Gulak's, Snaebjorn's and Silistar's Claws, the Staff of Mourning
  and the Shield of Lifeless Dreams. Each now uses the look of similar items of its kind and level.
- Automatic raid rallies give their army time to arrive. A rally used to fail if its army was not staged within 90
  minutes of opening, counting the time spent gathering at the hub town: the Tuscaran Glacier rally needed 84 minutes
  to gather at Hagall and had six left for the trip. A slow hub now sets out after 30 minutes once 120 have arrived
  (late arrivals follow on), and after leaving the hub the army always has at least 45 minutes to stage.
- Raiders on their way to a rally are no longer sent back to their capital by the stuck-bot check. During the
  Cuuldurach rally it sent over 300 of them to Tir na Nog, and only 13 reached the dragon.
- Bots that end up on a small cut-off patch of ground step back onto real ground at once, instead of failing route
  after route first. In Vindsaul Faste this happened to bots heading for the stables almost every day, and it also
  dropped members from raid parties. A walled-off ridge in the south-east of Lough Derg that bots got stuck on during
  the Cuuldurach rally is now treated the same way.
- Bots using the teleporter in Vale of Mularn no longer stand inside the bindstone next to it.
- Siege and battle announcements read properly. They named the target twice, for example "Our army has laid siege
  to the keep keep Caer Sursbrooke!"; now it is "Our army has laid siege to Caer Sursbrooke!" (and "the relic
  keep Dun Dagda" for relic keeps).
- Animist bots plant their mushrooms only during a fight, and never in a city or town. They used to plant one
  whenever they stood still between fights, so towns filled up with them.
- Siege armies attack together. The first bot of a marching army to reach a keep used to start the battle on its own
  (Caer Berkstead's siege began with one attacker there); now the battle starts once most of the army has arrived.
- Bots killed while attacking an enemy keep wait longer for a healer in their group to raise them, and if they
  release, they come back at their realm's muster point for that frontier, as their frontier teleporter would
  bring them, instead of in their capital. At Dun Dagda the Midgard relic army went from
  121 bots at the keep to none in an hour while every fallen attacker walked back from Jordheim.
- Darkness Falls raid members who cannot join their realm's route from where they stand wait half a minute before
  trying again. Over a hundred of them retried it on every turn, which made players rubberband.
- Less lag during the Darkness Falls raids. Raiders on their way to a boss with no route to their spot (Prince
  Abdin) searched for one on every turn, a search that takes up to five seconds, because the raid's spot moves a
  little whenever the raid advances. A failed search now counts for every spot near it for a minute, for all
  raiders at once.
- Less lag from bots trying to sell loot to merchants they can't reach. Bots picked Kel and Kuz, the giant merchants
  on top of the hill at Grimspound in Dartmoor, which they have no way up to. They searched for a way there every
  turn for 20 minutes or more. A bot that has tried for a minute without getting any closer to a merchant now
  chooses another one, and once two bots have given up on it, every bot skips that merchant for half an hour. Bots
  that are simply far away, such as across Camelot from the Realm Exchange, still walk there.
- Less lag at keep sieges. Pets and group members whose way was blocked by a closed keep door looked for a way
  around it twice a second. When there is no way around, they now wait a few seconds before looking again.
- Less lag overall: the server creates far less short-lived data while bots choose hunting spots, look after their
  pets and plan paths, so memory clean-up pauses come less often.
- Battlegrounds with only a few bots of a realm no longer announce keep sieges that go nowhere. A realm with too few
  bots in a battleground for an assault (Thidranki often had one or two) now fights in the open field instead of
  waiting forever for an assault group. Sieges that involve a player are still announced.
- Battleground assaults on a keep whose gates are all shut now march on the nearest gate and batter it, instead of
  trying to walk straight to the keep lord through the closed keep.
- Relic and keep sieges: the attacking army's healers now raise any fallen comrade near them, not only members
  of their own group, and the fallen wait for that raise. Before, a dead attacker came back on foot from its realm's
  frontier muster, so the army at Dun Dagda melted away even while it was winning the fight.
- Midgard and Hibernia bots can leave the battlegrounds again. Their own portal keep's door blocked the way out, so
  they could not go home to train (Albion's way out has no door).
- Warbands no longer pick meeting spots around Castle Sauvage that are cut off from the keep; bots that arrived by
  teleporter used to get stuck walking to them.
- Bots that drop into the sunken patch near the Realm Exchange in Camelot are put back on the street at once.
- Bots take a town teleporter only when it really is quicker: the walk to it is now judged by the actual route, not
  the straight line, so they no longer head for a teleporter that is only reachable by a long detour.
- Town teleporter trips fail less often. A bot that planned a teleporter trip and then joined a group, or got a new
  task, kept walking to the teleporter for its old plan; that was a quarter of the failed trips.
- Galladoria: the Earthmenders keep their wounds while raiders are in their room, and they fight back. Each one
  healed to full half a minute after the primals pulled the raid away, and because they never attacked, raiders
  busy with the primals never turned on them, so every Earthmender was given up in turn.
- Bots no longer walk into the tower wall at Vindsaul Faste. The keep's ground on the Yggdra Forest border is a narrow
  strip along a tower, and the bots' route planner took a crossing point just behind the wall, inside the sealed
  tower room, for one it could reach. Bots stood at the wall and gave up their trip, 74 times in one evening.
- Parties no longer stall at zone borders. When the leader crossed into the next zone, the other members' places in
  the formation lay over the border, where the members' route planning does not reach, and they stood still until
  the leader was far enough in. They now step over the border right behind the leader.
- Realm armies and siege parties march to keeps without stopping at every waypoint of their route. Each bot walked
  into the waypoint it had just reached, stopped there and waited for its next decision before walking on. Bots
  walking to a stable master for a horse turn for it at once in the same way.
- Group travel is smoother where a party member's place in the formation cannot be reached (behind a wall, over a
  ledge, the far side of a narrow bridge): the member now walks just behind the leader for a few seconds instead of
  standing still until its place moved somewhere it could reach.
- Town teleporter trips no longer walk away from the teleporter. A bot already on its way somewhere (to its camp)
  that decided to take a teleporter kept walking its old way, away from the teleporter, until the trip failed and
  the bot avoided that teleporter for ten minutes: 162 of the 175 failed trips in one evening. It now turns for the
  teleporter at once, and a trip the bot is led away from (by its party) is dropped without blaming the teleporter.
- Battleground stormers turn for the keep gate, and then for the lord, the moment their step changes, instead of
  first finishing the walk they were on.
- Siege armies raise their fallen during the battle. Healers of the attacking realm raise a fallen comrade near them
  once nothing has hit them for a few seconds and no enemy stands by the body, as players did from the back line.
  Before, they raised only when out of combat, which at a keep under siege never happened: the dead waited for a
  raise, then walked back from their realm's frontier, and the army at Dun Dagda melted away.
- Galladoria: raids no longer stand round a monster that a pet class is charming (a "grogan" held 230 raiders for
  four minutes), no longer chase the Watery Vortexes over the water primal's pool (out of reach, back every six
  minutes, and they wiped most of the raid), and send idle raiders onto a target that only a handful are fighting
  (the Guardian Earthmender took 20 minutes with one to seven raiders on it while dozens stood by).
- Siege crews set up rams past the wrecks of broken engines. After Dun Dagda's outer gate fell, the wrecks of the
  first rams blocked every spot in front of the Relic Gate, so no new ram could be placed.
- Raid parties that find no free spot at the raid's next stop walk there anyway instead of waiting where they were;
  and a raid boss with a very large health pool is no longer given up as "taking no damage" while a few raiders
  wear it down (Galladoria's Corrupt Primal of Fire).
- Party members keep walking with their leader instead of reaching the spot they were heading for and pausing
  until their next step: they now aim a little further ahead of a walking leader.
- Bots on their way to Dun Lamfhota no longer bounce between Emain Macha and Breifine. Coming into Breifine they
  were taken to be on a cut-off ledge and sent back to Emain Macha, then into Breifine again, over and over.
- Every party of a dragon raid can reach its spot round the lair. One of Cuuldurach's 38 staging spots lay on
  ground the route planner did not link to the lair, so the party sent there was routed out through Shannon
  Estuary and never arrived. A full check of every dragon, relic keep, Apocalypse and Olcasgean route found no
  other spot bots cannot reach.
- Caer Sidi: the raid keeps climbing to the final encounter. The way up is long, and the raid used to give up the
  climb after three minutes and fall back.
- Bots of any task can leave their realm's portal keep through its door (136 Hibernians stuck inside Hibernia's
  portal keep in Odin's Gate kept giving up their routes).
- Bots crossing a zone border find a way where all the best-looking crossings lead into dead ends, such as sealed
  tower rooms at Vindsaul Faste coming from Yggdra Forest. Routes that already worked are unchanged (checked on
  360 recorded bot trips).
- Less memory churn on a full server: a once-a-minute database check of every retired bot's commands now looks at
  each one once, and pet classes choose their pet buff and heal without building lists every turn.
- Galladoria: the Organic-Energy Mechanism no longer breaks spell casting on the server. It casts its damage shield
  each time it is hit, and with a raid hitting it at once these casts overran the spell queue (an error on every hit,
  42 in one fight), so its fight never played out. Every scripted monster that casts when struck is fixed with it.
- Caer Sidi: raids no longer get stuck clearing Lich Lord Sanguis's blood mages after he is dead. They kept
  appearing, and the raid fought them for 40 minutes instead of moving on to Apocalypse.
- Caer Sidi: the Skeletal Sacristan stops walking while he is being hit and walks on 10 seconds after the last
  blow. A raid of 300 chased him round his room for 40 minutes without catching him.
- Battleground assaults go for the keep lord, in through a broken main gate. In all four battlegrounds the gates
  fell and the guards died for hours while every lord stayed untouched: the attackers waited in the courtyard, and
  the ones heading upstairs to the lord stopped at a small side door that can't be broken.
- Groups on their way to a hunting ground no longer freeze in front of a monster on the road. Only the group's
  leader watched the road ahead and only its puller was allowed to start a fight, so when the two were different
  bots the whole group stood still, sometimes for an hour and a half, until its task ran out. The puller now takes
  the monster on, and a group that can't get ready to fight it within about a minute goes around it or walks on.
- Bot groups travel as one pack, the way your companions follow you. Each member used to walk to a spot just
  ahead of its leader, stop there and wait a few seconds for its next decision, so a travelling group moved in
  stops and starts. Members now keep their place around the leader continuously while it walks, save their
  ordinary buffs for when the group stops, and carry on as soon as a cast ends.
- Less lag: fewer route checks for bots waiting to board a horse in town, and less memory churn from pets and bots
  checking their abilities and skill timers.

**Quests**
- Bounties count every monster with the bounty's name. With a svartalf foister bounty, any svartalf foister you
  kill counts, whatever its level and wherever it is. Before, Hard and Very Hard bounties ignored ones more than a
  level below the target, and only ones in the bounty's own lands or dungeon counted. How bounties are picked, how
  many kills they need and their rewards are unchanged.
- Every classic quest can now be started by talking to its giver. In 20 of them one line of the giver's
  conversation was missing, so the giver showed a quest marker but nothing you could click led to the quest (Aegan
  in Mularn only said "You have learned much since I spoke to you last."). The missing lines are back, word for
  word from the period quest walkthroughs. The broken quests were:
  - Evenings Empty Blessing: Aegan, Mularn (Midgard, levels 15 to 50)
  - Lover's Circle: Sugnhild, Huginfell (Midgard, levels 15 to 50)
  - Better Off Buried: Gudlor, Huginfell (Midgard, levels 27 to 50)
  - Uncle Thulin: Achae Smith, Fort Gwyntell (Albion, levels 25 to 50)
  - Cleric Mulgrut: Hugh Gallen, Prydwen Keep (Albion, levels 5 to 50). He also names Mulgrut's son, Durren, again;
    he used to say your name instead.
  - Tale of Druids: Vadri Pade, the Magic Shoppe in Camelot (Albion, levels 10 to 50). This quest has a copy for
    every Albion class, 15 in all, and every one was broken.
- Quest markers update when you level up. A quest giver whose quest opens at your new level shows its marker
  straight away; it used to show nothing until you logged out and back in (Troya in Howth at level 2, for example).
- Fjall (the Healer and Shaman epic step "Search out Fjall and his fellows") is back in the fenrir fort in Yggdra
  Forest, in the first hut on the right as you come in through the gate, as the period guides describe. He stood for
  good, alone, outside Vindsaul Faste among the gate guards. Now four fenrir friends keep him company, and every so
  often the group runs south of the lake to the Vindsaul gate and back. The quest's map marker points to the hut.
- Guards fight a quest monster that wanders far from its home, as the Vindsaul gate guards fought Fjall and his
  friends. Quest monsters that live among guards, such as the thieves inside Camelot, are still left to you.

**The new client (0.37c)**
- The Magic tab lists each specialization's spells by level, lowest first, as the original client does. They were
  grouped by specialization but mixed by spell type.
- Stable rides seat you on your mount. On horses you jittered beside and behind the horse, and on flying routes
  (gryphons, amphiteres) the mount jumped between positions and turned wildly during the flight.
- The Cabalist's simulacrums look as they do in the original client: smooth, marble-tinted stone with their belt,
  face and muscles. They were drawn grainy, with their detail texture stretched and repeated over the body.
- Ghostly NPCs (Spiritmaster pets, frost spectres, the spectral snake, forest skrats) no longer flicker between dark
  and bright white, even when several of them move about together, and they keep their colours under the ghostly look.
- Half Ogre, Frostalf and Shar NPCs (Elizabeth the Smith in Mularn and every other NPC of those races) are drawn as
  their races in their outfits, like every other race. They were one old model with a single skin over every part.
- The release window when you die has the original client's four choices: release (to your bind point), release to
  the city, to your house, and to your bind, with the countdown bar. There was a single release button.
- Monsters stand, fly and hover at their proper height. Low-flying giant cicadas, skadedjur and terror birds were
  sunk halfway into the ground, hovering monsters such as cold lights sat on the ground, and a flying monster's body
  stayed in the air when it died; it now falls to the ground.
- The training window (at a trainer or with /train) previews what each specialization teaches, as the original
  client does: click a specialization and a panel beside the window lists its spells, styles and abilities with
  their icons and the level each comes at, the ones you know dimmed and the next level's in gold.
- Combat styles leave their colored trail behind the weapon the way the original client draws it: one smooth streak
  that follows the swing as it happens. It was drawn late and choppy, as a few separate copies of the weapon.
- Delling Crater's entrance to Tuscaran Glacier works again: walking into it zones you in. The new client was missing
  that zone-in spot.
- Tuscaran Glacier's ice is far less shiny, closer to the original client's: its reflection no longer covers the
  blue ice in a white glare.
- Ground-target spells work: an Animist's turrets no longer fail with "Your ground target is not in view." The new
  client kept telling the server your ground target was out of sight.
- The group window's DISBAND removes the member you have targeted (clicked in the world, on their name in the group
  window, or with the group-target keys) instead of breaking up the whole group; with no group member targeted you
  leave the group, as before.
- Binding a key in the options keeps the key list where it was (it jumped back to the top) and shows which action
  the key was taken from.
- The UI SCALE slider resizes the interface when you let go of it; it resized under the mouse while dragging.
- Click and drag on the character screens turns the character again (right drag zooms on the customize screen).
- Combat style trails no longer flood the game with errors when the weapon disappears mid-swing (a companion
  vanishing, a weapon swap): one two-minute fight logged over 12,000 errors, which also cost frame rate.
- Reflective surfaces no longer show white streaks fanning out from one point and swinging with the camera (seen on
  Tuscaran Glacier's floor): the reflection is now worked out per corner of each triangle, as the original client did.
- Flying monsters (Dyranapur, Lydsyg, the giant cicadas) have their names and click areas up with their flying
  bodies; their names were drawn on the floor beneath them.
- Found while testing (server, not changed): Dyranapur and Lydsyg in Iarnvidiur's Lair heal to full and walk back to
  their spawn every 1.5 seconds whenever no one they will attack is near them, so a group of companions alone can't
  bring them down. A fight against Dyranapur with a full companion group ran smoothly in the new client (no slowdown,
  steady memory); the reported crash there wasn't reproduced, because the boss never cast its Black Plague.
  A two-minute simulation of that fight (Black Plague landing on a full group of eight every second, plus the
  boss's Lifedrain) didn't crash or slow down the new client either; memory levelled off. It did show a few
  single-frame hitches of up to two seconds whose cause isn't known yet.
- The small group window's names line up with the power gems and health bars; they sat a little higher.
- Fewer hitches in busy fights: a spell effect's files now load in the background, starting as soon as the cast
  begins, instead of freezing the game for up to a second and a half the first time each effect appeared (most often
  with companions or gamebots casting). An effect that isn't ready yet shows a moment later.
- A character that dies mid-cast plays its death instead of standing frozen in its cast until its body sinks (seen
  with a Bonedancer companion's boneknitters after /disband); its glowing hands go out at once.
- Invisible gamebots and pets: the new client now asks the server for any character it hears about but was never
  sent, as the original client does, so they appear. The server can mark a character as already shown to you without
  ever sending it, and the new client never asked for it, so those characters fought and moved with no body or name.
- F12 writes a report of nearby characters that aren't drawn and why (bot_report.log in the game's save folder), to
  help track down any that still go missing. The key can be changed under Options > CONTROLS > Misc.
- Riding a stable master's horse: your horse gallops (its hoof sounds were missing), and it no longer sinks into the
  ground going over hills (seen from Huginfell to Raumarik).
- Monsters that borrow another creature's animations no longer float while fighting or swimming: their animations
  keep the heights from the original client's files (428 monster models redone). The windswept wraith, mummy hags,
  liches and gray spectres crouch on the ground in combat, as the original client shows them.
- Options > AUDIO has separate SOUND EFFECTS (spells, melee, monster and character noises, footsteps, doors) and
  AMBIENCE (the zone's own background sounds) volume sliders; they were one slider.
- Pets and monsters (dread commanders, boneguards ...) put their weapons away as soon as the server says their fight
  is over, as in the original client, instead of holding them out for several more seconds.
- Gamebots and companions swim at the surface with you; they were drawn well under the water with only their names
  on top.
- Savages' claw and fang greaves sit on the forearms the way the original client draws them: the right-hand one no
  longer floats off the hand, the off-hand one faces the right way, and a Savage with a greave in each hand shows
  both (the left one was dropped).
- Monsters, horses and pets no longer turn solid black (and fire smoke pink) partway through a session. Loading the
  next zone in the background read a ground texture back from the graphics card in the middle of a frame, which
  broke drawing for every texture loaded after it; that value is now read from the texture file instead.
- Clicking something on the ground (your gravestone, loot, a relic) targets it, as in the original client, instead of
  trying to pick it up straight away. Your own grave can be targeted for /pray again; it used to answer "too far away
  to pick up" or "You can't get that!". Pick up what you have targeted with the Get Item key (G), and target the
  nearest loot within range with the Nearest Loot key (F7; graves are skipped). Both can be changed under Options >
  CONTROLS.
- Fist weapons stab forward: fang greaves, moon claws, moon fangs and the slash fist weapons used to stick out
  sideways or upward from the fist; they now point along the arm in both hands. The Pict fist weapons stood straight
  up and are fixed too.
- Gamebots' and companions' weapons show their dyes and weapon glows; they were always drawn plain.
- Guilds (with the server's gamebot guilds):
  - A guild window (Shift+G, can be changed under Options > CONTROLS): your guild's and alliance's members with
    level, class, rank, group, zone or last seen and note, sortable, ten to a page, online only or everyone, with
    Send, Invite, Promote, Demote, Remove, Guild Info, MOTD and Leave Guild. Gamebot members are listed too.
  - Guild emblems are drawn on cloaks and shields the way the original client draws them (its colours, patterns and
    logos): on your own gear, other players', gamebots' and companions', and on the character select screen.
  - The emblem picker (the emblemer, /gc emblem) has the original window with a turning shield to preview the
    emblem and every logo to choose from.

## 0.36 / 0.36b / 0.36c — 2026-10-16

Everything added since 0.35, in one complete download, and a new game client. The overview is in
[docs/RELEASE-0.36.md](docs/RELEASE-0.36.md); every change is listed below.

**Download and setup**
- Three editions of the same game on the same server:
  - **0.36b** includes the Sluaghbinder.
  - **0.36** has the classic class list only.
  - **0.36c** is 0.36b or 0.36 with a second, new client added (see below).
- One small downloader zip per edition: unzip it and double-click the `.cmd` inside. It fetches the game in
  checked parts, as in 0.35, and continues where it stopped if you run it again.
- Every install gets its own account on the first ENTER REALM, an empty world for its own bots, default launcher
  settings and a client settings profile that is new for 0.36. Every keep, relic and battleground keep starts with
  its original owner and level, and every keep door whole.
- The progress transfer tool now also imports **0.35 and 0.35b** saves, as well as every earlier version.
  See [docs/TRANSFER-PROGRESS.md](docs/TRANSFER-PROGRESS.md).
- Every database and client change listed below is already in the download.

**The new client (0.36c)**
- A second game client for the same game, built in Godot 4. The zones, buildings, characters, monsters, spell
  effects, sounds, music and interface are converted from the original client's own files, with textures upscaled
  4x. It has the 1.65-era interface (quickbars, map and war map, quest journal, group and raid windows, pet window,
  trainer, merchants, the Realm Exchange), WoW-style movement, rebindable keys and graphics presets.
- It shares everything with the original client: the same server, account, characters, bots and world. A 0.36c
  folder has both: **START OFFLINE DAOC.cmd** plays with the original client, **START OFFLINE DAOC - NEW
  CLIENT.cmd** with the new one. Use one launcher at a time.
- **DOWNLOAD-AND-PLAY-v0.36c.cmd** asks for 0.36b or 0.36, installs that complete game and then the new client in
  one go. **ADD-NEW-CLIENT-TO-0.36-OR-0.36b.cmd** adds the new client to an existing 0.36b or 0.36 folder without
  changing its saves.
- On a 0.36 game (classic classes only) the new client's character creation offers the classic classes only: the
  launcher tells it the game's class setting (`--custom-class=off`).
- The new client's download comes in independent, checked parts: each is unpacked and deleted before the next, so
  the install never needs twice its size on disk.
- It needs a graphics card that supports Vulkan or DirectX 12. It does not need the .NET Framework 3.5 feature.
- Its source code is in [source/new-client](source/new-client); see [docs/NEW-CLIENT.md](docs/NEW-CLIENT.md).

**Bots and raids**
- Forced Darkness Falls raids start again for all three realms. A quest monster added to Darkness Falls with the
  classic quests (Archivist Borath) made the bots' Darkness Falls route check fail, so every entrance was refused.
  Quest monsters no longer count in that check. If a forced dungeon raid cannot find a route, it now says which
  entrance failed and why, before any bots are reserved.
- /raid companions attack the enemy keep door when no defenders are around, instead of standing at it. Before, only
  the ram crew's leader worked the door; now the other squads join in, and the bodyguard squad helps when you hit
  the door yourself.
- Bots find their way to hunting spots that can only be reached the long way around. The server's path search gave
  up when the way to a far point first leads away from it, so bots dropped the camp ("no safe connected zone seam")
  or stopped at a dead end. Over 1,600 Hibernian trips failed in one night, most of them around Sheeroe Hills, whose
  main ground joins the rest of Hibernia only through Bog of Cullen. The server now keeps a map of short, checked
  paths through each outdoor zone (navmesh\roads.json) and uses it when the direct search fails, and it plans
  zone-to-zone trips over ground that actually connects.
- Raids no longer stall at a cramped encounter. When only a spot or two near the target passed the checks
  (Tuscaran Glacier's griffon ledge had room for 1 of 38 parties), the other parties stayed where they were, the
  target was skipped untouched, and the raid then waited for "formed parties" at that spot forever: a Tuscaran
  Glacier raid sat there for eight hours. Parties now share a checked spot when there is no free one, and when no
  raider reaches the raid's front for three minutes the front moves to where the raid actually stands.
- Bots reach raid, dragon and keep-siege rally points they used to give up on: points that can only be reached
  the long way around (now found through the new path map), and rally points checked while the bot was swimming
  (the check looked for ground at the water's surface and found none). This stopped attackers of relic and keep
  sieges from ever arriving.
- The Hibernian dragon raid takes a much shorter way to Cuuldurach (about a third less walking). It no longer
  marches south through Bog of Cullen and turns back: it crosses into Sheeroe Hills at the only place the Bog's main
  ground joins it. In Sheeroe Hills it follows the southern valley to the lair instead of cutting through the middle
  of the zone. The bots' map of short paths through each zone now knows how far each piece really is to walk (two
  points across a ravine looked close but are a long walk apart), so long walks in other zones are shorter too.
  Every path still follows the walking map; no shortcuts over ground it does not allow.
- Darkness Falls raids can fight Princess Nahemah. She stood on a dais that the bots' walking map does not join
  to the rest of the floor, so raids could neither reach nor see her and skipped her untouched. She now stands
  on the floor in front of her dais.
- High Lord Saeor and High Lord Oro in Darkness Falls also stood on daises the walking map does not join to the
  floor, just out of melee reach from it. Raids skipped Oro untouched and took 40 minutes on Saeor. Both now stand
  on the floor in front of their daises.
- Raids gather before the last boss. A Darkness Falls raid reached Legion with only eight raiders at hand and the
  rest were pulled in a few at a time as they arrived, so it wiped and Legion healed back to full. The raid now
  waits out of Legion's reach (and an epic dungeon's last boss's) until half of its living raiders are there, then
  goes in together; after a wipe it gathers again.
- Keep and relic sieges start when the army arrives together. The army's leader reached the keep alone and the
  siege began with one attacker there while the rest were still far down the road, and the siege was soon given up
  as defended. The leader now waits at the siege camp, at most five minutes, until most of the army has caught up.
- Bots stuck on a small loose piece of the walking map beside the real floor (by the Aegir's Landing teleporter)
  step onto the floor instead of failing every route; this also happens when they log in there.
- Bots use town teleporters more reliably. When the side of the teleporter a bot comes from cannot be walked to,
  it tries the other sides, and remembers the spot that worked. A bot planning a long walk to a teleporter no longer
  gives up after five minutes while still on its way. After teleporting, a bot no longer heads for the horse it
  picked before the jump on the other side of the realm.
- Sieges no longer start with one attacker at the keep. A bot that walked in on its own could open the siege while
  the army was still far down the road, and the siege was soon given up as defended. A siege now opens when the
  army's leader arrives with most of the army, or when most of the army that marched stands at the keep.
- An army no longer waits forever behind a leader that is busy elsewhere (the Castle Excalibur relic army waited
  45 minutes at Hadrian's Wall while its leader's group sat at a meeting point). If the leader stops leading for
  30 seconds, the next marcher takes over.
- Level 50 casters and pet classes that kill from one spot are no longer sent back to their capital as "stuck"
  every fifteen minutes. A kill counted as progress only when it gave experience, which level 50s no longer get.
- Bots leave their pets behind at Prince Ba'alorien in Darkness Falls. He kills every pet near him and heals by its
  health, so raids full of pet classes could never wound him. Pet classes now release their pets and do not summon
  near him; he is unchanged.
- A boss left outside its leash range with nobody fighting it now walks home and resets, as it should. Aidon the
  Archwizard in Hall of the Corrupt stayed out there, immune, for over an hour because raiders stood near him, and
  the raid to Summoner's Hall could not get past.
- Bots that log in inside Summoner's Hall (after a raid ended there) walk out through the hall's exit. They found no
  way out and stood still until they were sent back to their capital fifteen minutes later.
- Relic sieges give the army two hours to reach the relic keep. The march into the enemy realm took longer than the
  one hour it had, and the rally failed while the army was still on the road.
- Battleground armies attack the central keep together. Each realm gathers on its side of the keep and goes in once
  most of its bots are there; when the attack is spent it gathers again. Before, half of the bots fought in the open
  field and only a few ever reached the keep at the same time, so gates fell and were repaired for hours and no
  keep lord was ever attacked.
- Bot raids pull the last boss of Summoner's Hall, Darkness Falls and the epic dungeons together (see above); this
  took the Midgard raid through Grand Summoner Govannon on the first try.
- Bots no longer give up on a town teleporter while walking to it along a winding path, and the server no longer
  freezes for several seconds to take a memory snapshot (it now does so only when asked).
- Fewer server-wide slowdowns during raids: when no free spot near a boss could be found for a party, the search ran
  again on every turn; it now waits half a minute.
- A boss that stays outside its leash range for a minute and a half walks home and resets even while raiders are
  fighting it. Princess Nahemah in Darkness Falls fought raiders at the edge of her leash for over twenty minutes,
  immune the whole time, while the raid tried to draw her back.
- An army's march leader who is fighting on the way keeps leading. Armies that had to fight their way to a keep
  lost their leader every minute and stopped to pick a new one.
- The bosses of the epic dungeons, Summoner's Hall and Darkness Falls no longer heal to full in the middle of a fight.
  They reset whenever their list of attackers was empty for a moment, which happens in a large raid while the boss is
  still being hit: Hurionthex in Galladoria went from 31% back to full again and again. They now reset once nobody
  has fought them for a few seconds, as when a raid wipes or leaves.
- Soul Reckoner in Caer Sidi can be killed. He takes no damage while any of his souls lives, and the first souls of
  every spawn were placed outside the world, where they never appeared and could never be killed, so he stayed immune
  for good. This affected players too.
- Darkness Falls raids no longer give up on a prince they are still walking to. A prince reached along the long
  waypoint route inherited the previous prince's waiting time, and Prince Ba'alorien was skipped at full health just
  as the raid arrived. A raid that is still closing in on its target is never counted as stalled.
- A siege started by hand (or a relic siege) takes over its realm's army when the realm is still gathering for an
  automatic siege. A forced relic siege of Dun Dagda got no attackers at all because Midgard's army was mustering for
  Dun Crauchon.
- Battleground armies no longer wait to gather for an assault while the central keep's gates are already down. The
  gates of Dun Abermenai stood broken for an hour at a time while each realm kept gathering, and its lord was never
  attacked. Bots took a battleground keep (Dun Abermenai, Albion) for the first time on the next run.
- While a bot raid is inside Darkness Falls, Summoner's Hall, Caer Sidi or Galladoria, the ordinary monsters there
  take ten minutes to come back. Darkness Falls monsters returned every two to three minutes, so the raid fought the
  same corridors for forty minutes and never reached Prince Ba'alorien. Bosses keep their own timers, and nothing
  changes when no raid is inside.
- A realm's army marching to a siege no longer waits for its groups to meet up first. The army for a relic siege of
  Dun Dagda spent ninety minutes with its groups gathering at their own meeting points, some still at home.
- Relic sieges gather their army longer before marching (25 minutes, extended up to twice while the army is short).
  The Dun Dagda army set out with a third of its strength, arrived with a dozen and was destroyed.
- Darkness Falls raids gather in front of Legion instead of at a spot on the other side of the dungeon. The meeting
  point for a far-away final boss was placed wherever the route search gave up, and no raider ever reached it.
- Raiders who cannot find their way inside the raid's dungeon rejoin the raid instead of being sent home. Raiders
  arriving at the Darkness Falls entrance were sent back to their capital, rode back, and failed again, for hours.
- Prince Ba'alorien and Prince Asmoien in Darkness Falls devour one pet every five seconds, the nearest, instead of
  every pet near them at once, and bots keep their pets away from Asmoien too. Each pet still heals them as before.
- Soul Reckoner in Caer Sidi takes a quarter of the damage while his souls live, instead of none, and heals less
  when he kills someone. His souls return every minute during the fight, so a raid could clear them and still never
  hurt him.
- With these changes bot raids finished Darkness Falls (Legion defeated by Midgard) and killed Prince Ba'alorien
  and Soul Reckoner for the first time.
- Bot status saves no longer pile up duplicate entries in their save queue (it grew by about 40,000 an hour).
- Hurionthex in Galladoria no longer heals to full each time he changes form. Every form change restored all his
  health, so raids that had him at 13% saw him at 96% moments later. This affected players too.
- Epic dungeon raids go for the named encounters and the final boss after their first hour, fighting other monsters
  only when attacked, and do not chase a boss they already killed when it returns. Caer Sidi's raid kept clearing
  returning monsters for hours and never reached Apocalypse.
- Armies marching on a keep or relic keep go around the enemy keeps and towers on the way, and keep well away from
  the keep where the defenders gather. The leader also waits longer at the target for the rest of the army before
  the attack begins. The army for Dun Dagda lost 146 bots on the march and arrived with 11.
- Bots walking a long way to a town teleporter no longer give up while they are still getting closer.
- Fewer server-wide hitches during Darkness Falls raids. Every raider whose post was not in direct reach re-ran the
  same large route search on every AI turn (about a second each); the answer is now shared and remembered.
- A marching army that has not moved for four minutes pushes on for two: the leader stops fighting and walks, and the
  others only fight back when attacked. Marchers also stop joining fights against enemies they cannot reach. Midgard's
  army for Dun Dagda fought for over 40 minutes on one ridge in Emain Macha, mostly against Hibernians far below it.
- Bot rams and trebuchets attack a keep's doors in the right order at keeps with ten or more doors. Doors 10 and 11 of
  a relic keep were read as doors 0 and 1, so the rams went for an inner door beside the keep while the gate stood and
  never found a place to set up.
- A raid started from the launcher ends that realm's automatic raids that have not started yet, so they no longer
  take the bots it needs. An automatic Summoner's Hall rally held 141 of the 200 bots a forced Caer Sidi raid needed.
- Raiders who die inside an epic dungeon release at the raid's rally town (for example Fort Gwyntell for Caer Sidi)
  instead of their capital, and come straight back. Caer Sidi's raid was down to 67 of 300 by the time it reached
  Bane of Hope.
- Bots take Caer Sidi's lift between floors in one step. They used to stop halfway up and hang between the floors,
  cut off from the raid.
- Epic dungeon raids fight the helpers a boss fight spawns (healers, summoned adds) before the boss itself.
- Olcasgean's fight in Galladoria now starts when the bot raid reaches the waterfall bridge. The hidden script that
  starts it (and other hidden encounter scripts like it) only ran while someone stood close to it, and it sits far
  from the bridge, so the raid waited there for an hour and the fight never began. A bot raid anywhere on the bridge
  starts it; players still start it from the usual spot.
- Galladoria's Earthmenders heal only the Corrupt Primal of Earth, not each other. Healing each other, none of the
  four could ever be brought down.
- Soul Reckoner in Caer Sidi can be hurt anywhere in his hall and just outside it. Pulled a short way out of the room,
  he took no damage at all.
- With these changes bot raids killed Hurionthex in Galladoria for the first time (three times since), started the
  Olcasgean fight for the first time, finished Tuscaran Glacier twice more, and Midgard's relic army reached Dun Dagda
  and brought its gate down to 3%.
- Relic sieges gather their army for longer (up to 85 minutes) while it is short of 120. An army of 98 reached Dun
  Dagda with about 60, and the relic guards and defenders killed them in four minutes; an army of 135 had brought the
  gate down to 3%.
- Caer Sidi's horsemen (Fames, Morbus, Funus) no longer heal to full the moment they are pulled past the edge of
  their area. They give up the fight and walk back, keeping the damage they have taken. This affected players too.
- Apocalypse fights a bot raid properly after his flights to the ceiling. He only picked up players again afterwards,
  so against bots he found nobody to fight and reset to full health.
- Bot raids shoot down flying monsters within reach of their spells and bows. Galladoria's Corrupt Primal of Air
  circles high above the floor and never fights back, so bots left it alone, and Olcasgean only wakes when all four
  primals are dead.
- One bot stuck on a bad spot no longer freezes the server. A bot whose route kept failing in the same place was moved
  a short way off, walked back and failed again, every few seconds, and each try ran a search that froze the whole
  game for 2 to 9 seconds: every bot slowed to a crawl and players were rubberbanded. Now a third failure in the same
  spot sends the bot somewhere safe, and the search near the Albion and Hibernia starter dungeons is capped and
  remembers its answer.

**Server**
- Fixed a slow memory leak that made long sessions stutter. Every time the server saved something (a bot, an item, a
  character) it kept the previous save as well, and the one before that, without end. With 6,000 bots the memory in
  use grew by 250-500 MB an hour, and after many hours the server froze for 2-3 seconds at a time.
- Dead pets no longer stay in memory. A monster that had fought a pet kept it on its list of enemies until it fought
  again, so idle monsters held thousands of dead pets (9,111 Theurgist earth pets in one test). A pet now leaves those
  lists when it is gone.
- NPCs no longer stay invisible after a teleport. Sometimes, after teleporting with companions, every NPC in a
  large area around you was invisible until you walked away and came back. The server sends each NPC to the game
  once, and the game could miss them while it was still loading after the jump. The server now sends everything
  around you again shortly after each teleport, as the GM command /jump refresh does.
- The server logs now record its memory use every five minutes and every garbage-collection pause of a quarter
  second or more, and save a heap snapshot when the memory actually in use grows large, to track down the long
  freezes in long sessions. (The snapshot used to trigger on the total heap, which is mostly freed memory waiting to
  be reclaimed; taking it froze the server for about nine seconds for nothing.)
  Create an empty file named gc-diagnostics.off in runtime\server to turn the detailed sampling and snapshots off.

**Classes**
- Necromancer bots have their servant use its area debuffs (Shroud of Affliction, Vortex of Anguish and their ranks)
  on a single enemy too; they were almost never cast. Billowing Death and its ranks, which burst around the servant,
  are only commanded once the servant stands by the target, instead of from far away where they hit nothing.
- Necromancer and Bonedancer pets look as they did in 1.65, using models from the original 2003 game client instead
  of later ones. Necromancer servants: zombies, a Briton undead, a Roman legionnaire and a blue wight, with nothing
  in their hands; only the abomination (a glowing demon knight) carries a weapon, its red-glowing two-handed sword.
  Bonedancer commanders: the classic bone dancer commander with one plain Norse weapon and no shield or helm.
  Minions: grey skeletons with Norse weapons (hammers for guardians, axes for the fighters, a bow and short sword
  for archers, a staff for casters), no bucklers. Only the look changes; pets fight as before, apart from the melee
  minions now swinging axes and the debuffers a short sword.
- The Sluaghbinder's dullahan and zombie defender now swim when they are in water, instead of standing still (0.36b).
- The Sluaghbinder has realm abilities (0.36b): the usual list of a hybrid melee and pet class (stat increases,
  Toughness, Purge, Serenity, Wild Arcana, Determination, First Aid, Ignore Pain, Hail of Blows, the Masteries of
  Arms, Pain and Blocking, the three Rains and Wild Minion), and its own: Crippling Plague. It is instant, with a
  one-minute reuse: the plague slows an enemy a little at first and more every second until, after 10 seconds, it
  can hardly move; then it bursts and poisons everyone close by. Curing it in time stops the burst. Its look is
  borrowed from the Sluaghbinder's plague spells for now.

**Quests**
- Tainted Magic (Albion) can be finished: its first step, getting the ales, never completed. A search of a
  player's whole inventory always came back empty, so the server never saw the ale it had just handed over. The
  same fault made some quests hand the same letter or quest item over again every minute.
- Commencement (Master Torr) is offered to every class once you have finished his Immediate Resolution. Only three
  classes could get it before.
- Named monsters that quests send you to kill come back within five minutes (some took up to 100 minutes, such as
  Throatripper, Sir Gerenth, Fallen One, Stripe and Zrit-Zrit).
- You can take an enemy relic with your companion bots: they count toward the attackers needed on the relic pad,
  and eight are needed now (was sixteen), so you and seven companions are enough.
- Classic quests now give their item rewards. 940 reward items that were missing are in, so every one of the
  classic quests (1.65 and classic Shrouded Isles) hands out the items it names, including the "choose your
  reward" endings. Stats come from the item pages as they were in 2002-2004 (573 items), a 1.65-era server
  (1 item), or today's Allakhazam pages with later bonus kinds removed and the total kept to period strength
  (308 items). No page records what an item looks like, so each borrows the look of a similar item of the same
  slot and type.
- 53 rewards have no item page anywhere. Their stats are made up to fit the item and its quest, and each has a
  short description.
- Rewards made for each class now go to each class: Viraniel's belts, Banba's cloaks, the Stoneheart weapons, and
  the Ley Infused and Trustan's belts give you the version made for your class.
- 13 classic quests can be finished again. Their "bring N items to an NPC" steps were set up as fights against that
  NPC, and nothing dropped the items, so they stopped there: Fortune of Few, Academy Research, Traveler's Way --
  Church Aid, Lost Townfolk of Bran Llyr, In the Interest of Plants, Silver Run, Sylvan Shadows, Secret of Nuada's
  Silver A, Traveler's Way (Midgard), Feathers, Hill Cat Hunt, Hill Cat Hunt -- Savage Version and New Crest. You
  now hunt the monster the walkthrough names (it drops the item while you are on that step), and the map marker
  points to its camp, then hand the items in.
- Fortune of Few: buy the five bronze war mattocks from a weapon merchant (Dougal Heathis in Camelot, or Alburn
  Hale beside Master Graent) and Master Graent pays you their cost, as in the original quest.
- Quests with search steps no longer lose their search areas at random when the server starts.
- Quests give much more experience, especially between levels 20 and 40. Quest experience is now a share of your
  current level, as it was on the live servers in 2002-2004, set at the high end of what players recorded then: a
  typical quest pays close to a bubble (about 8-10% of a level), wherever you are within its level range. Quests
  between 21 and 40 used to pay only 1-3% of their lowest level; they now pay 3 to 6 times more, and more again if
  you do them above their lowest level. Low-level quests were already close and change little. No quest pays less.
- Quest NPCs hold a conversation instead of saying everything at once. In 55 classic quests an NPC said every stage
  of its recorded conversation in one box; Merle the Old in Information is the Key said all four mud hand-ins, the
  choice of topics and the answer at once. Each NPC now says its lines in the order the period walkthroughs recorded
  them: a [keyword]'s answer when you click it, the hand-in lines when you hand in, "Did you get me the mud?" when you
  come back, and a longer speech one part each time you talk to the NPC again. No line was added or removed.
- The epic quests answer whispers and give their rewards again (many 0.35 reports). The quest scripts looked for
  their NPC at one exact spot; the classic quest NPCs added in 0.35 stand a few steps away (Brigit in Tir na Nog,
  Danica and Elizabeth in Mularn), so each script made an invisible-to-you twin, and only the twin listened. The
  scripts now use the NPC that is there. This also covers the Shrouded Isles, Atlas and battleground quests.
- Handing in an epic quest item with a full backpack no longer loses the item. Nine epics took it before checking
  for room for the rewards.
- Quest steps that ask you to whisper a word now show it as a clickable [word] when you talk to the NPC.
- If you lose an item a quest step needs, or never got the one a step starts with, the quest gives it to you again.
- You can now talk to quest NPCs that are monsters of a hostile faction while you are on their step or can take
  their quest (Tiomoid in Lough Gur, Trefor and Curmie, Shiake, the Shrouded Isles tribe turn-in NPCs; 67 in all).
  They used to attack you or give you a dirty look. To everyone else they are the same monsters as before.
- Every classic quest can now be finished from start to finish. Quest monsters that were missing are placed where
  period sources put them, at their period levels: curmudgeon puggards in Cruachan Gorge (Lost Townfolk of Bran
  Llyr), and for collection items pictish warriors, shadowhunter she-wolves and Tylwyth Teg huntresses (Pennine
  Mountains, Hadrian's Wall), frost giants and the ghostly and ghastly invaders (Midgard and Breifine), forest
  stalkers, lesser zephyrs, white boars, speghouls and billious goo. The turn-in NPCs Asdis and Aserod Ilonus are
  in, and eight collection items now drop from the monsters their quests name.
- 28 quest steps whose target only existed as a friendly NPC now give you the fight: the target appears and comes
  for you at the place the walkthrough gives (for example Gokstad, Fressen, Borwyr the Cursed, Aricoer at the broken
  tower in Cornwall, the Guardian by the Deamhan Creigs, the two guards in List of Denial). Town guards leave these
  fights to you. Six targets were misspelled or in the wrong place: Klippa, Broch, Nimh, Arica, Ulfgar and Eibhlin.
- Quest NPCs have the race and gender their quests describe: Erdola in Connla is a firbolg woman (she was a Celt
  man), the missing man in Howth is a man, and Cailbyn, Odlran, Tomos, Mystemas, Edmond, Raiaya, Carylina, Norma
  and Meriach are corrected the same way.

**World**
- Dying to the guards of a battleground's central keep while it is still unclaimed no longer costs experience or
  leaves a gravestone. Those guards have no realm, so the death counted as a PvE death; any keep guard's kill now
  counts as an RvR death, as it already did for a keep held by an enemy realm.
- Removed the hostile copies of friendly NPCs that 0.35's classic bestiary spawns added: the level 49
  "commander" outside Camelot (on the way to Humberton) that attacked players and bots, and six "innocents"
  in Dodens Gruva. The friendly Commander and the Midgard innocents are unchanged.
- NPCs no longer show quest notes under their names (such as "<Part of Against the Grain Quest>"). Ten NPCs did:
  Laridia the Minstrel, Yetta Fletcher and Eowyln Astos in Cotswold, and the Master Visur teleporters. Yetta, Eowyln
  and Master Visur show their trades again (Arrows Merchant, Dye Merchant, Teleporter).
- Laridia the Minstrel in Cotswold now stands inside the house next to where she was, instead of outside against
  its wall.

**Launcher**
- The note beside "Make Me a GM" on the Options tab now also says that monsters will not attack you while
  /gmstealth is on.
- The launcher has a new-client mode for 0.36c: the copy in new-client\runtime starts the new client and runs the
  game's own server (named in server_root.txt), so both clients use one account, one bot roster and one world. It
  shows VERSION 0.36c.

## 0.35 / 0.35b — 2026-10-08

Everything added since 0.34, in one complete download. The overview is in
[docs/history/RELEASE-0.35.md](docs/history/RELEASE-0.35.md); every change is listed below.

**Download and setup**
- One complete download in checked parts, with two editions, as in 0.34:
  - **0.35b** includes the Sluaghbinder.
  - **0.35** has the classic class list only.
- The 0.35 game client now has every client improvement of 0.35b (the classic frontier war map, red map
  markers for every quest and the working QUEST GUIDE button); only the Sluaghbinder class is left out.
- The launcher shows which edition is installed: **VERSION 0.35b** or **VERSION 0.35**.
- Every install still gets its own account on the first ENTER REALM, an empty world for its own bots and
  default launcher settings, and a client settings profile that is new for 0.35.
- The progress transfer tool now also imports **0.34 and 0.34b** saves, as well as every earlier version.
  See [docs/TRANSFER-PROGRESS.md](docs/TRANSFER-PROGRESS.md).
- Every database and client change listed below is already in the download.
- Quest Guide walkthroughs are the Allakhazam (camelot.allakhazam.com) quest pages as archived in 2001-2004,
  written by Allakhazam's contributors; each guide names its source.

**Companion bots and gamebots**
- Your /spawn and /raid companions keep their pets when you travel. Their pets used to die on every teleport
  and horse ride (the old frontier teleporters and stable routes included), and the pet classes kept
  re-summoning. A companion's pet now comes along within a region and rejoins it at the end of a horse ride,
  as your own pet does.
- The /raid 40 and /raid 80 window keeps your raid when you change zones. Zoning (for example into
  Galladoria) used to show every spot in the raid window as empty although the whole raid was still
  with you; the window now refills itself once the new zone has loaded.
- Spiritmaster bots fight with their spells. They used to cast their pet's damage shield, which is a
  channelled focus spell, and then stand still with full power while the pet fought. Like Cabalist and
  Enchanter bots, they no longer cast damage shields; every spec uses its damage spells.
- Pet classes buff their pet much faster. Out of combat the next pet buff follows right after the
  last one finishes casting instead of 15 seconds later (the Sluaghbinder already worked this way). A
  buff that did not take hold is retried after the old wait, and in combat the old spacing stays.
- Self "absorb the next hit" shields (Spiritmaster Protecting Spirit, Cabalist Barrier of Warding,
  Enchanter Barrier of Negation, Bonedancer Armor of Bone, Animist Shield of Wood and the rest) go back
  up as soon as nothing is fighting the bot, before it sits down to rest, instead of long after the
  fight.
- Casters that run out of power fight with their staff instead of standing still. Every caster, pet
  classes included (Eldritch, Bonedancer, Spiritmaster, Enchanter, Cabalist, Theurgist, Animist and
  the rest), walks in and melees alongside its pet, then casts again as soon as it has the power, and
  it follows its target if the target runs off. A Necromancer's shade has no melee.
- Bots always use their best damage spells. They no longer fall back to a low rank, or to the top
  spell of a line they barely trained, to save power (a level 50 Bonedancer was hitting for 1 damage
  with a level 4 lifedrain). If they can't afford their real spells, they fight in melee instead.
- Healers walk into range of a group member who needs healing, and buffers walk into range of a
  group member who is missing a buff. They give up on someone they can't get closer to instead of
  trying forever, and they ignore members who are far away or in another zone.
- Skald and Warrior bots can now be built with a two-handed weapon as well as one-hander and shield.
  Until they have a usable two-hander they fight with their one-hander. New gamebots and newly summoned
  /spawn companions only; existing gamebots keep their build.
- Healing classes (Shaman, Druid, Healer, Cleric, Friar, Warden, Paladin) never root the monster they
  are killing. In a group they root an add that runs at the party. Heals always come first.
- Savage bots use their health-cost buffs only from 70% health (damage and attack speed; evasion too
  above 90%), never their resist buffs, and never on a nearly dead target. They used to keep up to four
  buffs at 5% health each.
- Solo Paladins hold one chant (damage in a fight, combat heal below 70% health, endurance otherwise)
  instead of rotating chants and briefly targeting themselves mid-fight.
- /pull starts the pull at once: resting companions stand up instead of refusing. /grind is still
  careful, but Paladins, Wardens, Bards, Minstrels and Skalds only need 75% power and endurance, and
  their own chants no longer hold the pull, so chanting groups pull again.
- Bring A Friend now counts your companion bots. Your /spawn and /raid helpers (and their pets) count
  as group members the way other players would, so monsters bring friends when your companions pull
  or fight with you, and a /raid 40 or /raid 80 can pull a lot more of a camp. Gamebots are unchanged.
  Server setting: baf_companion_bots_count (on by default).
- A bot's pet that gets stuck in rock and makes no progress toward its owner for 10 seconds is put
  back on walkable ground beside its owner. Player pets are unchanged.
- Minstrel bots no longer freeze retrying a flute mesmerize that can't land; they pick their next
  action instead. Valewalker bots stop recasting a weaker rank of a weapon proc they already have. A
  Warden skips its travel speed chant when a groupmate has a faster speed song and keeps bladeturn up
  instead. (Ported from the stefanrows/OfflineDAoC fork.)
- Savage bots no longer swap between a shield and their hand-to-hand offhand weapon every few seconds.

**Gamebots**
- Gamebots stop a short way out from a teleporter NPC (town teleporters and the battleground teleporters),
  spread around it, instead of walking onto the NPC itself, so players can click the teleporter again.
- Gamebots no longer work the border keep door levers, which kept you from using them. They walk through
  the doors and leave the levers to you.
- An enemy gamebot's real name no longer shows when it dies ("... dies!") or in the "worth no realm points"
  message; you see its race there too, as everywhere else.
- Stealthed enemy gamebots are invisible unless you are within detection range, like stealthed players.
  They used to show as a ghostly outline.
- Keep guards, archers and lords, gamebots and pets no longer drop items. Gamebots stand in for players, and
  killing a player never dropped loot. A raid's siege used to fill your bags with random gear.
- Gamebots earn realm points for PvP kills, the same share a player would get, whether they kill an
  enemy gamebot or a player, and their realm rank now goes up with their realm points.
- Gamebots spend their realm points on realm abilities at their class trainer, like a player. Each
  follows a build for its kind of class (melee, caster, healer or archer) with the real costs, levels
  and prerequisites: augmented stats, Toughness, the masteries, Avoidance of Magic, Determination and
  so on. In a fight they use Purge against stuns and mezzes, Ignore Pain when nearly dead, Second Wind
  when out of endurance, and First Aid after a fight.
- Gamebots pick zone-border crossings whose ground actually continues to where they are going, so they
  stop walking onto mountain shelves that only connect along the border (Lough Derg and the Valley of
  Bri Leith, Vindsaul Faste) and find the crossing from Salisbury Plains into Black Mountains South.
  A gamebot saved in a walled-off spot it can never walk out of is moved out when it logs in.
- Gamebots no longer give up a camp because of grey monsters on the way, and they fight green and blue
  packs on their route instead of turning back (stronger monsters and big packs are still avoided).
- A gamebot spends a few realm points at its next training visit; only three or more unspent points
  send it to the trainer on their own.
- Keep sieges: RvR warbands no longer wait at the frontier teleporter for all eight members; after a
  minute the ready members take the porter and the rest follow. Siege operators keep the engine kit
  they carry instead of going back to buy a different one, only a melee attacker next to the operator
  interrupts the job (wall archers no longer stop every ram), and attackers no longer buy ballistas.
- Smooth group travel: group members aim for their spot beside where the leader is about to be and
  keep walking with the leader instead of stopping and starting. They always move at their own full
  speed (only the game slows a bot down) and run a bit faster when they fall behind. The leader's stops to let the group catch up are shorter, and a member
  that stays stuck for 45 seconds no longer holds the whole group in place. (Group motion ported from
  the stefanrows/OfflineDAoC fork.)
- Gamebots look ahead on their way to a camp instead of walking into aggressive monsters. A monster
  they can handle is pulled on their own terms (casters open from range instead of being jumped), a
  stronger one or a pack is walked around, and only when there is no way around and no chance do they
  pick a different camp. Groups do the same, led by their leader. Dungeons keep their own corridor
  clearing. Still being tested: the extra look-ahead may add some server strain with very large bot
  counts. It can be switched off with the server property `bot_route_threat_awareness`.
- A group no longer freezes in place when one member is fighting somewhere else. Members walk over to
  help a groupmate who is fighting out of reach; a fight far from the leader, or one where nothing has
  been hit for a minute (a monster the bot can't reach), no longer holds the whole group.
- Level 50 bots grinding alone pick spots with enough monsters worth their time (top-of-green and up)
  and no longer stand around waiting for a rare green among grey monsters. Companions no longer wander around slowly while
  you stand still; they stay in formation.
- A camp that one group can't reach is benched for other groups too after three groups report it.
- Bots no longer get stuck on the slope by the paralyzers in Iarnwood. Routes there passed through a
  point where several pieces of the walking map meet, and bots stalled on it; route points and sight
  checks at spots like this are handled properly now, everywhere in the world.
- Avalon City is a bot goal: over 1,000 spawns there were checked for a two-way walking route from a
  real entrance.
- Bots take Korlis's gryphon from Hagall to the Iarn Dwarf Camp instead of swimming to Modernagrav.
- Bot group names in the launcher and the logs name their realm ("albion-...").
- Bots that stop to pull a monster in their way now actually attack it. The pull could run out before
  the bot's next decision, so it stood still and pulled the same monster again every few seconds, often
  for many minutes (about 600 times an hour across the server). A monster that still won't come after
  three pulls is walked past.
- Every group of three or more now pulls from range: a ranged member pulls the monster back to the
  waiting party instead of the tank walking into the pack. While the target is still out of range the
  party keeps walking toward it. Groups kill noticeably faster (level 50 groups about 60% more kills an
  hour, low-level groups about 50% more experience an hour).
- Gamebots use the town teleporters (Master Visur, Stor Gothi Annark and Channeler Glasny, by the bind
  stones and in the capitals) like players do, but only to their own realm's destinations. A bot takes
  one only when it is clearly quicker than walking or a stable-master horse, and still rides horses
  (including the Shrouded Isles flights) where those are quicker. Groups, warbands and raid parties
  gather at the teleporter and go together, and RvR bots use them to reach their border keep. They land
  on the ground, even where a destination sits in the air (Mularn). In testing, bots reached their camps
  about 40% faster and raids filled their rally points much sooner. On by default; the server property
  `bot_use_town_teleporters` turns it off. These teleporters are not authentic to 1.65; they come from
  later live versions of DAoC. Most freeshards have them, and Atlas, a classic 1.65 freeshard that has
  since closed, had them too.

- Gamebots can take part in the battlegrounds. A fourth goal, Battlegrounds %, sends bots to the
  battleground of their level by a realm teleporter: Abermenai (15-19), Thidranki (20-24), Murdaigean
  (25-29) or Caledonia (30-35). There they fight the other realms, attack the central keep when another
  realm (or its neutral guards) holds it, and defend it when their realm does. Bots outside those levels
  do solo or group PvE with that share instead. The goal is off (0%) unless you turn it on.
- Bots no longer stand for minutes "fighting" an enemy player or bot they can't reach or hurt: after 90
  seconds without landing any damage they move on and ignore that target for five minutes.
- A bot that hit a route problem while travelling to its camp could stop thinking for that turn with an
  error; it now just picks a new camp.
- Enemy-realm gamebots are shown the way enemy players were in the period game: by their race (for
  example "Saracen"), with their realm rank title where a guild name would be, never by their own name.
  This holds for their nameplate and for combat, spell, resist and death messages. Gamebots of your own
  realm, pets, companion bots and game masters keep their real names.
- RvR bots stay on the frontier they picked for 20 to 25 minutes. They used to re-plan every minute
  across all three frontiers and kept porting home and out again through the portal keeps (one bot
  ported 11 times in half an hour). A relic on the move, a siege or a defense call still pulls them
  wherever they are needed.
- Battleground monsters are camps for solo and group PvE bots whose level fits that battleground.
- Bots crossing between zones prefer crossings that lead onto the zone's main ground instead of onto a
  ledge or plateau with no way down (the East Svealand plateau caught over a hundred bots in one run).
- A defending army that has gathered fewer than half its defenders waits up to four more minutes for
  the rest before it marches, so defenders arrive together instead of a handful at a time.
- Siege armies march as one column. After the muster, the army follows a march leader to the siege
  camp, members keep close to the leader, the leader waits briefly when most of the column has fallen
  behind, and anyone in the column helps a member under attack. Armies used to walk the whole way one by
  one and lost half their strength to roaming enemies before reaching the keep.

**Realm events and raids**
- Raids lead bosses back home. Many raid bosses (every epic dungeon and Summoner's Hall final boss
  among them) can't be hurt once they are drawn away from their lair. Bots now stop attacking such a
  boss and the one it chases runs back toward its lair so it follows, the way players do it; the raid no
  longer skips a boss for this (Summoner Roesia was skipped at 28%).
- Caer Sidi: Skeletal Sacristan walks his loop slowly enough for a raid to follow him (he used to walk
  faster than bots run), and spells hurt him a little more.
- When a raid boss takes no damage for three minutes because the whole raid is busy with its guards
  and adds, some of the fighters switch to the boss (Princess Nahemah in Darkness Falls was skipped at
  full health with 65 raiders next to her).
- New neutral raid events: Summoner's Hall and Darkness Falls. Every realm can run its own 300-bot
  expedition there, forced from the launcher (new "Neutral raid" events) or started automatically like
  the dragons and epic dungeons, so two (rarely three) realms can be inside at the same time. Each realm
  musters in its own territory and uses its own way in (Summoner's Hall through Hall of the Corrupt,
  Dodens Gruva or Marfach Caverns). Summoner's Hall: the three summoners, then Grand Summoner Govannon.
  Darkness Falls: the High Lords, the Princes and Princess Nahemah, then Legion. Bots of different
  realms fight each other on sight in Darkness Falls, Summoner's Hall and the dungeons leading to it.
- Forced raids form every party they reserved. A raid could stop forming parties partway through the
  muster (Darkness Falls stuck below 200) and never start.
- Raid members who died or arrived late rejoin a running Darkness Falls raid even after the raid has
  moved deeper in, and members whose way to the dungeon keeps failing switch to ordinary travel instead
  of waiting at a stable for good.
- Bots can now hurt scripted named monsters. Many bosses only took damage from players and their pets,
  so bots hit them for nothing: Grand Summoner Govannon and the summoners, Legion, and named monsters in
  Hall of the Corrupt, Marfach Caverns, Tur Suil and the classic zones. Bots now count like players.
- Galladoria: bots can now beat Xaga. Tine and Beatha fly low enough to be hit and stop circling the
  room while they are being fought, and Beatha's attacks no longer heal Xaga.
- Forced raids from the launcher start as soon as 200 bots are staged (and a dragon has landed), with
  no 45-minute muster and no time limit. They run until the encounter is defeated, you press the new
  STOP EVENT button (no cooldown after a stop), or the server stops. The 300 recruits are the level-50
  bots closest to the rally point.
- Forced rallies fill much faster: parties form from whichever reserved bots are free, closest first,
  bots on a horse included, so one busy bot no longer holds seven others back. A member with no route
  to the rally point leaves its party instead of blocking it.
- A raid party that loses a member (one with no route to the rally point) takes a replacement: the
  closest free level-50 bot of the realm, preferring one that can fill the same role. The newcomer
  walks to the raid like a late arrival, so a 300-bot raid no longer stays short at 296.
- A realm never runs the same automatic dragon or epic dungeon event twice in a row.
- Forced epic dungeon rallies no longer announce in faction chat that a dragon must land.
- Epic dungeon raids check their next target from every party, not only from the front. A raid with no
  reachable encounter for 20 minutes ends as "Route blocked" instead of holding 300 bots for hours.
- Raid members now attack a boss when the raid calls them to it. The call used to run out before the
  bots acted on it, so bosses such as Summoner Cunovinda were skipped at nearly full health. Summoner's
  Hall has now been cleared by bots on their own: an automatic Midgard raid killed all three summoners
  and Grand Summoner Govannon.
- Keep and relic sieges: the attacking realm gathers its army at its own guarded portal outpost inside
  the enemy frontier, then marches on the keep together once it is strong enough to start the siege
  (or after 20 minutes), instead of warbands arriving one at a time and being picked off. Attackers
  fighting at the keep now count as present, and bots that left their RvR task no longer keep holding a
  place in the siege.
- A short line at the top of the screen (no sound, nothing in the chat box) tells every player, of any
  realm, when a keep or relic siege begins. Your own realm's raids get the same kind of line when they
  start forming, automatic or started from the launcher (dragons, epic dungeons, Summoner's Hall,
  Darkness Falls), so you can join the bots at the rally point.
- Bots now set up and use siege weapons the way players did. They had never managed to place one. Rams go
  right in front of the outer gate, then a new ram at the inner keep door, and the bots standing at a ram crew
  it like riders. Trebuchets pound the doors from range, catapults lob shots onto defenders and wall archers,
  and ballistas take on enemy siege engines. Defenders set up inside their walls. Siege crews get their engine
  on the spot instead of leaving to shop for one, and they take over any idle engine of their kind, a ram you
  drop included. Bot archers and casters shoot back at archers on the walls, and melee bots stop chasing a
  target up on a wall they can't reach.
- A siege army that has marched on a keep attacks as soon as it gets there, instead of waiting at its posts
  and being whittled down.
- Attacking an enemy keep yourself now raises a defense sized to your force. Alone, in a group, with a /raid 40
  or a /raid 80, up to 24, 48, 128 or 240 defenders answer. Your own realm and the third realm send up to 8,
  16, 48 or 96 each. A horn sounds and the announcement matches the size: a small defense force, a defense
  force, a large defensive force or a massive defense force has been rallied. The defenders gather briefly at
  a nearby keep and march in as one big wave once 80% of them have gathered (or after ten minutes), while your
  realm's warbands and the third realm's arrive however they can. Joining a siege the bots planned themselves
  raises no alarm.
- If you take the keep, the realm that lost it comes back once to take it back. It gathers its army at its
  outpost (80% of it, or ten minutes), marches, and strikes the moment it arrives, sized like the defense you
  beat (up to 240 against a /raid 80), and it fights for up to 30 minutes. You'll need to hold the keep.
- Bot armies bring more siege equipment: an army sets up two rams and a trebuchet on the gate (trebuchets
  hit doors hardest), plus a catapult and ballistas, and every bot standing at a ram helps it hit harder.
  Defenders set up catapults and ballistas inside the keep. A smaller force still brings a ram.
- A siege ends as defended once the attacking army has been driven off and stays gone for ten minutes,
  so the defenders go back to the world instead of waiting out a four-hour timer.
- Siege weapons can no longer hurt a keep lord, and bots and their pets can't hurt him either while the gate or the
  keep door is still standing (their spells and arrows used to reach him through the walls). You have to break in
  and fight your way to him. Rams also go for the keep door after the gate, not the small side postern, and
  bots now set up rams at the keep door once the gate is broken (they used to look for a spot inside the keep,
  behind the door).
- Your /raid 40 and /raid 80 fight keep sieges on their own. When you attack an enemy keep, the raid splits into
  squads of eight. One squad stays with you as a bodyguard. The others set up rams, catapults, trebuchets and
  ballistas, shoot the archers off the walls, hold the gate, and storm the keep lord once the doors are down.
  When you defend a friendly keep, they hold the doors with siege engines in the courtyard. They stay at
  their posts until the fight is over, helping you only when you're nearby, then regroup on you. If you pick
  up a relic they escort you, and a relic you leave lying for 20 seconds is picked up by one of them. In the
  frontier your raid also engages enemy gamebots on its own, within 2,000 under /aggressive or 1,000 under
  /defensive, and it is called back if it chases too far. Archers up on the walls are left to the raid's
  ranged bots and healers: a melee bot drops a target it can't walk to instead of standing under the wall,
  and bots in the frontier are never moved up onto a keep wall while the gate still stands.
- Every realm is told when a raid kills the final boss of an epic dungeon, Summoner's Hall or Darkness
  Falls ("Albion's forces have defeated Legion in Darkness Falls!"), enemy realms' victories included,
  the same way keeps and relics taken are announced.
- Battleground keep sieges are announced too: when a realm starts attacking a battleground's central
  keep, and when the keep is taken.
- Grand Summoner Govannon has 200,000 health (was 300,000); raids wore out against him at half health.
- Tuscaran Glacier: the raid could get stuck for hours on a small walkable patch hidden under the ice
  floor. Those hidden patches are removed from the Tuscaran Glacier and Galladoria navigation maps, so
  nothing can be routed onto them; every real room is still reachable.

- A siege army that is still mostly on the road when its gathering time runs out waits ten more minutes
  for the rest, instead of marching with too few to start the battle. Bots waiting at a siege rally are
  no longer moved back to town by the "stuck" check.
- Bigger, quicker sieges. A keep siege now draws up to 128 attackers, 64 defenders and 64 from the third realm;
  a relic siege 240, 120 and 120. The third realm joins in half of all sieges. Armies gather for 12 minutes
  (one 5-minute extension if most are still on the road) instead of 20 plus 10.
- The launcher's RvR tab shows a siege rally's gathering time ("marching in 6m", then "Marching to the
  keep") and the attack window separately. The old "Preparation" countdown was the whole attack window and
  kept running after the army had marched, so relic rallies looked like an hour of preparation.
- Bots staged at their realm's second border keep (Druim Cain, Snowdonia Fortress, Vindsaul Faste) walk into
  the frontier through that keep's gate. Frontier medallions are only sold at the first border keep, and
  Hibernian siege recruits at Druim Cain used to stand still for a quarter of an hour trying to reach one.
- Siege armies move as full groups. Bots that answer a siege, a defense or a relic battle on their own are
  formed into parties of eight with the other nearby recruits of the same side, so attackers and defenders
  travel and fight as groups instead of one by one.
- Every side goes after a stolen relic wherever it is: the thieves escort it home, and the defenders and the
  third realm chase it down, whether it is carried with an escort, carried alone or lying on the ground. This
  now also happens when the relic is snatched before the siege battle has formally begun (that used to end
  the event and send everyone home).

**World**
- The realm war map (KEEPS) shows the classic frontier. Its zone tiles are the classic frontier zone maps, and every
  keep, border keep and relic keep icon sits where that keep really stands, on all three realm pages; the game
  client used to draw New Frontiers terrain with the keep icons at New Frontiers spots. Your own position shows on
  the war map while you are in the frontier, and the milegate icons the classic frontier never had are gone.
- Bots cross from zone to zone only where real ground meets, so long trips (for example from Hibernia's
  home zones into the frontier) no longer end on a walled-off mountain shelf. Bots riding a stable-master
  horse now land on the ground at Castle Sauvage, Svasud Faste and other stops where the route used to end
  in the air or under the ground.
- Bots walk where players walk. The bot navigation maps for 63 outdoor zones were rebuilt from the
  game client:
  - Every invisible wall in the game now stops bots. The old maps were missing about half of them, so
    bots crossed zone edges, climbed mountains players can't climb and walked off the edge of the map.
  - Towns use the classic town layouts of this client. The old maps were built from the later "New
    Towns" layouts, so bots walked through houses and fences in towns such as Mularn.
  - Svasud Faste (the Uppland border keep) was missing from the map; bots walked through its walls.
  - Hibernian Celtic huts can be entered again: their doorways were sealed on the old maps, so bots
    couldn't reach the merchants and trainers inside.
  - House door panels no longer block bots. Keep gates still do while they are closed.
  - Zone borders on slopes line up, so bots cross them like players (for example from Cornwall into
    Dartmoor, which bots could only reach over the hills before).
  The capital cities and dungeons keep their current maps for now.
- NPC placement fixes: Borra and Truda in Gripklosa no longer stand inside their house floor, Baldhere
  in Knarr no longer stands on the furniture, and a broken copy of Hibernia's stable master Truichon
  that had been placed in Albion is removed.
- The Shrouded Isles portals in Cotswold, Mularn and Mag Mell are solid like their Shrouded Isles
  counterparts: you walk up the steps onto the platform, and stepping onto it takes you to the
  Shrouded Isles (not while fighting or carrying a relic). Bots climb onto the platform and use the
  portal the same way instead of walking through it. Needs the updated client files.
  Note: the portals at Cotswold, Mularn and Mag Mell are from patch 1.74 and are not accurate to
  1.65. The original 1.65 portals near Adribard's Retreat (Avalon Marsh), Nalliten (Gotar) and Connla
  (Shannon Estuary) are still there and still work, and the way back from the Shrouded Isles still
  leads to those original locations.
- Two more Midgard helmets show the face: the fine alloy heavy starkakedja helm (model 834) and the
  fine alloy superior war circlet (model 1291) and its variants, extension 3.
- Three doors that always said "too far away" work: the Mag Mell hall door, a Pennine Mountains door
  and Midgard Mile Gate 2 in Cathal Valley. Each had a stray copy in another zone that replaced it.
- The Trollheim animated weapons are visible: a floating Norse axe, sword, hammer, spear and round
  shield. Their damage and attack speed are unchanged.
- The Heretical Hermit in Campacorentin Forest is always the level 20 Hermit. He shared a template
  number with the gray wolf pup, so he sometimes appeared as a level 1 pup, and the starter-area pups
  could appear as a level 20 Hermit.

- The battlegrounds are open from every realm teleporter (Master Visur, Stor Gothi Annark, Channeler
  Glasny and their apprentices): say [Battlegrounds] to see the brackets and go to yours. The frontier
  medallion teleporter uses the same brackets, now with all four battlegrounds. Entry is by level only:
  there is no realm point cap on this server.
- Midgard characters leaving a battleground are sent to Svasud Faste again. The exit looked for a
  location that doesn't exist and left them where they were.
- All four battlegrounds have their monsters, never above the battleground's level: Abermenai (15-19),
  Thidranki (20-24), Murdaigean (25-29) and Caledonia (30-35), from period sources. Dun Murdaigean and
  Dun Abermenai have their central keeps with Renegade guards and a Renegade Chieftain; the launcher's
  reset-all-keeps button resets them with the rest.
- The Coruscating Mine has fewer monsters. Many of its camps had been entered twice a few steps apart,
  so groups pulled double camps; the duplicates are gone, and the monsters that period bestiaries show as
  rare there (unseelie viewers, casoliths, vein golems, lode protectors and the like) are rarer again.
- Howth and Connla in Hibernia have their low-level monsters back: feccans, skeletal pawns, villainous
  youths, water beetles, spraggons and the rest of the level 1-15 populations, placed where period
  radar logs saw them. There used to be no monsters at all near Howth.
- Monster populations restored from period bestiaries across the classic and Shrouded Isles zones: 223
  species that period radar logs, Illia's Camelot Bestiary and Uthgard agree were missing are back, and
  camps the server never populated have their monsters again (5,677 spawns, placed where 1.65 radar logs
  saw them, never inside towns). Seven species no source lists in their zones are gone, and monsters whose
  levels were 5+ off have their period levels. Bots grind the new camps, and the new monsters count for
  bounties and for the charm lists. The epic dungeons and Darkness Falls are unchanged. A lynx cub, a
  bodachan sabhaill and a mudman that stood inside Howth's walls are gone.
- Classic quests (1.65 and classic Shrouded Isles) for players: **448 quests** (1,302 quest versions, counting the
  per-class versions of the epic and trainer quests), from the original class epic lines to the trainer, optional
  and kill-task quests, with their recorded dialogue, experience and the quest NPCs and monsters the server
  lacked (placed from the period walkthroughs, Warcry spoilers and period bestiaries). Steps that need more than a
  conversation or a kill work too: using an item at a place, reaching a place, trading a monster's drop to an NPC
  (kill tasks, repeatable), choosing your reward by whispering its name, typing the words of power in /say at the
  right place (at night where the walkthrough says so), an NPC that another one calls up for you (the Enchantress
  summons Lucan in Morven's Return), a quest NPC who turns on you (Ciar in Enchanting Willow), and more. Items an NPC hands you are re-issued if you lose
  them, and quest items can be dropped. Kill-task monsters and the creatures a quest collects from live in camps of
  six to eight that bots grind too. **Quest rewards are still in progress:** many reward items are not in yet and
  are added as their period item pages are collected.
- Red map markers for every quest. Each quest in your journal shows its next step with its own red marker on the
  map, including the spot where a quest monster will appear. The game client used to draw only bounty markers
  red; reputation quests, the Sluaghbinder epic and the classic quests showed green dots that looked like bots.
- Quest Guide: the Quest Journal's QUEST GUIDE button (it used to say BOUNTY MAP and did nothing), /questguide, or
  /task with no guard, merchant or crafter targeted shows the Allakhazam walkthrough of your active quests as it read
  in 2001-2004 (period copies for 424 of 448 quests; the rest from today's page with later-patch notes removed), a
  page at a time; click the button again for the next page. Quests made for this server show their own journal text.
  /questguide <quest name> [page] opens any classic quest's guide. /task on a guard, merchant or crafter asks it
  for a task, as in the original game; /task status shows your current task.
- Quest NPCs added for the classic quests look like people when they are people. A quest NPC with no recorded
  appearance used to borrow a monster's look from a word in its name (the missing man in Howth was a small earth
  golem); it now looks like a townsperson nearby. The West Wind's journal says where Bairfhionn stands (outside the
  Alainn Cuir in Tir na Nog), and its "ask in Howth" step now has you ask a townsperson instead of an ambient pixie.

**Sluaghbinder (0.35b)**
- The Epic Spells page shows only the raise ranks whose quest you have finished.
- Muirenn's quests get a red map marker like bounties: on the target while hunting and on Muirenn
  after the kill.
- Epic quests: the level 30 Fomor Gravewarden clue points to Caillte Garran, where it spawns; the
  level 20 Mirebound Ossuary no longer stands on top of Frang; the level 40 Mirewood Death-Scribe no
  longer stands inside a tree.

**Launcher**
- The launcher scales with Windows display scaling and with "Make text bigger", so it is no longer
  tiny on a 4K monitor. It always fits the screen. For a 4K monitor left at 100% scaling, put a number
  such as 150 in a file named launcher-ui-scale.txt next to the launcher.
- New realm emblem in the header, based on the classic Camelot knot.
- BOT AI DELAY shows the server's real average bot and monster AI time per tick over the last minute.
- Realm Records: new DELETE ALL RECORDS button (clears only the launcher's event history, no game
  data), and "Stopped by player" and "Route blocked" outcomes.

- The XP Settings tab is now called Options. It has two new switches, both on by default and changeable
  while the server is stopped: RvR and battleground announcements (keeps and relics taken, sieges
  started), and PvE realm event announcements (raids forming, final bosses defeated).
- Bot Goals Setting has a Battlegrounds % column next to Solo PvE, Group PvE and RvR (not for level 50).
- A new Battlegrounds tab shows each battleground's level bracket, who holds its central keep, and how
  many bots of each realm are inside or on their way.

**Server**
- Monster spells of the "no variance" damage type (Scorcher DD, Scorcher DD AE and others) now scale with the
  caster's level like other monster spells, instead of always hitting for their base damage.
- Server log labels name Albion "Albion" instead of "_FirstPlayerRealm" (bot and group entries).
- With many bots online the server creates about a third less short-lived memory per second (bot
  registry, pet upkeep, frontier enemy checks and more), so it pauses less often to clean up.
- The server no longer keeps one CPU core at 100% when the launcher is closed or crashes while the
  server is still running (thanks to lometur, pull request #3).
- The server log notes every minute with a stall of a second or more (slowest stage, garbage
  collection pause, the slowest AI turns and a likely cause), and the very noisy short AI-turn lines
  are only written for turns of 100 ms or more.

**For modders**
- The navmesh builder (`source/development-tools/OpenDAoC-BuildNav`) builds every point of each zone wall (bots
  used to cross zone edges and climb mountains through gaps), keeps doors and the far-LOD stand-ins from sealing
  doorways, and matches zone-edge terrain to the neighbouring zone, so the rebuilt meshes follow the paths players
  can walk.

**Credits**
- Thanks to the [stefanrows/OfflineDAoC](https://github.com/stefanrows/OfflineDAoC) fork (Stefan Rows
  and Aaron Zielke) for the smooth group travel code (without its pace matching) and the Minstrel, Valewalker and Warden fixes,
  and for the shield-swap fix that inspired the Savage one.

## 0.34 / 0.34b — 2026-10-03

Everything added since 0.33, in one complete download. The overview is in
[docs/RELEASE-0.34.md](docs/RELEASE-0.34.md); every change is listed below.

**Download and setup**
- One complete download, about 5 GB in checked parts, with two editions, as in 0.33:
  - **0.34b** includes the Sluaghbinder.
  - **0.34** has the classic class list only.
- The launcher shows which edition is installed: **VERSION 0.34b** or **VERSION 0.34**.
- Every install still gets its own account on the first ENTER REALM, an empty world for its own
  bots and default launcher settings. Its client settings profile is new for 0.34, so a 0.34
  install never reuses a 0.33 copy's client settings.
- The progress transfer tool now also imports **0.33 and 0.33b** saves, as well as every earlier
  version. The 0.33 world database has the same layout as 0.34, so every saved table comes across
  as it is. See [docs/TRANSFER-PROGRESS.md](docs/TRANSFER-PROGRESS.md).
- Every database and client change listed below is already in the download. The scripts that
  made them are kept for modders; see "For modders" at the end of this entry.

**Companion bots**
- Pets keep one target when the party fights several enemies. They finish their spells, heals
  included, instead of restarting them, and they still step in when an enemy attacks their owner.
- Pet heals go to the most injured ally, companion bots in the group included, instead of the
  first injured ally found.
- Ranged companions (Enchanter, Wizard and the other casters) walk up to a distant target that you
  or your pet are fighting instead of standing still, and their own pets join the fight.
- Melee companions and melee gamebots no longer rubberband while running at their target. Every
  AI tick they dropped their chase order, so the attack code stopped them and started them again
  about twice a second.
- Bots and pets with more than one damage-over-time spell use all of them when they stack. They
  used to skip a second one whenever the target already had any damage over time on it.
- Companion bots and gamebots no longer wear gear built on another realm's model. 256 items in
  the database are marked for one realm but use another realm's model: 77 Albion, 130 Midgard and
  49 Hibernia. For example, the Midgard "woven pointed steeple" wizard hats use the Hibernia hat,
  which has no Valkyn or Troll shape, so it didn't fit a Valkyn's head. Bots now skip those items
  when choosing gear. The items are still there for players. A companion that is already summoned
  keeps its gear until you summon it again.

**Gamebots**
- Nine hungry shriller (Caillte Garran) and Cliffs of Moher spawns that sit next to aggressive
  monsters 15 or more levels higher are no longer used as bot camps or pull targets. The
  monsters are still there for players, and every other spawn of the same monster is still a
  normal bot goal.
- To make up for that, Cliffs of Moher gets four more bantam spectres and two more koalinth
  sentinels next to the safe ones, spread out on open ground away from trees, rocks and
  high-level monsters. Bots now have five bantam spectres and four Moher sentinels to grind.
- Gamebots no longer hunt the 13 wiggle worms in Bog of Cullen. The worms are level 0, but most
  monsters in that zone are level 40 or higher, so level 1 bots walked across Hibernia into it
  for almost no experience and often died on the way. The worms are still there for players.
- The Shrouded Isles neutral towns are always open to bots. Mantid (Krrzck), lammia (Cryptos
  Mythicos) and iarn dwarf (The Remnants) town residents near their faction's stable masters never
  attack bots, and bots never attack them, so bots can ride the wyvern, dragonfly and gryphon
  routes. Players still need reputation.
- A gamebot's pet now casts its group buffs on the bot and the bot's party. The Sluaghbinder's
  zombie priest used to recast its 20-minute Grave Renewal on itself forever and never follow its
  bot, because the buff only ever reached the pet.
- Bot progress is saved more evenly. With thousands of bots, saves for loot and gear changes took
  every save slot, so bots whose bags rarely changed (healers, full backpacks) could go about two
  hours without being saved. Routine saves now always get part of every batch, at the same total
  write rate, so every bot stays within a few minutes of its saved state.
- Tank bots, gamebots and companions alike, now use instant taunts (the Paladin, Friar and
  Sluaghbinder taunts). The bots only looked for taunts with a cast time, so these were never
  used. A tank bot taunts a monster that attacks someone in its group.
- A gamebot leaves a camp after three pulls in a row never reach their target. Bots used to stand
  still for over an hour retrying monsters they could not reach (bear cubs across water, corpse
  flickers on a ledge). Such a camp is also benched for other bots for a while.
- A solo gamebot whose camp turned grey after a level-up picks a camp for its new level. It used
  to keep killing grey monsters for no experience until the 15-minute stuck rescue moved it.
- Outdoor camps are chosen in proportion to how many monsters they have (up to six), instead of
  every spot being equally likely. Midgard bots had spent most of their time at one- or two-spawn
  spots, where kills are about a quarter slower.
- Camps where several solo bots keep dying for few kills are skipped by solo bots for a few
  hours (3 hours, doubling to 24 on repeats). Deaths on the way to a camp count against it, so
  bots stop being sent along the deadliest routes. This adds no work while bots move.
- A pet summoned at the very edge of the map now appears on top of its owner. The game places a
  new pet a few steps in front of its owner, and when that spot was off the map the summon
  crashed and the server removed the caster from the world. One Necromancer gamebot standing at
  the edge of Avalon Isle was logged back in and thrown out again about every 20 seconds, so the
  launcher showed one bot short (5,999 of 6,000). Players summoning there are covered too.
- RvR groups now pick as leader the bot closest to its realm's border keep (Castle Sauvage,
  Svasud Faste or Druim Ligen) instead of a random one. Random leaders were often solo roamers deep
  in an enemy realm's frontier who could not get back to the keep within the 20-minute gathering
  window, so about one RvR group in nine fell apart. The three realms take turns forming groups.
  PvE groups are unchanged.
- Epic dungeon raids (Galladoria, Tuscaran and the others):
  - A raid bot stuck on a ledge or a separate piece of floor inside the dungeon rejoins the raid
    at its current position after three failed tries from the same spot. It used to retry every
    30 seconds for the whole four-hour raid.
  - Bots fighting anywhere inside their raid's dungeon are no longer pulled out to town by the
    15-minute stuck rescue. Long boss fights where bots stand still looked like being stuck.
- A bot stuck in a small trapped spot (for example by the Druim Ligen stable master) no longer
  resets its stuck count by inching forward a little. Some bots looped there 80 times; now the
  existing safe move-out happens after three failures.
- Bots no longer try to pull flying monsters hovering far over their heads, such as the griffon
  gliders over Gripklosa, which they could never reach. Flyers close to the ground still count, and
  a high flyer that attacks a bot is still fought.
- An automatic level-50 raid that has waited 75 minutes with at least 180 of its 200 bots now goes
  in, instead of failing at 90 minutes for being a few short (Caer Sidi missed by 4 and 14). Raids
  started by a player are unchanged.
- When a nearby healer bot does not revive a dead group member, the server log now says why
  (out of power, casting, under attack, no line of sight or the cast refused), at most once a
  minute per bot. This only adds a log line; it does not change what the healers do.
- Siege bots no longer freeze on the way to an enemy keep. Bots walked toward the keep in one
  very long leg and some stood still for 15 minutes, so no siege engine was ever placed (77
  Hibernian siege bots froze in one evening). They now walk long routes in shorter checked legs,
  plan again from where they stand if they stop moving for a minute, and set that siege job aside
  for a couple of minutes after three freezes at the same spot.
- Bots bouncing between two spots in a keep courtyard (Nottmoor) are now recognised as trapped
  and moved out by the existing safe move-out.
- Dragon raids (Golestandt, Gjalpinulva and Cuuldurach):
  - A dragon fighting on the ground counts as landed wherever it is, so the whole raid joins in.
    Most of the raid used to wait for a landing when the fight drifted away from the lair.
  - A dragon whose flight stalls flies home and lands, and a landing that gets stuck is finished,
    so a dragon can no longer stay in the air for the rest of the raid. Throws and teleports stay
    off, as before.
- Epic dungeon raids set aside a target nobody has damaged for five minutes and try it again
  later, and skip monsters flying out of reach, instead of holding in place. Final bosses are never
  set aside. The server log now says why a raid is holding.
- Paladins and Bards stop their chant and song upkeep while a groupmate in range waits for their
  resurrection, so their resurrection spell is no longer refused for another spell already queued.
- Summoner's Hall is no longer handed to bots as a dungeon goal. It can only be reached through
  other dungeons, and only 1 of 268 attempts ever got there. The monsters stay for players.
- Shaman bots, gamebots and companions alike, now fight as hybrids instead of standing at spell
  range. They cast their bolt and nuke when the 20-second recasts are ready, keep their damage
  over time and disease on the target, and fight in melee in between. A ready nuke or an expired
  damage over time stops the melee for the cast, and they still stop to heal. A Shaman being hit
  stays in melee, since the cast would be interrupted.
- Shamans no longer root the monster being killed, since the first hit breaks the root. Solo
  Shamans never root in PvE; in a group, a Shaman roots an add that is still running at the party
  while the group fights something else. Roots in PvP are unchanged.
- [PLAY.md](docs/PLAY.md) now recommends bot populations (500, 1000, 1500 or 2000 per realm)
  and explains the name-generation ceiling of about 18,800 bots.

**Gamebots and companions: parry, block and evade**
- Gamebots and companion bots now parry, block and evade like players. The combat code treated
  them as ordinary monsters, which use a fixed chance from their template (normally none), so
  their class abilities, Parry and Shields training, dexterity, quickness and shields did nothing.
- They now use the same formulas and rules as players:
  - **Parry** needs the Parry specialization (or a parry buff) and a melee weapon, never a bow.
  - **Evade** needs the class's Evade ability, which comes with level like a player's, or an evade
    buff. It works from the front, or from every side with Advanced or Enhanced Evade.
  - **Block** needs the Shield ability, a real shield and a one-handed weapon. It is scaled by the
    shield's quality and condition, and the shield's size limits how many attackers it can block.
- The player-versus-player caps (50% parry, 50% evade) apply whenever both sides are players or
  bots.
- Bots, tanks and evade classes especially, are noticeably harder to kill.

**Bounty Masters: normal, hard and very hard bounties**
- The Bounty Master now offers three kinds of leveling bounty:
  - a **normal bounty**, with monsters at your level as before;
  - a **hard bounty**, with monsters about 6 levels above you;
  - a **very hard bounty**, with monsters about 12 levels above you.
- Every difficulty needs the same number of kills: 5 below level 20, 10 in your 20s, 15 in your
  30s and 20 in your 40s. It used to climb from 5 to 50.
- Rewards by difficulty:

  | Difficulty | XP | Class items (1-3) |
  |---|---|---|
  | Normal | 2 bulbs | 1 level above you |
  | Hard | 4 bulbs | 3 levels above you |
  | Very Hard | 8 bulbs | 5 levels above you |

  A bulb is a tenth of a level at the level you took the bounty. Gear never goes above level 51.
- Rerolling still halves the XP. On a hard or very hard bounty, a reroll keeps that difficulty or
  picks an easier one, and pays half of the one you end up with. For example, a very hard bounty
  rerolled to normal pays 1 bulb.
- How hard and very hard targets are picked:
  - Only monsters with at least two spawns are used.
  - A level with fewer than eight different monsters also uses monsters one, then two levels
    lower, but never drops below 4 levels above you for hard, or 10 for very hard. The Bounty
    Master explains this when offering the bounty.
  - Monsters go up to level 61. The great foes stay level-50 bounties only.
- On hard and very hard, a same-named monster elsewhere in your realm only counts if it is at most
  one level below the marked one.
- Level-50 great-foe bounties are unchanged.
- **Bounties carried over from 0.33:** an unfinished 0.33 bounty brought over with the progress
  import becomes a normal bounty with the new kill count when you log in. Your kills so far are
  kept, so it may be ready to turn in right away. The Bounty Master also offers **[update bounty]**
  once, free, to swap it for a new bounty at any difficulty.

**Sluaghbinder (0.34b only)**
- The Cairn armor buffs say "You are surrounded by an unholy aura." instead of a holy aura, and
  the Cairn strength buffs say "You are filled with the power of the cairn!" instead of the
  strength of Thor and the gods.
- Dullahan's Bulwark taunts cost power (2, 6, 10, 14 and 18 by rank) and share one 15-second
  recast across every rank. They used to be free with a 4-second recast per rank.
- Cairn Ward (Dullahan's Bulwark 20), an armor buff the core Cairn Oath line already outclassed,
  is replaced by a parry buff with its own icon: Barrow Deflection (+4% parry at 20), Barrow
  Riposte (+6% at 32) and Barrow Wardblade (+8% at 44). It lets the Sluaghbinder parry without
  the Parry specialization, and it works for Sluaghbinder bots too.
- Sluaghbinder spells cost power only. They also took 5 endurance each; styles still cost
  endurance. This applies to players, gamebots and companions.
- Every Sluaghbinder life drain (the five Abhartach's Bane Vitality drains, Guardian Lifesteal
  and Dullahan's Blood Tithe) plays the naburite drinker's drain animation.
- The five Abhartach's Bane Vitality drains share one 4-second recast. Each rank used to have
  its own, so all five could be fired back to back.
- Bane-specced Sluaghbinder bots use both their Abhartach's Rot and Abhartach's Bane damage over
  time, which stack, and the Dullahan pet's Grave Rot stacks with both.
- The Abhartach's Bane damage over time (Withering through Final Rot) plays a plague spore cloud,
  so it looks different from the baseline Abhartach's Rot. Cosmetic only.
- The Cairn strength buffs, the Sluaghbinder's own (Cairn Vigor, Fortitude and Oath) and the
  ones it casts on its pets, show a green hand glow and a green rune emblem on the ground instead
  of the blue Midgard Thane and Bonedancer animations they borrowed. Thanes and Bonedancers still
  look blue. Cosmetic only.
- The zombie priest is now the **ghastly healer**: a floating ghost on the badh's skeleton, with
  its own casting animation and a dagger instead of a staff. It has its own reshaped mesh (a
  longer ragged ghost tail, gaunter waist, longer hair, a broken circlet and claw-like fingers),
  a corpse-pale grave-shroud skin and a ghostly wailing voice instead of a dwarf-female one. World
  badh monsters are unchanged.
  - Its summon spell is now "Raise Ghastly Healer", and its "Priest's Mending" is now "Ghastly
    Mending".
  - Its stats, spells and healing AI are unchanged.

- New level 50 reward: the **Dubh Sluagh** set, a black gothic Celtic set with grave-green
  accents. Muirenn gives it when you finish the level 50 Sluaghbinder epic quest.
  - Seven armor pieces: Cairnwarden's Helm (a full helm with glowing green eyes), Hauberk of the
    Dubh Sluagh, Vambraces of the Restless Host, Gauntlets of the Grave-Grip, Greaves of the
    Barrow Road, Sabatons of the Silent March, and the Mantle of the Sluagh Host (a midnight-black
    weathered leather cloak with a U-shaped Celtic border and fur trim).
  - Three weapons: **Cairnbreaker** (mace, 16.5 DPS, 3.7 speed, blackened iron with a ghost-green
    crystal), **Cairnfire Aegis** (large shield with a 3D bone skull over grave-green ghost fire)
    and **Reaper of the Host** (two-handed scythe, 16.5 DPS, 5.5 speed).
  - Level 51, quality 100. Each piece matches the stats of the best item for its slot.
    Sluaghbinder only.
  - The set cannot be traded or dyed (trying to dye it tells you why), sells for 1 copper, is in
    no loot table, and bots never wear it.
  - Lost a piece? Ask Muirenn to **[reclaim the set]** any time after the quest. She replaces
    every piece you no longer have.

**Animist bots (gamebots and companions)**
- Animists use their shrooms by what they do, the way the class was played, instead of picking
  the highest-level shroom spell at random:
  - The permanent shroom and one damage shroom open the fight.
  - Each resist Vent is kept up once (both in a group, one solo). Vents never count against
    damage shrooms, and no duplicate Vents are planted.
  - Then the highest-rank damage shroom is added for as long as the monsters still need it, so
    a weak monster gets one or two and a tough one or a pack gets many more.
  - The old limit of three temporary shrooms and the extra 6.5-second wait are gone. The bot
    keeps 20% power in reserve, and the server's normal shroom limits still apply.
- Every spec plays to its strengths:
  - Verdant uses its taunting permanent shroom with Briar bursts and Ligneous ablatives.
  - Creeping adds a Spore in group fights where someone is being hit in melee.
  - Every spec heals its permanent shroom.
  - Between fights, a permanent shroom of the wrong kind or an outgrown rank is replaced.
- Animists stand inside shroom range (850) instead of nuke range (1,500) and plant every
  shroom, the permanent one included, beside themselves instead of next to the enemy. If the
  monster moves out of reach they walk up first instead of planting.
- Damage shrooms stop at two in crowded camps, and bursts and Spores wait until no other
  monster would be hit, so shrooms pull fewer extra monsters.
- Companion Animists keep planting while you move around during a fight.

**Charm pets**
- The charm creature menu (Sorcerer, Minstrel, Mentalist and Hunter) tags every choice with how it
  fights: **Caster** (casts an attack spell from range), **Archer** (shoots a bow) or **Melee**.
  The few creatures whose spells or gear are rolled at random show both, for example "Archer or
  Melee".
- Creatures that cannot move are no longer offered as charm pets, to players or to gamebots: the
  Darkness Falls clinging soul, the gurite and siabra lookouts, and the target and training dummies.
  They used to sit where they were summoned and never follow or chase. They are unchanged in the
  world.

**Shrouded Isles reputation**
- New repeatable reputation quest in each realm, from a faction emissary:
  - Hibernia: **Kzzirrak** `<Krrzck Emissary>`, a mantid beside Zrrazk inside Necht.
  - Albion: **Ysslith** `<Cryptos Mythicos Emissary>`, a lammia beside Vilmalin at Caer Diogel.
  - Midgard: **Hrodvar Deepvow** `<The Remnants Emissary>`, an iarn dwarf beside Korlis in Hagall.
- Kill 10 of a common enemy of that faction, chosen near your level without going over and never
  above level 45. A red dot marks the hunting ground on that zone's map, and the journal tracks
  your kills.
- Each turn-in gives +10 reputation with the faction. As with bounties, you can reroll for a
  different target, and that hunt then gives +5. The hunted monsters' own faction likes you 10
  less, but only if it already attacked on sight; friendly factions are never touched.
- Reputation runs from -100 to +100, and everyone starts at -100. The faction's stable masters
  serve you from -50, and its town guards stop attacking on sight above -75.
- The emissaries say which stable masters check reputation: Zrrazk, Dalniver and Calvine in
  Hibernia; Nimea and Callisa in Albion; Minerva in Midgard. Korlis and Vilmalin serve everyone
  of their realm.
- A faction stable master that turns you away now tells you your reputation, the -50 you need,
  and which emissary to see.
- Bounty quests never send you after monsters whose death would cost reputation with one of these
  three factions or their allies. For example, Hibernian bounties no longer pick mantids or ashen
  treants.

**Bonedancer pets**
- The commander's shield now shows. The game was using a model that is a campfire in the client,
  so one-handed commanders looked empty-handed on that side. They now carry the skull shield the
  Necromancer's reanimated servant uses. Two-handed commanders still have an empty off hand.
- Every bone debuffer rank (Bonebreaker to Bonehexer, Darkness line) now wields the commander's
  two-handed bone mace instead of a one-handed mace. It swings slower and hits harder, with the
  same damage per second as before. A debuffer already out keeps its old weapon until it is
  summoned again.

**World**
- The Cliffs of Moher phaeghoul that spawned inside a dead tree now spawns in open ground nearby.
- Leptus in Domnann is always level 6. Its template randomly rolled level 6 or level 51, which put
  a roaming level 51 monster among the starter creatures.
- The level 6-7 venomous spore seeds in Cothrom Gorge are removed. They sat inside the level
  42-55 venomous spore field and drew low-level bots across the Shrouded Isles to die there. The
  level 48-55 venomous spores are unchanged.
- The Celtic scale helm and the five other helmets on the same mesh (for example Animalbound
  Osnadur Tha Coif) are visible again with extension 2. They used to hide the whole head.
- The Norse leather cap (for example the rawhide starklaedar cap) and the ten other helmets on the
  same mesh are visible with extension 2. The cap was invisible on a Shaman companion.
- The tendrils in Aegir's Landing, and the chokers, shacklers and the Throttler in Dales of Devwy,
  are no longer invisible.
  - They use the game's invisible model, so they used to show only a floating name.
  - Like the stranglers next to them, they now show a Tangling Vines effect. Their stats, levels
    and loot are unchanged.
  - Also like the stranglers, they are no longer offered as bounty, reputation-hunt or charm
    targets.
- The Realm Exchange NPCs in Jordheim and Camelot have moved:
  - Jordheim's (for example Brynhild; each install picks the names) is back on the small ledge by
    the Name Registrar. It had ended up in the narrow hallway by the vault keeper, where bots
    traded with it through a wall.
  - Camelot's (for example Adalyn) now stands in the open courtyard in front of the benches,
    instead of on the crowded vault-keeper platform.
  - Each has a guard on either side, as before. The Tir na Nog exchange is unchanged.
- Bots pick a trading spot with a clear line to the Realm Exchange NPC, so they no longer trade
  through a pillar or wall. If there is no such spot, they choose one the old way. Both new spots
  were checked on the real city maps from every gate, and the nearby merchants stay reachable.
- The fire trap in Amminus Pilus's hall in the Catacombs of Cardova (Pilus'Fury, the smoke that
  burns anyone standing on the hot spots) no longer breaks when it burns a gamebot Necromancer's
  pet. It expected every Necromancer pet to belong to a player, crashed on a bot's pet, and the
  server removed the trap until Amminus Pilus respawned, so the hall often had no trap at all. It
  burns the same spots for the same damage as before.

**For modders: the database and client scripts**

All of the above is in the 0.34 / 0.34b download, so players don't need these. They are the
scripts that made the database and client changes, kept so modders can see how each change was
made or apply it to their own copy. Each one previews its change first and can be undone. Close the
game, launcher and server before running one.

| Script | What it changes |
|---|---|
| `tools/claude-version/add_moher_spectre_sentinel_spawns.py --apply` | The extra Cliffs of Moher bantam spectres and koalinth sentinels |
| `tools/claude-version/fix_moher_phaeghoul_tree.py --apply` | Moves the Cliffs of Moher phaeghoul out of the dead tree |
| `tools/claude-version/fix_cothrom_seeds_and_leptus.py --apply` | Leptus always level 6; removes the Cothrom Gorge spore seeds |
| `tools/claude-version/vine_monsters_strangler_effect.py --apply` | Makes the vine monsters visible (`--undo` reverts) |
| `tools/claude-version/move_realm_exchange_npcs.py --apply` | The new Jordheim and Camelot Realm Exchange spots (`--undo` reverts) |
| `tools/pet-art/install_bulwark_update.py install` | Sluaghbinder Bulwark taunts and the Barrow Deflection parry buff |
| `tools/claude-version/install_bane_drain_update.py install` | Sluaghbinder drain animation, shared Bane recast, Grave Rot and the Bane cloud |
| `tools/pet-art/install_cairn_green_buff.py install` | The green Cairn strength-buff visuals |
| `tools/claude-version/install_ghastly_healer.py install`, then `tools/pet-art/build_ghastly_healer.py` and `tools/pet-art/install_ghastly_healer_art.py install` | The ghastly healer's name, spells and private model |
| `extract_meshes.py`, `paint_set.py`, `shield_build.py`, `mace_build.py` and `weapons_build.py scythe` in `tools/pet-art/work/sluagh-armor`, then `tools/pet-art/install_sluagh_armor.py install` | The Dubh Sluagh set's art (`plan` previews it) |

The ghastly healer and Dubh Sluagh scripts need the 0.34 server, because the 0.33 server doesn't
know these changes.

## 0.33 / 0.33b — 2026-09-30

The full notes are in [docs/history/RELEASE-0.33.md](docs/history/RELEASE-0.33.md). In short:

**Download and setup**
- One complete download, about 5 GB in checked parts, with two editions:
  - **0.33b** includes the Sluaghbinder.
  - **0.33** has the classic class list only.
- Every install gets its own account on the first ENTER REALM, an empty world for its own bots,
  default launcher settings and its own client settings profile.
- The .NET runtime is bundled. Only the Windows .NET Framework 3.5 feature is still needed.
- A new progress transfer tool moves an account, characters, items, money, houses and bots from
  v0.3 through v0.32b and the "new class test" builds.

**Bots**
- Much less lag with thousands of bots: faster database access, fewer repeated route searches,
  and heavy scans moved off the main game loop.
- Stuck pulls are retried closer, then abandoned, for every class. Stuck casts are cleared.
- Resting casters and archers walk up to far or wandering targets.
- Archers that stall at close range switch to melee briefly.
- Bots never mesmerize their own kill target.
- Savages can use claws in their main hand, and they cast their health-costing buffs only at the
  target.
- Level 0 creatures are real targets. New bots use starter camps near home with checked routes.
- The stuck-bot watchdog counts earned XP as progress.
- Groups:
  - They prefer 8 bots but can form with 5–7, and smaller groups pick easier targets.
  - Bots waiting for a group grind outdoors.
- Darkness Falls: solo bots need level 25 and blue-or-easier targets, and groups inside stay
  inside.
- Nearly full bags are emptied at natural task breaks, never mid-grind.
- Camps that several bots can't route to are benched for a while.

**Sluaghbinder (0.33b)**
- New or refreshed art for the sturdy zombie, zombie magician, zombie priest, walking dead, zombie
  guardian and Dullahan.
- The zombie magician has a void blast attack, its own casting gestures and fixed melee
  animations. Its robe is lower-res with muted markings, and its shoulder spikes are folded in.
- The zombie priest heals party members and pets properly, for both player and bot owners.
- Pet health is rebalanced; damage is unchanged.
- Cairn armor buffs show a black and blood-red shield instead of the holy halo.
- Summons cast in 10 seconds (was 20) and show green hand glows.
- Every Sluaghbinder buff shows its own tooltip.
- The zombie guardian carries a unique rusted mace and tower shield.

**Everyone**
- The Necromancer's level 20 Necroservant carries a unique blackened bone hammer and grave shield.
- Faces show under every variant of the Hibernian "Helm 3" helmets, 15 models.

## 0.32 / 0.32b "Darkness Falls Beta" — 2026-09-25 to 2026-09-28

- **Darkness Falls opened** to all three realms. Bots grind its ordinary camps on staged,
  floor-aware routes and use their own realm's exits. Raid AI and the hardest encounters were not
  included.
- **Maintenance updates fixed:**
  - bot travel, groups, meetups and recovery
  - dungeon goals and Savage outdoor pulls
  - backpack selling
  - the display of the model 840 scale coif
  - the classic-side Shrouded Isles portal visuals
- **Bounty Masters** stopped repeating the last hunted monster.
- **0.32b** added the optional Sluaghbinder on top of 0.32, as a separate copy.

## 0.31 / 0.31b — 2026-09-20 to 2026-09-26

- **0.31 maintenance fixed:**
  - pet scaling
  - companion spell power and healing
  - the Isle of Glass dragonfly camp and similar route traps
  - Bonedancer helper upkeep
  - a world-loop freeze
- **Later additions:**
  - repeatable Bounty Masters in Cotswold, Mularn and Mag Mell
  - Bard combat and companion song fixes
  - a Shannon Estuary beach-rat camp
  - shared bot progression and route repairs
- **0.31b** introduced the optional Hibernian **Sluaghbinder** class:
  - its Acolyte-to-level-5 promotion
  - three core and three trainable lines
  - seven pets
  - Muirenn, the trainer in Tir na Nog
  - five chained epic quests
  - full companion and gamebot support

## 0.3 — 2026-09-15

The first public single-player release: Classic + Shrouded Isles on 1.65 rules, with autonomous
gamebots, recruitable companion bots, raids, realm events, a launcher, navigation meshes and
development tools.

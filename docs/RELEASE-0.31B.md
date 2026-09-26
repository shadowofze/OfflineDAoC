# Offline DAoC v0.31b — optional Sluaghbinder expansion

## Bard maintenance correction

The refreshed optional patch prevents a solo Bard gamebot from trying
group-only PvE add mez when no party exists. Grouped add control and PvP
mez are unchanged. This does not alter Sluaghbinder, monster data, or the
legacy v0.31b release asset.

## Latest shared Bard and Shannon camp repair

Both maintained play paths now stop Bard companions and gamebots from
repeatedly mesmerizing their ordinary PvE attack target. Bards continue
fighting, remain secondary healers, and may use mez on a separate full-health
add attacking the group. Companion Bards, Skalds, and Minstrels finish normal
buffs before stationary song twisting. Traveling companion Bards favor
speed/endurance; Skalds and Minstrels favor speed/health regeneration.
Autonomous song scheduling and non-Bard combat are unchanged.

The Shannon Estuary beach-rat camp gains ten spaced level-1/2 rats, and its
original rat becomes level 1. This optional installer applies the same narrow
world patch to its copied database; its existing pre-patch backup and rollback
restore the copy if the expansion is removed. The original selected game is
untouched. Older v0.31b assets and the preserved v0.3 release stay available.

## September 25 Bounty Masters

The current optional patch includes the realm-wide Bounty Master system also
present in maintained normal v0.31. Maelin Greenmantle waits in Mag Mell,
Dame Elowen Vale in Cotswold Village, and Yrsa Wolfmark in Mularn. Each gives
one repeatable hunt: a yellow-con monster and increasing kill count below
level 50, or a named major foe at level 50. The quest journal tracks progress.
Ordinary hunts grant two XP bulbs measured at the assigned level and scaled
by the server XP rate, plus 1–3 class items. Level-50 hunts grant 100 gold
and 1–3 high-utility class items instead. Rerolling a normal target is
unlimited but cuts its XP in half; refreshing an outleveled assignment is free.

The journal's **BOUNTY MAP** button opens the current map, where a red target
dot is visible only inside the assigned zone or dungeon. `/bountylocation`
refreshes it and explains that limitation. The old v0.31b assets remain
downloadable, but the latest manifest-selected patch includes the system.
The preserved v0.3 release does not gain these later changes automatically.

## September 23 shared bot maintenance

The latest optional patch includes the same class-neutral bot repairs as the
maintained normal v0.31 release: Savage camp/melee progress, viable reduced
PvE groups and local resurrection, post-entry dungeon blocker handling,
bounded Salisbury spirit and solo repeat-death recovery, and Mularn/stable
boarding corrections. No Sluaghbinder pet, spell, quest, or class rules were
changed by this maintenance pass. The older patch assets remain available as
historical snapshots; use the latest manifest-selected patch for a new install.

The refreshed optional source built with zero errors and passed 1,911/1,911
server tests at that point. The current Bard-guard maintenance source passes
1,981/1,981 server tests. No post-fix overnight live run has been observed yet.

`v0.31b` is an optional overlay on the normal v0.3/v0.31 playable release. It
does not replace either baseline and is not required for ordinary Classic +
Shrouded Isles play.

## If you only want to play

Use `DOWNLOAD AND PLAY v0.31b.cmd` from the repository copy, or download the
latest `Sluaghbinder-v0.31b-bard-guard-patch.zip` release asset and run
`INSTALL SLAUGHBINDER PATCH.cmd`. Select your clean v0.3 or v0.31 folder when asked. The installer
creates a new sibling folder ending in `-Sluaghbinder-v0.31b`; it never writes
back to the folder you selected. Start the new copy with its normal
`START OFFLINE DAOC.cmd`. If the optional class is not wanted, use the ordinary
v0.3/v0.31 downloader instead.

The patch keeps the selected copy's local account, characters, inventory,
settings, and bots. It writes a consistent pre-patch database backup and a
`ROLLBACK SLAUGHBINDER PATCH.cmd` command in the new copy. Stop the server and
game before rollback. The rollback restores only that patched copy; it does not
remove or alter the original.

Sluaghbinder is Hibernian and uses Celt or Firbolg. New characters begin as
Acolytes and are promoted by Muirenn at level 5. Core spells arrive with level;
the three optional paths are trained. The five epic quests are consecutive and
unlock the Epic Spells page and its stationary, player-only skeletal services.
Quest objectives and locations are intentionally discovered in-game; this file
does not spoil their clues.

The included launcher also carries the v0.31 exchange-layout repair: Eilwen's
two Sentinel Exchange Guards are placed beside her in Tir na Nog. The repair is
limited to those two Hibernian guard rows and does not move Eilwen or any other
broker.

The companion `/spawn` path now commits each Sluaghbinder to one advanced
build at creation. Bulwark companions start with blunt and shield, Bane
companions start with a scythe, and Covenant companions randomly choose either
weapon plan while retaining their pet-focused spell and buff line. The normal
Sluaghbinder spell/style filtering is build-specific, so a companion receives
only its chosen path's taunts, scythe/life-steal/rot tools, or pet enhancements.
Persistent autonomous Sluaghbinders retain their existing deterministic build
choice; this repair only makes the starter and reconciliation equipment honor
that choice.

The optional pet visuals are aligned with the class design: the level-7 Sturdy
Zombie uses the former Zombie Magician model without a weapon, while the
level-12 Zombie Magician uses the existing Murkman model and keeps its staff
and spell behavior.

The current v0.31b optional patch also gives Zombie Defender a private
rusted-plate appearance and a 33% larger body, while retaining its previous
shield. Dullahan has a private dark armored appearance, a 50% larger body,
a chain morningstar with an existing dark weapon effect, and no offhand shield.
Zombie Priest now carries a dagger without a buckler. These are pet-specific
assets and equipment; ordinary monsters keep their original looks. Covenant
companions and autonomous bots wait for their active one-minute pet
heal-over-time to end before casting it again. Other heals, builds, and
classes are not changed by that upkeep fix.

For LLM developers editing the pet art, see
[`LLM-SLUAGHBINDER-PET-TEXTURES.md`](LLM-SLUAGHBINDER-PET-TEXTURES.md).
The maintained source for this optional update is the
`release/v0.31b-sluaghbinder` branch and the explicit updated-source ZIP on
the v0.31b release. The original v0.31b tag remains a record of the initial
release; its automatic GitHub source snapshot does not include later fixes.

## Maintenance fixes included in the current 0.31b source

The optional source carries the same narrow camp, PvE recovery, Reaver Flexible
equipment, and stale service-assignment fixes as normal v0.31. They do not alter
Sluaghbinder rules, player-only skeletal services, realm-event groups, or the
preserved v0.3 baseline.

## If you want to develop with an LLM

Fork the repository and give the fork and `AGENTS.md` to the LLM. The complete
Sluaghbinder source is under `source/server`, the client-build-identifying
patch helpers are under `source/server/tools`, and the static overlay builder
is under `tools/sluaghbinder/build_overlay.py`. The patch executable source is
`source/tools/OfflineDaoc.SluaghbinderPatch`. Build the server and launcher in
the normal Release configuration, then regenerate the static overlay from a
disposable database. Never commit a runtime database, account file, logs,
backups, or private bot records.

The public launcher in this tag displays **0.31b**. The private 0.4 launcher is
not used or published here.

# Offline DAoC v0.32 — Darkness Falls Beta

This is the current **normal** Classic + Shrouded Isles release. It includes
Darkness Falls and the shared fixes through v0.31, but no Sluaghbinder class,
quests, or optional client assets. Choose the separate
[v0.32b Darkness Falls Beta](RELEASE-0.32B.md) only if you want Sluaghbinder.
The beta label stays until the owner has had time to test the bots in the
dungeon more thoroughly.

## Darkness Falls implementation

- Albion, Midgard, and Hibernia can all enter in this offline setup. Bots
  respect the actual entrance and region edges instead of teleporting through
  a closed portal. All three realms can be present in the dungeon at once.
- Autonomous bots can select ordinary, reachable dungeon monsters for solo or
  group grinding. Their routes stage parties at the entrance, use the installed
  walkable navigation mesh, check the floor and corridor before pulling, and
  fall back from a failed camp. Ordinary combat, recovery, XP, loot, and group
  defense still use the game's shared systems.
- The routes handle one-way descents and ledges as one-way movement. Bots leave
  by a valid exit for their own realm rather than trying to climb back up a
  drop. The central shared area allows opposing-realm bots to engage one
  another under the server's PvP rules.
- Players can use the existing seal vendors. Automated bot purchases of
  Darkness Falls gear are not claimed for this beta.

**Darkness Falls raid AI is not implemented.** Legion, the hardest level-70+
encounters, unreachable flying targets, and any route or monster that has not
been sufficiently verified are excluded from ordinary autonomous bot goals.
This is staged support for grinding and cross-realm activity, not a claim of
full dungeon completion. The owner has not completed a long live bot run in
Darkness Falls, so route and combat behavior may still need corrections.
Existing non-Darkness Falls raid features are separate from this beta.

## Other current changes included

v0.32 also carries forward the shared improvements published after v0.3:
repeatable Bounty Masters and their map markers, `/stables` route listings,
solo Bard PvE combat and group mez restraint, Bard/Skald/Minstrel companion
buff and travel song priorities, Shannon Estuary beach rats, and the Savage,
PvE party, dungeon corridor, death-recovery, and stable-route repairs. See the
[full changelog](../CHANGELOG.md) and preserved
[v0.31 notes](RELEASE-0.31.md) for their detail. Those changes are included
in this download; they are not claimed as new Darkness Falls mechanics.

## Download and upgrade

On the [v0.32 release](https://github.com/shadowofze/OfflineDAoC/releases/tag/v0.32),
download `DOWNLOAD-AND-PLAY-v0.32.cmd` and `Get-OfflineDAoC.ps1` into one new
folder and double-click the helper. It obtains and verifies the preserved
v0.31 base and `OfflineDAoC-v0.32-darkness-falls-beta-update.zip`, then builds
a **new** v0.32 playable folder. It keeps the v0.31 sibling available. Read
`READ ME FIRST.txt` in the new game folder and use `START OFFLINE DAOC.cmd`.
No Git or LLM is needed. [Detailed player instructions](PLAY.md) cover the
Windows requirements, first launch, and optional save import.

Keep an older installation intact until you have checked the new one. Stop
both servers before importing progress. The importer replaces progress in the
new copy; it does not merge two saves. To roll back, stop the new server and
launch the untouched older folder. Do not manually replace the new clean world
database with an old save database.

`Code > Download ZIP` and the matching source ZIP are editable source, not a
complete playable download. The normal source is on
[`release/v0.32-darkness-falls`](https://github.com/shadowofze/OfflineDAoC/tree/release/v0.32-darkness-falls).
Keep it paired with the v0.32 runtime. The v0.3, v0.31, and v0.31b releases
remain available as legacy versions.

## Verification

[Verification notes](VERIFICATION-0.32.md) distinguish source and package
checks from real client gameplay. The beta label and the dungeon limits above
remain in force even when automated checks pass.

# Offline DAoC v0.32b — Darkness Falls Beta with Sluaghbinder

This is the optional Hibernian Sluaghbinder version of the
[normal v0.32 Darkness Falls Beta](RELEASE-0.32.md). It carries the same
Classic + Shrouded Isles, Darkness Falls, Bounty Master, Bard/companion,
and bot-route updates, then adds the class. Choose normal v0.32 if you do
not want Sluaghbinder. The v0.3, v0.31, and v0.31b releases remain legacy
downloads.

## Darkness Falls Beta limits

Albion, Midgard, and Hibernia can enter the dungeon. Autonomous bots are
intended to grind ordinary reachable monsters, use staged floor-aware paths,
take their own realm exits after one-way descents, and engage opposing-realm
bots near the shared center. Players can use existing seal vendors. The owner
has not completed a long live bot test there. **Darkness Falls raid AI is not
implemented.** Legion, the hardest level-70+ encounters, unreachable flying
targets, and unverified routes or monsters are excluded from ordinary bot
goals. The [normal release notes](RELEASE-0.32.md)
explain this implementation in detail; the optional class does not remove
these beta limits.

## Optional class included

Sluaghbinder is a Hibernian player class. New characters start as Acolytes
and promote at level 5. Its three core lines and three advanced paths provide
distinct pets and abilities; Muirenn in Tir na Nog trains the class. The
five linked epic quests unlock player-only Epic Spells services. Companion
and autonomous Sluaghbinder bots use class-specific builds and pet behavior.
The private Dullahan and Zombie Defender visuals, Zombie Priest equipment,
and Covenant pet heal timing from v0.31b remain part of the optional version.
See the [legacy v0.31b notes](RELEASE-0.31B.md) for the original class detail.

## Download, source, and rollback

From the [v0.32b release](https://github.com/shadowofze/OfflineDAoC/releases/tag/v0.32b),
download `DOWNLOAD-AND-PLAY-v0.32b.cmd` and `Get-OfflineDAoC.ps1` into one new
folder. Double-click the helper. It verifies and assembles the normal v0.32
game, then applies the current v0.32b patch ZIP linked on that release page
in a new sibling copy. The normal v0.32 base stays intact. If you already have a
clean v0.32 folder, use the patch archive's included installer and its
instructions. The optional copy receives a rollback command; keep the base
and earlier versions until you have checked the new installation. Stop both
servers before importing saves or applying the patch.

The editable optional source belongs to
[`release/v0.32b-sluaghbinder-darkness-falls`](https://github.com/shadowofze/OfflineDAoC/tree/release/v0.32b-sluaghbinder-darkness-falls).
The normal v0.32 source belongs to its separate
[`release/v0.32-darkness-falls`](https://github.com/shadowofze/OfflineDAoC/tree/release/v0.32-darkness-falls)
branch. The source ZIP and GitHub's **Code > Download ZIP** are not complete
playable downloads. Use the matching source and runtime version when making
LLM changes. [Player instructions](PLAY.md) and [LLM instructions](LLM-QUICKSTART.md)
give the full setup steps.

## Verification

[v0.32b verification notes](VERIFICATION-0.32B.md) separate isolated source
tests from release-package and real-client checks. Beta does not imply the
bots have passed a long live Darkness Falls test.

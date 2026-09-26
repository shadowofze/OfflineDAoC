# Offline DAoC: start here

This project contains AI-developed customizations of existing DAoC server projects.
Read README.md, docs/DEVELOPMENT.md, and the relevant component's AGENTS.md before editing.

- Resolve paths from this checkout, never from the original author's Windows username.
- Distinguish real players, companion bots, and autonomous gamebots before changing AI.
- Preserve saves, real inventories/loot/coins, equipment upgrades, realm exchange,
  and existing travel behavior unless the owner explicitly asks to change them.
- Historical scripts in source/server/tools may contain absolute deployment paths
  and destructive migration operations. Read and parameterize them before use.
  Do not execute them just because they are present. They are not the bootstrap.
- Never start a game server or deploy over a running installation without permission.
- Build/test in a separate output tree. Never publish a live SQLite database,
  account.txt, bot profiles, credentials, logs, dumps, or previous Git history.
- Keep native client patch hash guards. Never apply a binary patch to an unverified
  client build. Texture atlases are not mesh/skeleton replacements.
- Do not globally rebuild/replace navigation to fix one local route without evidence.
- Report offline/static checks separately from real-client gameplay verification.
- The public v0.32 and v0.32b Darkness Falls releases are beta. Keep the
  normal v0.32 source and package free of Sluaghbinder; put the optional class
  only in v0.32b. Preserve v0.3, v0.31, and v0.31b as legacy releases.
- Darkness Falls ordinary grinding, access, player seal-vendor use, and local
  PvP are separate from raid support. Do not describe Darkness Falls raid AI,
  Legion, the hardest level-70+ encounters, unreachable flying targets, bot
  vendor purchases, or long-running bot behavior as implemented or verified.
- The user may customize their fork's rules. These are safety defaults, not a ban
  on intentional gameplay changes requested by the fork owner.

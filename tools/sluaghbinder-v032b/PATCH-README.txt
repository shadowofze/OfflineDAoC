Offline DAoC v0.32b - Darkness Falls Beta with optional Sluaghbinder

PLAYER QUICK START

1. Close the launcher, server, and game.
2. Extract this entire patch ZIP into a new folder.
3. Run INSTALL SLAUGHBINDER PATCH.cmd and select a normal v0.32 game folder.
4. The installer checks that base against the final v0.32 launcher/server
   hashes, then creates a separate -Sluaghbinder-v0.32b sibling folder.
5. Start that new folder with START OFFLINE DAOC.cmd.

If installation fails, the installer removes only its new incomplete sibling
copy. If Windows prevents cleanup, do not launch that incomplete folder; the
error will tell you its exact path. The original v0.32 folder stays intact.

The original normal v0.32 folder is not modified. Do not select a v0.3,
v0.31, v0.31b, or already-patched folder. The optional copy includes the
Sluaghbinder class, pets, trainer, quests, and private pet textures in addition
to v0.32's normal shared changes. A player can use the existing Darkness Falls
seal vendors; automated bot vendor purchases are not claimed.

Darkness Falls raid AI is not implemented. Legion, hardest level-70+ bosses,
unreachable flying targets, and unverified routes are excluded from ordinary
autonomous bot goals. The owner has not completed a long live bot test there,
so this release is labeled Beta.

ROLLBACK

Close the server, launcher, and game, then run ROLLBACK SLAUGHBINDER PATCH.cmd
inside the optional copy. It restores that copy's prepatch files and database.
This also replaces any progress made in the optional copy since installation;
back up your current account and save first if you need to preserve them.
The original normal v0.32 folder is unaffected.

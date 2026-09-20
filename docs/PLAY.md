# Download and play — no Git knowledge needed

## Before launching: enable .NET Framework 3.5

1. Open **Turn Windows features on or off** from the Windows Start menu.
2. Check **.NET Framework 3.5 (includes .NET 2.0 and 3.0)**.
3. Click **OK** and let Windows install the required files.
4. Restart the computer if Windows asks.

The bundled modern .NET runtime does not replace this legacy connector requirement.

## Get the GitHub download

Choose one path before downloading. Both paths are ordinary point-and-click
play; neither requires Git, an LLM, or programming knowledge.

- **Normal play (no Sluaghbinder):** choose **v0.31** for the current public
  maintenance build, or **v0.3** for the preserved original public baseline.
  Use that release's `DOWNLOAD AND PLAY.cmd` helper.
- **Optional Sluaghbinder play:** choose **v0.31b** and use the
  `DOWNLOAD AND PLAY v0.31b.cmd` helper. This installs the class into a new
  sibling copy and leaves the normal v0.31 game available separately.

1. Open the project's **Releases** page and download the helper for the path you
   chose plus `Get-OfflineDAoC.ps1` from the selected tag into the **same new
   folder**. For normal play this is `DOWNLOAD AND PLAY.cmd`; for Sluaghbinder
   play it is `DOWNLOAD AND PLAY v0.31b.cmd`.
2. Double-click that helper. The normal helper downloads the preserved v0.3 seed,
   checks its hashes, applies the small v0.31 update, and creates `playable`.
   The v0.31b helper also downloads v0.31, verifies the optional class patch, and
   creates `playable-v0.31b`. Allow roughly 35 GB of free disk space for the
   downloads, extracted game, and working room.
3. Open the new playable folder, read **READ ME FIRST.txt**, and run
   **START OFFLINE DAOC.cmd**.
4. In the launcher, click **START SERVER** and wait until it reports **RUNNING**.
   The launcher refreshes automatically when **ENTER REALM** becomes available;
   this can take several seconds. Click **ENTER REALM** and let loading finish.
5. Do **not** launch CoreServer.exe, connect.exe, or OpenDAoC separately. Do not
   manually enter an account or register: the game creates and uses its local account.
6. Choose a realm, create a character, select it, and enter the realm. Characters
   and progress save inside your extracted game folder.
7. This version starts with an empty bot roster. Use the launcher's **+ Lv.1** or
   **+ Lv.50** buttons under each faction to generate playerbots.

For the optional path, double-click **DOWNLOAD AND PLAY v0.31b.cmd** instead of
`DOWNLOAD AND PLAY.cmd`. It downloads the normal v0.31 baseline first, verifies
the Sluaghbinder patch, and creates `playable-v0.31b` with the optional class.
If you already have a clean v0.3/v0.31 folder, use `Sluaghbinder-v0.31b-patch.zip`
and `INSTALL SLAUGHBINDER PATCH.cmd`; the installer makes a separate copy and
places `ROLLBACK SLAUGHBINDER PATCH.cmd` inside it. If you do not want the class,
never run that optional helper or installer and simply play the normal folder.

The downloader verifies and extracts the v0.3 seed, then applies the verified v0.31
update without overwriting an existing destination. If you handle an archive
yourself, extract the **entire** archive into a normal folder; never run files from
inside a ZIP. Always keep older installations in separate folders.

The v0.31 GitHub launcher says **0.31**. The optional v0.31b launcher says
**0.31b**. The old v0.3 launcher and release still say **0.3**. The author's
private launcher label is separate from all public release numbers.

## Optional Sluaghbinder play path

To play the normal game without the class, follow the steps above and start the
v0.3 or v0.31 folder. To add Sluaghbinder, download `Sluaghbinder-v0.31b-patch.zip`
from the v0.31b release, extract it into a new temporary folder, and double-click
`INSTALL SLAUGHBINDER PATCH.cmd`. Choose the clean v0.3 or v0.31 folder. The
patcher creates a new `-Sluaghbinder-v0.31b` sibling copy, keeps local progress,
and writes a rollback command into that new copy. It never modifies the selected
base folder. Sluaghbinder is Hibernian; new characters begin as Acolytes and
follow the trainer's level-5 promotion path. The five epic quests and their
locations are discovered through in-game clues, so the public play guide does
not spoil them.

## First-time requirements

- A compatible 64-bit Windows PC with enough memory and a working graphics driver.
- CPUs without AVX2 support will not work.
- 16 GB RAM recommended minimum. 8 GB may work but is untested.
- The legacy connector requires the **.NET Framework 3.5** Windows feature. The
  package does not enable Windows features automatically. If required, enable it
  through Windows Features, as described in READ ME FIRST.txt.
- Initial downloading requires internet. The game itself runs against your local
  server; the bundled development dependencies also support offline work.
- Start with a small bot population and increase it gradually for your hardware.
  There is no honest guarantee that thousands of bots work on every PC.

The script is readable source. Its execution-policy option affects only its one
PowerShell process, not the machine's policy. If security software raises an alert,
do not disable antivirus: inspect/report the warning and verify the download.

## Where are the bots?

The release contains **no saved bots from the author**. Create your own using the
realm cards' **+ Lv.1** / **+ Lv.50** controls, and configure **Active Population**.
Hold **Ctrl** while clicking to create **100 bots**, or **Shift** for **10 bots**.
Without either key, the button creates one. For companions, `/spawn` opens the in-game picker. Level-50 characters can use
`/raid 40` or `/raid 80` (the total includes your character). See
**ALL SERVER COMMANDS.txt** for requirements and GM-only commands.

## Saves and old versions

Your accounts, characters, inventories and bot progress stay in your own
`runtime/data/opendaoc.sqlite3.db`; your login credentials stay in `runtime/account.txt`.
Back up both while the server and launcher are stopped. Never upload either file.
See the optional transfer procedure below. Do not replace the new world database
manually with an old database.

### Optional: transfer progress from an older version

Skip this if you want to start fresh. You can transfer before your first launch.

1. Keep your old folder intact. Extract/download the new version into a different folder.
2. Stop the server and close the game and launcher for **both** versions.
3. Inside the **NEW game folder**, double-click **IMPORT PROGRESS FROM OLD OFFLINE DAOC.cmd**.
4. Click **Choose OLD folder...** and select your old portable Offline DAoC folder.
5. Check the displayed account, character, bot, and inventory counts.
6. Click **IMPORT PROGRESS** and confirm. This **replaces existing progress in the
   NEW version**; it does not combine two saves.
7. Wait for the completion message before closing the transfer window.
8. Launch the NEW version using **START OFFLINE DAOC.cmd**, start the server, and
   click **ENTER REALM**. Imported credentials are used automatically.

The importer makes a recovery backup. Keep your old installation until you have
checked your characters and progress in the new one.

Only run one local DAoC server at a time. Stop it normally and wait for saves to
finish before moving folders, importing progress, or installing a changed build.
There is no automatic update that overwrites somebody's installation or custom fork.

## Interrupted download / errors

Run the download again. Already verified parts in `.downloads` are reused; failed
parts are downloaded again. The destination must not already exist. If extraction
was interrupted, choose another new destination with the PowerShell script's
`-Destination` option; do not point it at an existing game or save folder.

GitHub's **Code > Download ZIP** button downloads editable source, not the large
playable release. Players should use **Releases**, as with many other GitHub projects.

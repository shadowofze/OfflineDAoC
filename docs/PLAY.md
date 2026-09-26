# Download and play — no Git knowledge needed

## Before launching: enable .NET Framework 3.5

1. Open **Turn Windows features on or off** from the Windows Start menu.
2. Check **.NET Framework 3.5 (includes .NET 2.0 and 3.0)**.
3. Click **OK** and let Windows install the required files.
4. Restart the computer if Windows asks.

The bundled modern .NET runtime does not replace this legacy connector requirement.

## Get the GitHub download

Choose one beta path before downloading. Both are ordinary point-and-click
play; neither requires Git, an LLM, or programming knowledge.

- **Normal play:** choose [v0.32 Darkness Falls Beta](https://github.com/shadowofze/OfflineDAoC/releases/tag/v0.32)
  and `DOWNLOAD-AND-PLAY-v0.32.cmd`. This includes the current shared fixes
  and Darkness Falls, without Sluaghbinder.
- **Optional Sluaghbinder play:** choose [v0.32b Darkness Falls Beta](https://github.com/shadowofze/OfflineDAoC/releases/tag/v0.32b)
  and `DOWNLOAD-AND-PLAY-v0.32b.cmd`. It adds the class to a separate v0.32
  copy. The normal base stays available.

The [v0.3](https://github.com/shadowofze/OfflineDAoC/releases/tag/v0.3),
[v0.31](https://github.com/shadowofze/OfflineDAoC/releases/tag/v0.31), and
[v0.31b](https://github.com/shadowofze/OfflineDAoC/releases/tag/v0.31b)
downloads remain available as legacy versions.

1. On your chosen **Releases** page, download its named `.cmd` helper and
   `Get-OfflineDAoC.ps1` into the **same new folder**. Use files from the same
   release; an older helper may select an older version.
2. Double-click the helper. It downloads and hash-checks the required assets.
   The v0.32 path layers the new update over the preserved v0.31 game into a
   **new** playable folder and keeps the v0.31 sibling intact. The v0.32b path
   then installs Sluaghbinder in another **new** sibling copy. Allow generous
   disk space: roughly **40 GB free for normal v0.32** or **55 GB for optional
   v0.32b**, plus headroom for saves and future updates. No earlier copy is
   deleted automatically.
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

For the optional path, use **DOWNLOAD-AND-PLAY-v0.32b.cmd**. It assembles the
normal v0.32 game first, checks the optional patch, and makes the Sluaghbinder
copy. If you already have a clean v0.32 folder, use the current v0.32b patch
ZIP linked on that release page and its included install command, following
that archive's instructions. The patcher leaves the selected
base untouched and writes a rollback command into the optional copy. If you do
not want the class, simply play the normal v0.32 folder.

Never point an installer at an existing destination. If you handle an archive
yourself, extract the **entire** archive into a normal folder; never run files
from inside a ZIP. Keep earlier installations in separate folders.

The v0.32 launcher says **0.32** and the optional launcher says **0.32b**.
Legacy v0.3, v0.31, and v0.31b launchers keep their own version labels.

## Darkness Falls Beta

All three realms can enter Darkness Falls in this offline release. Autonomous
bots have staged dungeon paths for ordinary XP grinding, their own realm
exits, and opposing-realm fights near the shared center. Players can use the
existing seal vendors. Some descents are one-way, so plan a return via your
realm exit instead of retracing a ledge. The owner has not yet completed a
long live bot test there. Darkness Falls raid AI is not implemented; Legion,
the hardest level-70+ encounters, unreachable flying targets, and unverified
content are excluded from ordinary bot goals. See the
[v0.32 release notes](RELEASE-0.32.md) for the design and exact limits.

## Repeatable bounties (v0.31 onward)

Find the Bounty Master in Cotswold (Albion), Mularn (Midgard), or Mag Mell
(Hibernia). Accept one hunt, then use your quest journal to watch the kill count
and return to that master for the reward. Ordinary hunts count the named monster
in its assigned zone even when individual spawns have different levels.

Travel to the target's zone or dungeon before looking for the red map marker.
**BOUNTY MAP** in the journal opens your *current* map; it cannot show a distant
zone. Ask the master to **show location** or use `/bountylocation` for directions.
You may reroll a target as often as you like, but that contract pays half XP;
refreshing a contract you have outleveled has no penalty. Level-50 bounties
send you after major bosses for gear and gold rather than XP.

## Watch the demos

The [Sluaghbinder introduction](https://www.youtube.com/watch?v=EowrCcjigBY)
shows the optional class. The [v0.3 dragon raid demonstration](https://www.youtube.com/watch?v=zmh7YkajRx0)
shows the normal realm-event feature set. These are ordinary YouTube links; no
video files are downloaded as part of the game setup.

## Optional Sluaghbinder play path

To play the normal game without the class, start the v0.32 folder. To add
Sluaghbinder, use the v0.32b helper or patch above. The installer creates a
separate optional copy and leaves the selected normal base intact. Sluaghbinder
is Hibernian; new characters begin as Acolytes and
follow the trainer's level-5 promotion path. The five epic quests and their
locations are discovered through in-game clues, so the public play guide does
not spoil them. For an older install, follow the preserved
[v0.31b release instructions](RELEASE-0.31B.md).

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

1. Keep your old folder intact. Download the new version into a different folder.
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
checked your characters and progress in the new one. For a simple rollback,
stop both versions and launch the untouched older folder. If you applied the
v0.32b patch, use its rollback command only for the optional copy, then verify
your saves before continuing.

Only run one local DAoC server at a time. Stop it normally and wait for saves to
finish before moving folders, importing progress, or installing a changed build.
There is no automatic update that overwrites somebody's installation or custom fork.

## Interrupted download / errors

After a caught download or install error, run the helper again. Verified parts
in `.downloads` are reused, and any incomplete new copy is preserved under a
sibling `.failed-<id>` name for inspection. That folder can use substantial
disk space. If Windows or the machine stopped before the helper could handle
the error, rename the incomplete destination yourself or choose a different
new `-Destination`; never point the helper at an existing game or save folder.

GitHub's **Code > Download ZIP** button downloads editable source, not the large
playable release. Players should use **Releases**, as with many other GitHub projects.

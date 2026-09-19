# Development environment setup (WSL2 + Windows)

A tiered plan to go from this checkout to a verified build, test, and deploy
loop on the owner's machine. Execute **one tier at a time**, in order. Each
tier ends with a gate; stop and report if it fails. Do not start Camlann work
(`docs/CAMLANN.md`) until Tier 6 passes.

Read `AGENTS.md` and `docs/DEVELOPMENT.md` first. Their safety defaults apply.
Most importantly: never start the server, launcher, or client without the
owner's permission; never deploy over a running install; never commit or
publish a save, `account.txt`, backups, or logs.

## Machine facts (recorded 2026-09-19)

| Item | Value |
|---|---|
| Repo (WSL) | `/home/stefan/Development/Games/OfflineDAoC` (Ubuntu 24.04, WSL2) |
| Game install (Windows) | `D:\Games\OfflineDAoC` = `/mnt/d/Games/OfflineDAoC` (upstream v0.3 Complete Portable, 13 GB) |
| Save backups | `D:\Games\OfflineDAoC-backups\` (outside repo and install) |
| Dev state (Windows SDK caches, NuGet packages, scratch scripts) | `D:\Games\OfflineDAoC-dev\` |
| Original ZIP | `D:\Offline DAoC v0.3 - Complete Portable.zip` (6.7 GB, keep as pristine source) |
| Bundled Windows SDK | `D:\Games\OfflineDAoC\tools\dotnet\dotnet.exe`, SDK 10.0.400 |
| Bundled offline NuGet | `D:\Games\OfflineDAoC\tools\nuget-feed` via `D:\Games\OfflineDAoC\NuGet.Config` |
| WSL SDK | `~/.dotnet`, SDK 10.0.400 (per-user; PATH set in `~/.zshrc`) |
| System-wide Windows dotnet | runtime only, no SDK; do not rely on it |
| Current `GameType` | `Normal` (`runtime\server\config\serverconfig.xml`) |
| Save contents | 1 account, 3 characters, 151 autonomous bots; integrity ok |
| Disk | C: 95% full (do not put game or build output there); D: 73 GB free; E:/H: have lots of room |

Why not `C:\Games`: C: has about 25 GB free. The install alone is 13 GB, and
builds and backups grow. D: keeps the move an instant same-volume rename.

### Where things build and run

| Component | Target | Build/test where |
|---|---|---|
| Server (`source/server/Dawn of Light.sln`), server tests | `net10.0` | **WSL** (fast, Linux) |
| Launcher, launcher tests, ProgressImport, LauncherLayoutCheck | `net10.0-windows` | **Windows** bundled SDK (called from WSL via `.exe` interop) |
| Game server at runtime, launcher, client | Windows | `D:\Games\OfflineDAoC` only |

The server DLLs built on Linux are portable IL (`net10.0`), so they can be
deployed to the Windows runtime. Native pieces (SQLite interop, Detour
pathing) already ship in the runtime; the deploy must not replace them with
Linux builds.

---

## Tier 0 — Stop, back up, relocate  ✅ done 2026-09-19

Already executed; recorded so the state is reproducible.

1. Stopped `CoreServer.exe` (PID 21708, owner permission; it needed a forced
   stop after a normal termination request). No `-wal`/`-shm` left.
2. `PRAGMA integrity_check` on `runtime/data/opendaoc.sqlite3.db`: `ok`.
3. Backed up `runtime/account.txt`, `runtime/data/opendaoc.sqlite3.db`,
   `runtime/server/config/serverconfig.xml`, `runtime/server/bot-goals.json`,
   and `runtime/server/rvr-world.json` to
   `D:\Games\OfflineDAoC-backups\pre-move-20260919-150509\` with `SHA256SUMS`.
4. Moved `D:\Offline DAoC v0.3 - Complete Portable\Offline DAoC v0.3` to
   `D:\Games\OfflineDAoC` (same-volume rename), removed the empty parent, and
   verified the save hashes at the new path. No config referenced the old path.

**Gate:** ✅ The install lives at `D:\Games\OfflineDAoC`, and the backup
verifies.

---

## Tier 1 — Toolchains  (WSL SDK ✅, remaining checks open)

### Done

- .NET SDK 10.0.400 installed to `~/.dotnet` via Microsoft's
  `dotnet-install.sh` (no sudo). `~/.zshrc` exports `DOTNET_ROOT`, `PATH`,
  and `DOTNET_CLI_TELEMETRY_OPTOUT`.
- Verified: `dotnet --info` reports 10.0.400 on ubuntu 24.04 linux-x64, and
  a scratch `dotnet new console` + `dotnet run` printed output (ICU/SSL
  present, so no apt packages were needed).

### Steps

1. In a **new** shell: `dotnet --list-sdks` shows `10.0.400`. Non-interactive
   agent shells may not source `~/.zshrc`. Use `$HOME/.dotnet/dotnet` or export
   `PATH` in the command.
2. Windows SDK wrapper: create `tools/dev/winnet.sh` (committed, no absolute
   user paths baked in; install root from `OFFLINE_DAOC_ROOT`, default
   `/mnt/d/Games/OfflineDAoC`). It runs the bundled
   `tools/dotnet/dotnet.exe` with the same environment as
   `BUILD AND TEST SOURCE.cmd`: `DOTNET_ROOT`, `DOTNET_MULTILEVEL_LOOKUP=0`,
   `DOTNET_CLI_HOME` and `NUGET_PACKAGES` under `D:\Games\OfflineDAoC-dev\state`
   (not in the repo, not on C:), `DOTNET_CLI_TELEMETRY_OPTOUT=1`, and
   `DOTNET_GENERATE_ASPNET_CERTIFICATE=false`. A working hand-made version
   exists at `D:\Games\OfflineDAoC-dev\lt.cmd` (launcher tests); turn it into the
   committed wrapper.
   Paths passed to `dotnet.exe` must be Windows paths (`wslpath -w`).
3. `winnet.sh --list-sdks` prints `10.0.400`.
4. Windows prerequisite: .NET Framework 3.5 is already enabled (the game ran
   on this machine). Do not change Windows features.
5. Python: only needed for `tools/asset-tool` and the native patch builders.
   Use the bundled Python in the install when running those. Do not install
   anything extra now.

**Gate:** Both `dotnet --list-sdks` (WSL) and `tools/dev/winnet.sh
--list-sdks` report 10.0.400.

---

## Tier 2 — Baseline build and tests

Record the state **before** any change, so later failures are attributable.

### Server (WSL)

Build into an output tree outside the source dirs (`artifacts/` is already
git-ignored):

```bash
cd /home/stefan/Development/Games/OfflineDAoC
dotnet restore 'source/server/Dawn of Light.sln' -p:Configuration=Release
dotnet build 'source/server/Dawn of Light.sln' -c Release --no-restore \
  --artifacts-path artifacts/server
dotnet test source/server/Tests/Tests.csproj -c Release --no-build --no-restore \
  --artifacts-path artifacts/server \
  --logger "trx;LogFileName=baseline.trx" --results-directory artifacts/test-results
```

- Restore uses nuget.org online. If it must be offline, pass
  `--configfile` pointing at a copy of the install's `NuGet.Config` whose feed
  path is `/mnt/d/Games/OfflineDAoC/tools/nuget-feed`.
- If `--artifacts-path` breaks the solution's custom output layout (the
  projects may hard-code `Release\lib`), fall back to the default output and
  note which folders it wrote. Never write into `/mnt/d/Games/OfflineDAoC`.
- Some tests may be Windows-only (paths, `System.Data.SQLite` natives,
  `PerformanceCounter`). Record them as "platform skip/fail," do not fix them in
  this tier.

### Launcher (Windows SDK)

```bash
tools/dev/winnet.sh test "$(wslpath -w source/tools/OfflineDaoc.Launcher.Tests/OfflineDaoc.Launcher.Tests.csproj)" -c Release
```

Some launcher tests require that no DAoC server is listening locally (see
`docs/DEVELOPMENT.md`). Confirm nothing is running first.

Verified 2026-09-19 (commit for 0.4.3): **109/109 passed** with the bundled
SDK, restoring from the install's offline feed (`--configfile
D:\Games\OfflineDAoC\NuGet.Config`), with `DOTNET_CLI_HOME`/`NUGET_PACKAGES`
under `D:\Games\OfflineDAoC-dev\state`. Lessons:

- Do **not** pass `--artifacts-path` for the launcher tests. Two tests
  (`ExchangeDisplayTests`) read `MainForm.cs` relative to the default
  `bin/` layout and fail with `DirectoryNotFoundException` otherwise. Use the
  default (git-ignored) `bin/`/`obj/`.
- Set `DOTNET_GENERATE_ASPNET_CERTIFICATE=false` (as `BUILD AND TEST
  SOURCE.cmd` does). The first run without it installed an untrusted ASP.NET
  dev certificate in the owner's Windows user store; it is harmless and can
  be removed with `dotnet dev-certs https --clean`.

### Record

Add a "Baseline" section at the end of this file with: date, commit, server
passed/failed/skipped counts and the names of any failures, and launcher
counts. Do **not** fix failures here; list them.

**Gate:** Both suites have run to completion, and the results are recorded.
The server solution builds with 0 errors on WSL.

---

## Tier 3 — Line-ending guard

Current state (`git ls-files --eol`): 2549 CRLF, 351 LF, and 195 **mixed**
`.cs` files. `.editorconfig` asks for CRLF, but the tree is not uniform.
Forcing `eol=crlf` would rewrite about 550 files and bury real diffs.

### Steps

1. Add a root `.gitattributes` that **disables conversion** so Git never
   rewrites endings on either OS:

   ```gitattributes
   # Preserve line endings exactly as committed (tree is mixed; see docs/DEV-SETUP.md).
   * -text
   ```

2. Do **not** run `git add --renormalize`. Verify that
   `git status` shows only `.gitattributes` after adding it.
3. Add one line to `AGENTS.md` safety defaults: "Preserve each file's
   existing line endings. Do not convert whole files between LF and CRLF."
4. Check that WSL tools preserve CRLF: edit a CRLF `.cs` file with the agent's
   edit tool, then `git diff` shows only the intended lines (no `^M` churn).

**Gate:** A round-trip edit of a CRLF file produces a minimal diff.

---

## Tier 4 — Parameterized deploy and restore scripts

Replace the historical `source/server/tools/deploy_*.ps1` pattern (hard-coded
`C:\Users\thedo\...`, fixed hashes) with one reviewed script. **Do not run
or edit the historical scripts.**

### `tools/dev/Deploy-OfflineDAoC.ps1`

Parameters: `-InstallRoot` (required, e.g. `D:\Games\OfflineDAoC`),
`-ServerBuild` (folder with built `GameServer.dll` etc.), optional
`-LauncherBuild`, and `-Apply` (switch). Default is a **dry run**.

Behavior, in order:

1. Refuse if any of `CoreServer`, `OfflineDAoC`, `game`, `game.dll`,
   `camelot`, `connect` is running (the same process list the historical
   scripts use).
2. Resolve every target path and refuse any path outside `-InstallRoot`.
3. The deploy set is **explicit**, not "copy everything":
   - Server: `GameServer.dll/.pdb`, `CoreBase.dll/.pdb`,
     `CoreDatabase.dll/.pdb`, `CoreServer.dll/.pdb` into **both**
     `runtime\server\` and `runtime\server\lib\` where they already exist there
     (the runtime keeps copies in both; match what's present, never add new
     ones).
   - Third-party DLLs only if the build's version differs **and** the owner
     approves (print them in the dry run; do not copy by default).
   - Never touch: `runtime\data\*`, `account.txt`, `serverconfig.xml`,
     `bot-goals.json`, `rvr-world.json`, `client-opendaoc\*`, native
     interop/pathing DLLs, `logs\*`.
   - Launcher (optional): `runtime\OfflineDAoC.dll/.exe/.pdb/.deps.json/
     .runtimeconfig.json` from a **Windows** build.
4. Dry run prints a table: relative path, installed hash, new hash, action.
5. With `-Apply`: back up every file it will overwrite to
   `<InstallRoot>-backups\deploy-<timestamp>\` with a `manifest.json` (path,
   old hash, new hash), copy, re-hash, and verify. The protected save files are
   hashed before and after; abort if they changed.

### `tools/dev/Restore-OfflineDAoC.ps1`

Takes `-InstallRoot` and `-Backup <deploy-folder>`. Same running-process
refusal, and it restores exactly the manifest's files after checking that the
installed hashes still equal the manifest's "new" hash (otherwise refuse).

### `tools/dev/deploy.sh` (WSL convenience)

Converts paths with `wslpath -w` and calls
`powershell.exe -NoProfile -ExecutionPolicy Bypass -File ...`, forwarding
`-Apply` only when the caller passes it.

### Tests

- Pester is not bundled; keep logic testable by putting it in functions and
  adding a small launcher-test-style C# or PowerShell self-check against a
  **temporary fake install tree** (created under the scratch or `artifacts/`
  folder), never against `D:\Games\OfflineDAoC`.
- Cases: dry run changes nothing; a running process is refused; an outside-root
  path is refused; `-Apply` then Restore returns the original hashes; the
  protected files are untouched.

**Gate:** Self-checks pass on a fake tree. A dry run against the real install
lists only the expected server DLLs and changes nothing.

---

## Tier 5 — First real deploy and client smoke test  (owner present)

Needs the owner's explicit permission to start the server and client.

1. Build the server from `main` (Tier 2 command).
2. Dry run against `D:\Games\OfflineDAoC`, and have the owner review it.
3. `-Apply`. Record the backup folder.
4. The owner starts **START OFFLINE DAOC.cmd**, then **START SERVER**, waits for
   RUNNING, then **ENTER REALM**, logs in with an existing character, walks,
   fights a mob, and exits cleanly.
5. Check `runtime\logs` for new exceptions compared with before the deploy.
6. Stop everything. Practice **Restore** once, then re-deploy.

Report static results (hashes, dry-run output) separately from the owner's
in-game observations.

**Gate:** An unchanged-code build deploys, runs, and restores without touching
the save.

---

## Tier 6 — Hand-off to Camlann

1. Commit the Tier 1–4 files (`tools/dev/*`, `.gitattributes`, doc updates)
   on a branch with a PATCH bump per `AGENTS.md`, and open a PR.
2. Add the verified dev-loop commands to `docs/DEVELOPMENT.md` (a short
   "WSL2 + Windows" section pointing here).
3. Before Camlann Tier 0 (which **wipes the save**): make a fresh named backup
   of the save the owner wants to keep, for example
   `D:\Games\OfflineDAoC-backups\pre-camlann-<date>\`, and confirm with the
   owner.

**Gate:** The dev loop (build on WSL → dry run → deploy → play → restore) is
documented, committed, and has been run once end to end. Camlann Tier 0 can
start.

---

## Agent rules for this plan

- Work tier by tier; report each gate result before moving on.
- Never start or stop the server/launcher/client without owner permission. If
  a process is running, stop and ask.
- Never write build output, caches, or NuGet packages to C: or into
  `D:\Games\OfflineDAoC` (except deploys through the Tier 4 script).
- Never commit anything from `D:\Games\OfflineDAoC*`, `artifacts/`, or
  developer-state folders.
- If a step needs sudo, admin rights, or a Windows feature change, ask first.

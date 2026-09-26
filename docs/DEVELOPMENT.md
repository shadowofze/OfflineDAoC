# Development and reproducibility

## Baseline and layout

The current normal source branch is `release/v0.32-darkness-falls`; its matching
v0.32 Darkness Falls Beta download includes the shared maintenance fixes and
no Sluaghbinder class, quests, or optional client assets. The matching optional
branch, `release/v0.32b-sluaghbinder-darkness-falls`, adds Sluaghbinder on top
of v0.32. The v0.3, v0.31, and v0.31b tags and releases stay available as
legacy versions. Portable account bootstrap and default settings are
release-specific differences.

The complete release's runtime/server contains the installed binaries and
navigation meshes. Runtime/data contains a cleaned world database. Runtime/client-opendaoc/app
contains the compatible game installation. Never use the author's old absolute paths.

The optional v0.32b Sluaghbinder expansion is a copy-first overlay on a
verified v0.32 baseline. Its class source is in the optional `source/server`
tree, its client
build-identifying helpers are under `source/server/tools`, and its static data
overlay builder/installer sources are under `tools/sluaghbinder` and
`source/tools/OfflineDaoc.SluaghbinderPatch`. The public launcher labels are
0.32 and 0.32b; a private local label must not be copied into a fork.

Darkness Falls uses region 249. Entrance access, one-way ledges, realm exits,
shared-center PvP, and autonomous bot routes have separate authority checks.
The bot catalog must exclude Legion, the hardest level-70+ encounters,
unreachable flying targets, and unverified targets. Darkness Falls raid AI
is not implemented. Read `docs/RELEASE-0.32.md` and
`docs/VERIFICATION-0.32.md` before changing those boundaries; do not treat
policy tests as a long live gameplay test.
The raid-event manifest freezes 85 exact NPC IDs. High Lord Oro alone has an
explicit 65–70 startup-template level allowance; his saved level may change
on restart, but any other level, moved spawn, or changed identity must still
close the ordinary-goal certificate. Do not widen this to a general raid
level tolerance or add those bosses to grind goals.

The runnable release also bundles tools/dotnet and tools/nuget-feed for offline C#
development, the navigation builder/native dependencies, and the texture tool's
Python runtime. A GitHub source ZIP alone is not the complete runtime download.

## Build (does not deploy or start the server)

Use a .NET 10 SDK on Windows, or the SDK in the complete release. From the root:

```powershell
dotnet restore 'source/server/Dawn of Light.sln' -p:Configuration=Release
dotnet build 'source/server/Dawn of Light.sln' -c Release --no-restore
dotnet test source/server/Tests/Tests.csproj -c Release --no-build --no-restore
dotnet restore source/tools/OfflineDaoc.Launcher.Tests/OfflineDaoc.Launcher.Tests.csproj
dotnet test source/tools/OfflineDaoc.Launcher.Tests/OfflineDaoc.Launcher.Tests.csproj -c Release --no-restore
```

For offline restore, use the release's NuGet.Config and set NUGET_PACKAGES to a
local developer-state directory. Some launcher tests require that no DAoC server
is listening locally; a running server can correctly trigger the save lock.

If the complete download is in `<playable-folder>`, its SDK is
`<playable-folder>/tools/dotnet/dotnet.exe` and its offline configuration is
`<playable-folder>/NuGet.Config`. These are explicit alternatives to a globally installed SDK.
The release's BUILD AND TEST SOURCE.cmd is the ready-made offline build entrypoint
for the source bundled inside that complete download. It never deploys a build.

## Native client modifications

The native raid and bot-map builders are in source/server/tools, with validation
tests alongside them. They patch a specific verified x86 binary; they are not the
original client's C++ source. Their baseline hashes and dependencies matter.
Historical scripts may refer to backup input paths on the author's PC: these must
be parameterized and the required baseline supplied before rerunning. Do not
substitute an arbitrary game.dll or remove a failed hash guard. The supported
`tools/build-client-raid.py` wrapper resolves inputs from `--distribution`, includes
the exact baseline, and stages into a fresh `--output` directory. The older
v0.31 release verification recorded a byte-for-byte rebuild of its installed
game.dll; this is not a v0.32 verification claim.

Test texture-tool source with `python tools/test-assets.py --distribution playable`
(or the actual complete-download path). The wrapper resolves the read-only fixtures
for the source-checkout layout; modifications happen only in temporary test copies.

## World data, navigation, and customization

Keep the clean world definitions and all current navmeshes available to the LLM.
Source route resources alone are not a replacement for the generated native mesh
files or world spawn data. Preserve the current validated meshes until a focused
rebuild is requested. The texture tool converts atlases while preserving carrier
models and bindings; it does not create a usable 3D mesh from a PNG.

## Safe sharing

Never commit a database after playing. It will contain accounts, characters, bot
profiles, inventories and economy history. Generate a fresh sanitized seed in a
separate copy, check every progress table, clear free pages with VACUUM, and verify
integrity before sharing it. Exclude credentials, logs, diagnostics and old backups.

Default setup is local-only. Running a public multiplayer server is a separate
security/deployment project; do not expose this local configuration to the internet.

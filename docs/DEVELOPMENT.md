# Development and reproducibility

## Baseline and layout

This fork's launcher displays `DisplayVersion` from
`source/tools/OfflineDaoc.Launcher/MainForm.cs` (see CHANGELOG.md). The upstream
playable download remains GitHub v0.3; that package is the runtime, world data,
and navigation baseline. Do not confuse this fork's three-part version with the
original author's private 0.4 launcher label. Portable account bootstrap and
default settings are release-specific differences.

The release's runtime/server contains the reference installed binaries and 99
navigation meshes. Runtime/data contains a cleaned world database. Runtime/client-opendaoc/app
contains the compatible game installation. Never use the author's old absolute paths.

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

If the complete download is in `playable`, its SDK is
`playable/tools/dotnet/dotnet.exe` and its offline configuration is
`playable/NuGet.Config`. These are explicit alternatives to a globally installed SDK.
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
the exact baseline, and stages into a fresh `--output` directory. In release
verification this rebuilt the installed game.dll byte-for-byte.

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

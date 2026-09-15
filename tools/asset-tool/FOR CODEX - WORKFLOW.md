# Private texture workflow

Use this tool for future undead hero body/sword artwork instead of rebuilding an
ad-hoc converter. Read README - START HERE.txt and inspect assets.json first.
Paths resolve relative to this folder; no user-specific absolute paths required.

## CLI

Use bundled `runtime/python.exe -E -s asset_tool.py` (or TEXTURE TOOL CLI.cmd):

```
list
export undead-hero-body
export undead-hero-sword
build "PROJECT PATH" "ARTWORK PNG" --maximum 2048
install "BUILD PATH" --preview-reviewed
rollback "BACKUP PATH"
validate "DDS OR MPK PATH"
```

Export prints the project path. Build prints the build path. Inspect the comparison
image before passing --preview-reviewed. Do not manufacture review confirmation.
Use `runtime/python.exe -E -s -m unittest discover -s tests -v` for isolated tests.
Tests copy only needed assets to temporary folders; never install into the live game.

## Verified history and constraints

On 2026-09-08 the user confirmed A/C visible, B/D invisible, then all four and the
actual pet visible after moving monnifs 986 before six blank rows. This was the
visibility blocker. Body NIF is byte-identical to B_Band01.NIF; sword differs only
in its private texture filename. Do not rebuild the mesh to fix a lookup problem.
The user subsequently reported the corrected 2048 textures rendering too.

Original custom DDS writer used Pillow's incorrect top-mip size field (8204 rather
than 2097152 for 2048 DXT1). The writer now fixes this and validates every mip.
Keep the working reference header except width, height, top-mip byte count and mip
count. MPK output must refresh BOTH compressed offsets and expanded-memory offsets.

Private: model 2073 -> monnifs 986 -> uh_hero01; skin 6686 -> skin099.mpk /
uh_hero001.dds. Sword object 4808 -> items 1639 -> uh_sword001, DDS + TGA.
The tool never changes these bindings, meshes, stats, sounds, spell effects or AI.

Do not install while game/server/launcher is running. Keep all original draugr
and sword assets unchanged. Profile guards/baselines deliberately fail closed on
drift; investigate before updating them. No fallback to modifying shared assets.
New meshes and new private asset registration are separate reviewed work.

## Runtime

Bundled Python 3.12 with Pillow and Tcl/Tk. No network, pip, .NET or server runtime
needed. Third-party notices are in runtime/LICENSE.txt and THIRD PARTY NOTICES.

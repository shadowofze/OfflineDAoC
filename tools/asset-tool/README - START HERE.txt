OFFLINE DAOC PRIVATE TEXTURE TOOL
===============================

Open OPEN TEXTURE TOOL.cmd. No Python installation or downloads required.
Keep this entire folder directly inside Offline DAoC, beside its runtime folder.

1. Choose Undead hero - body texture OR Undead hero - exclusive sword texture.
2. Click Export reference. Give the exported EDIT THIS TEXTURE.png to your image
   editor/ChatGPT. Keep the exported reference files unchanged.
3. Choose the edited PNG returned by the editor. Click Build and preview.
4. Compare left (current reference) and right (converted artwork). The texture
   sections must have the same layout. Check the review box if satisfied.
5. Close the game, server AND launcher. Click Install private textures.
6. Restart normally and resummon undead hero. File verification cannot prove
   how an edited texture looks in the actual game; check the face and sword.

Export and Build are safe while playing. Install and Rollback require shutdown.
The default maximum is 2048. A smaller image is resized to the next power of two,
up to the selected maximum; selecting a maximum is not a request to always reach it.

ROLLBACK
Click Rollback an install and select its timestamped folder inside backups.
Only the affected private texture files are backed up, not your game folder,
accounts, characters, bots, database or settings. A backup cannot overwrite later
edits: restore the newest applicable backup first. Keep backups until satisfied.

If installation stops, read the error. Do not bypass checksum/profile errors.
If power was lost during installation, the receipt and original files remain in
backups. A stale INSTALL IN PROGRESS.lock deliberately blocks more writes: ask
Codex to verify no tool process is running, review the prepared receipt and remove
only that stale lock before restoring the interrupted installation.

WHAT THIS DOES
- Imports texture atlases, not new 3D models.
- Converts to legacy DXT1/DXT5 DDS with correct byte sizes and full mipmaps.
- Preserves the working DDS header properties and original transparency mask.
- Preserves normalized UVs by keeping the atlas orientation and aspect ratio.
- Updates the private sword TGA fallback alongside its DDS.
- Validates MPK checksums, compressed offsets AND expanded-memory offsets.
- Checks the custom body's registration is before blank table rows.
- Never edits NIF meshes, animation files, model registration, gameplay code,
  the database, original draugr textures or original sword assets.

LIMITS
A PNG cannot describe a rigged 3D mesh. This tool cannot create/repair geometry,
animations or skeletons, nor automatically undo AI moving atlas sections around.
Aspect-ratio checks cannot prove UV artwork alignment. A human must review it.
Transparency follows the working reference; new transparent effects need a
separate review. Only the two registered private assets are supported initially.
Adding a new asset requires Codex to register and verify its exclusive paths and
working model mappings. Do not point a profile at an original shared game asset.

PROMPT FOR CHATGPT
Edit this existing DAoC texture atlas, not the 3D model. Preserve the exact
placement, proportions, orientation and boundaries of every texture section so
it still aligns with the original model's UV mapping. Do not rearrange sections,
crop, add borders or turn it into a character portrait. Apply these changes:
[YOUR CHANGES]. Preserve existing transparency. Return a PNG, preferably 2048 by
2048 for this square atlas.

REUSE
Export a fresh project after each installation. This prevents old projects from
overwriting a newer texture accidentally. Projects, previews and backups remain
inside this tool folder. Nothing runs in the background after you close the tool.

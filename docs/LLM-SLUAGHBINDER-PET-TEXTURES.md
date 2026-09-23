# For LLM developers: Sluaghbinder pet models and textures

This is a technical handoff for editing the **optional v0.31b** Dullahan and Zombie Defender appearances. The maintained implementation lives on the `release/v0.31b-sluaghbinder` branch, not in the unchanged v0.31 playable baseline. Read `AGENTS.md`, `docs/DEVELOPMENT.md`, and `docs/LLM-QUICKSTART.md` first. Work on a copied Sluaghbinder installation with the client, server, and launcher closed for installation. Do not edit a player's original v0.3/v0.31 installation or shared monster assets.

## Programs and files

| Purpose | Program or file |
|---|---|
| Inspect old Gamebryo `.NIF` geometry, UVs, texture references, and attachment nodes | NifSkope (the locally tested viewer was 2.0). It is a viewer/editor for NIF structure, **not** a texture painter. |
| Paint the existing UV atlas without moving its islands | A layer-capable raster editor such as GIMP or Krita, or deterministic code using Python 3 with Pillow. AI image generation is not required. |
| Export, convert, preview, validate, install, and roll back legacy DDS/MPK texture changes | The included `OFFLINE DAOC ASSET TOOL` in a complete playable download, and its source in `tools/asset-tool`. The generic bundled profile does **not** automatically register new Sluaghbinder assets; create a guarded private profile before using it on these pets. |
| Inspect/rebuild DAoC `MPAK` archives and catalog rows | `tools/asset-tool/archive.py` and the optional patch's client-asset merge code. Keep a byte-identical backup of every archive being changed. |
| Change pet stats/equipment, if that is separately requested | `source/server` plus the static overlay builder at `tools/sluaghbinder/build_overlay.py`. Texture work alone does not change stats. |

A PNG is a **texture atlas**, not a mesh. It cannot create geometry, armor thickness, a new skeleton, or an animation. NifSkope may show a model white when the texture is packed in an MPK or its path is unresolved; that alone does not prove the game will show white. Conversely, pink in-game is a failed texture lookup, not proof of a bad mesh.

## The two private chains

| Pet | Server template and model | Client catalog chain | Private files |
|---|---|---|---|
| Zombie Defender (template name may be `zombie guardian`) | `NpcTemplate` `60170005`, model `2494` | `gamedata.mpk`: `monsters.csv` model `2494` -> NIF `987`, skin `7128`; `monnifs.csv` `987` -> `Sluaghbinder_ZombieDefender`; `skins.csv` `7128` -> `sluagh_zombie_defender_body.dds`, bank `99` | `figures/Sluaghbinder_ZombieDefender.NIF`; DDS entry in `figures/skins/skin099.mpk` |
| Dullahan | `NpcTemplate` `60170007`, model `2495` | `gamedata.mpk`: `monsters.csv` model `2495` -> NIF `988`, skin `7129`; `monnifs.csv` `988` -> `Sluaghbinder_Dullahan`; `skins.csv` `7129` -> `sluagh_dullahan_body.dds`, bank `106` | `figures/Sluaghbinder_Dullahan.NIF`; DDS entry in `figures/skins/skin106.mpk` |

The stock Decaying Marshman and Headless Corpse use their own model/skin rows. **Do not overwrite their NIFs, DDS entries, or catalog IDs.** These two private NIFs are copies with their own names so a pet-specific appearance does not change ordinary monsters. The equipped chain morningstar uses the existing client object `862` (object -> item `268` -> stock `B_FLX_morningstar01_M.NIF`) with an equipment dark effect `54`. A separately named private morningstar NIF may be present in the isolated client, but it is **not** the equipped object and should not be substituted by assumption.

## Safe texture edit, end to end

1. Back up the copied client's `gamedata.mpk`, the relevant `skin099.mpk` or `skin106.mpk`, and its private NIF. Record SHA-256 hashes. Never overwrite the base monster assets.
2. Confirm the server template points to model `2494` or `2495`, then resolve its `monsters.csv` -> `monnifs.csv`/`skins.csv` chain. Verify the named DDS entry actually exists in the stated archive. Do this **before** painting.
3. Export the existing private DDS to a PNG with its original atlas layout visible. Preserve the dimensions, orientation, UV island positions, and existing alpha behavior. Paint armor, cloth, scars, gloves, boots, and exposed areas **within those same regions**. For example, the Defender's rusted plateskirt belongs on the skirt region; armor painted over the face region will render on the face. A flat portrait or rearranged atlas will wrap incorrectly.
4. In NifSkope, inspect the copied NIF and its UV map and, if needed, point the viewer at an extracted copy of the DDS for preview. A NIF preview is helpful but is not an in-game test. Do not use NifSkope's save operation merely to view a model; an unintended NIF rewrite may break this old client.
5. Convert the edited PNG to the same legacy DDS family as the working private reference, with a complete mip chain and valid header sizes. Keep the original alpha mask unless transparency is an explicit, separately tested change. The private texture tool's `export`, `build`, `validate`, `install --preview-reviewed`, and `rollback` flow is the reference implementation; review its left/right comparison before installation. Do not claim preview approval from an LLM alone—have the player inspect it.
6. Replace **only** the matching private DDS entry in the matching skin MPK. Keep all other entries, timestamps, and flags intact. Rebuild the MPK directory with case-insensitive filename sorting and correct compressed **and** expanded-memory offsets and CRCs. This old client's lookup showed a bright-pink Dullahan when the entry was merely appended unsorted. The exact DDS filename in `skins.csv` must match the archive entry.
7. Validate MPK read/write round-trip, entry uniqueness, CSV bindings, and file hashes. Close the launcher, client, and server; install into the copied optional-class client; then relaunch and summon a **fresh** pet. Check front, back, arms, face, boots, skirt, weapon, and motion in-game. If it is pink, roll back the archive and inspect the private skin lookup before altering geometry.
8. Keep a rollback record for each changed path and its prior hash. Restore only the copied installation on rollback. Never publish a played runtime database, accounts, inventory, bot records, settings, logs, or unrelated client archives.

For a new optional release, the patcher must merge these **private** catalog rows and DDS entries into each supported base client rather than replacing every customer's entire `gamedata.mpk` or skin archive. It must back up the pre-merge archives and delete newly created private NIFs on rollback. Test installation and rollback on disposable clean v0.3 and v0.31 copies, then perform real-client visual QA. A compiler pass or MPK CRC check alone cannot verify UV alignment or appearance.

## Scenario notes

- **Dullahan:** The working body is a headless corpse with dark, worn armor painted into its existing atlas while keeping battle damage readable. Its separate chain morningstar, dark enchantment, no offhand, and 50% size increase are equipment/template decisions; do not try to bake them into the DDS. The stock headless monster must still look normal.
- **Zombie Defender:** Keep the zombie head and facial atlas untouched. Put uniform rusty iron plate, a clearly plated skirt, gloves, boots, and controlled missing-plate areas on the correct body UV regions. Its old shield and 33% size increase are independent of the body atlas. The stock Decaying Marshman must still look normal.

If a profile or archive hash differs, stop and investigate. Do not remove a hash guard to force an install onto an unknown client build. Preserve the last known-good skin archives so the player can return to the original corpse look if a private texture fails.

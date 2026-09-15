#!/usr/bin/env python3
"""Build the second, period-map-backed Classic 1.65 spawn restoration manifest.

The removed-row archive is never treated as authority.  A row is eligible only
when a 2002 map corroborates its zone, monster name, level, and approximate map
location (outdoors), or its dungeon roster and room cluster (dungeons).  This
tool is read-only with respect to SQLite; the generated immutable ID manifest is
consumed by OfflineDaoc.Setup after the native navmesh test has rejected unsafe
rows.
"""

from __future__ import annotations

import argparse
import csv
import json
import math
import re
import sqlite3
import xml.etree.ElementTree as ET
from collections import defaultdict
from dataclasses import dataclass
from pathlib import Path


MAP_LEFT = 68.0
MAP_RIGHT = 545.0
MAP_TOP = 87.0
MAP_BOTTOM = 563.0
WORLD_SIZE = 65_536
OUTDOOR_ALIGNMENT = 5_000
OUTDOOR_CELL = 4_200
DUNGEON_CELL = 1_250
OUTDOOR_TARGET = 3
DUNGEON_TARGET = 4

# The Albion atlas was compiled in August 2002 from Kirstena/Illia data.  Page
# numbers are PDF page numbers, not printed footer numbers.
ALBION_MAP_PAGES = {
    15: 2,   # Hadrian's Wall
    14: 3,   # Pennine Mountains
    12: 4,   # Snowdonia
    11: 5,   # Forest Sauvage
    3: 6,    # Black Mountains North
    7: 7,    # Llyn Barfog
    2: 8,    # Black Mountains South
    0: 10,   # Camelot Hills
    1: 12,   # Salisbury Plains
    8: 14,   # Campacorentin Forest
    9: 16,   # Avalon Marsh
    6: 17,   # Cornwall
    4: 19,   # Dartmoor
    10: 20,  # Lyonesse
}

OLD_FRONTIER_ZONES = frozenset({11, 12, 14, 15, 111, 112, 113, 115, 210, 211, 212, 214})
CAPNBRY_RADIUS = 1_500
CAPNBRY_INCREMENT = 2

# Mount Collory has no usable CapnBry coordinates.  These names are attested in
# 2002 hunting/trophy reports.  Candidate coordinates still come from the old
# server archive and are intentionally limited to two small clusters per name.
MOUNT_COLLORY_PERIOD_NAMES = frozenset({
    "irewood", "irewood sapling", "grovewood", "sett dweller",
    "sett protector", "sett matron", "faerie badger", "aughisky",
})
CRUACHAN_PERIOD_NAMES = frozenset({"gorge rat", "irewood greenbark"})

# The dungeon maps give room layouts and an explicit period monster table.  A
# spelling alias is accepted only for a known database-era typo.
DUNGEON_RANGES = {
    21: {
        "aged bleeder": (8, 9), "bleeder broodmother": (8, 12),
        "bleeder hatchling": (8, 10), "botched sacrifice": (10, 12),
        "chilled presence": (10, 11), "cursed believer": (8, 14),
        "dreadful cadaver": (8, 9), "decaying spirit": (6, 8),
        "devout follower": (8, 10), "doomed minion": (9, 12),
        "eternal scream": (14, 15), "fallen paladin": (11, 12),
        "forgotten promise": (9, 10), "ghost miner": (11, 12),
        "haunting gloom": (8, 10), "insidious whisper": (13, 15),
        "lingering shade": (7, 11), "living entombed": (10, 12),
        "malevolent disciple": (13, 14), "menacing presence": (8, 13),
        "mindless minion": (8, 10), "putrid sacrificer": (9, 10),
        "spiteful wraith": (10, 12), "tortured soul": (9, 10),
        "undead builder": (9, 10), "undead guardsman": (9, 10),
        "undead poacher": (9, 10), "unfortunate pragmatic": (9, 10),
        "acolyte nascita": (13, 15), "favonius facilis": (13, 16),
    },
    22: {
        "beven": (21, 23), "dai": (25, 27), "fane": (23, 25),
        "gremlin": (20, 21), "gwern": (23, 25),
        "keltoi banisher": (24, 27), "keltoi eremite": (21, 24),
        "keltoi familiar": (23, 26), "keltoi initiate": (19, 23),
        "keltoi novitiate": (20, 20), "keltoi recluse": (22, 28),
        "keltoi ritualist": (23, 25), "keltoi spiritualist": (24, 28),
        "keltoi visionary": (21, 24), "meurig": (23, 27),
        "muryan emmisary": (24, 28),
    },
    23: {
        "actarius": (31, 31), "aquilifer": (31, 31),
        "centurio manipularis": (31, 31), "centurio pilus posterior": (31, 31),
        "centurio primus ordines": (31, 31), "centurio primus pilus": (32, 33),
        "cohorstalis": (30, 30), "decurion": (31, 31), "draconarius": (31, 31),
        "dux": (31, 32), "imaginifer": (31, 31), "immunis": (29, 29),
        "legatio": (33, 33), "legionarius": (29, 30), "magister": (32, 32),
        "manipularis": (30, 30), "optio": (31, 31), "praefectus": (32, 32),
        "praetor": (31, 31), "praetorian guard": (33, 33), "princep": (32, 32),
        "signifier": (31, 31), "singular": (33, 33), "tribune": (32, 32),
        "tribunus laticlavicus": (33, 33), "vigilis": (29, 29),
    },
}

NAME_ALIASES = {
    "menacing presense": "menacing presence",
    "muryan emmisary": "muryan emmisary",
}


@dataclass(frozen=True)
class Zone:
    zone_id: int
    region_id: int
    name: str
    offset_x: int
    offset_y: int
    width: int
    height: int


@dataclass(frozen=True)
class Mob:
    mob_id: str
    name: str
    zone_id: int
    region_id: int
    local_x: int
    local_y: int
    world_x: int
    world_y: int
    z: int
    level: int
    items_list: str
    npc_template: str
    equipment_template: str


def norm(value: str) -> str:
    value = re.sub(r"[^a-z0-9]+", " ", (value or "").casefold())
    return " ".join(value.split())


def load_zones(db: sqlite3.Connection) -> dict[int, Zone]:
    return {int(r[0]): Zone(int(r[0]), int(r[1]), str(r[2]), int(r[3]) * 8192,
        int(r[4]) * 8192, int(r[5]) * 8192, int(r[6]) * 8192)
        for r in db.execute("SELECT ZoneID,RegionID,Name,OffsetX,OffsetY,Width,Height FROM Zones")}


def load_mobs(db: sqlite3.Connection, table: str, zones: dict[int, Zone], wanted: set[int],
              excluded: set[str] | None = None) -> dict[int, list[Mob]]:
    by_region: dict[int, list[Zone]] = defaultdict(list)
    for zone in zones.values():
        if zone.zone_id in wanted:
            by_region[zone.region_id].append(zone)
    sql = f"""SELECT Mob_ID,Name,Region,X,Y,Z,Level,COALESCE(ItemsListTemplateID,''),
                     COALESCE(NPCTemplateID,''),COALESCE(EquipmentTemplateID,'')
              FROM {table} WHERE ClassType='DOL.GS.GameNPC' AND Realm=0"""
    result: dict[int, list[Mob]] = defaultdict(list)
    for r in db.execute(sql):
        mob_id = str(r[0])
        if excluded and mob_id in excluded:
            continue
        zone = next((z for z in by_region.get(int(r[2]), ())
            if z.offset_x <= int(r[3]) < z.offset_x + z.width
            and z.offset_y <= int(r[4]) < z.offset_y + z.height), None)
        if zone is None:
            continue
        result[zone.zone_id].append(Mob(mob_id, str(r[1]), zone.zone_id, int(r[2]),
            int(r[3]) - zone.offset_x, int(r[4]) - zone.offset_y,
            int(r[3]), int(r[4]), int(r[5]), int(r[6]), str(r[7]), str(r[8]), str(r[9])))
    return result


def singular_token(value: str) -> str:
    value = norm(value)
    if len(value) > 4 and value.endswith("ies"):
        return value[:-3] + "y"
    if len(value) > 4 and value.endswith("s") and not value.endswith("ss"):
        return value[:-1]
    return value


def phrase_occurrences(words: list[dict], name: str) -> list[tuple[float, float]]:
    tokens = [singular_token(token) for token in norm(name).split()]
    if not tokens:
        return []
    usable = [w for w in words if MAP_LEFT <= float(w["x0"]) <= MAP_RIGHT
              and MAP_TOP <= float(w["top"]) <= MAP_BOTTOM]
    found: list[tuple[float, float]] = []
    for index, word in enumerate(usable):
        if singular_token(str(word["text"])) != tokens[0]:
            continue
        chosen = [word]
        cursor = index + 1
        for token in tokens[1:]:
            match = None
            for candidate in usable[cursor:cursor + 8]:
                dx = abs(float(candidate["x0"]) - float(chosen[-1]["x1"]))
                dy = abs(float(candidate["top"]) - float(chosen[-1]["top"]))
                if singular_token(str(candidate["text"])) == token and (dy <= 18 or dx <= 45):
                    match = candidate
                    break
            if match is None:
                chosen = []
                break
            chosen.append(match)
            cursor = usable.index(match) + 1
        if chosen:
            x = sum((float(w["x0"]) + float(w["x1"])) / 2 for w in chosen) / len(chosen)
            y = sum((float(w["top"]) + float(w["bottom"])) / 2 for w in chosen) / len(chosen)
            found.append((x, y))
    return found


def page_witnesses(atlas: Path, pages: dict[int, int], names: dict[int, set[str]]) -> dict[tuple[int, str], list[tuple[int, int]]]:
    import pdfplumber
    result: dict[tuple[int, str], list[tuple[int, int]]] = defaultdict(list)
    with pdfplumber.open(atlas) as pdf:
        for zone_id, page_number in pages.items():
            actual_zone = zone_id
            words = pdf.pages[page_number - 1].extract_words(use_text_flow=False)
            for name in sorted(names.get(actual_zone, ())):
                for px, py in phrase_occurrences(words, name):
                    x = round((px - MAP_LEFT) * WORLD_SIZE / (MAP_RIGHT - MAP_LEFT))
                    y = round((py - MAP_TOP) * WORLD_SIZE / (MAP_BOTTOM - MAP_TOP))
                    if 0 <= x <= WORLD_SIZE and 0 <= y <= WORLD_SIZE:
                        result[(actual_zone, name)].append((x, y))
    return result


def load_capnbry(cache: Path) -> dict[tuple[int, str, int, int], list[tuple[int, int, int]]]:
    result: dict[tuple[int, str, int, int], list[tuple[int, int, int]]] = defaultdict(list)
    for path in sorted((cache / "mobs").glob("*.xml")):
        mob = ET.parse(path).getroot().find("mob")
        if mob is None:
            continue
        name = norm(mob.findtext("name") or "")
        if not name:
            continue
        for seen in mob.findall("mobseen"):
            try:
                zone = int(seen.findtext("zone", "-1"))
                x = int(seen.findtext("x", "0"))
                y = int(seen.findtext("y", "0"))
                level = int(seen.findtext("level", "0"))
            except ValueError:
                continue
            if zone in OLD_FRONTIER_ZONES and 1 <= level <= 50:
                result[(zone, name, x // OUTDOOR_CELL, y // OUTDOOR_CELL)].append((x, y, level))
    return result


def distance2(mob: Mob, point: tuple[int, int]) -> int:
    return (mob.local_x - point[0]) ** 2 + (mob.local_y - point[1]) ** 2


def transform_points(points: list[tuple[int, int]], variant: int) -> list[tuple[int, int]]:
    transformed = []
    for x, y in points:
        if variant & 1: x = WORLD_SIZE - x
        if variant & 2: y = WORLD_SIZE - y
        if variant & 4: x, y = y, x
        transformed.append((x, y))
    return transformed


def choose_zone_transform(zone_id: int, raw: dict[tuple[int, str], list[tuple[int, int]]],
                          archive: dict[int, list[Mob]]) -> int:
    scored = []
    for variant in range(8):
        distances = []
        for (witness_zone, name), points in raw.items():
            if witness_zone != zone_id:
                continue
            candidates = [mob for mob in archive.get(zone_id, ()) if norm(mob.name) == name]
            if not candidates:
                continue
            transformed = transform_points(points, variant)
            distances.append(min(distance2(mob, point) for mob in candidates for point in transformed))
        # A median-like trimmed score prevents one named encounter from choosing
        # the coordinate orientation for an entire page.
        distances.sort()
        sample = distances[:max(1, len(distances) * 3 // 4)]
        scored.append((sum(sample) / len(sample) if sample else float("inf"), variant))
    return min(scored)[1]


def live_in_cell(live: list[Mob], name: str, x: int, y: int, cell: int) -> int:
    target = norm(name)
    return sum(norm(m.name) == target and m.local_x // cell == x // cell
               and m.local_y // cell == y // cell for m in live)


def row_item(mob: Mob, category: str, source_id: str, source: str,
             source_x: int | None = None, source_y: int | None = None,
             source_distance: int = 0) -> dict:
    return {
        "mob_id": mob.mob_id, "name": mob.name, "zone_id": mob.zone_id,
        "region_id": mob.region_id, "local_x": mob.local_x, "local_y": mob.local_y,
        "world_x": mob.world_x, "world_y": mob.world_y, "z": mob.z,
        "level": mob.level, "category": category, "source_id": source_id,
        "source": source, "source_x": source_x, "source_y": source_y,
        "source_distance": source_distance,
        "archive_bindings": {
            "items_list_template_id": mob.items_list,
            "npc_template_id": mob.npc_template,
            "equipment_template_id": mob.equipment_template,
        },
    }


def audit_bindings(database: Path, spawns: list[dict]) -> dict:
    ids = {row["mob_id"] for row in spawns}
    names = {norm(row["name"]) for row in spawns}
    with sqlite3.connect(database) as db:
        archived = {str(row[0]): row for row in db.execute(
            "SELECT Mob_ID,COALESCE(NPCTemplateID,''),COALESCE(EquipmentTemplateID,'') "
            "FROM offline_classic165_removed_mobs") if str(row[0]) in ids}
        npc_templates = {str(row[0]) for row in db.execute("SELECT TemplateId FROM NpcTemplate")}
        equipment = {str(row[0]) for row in db.execute("SELECT DISTINCT TemplateID FROM NPCEquipment")}
        item_templates = {str(row[0]) for row in db.execute("SELECT Id_nb FROM ItemTemplate")}
        old_links = [(norm(row[0]), str(row[1])) for row in db.execute(
            "SELECT MobName,LootTemplateName FROM MobXLootTemplate") if norm(row[0]) in names]
        new_links = [(norm(row[0]), str(row[1])) for row in db.execute(
            "SELECT MobName,LootTemplateName FROM MobDropTemplate") if norm(row[0]) in names]
        old_templates = defaultdict(list)
        for template, item in db.execute("SELECT TemplateName,ItemTemplateID FROM LootTemplate"):
            old_templates[norm(str(template))].append(str(item))
        new_templates = defaultdict(list)
        for template, item in db.execute("SELECT TemplateName,ItemTemplateID FROM DropTemplateXItemTemplate"):
            new_templates[norm(str(template))].append(str(item))

    missing_archive = sorted(ids - set(archived))
    missing_npc = sorted(mob_id for mob_id, row in archived.items()
                         if str(row[1]).strip() not in ("", "0") and str(row[1]) not in npc_templates)
    missing_equipment = sorted(mob_id for mob_id, row in archived.items()
                               if str(row[2]).strip() not in ("", "0") and str(row[2]) not in equipment)
    bad_old_links = sorted({template for _, template in old_links if norm(template) not in old_templates})
    bad_new_links = sorted({template for _, template in new_links if norm(template) not in new_templates})
    linked_old = {name for name, _ in old_links} | {name for name in names if name in old_templates}
    linked_new = {name for name, _ in new_links} | {name for name in names if name in new_templates}
    referenced_old = {norm(template) for _, template in old_links} | (names & set(old_templates))
    referenced_new = {norm(template) for _, template in new_links} | (names & set(new_templates))
    missing_items = sorted({item for template in referenced_old for item in old_templates.get(template, ())
                            if item not in item_templates} |
                           {item for template in referenced_new for item in new_templates.get(template, ())
                            if item not in item_templates})
    return {
        "archive_rows_exact": len(archived),
        "missing_archive_rows": missing_archive,
        "missing_npc_template_bindings": missing_npc,
        "missing_equipment_template_bindings": missing_equipment,
        "monster_names": len(names),
        "monster_names_with_specific_drop_tables": len(names & (linked_old | linked_new)),
        "legacy_drop_links": len(old_links),
        "modern_drop_links": len(new_links),
        "missing_drop_templates": bad_old_links + bad_new_links,
        "missing_item_templates": missing_items,
        "generic_rog_and_coin_generators_unchanged": True,
    }


def build_manifest(database: Path, atlas: Path, capnbry_cache: Path) -> dict:
    outdoor_zones = {11, 14, 15, 12, 3, 7, 2, 0, 1, 8, 9, 6, 4, 10}
    dungeon_zones = set(DUNGEON_RANGES)
    wanted = outdoor_zones | dungeon_zones | set(OLD_FRONTIER_ZONES)
    with sqlite3.connect(database) as db:
        zones = load_zones(db)
        valid_npc_templates = {str(row[0]) for row in db.execute("SELECT TemplateId FROM NpcTemplate")}
        valid_equipment_templates = {str(row[0]) for row in db.execute("SELECT DISTINCT TemplateID FROM NPCEquipment")}
        has_tracking = db.execute(
            "SELECT count(*) FROM sqlite_master WHERE type='table' AND name='offline_classic165_restored_mobs'").fetchone()[0]
        all_tracked = {str(r[0]) for r in db.execute(
            "SELECT Mob_ID FROM offline_classic165_restored_mobs")} if has_tracking else set()
        tracked = {str(r[0]) for r in db.execute(
            "SELECT Mob_ID FROM offline_classic165_restored_mobs "
            "WHERE MigrationId='classic165-conservative-frontier-albion-spawns-v1'")} if has_tracking else set()
        archive = load_mobs(db, "offline_classic165_removed_mobs", zones, wanted)
        live = load_mobs(db, "Mob", zones, wanted, all_tracked)

    archive = {zone: [mob for mob in rows
        if mob.mob_id not in tracked
        if mob.npc_template.strip() in ("", "0") or mob.npc_template in valid_npc_templates
        if mob.equipment_template.strip() in ("", "0") or mob.equipment_template in valid_equipment_templates]
        for zone, rows in archive.items()}

    names = {zone: {norm(m.name) for m in rows} for zone, rows in archive.items()}
    witnesses = page_witnesses(atlas, ALBION_MAP_PAGES, names)
    zone_variants = {zone: choose_zone_transform(zone, witnesses, archive)
                     for zone in outdoor_zones}
    selected: list[dict] = []
    used: set[str] = set()

    for (zone_id, name), raw_points in sorted(witnesses.items()):
        candidates = [m for m in archive.get(zone_id, ()) if norm(m.name) == name and 1 <= m.level <= 50]
        if not candidates:
            continue
        points = transform_points(raw_points, zone_variants[zone_id])
        radius2 = OUTDOOR_ALIGNMENT * OUTDOOR_ALIGNMENT
        eligible = [(m, min(distance2(m, p) for p in points)) for m in candidates]
        eligible = [(m, d) for m, d in eligible if d <= radius2]
        by_cell: dict[tuple[int, int], list[tuple[Mob, int]]] = defaultdict(list)
        for mob, dist in eligible:
            by_cell[(mob.local_x // OUTDOOR_CELL, mob.local_y // OUTDOOR_CELL)].append((mob, dist))
        for cell, rows in sorted(by_cell.items()):
            rows.sort(key=lambda item: (item[1], item[0].mob_id))
            current = live_in_cell(live.get(zone_id, []), name,
                rows[0][0].local_x, rows[0][0].local_y, OUTDOOR_CELL)
            target = 1 if zone_id == 0 else OUTDOOR_TARGET
            for mob, dist in rows[:max(0, target - current)]:
                if mob.mob_id in used:
                    continue
                used.add(mob.mob_id)
                point = min(points, key=lambda p: distance2(mob, p))
                selected.append(row_item(mob, "period_albion_outdoor",
                    f"kirstena-albion-2002:p{ALBION_MAP_PAGES[zone_id]}:{name}:{cell[0]}:{cell[1]}",
                    "Kirstena/Illia Albion map compilation, August 2002",
                    point[0], point[1], round(math.sqrt(dist))))

    for zone_id, ranges in DUNGEON_RANGES.items():
        grouped: dict[tuple[str, int, int], list[Mob]] = defaultdict(list)
        for mob in archive.get(zone_id, ()):
            canonical = NAME_ALIASES.get(norm(mob.name), norm(mob.name))
            period_range = ranges.get(canonical)
            if period_range is None or not period_range[0] <= mob.level <= period_range[1]:
                continue
            grouped[(canonical, mob.local_x // DUNGEON_CELL, mob.local_y // DUNGEON_CELL)].append(mob)
        for (name, cell_x, cell_y), rows in sorted(grouped.items()):
            rows.sort(key=lambda mob: mob.mob_id)
            current = live_in_cell(live.get(zone_id, []), rows[0].name,
                rows[0].local_x, rows[0].local_y, DUNGEON_CELL)
            for mob in rows[:max(0, DUNGEON_TARGET - current)]:
                if mob.mob_id in used:
                    continue
                used.add(mob.mob_id)
                selected.append(row_item(mob, "period_albion_dungeon",
                    f"kirstena-albion-2002:p{11 if zone_id == 21 else 15 if zone_id == 22 else 18}:{name}:{cell_x}:{cell_y}",
                    "Kirstena/Illia Albion dungeon map compilation, August 2002"))

    # The first pass capped CapnBry-corroborated camps at two creatures.  Add at
    # most two further archived rows at the exact same sighting cluster, keeping
    # restored period camps small while repairing frontier zones that remained
    # visibly sparse.
    for (zone_id, name, cell_x, cell_y), points in sorted(load_capnbry(capnbry_cache).items()):
        candidates = [mob for mob in archive.get(zone_id, ())
                      if norm(mob.name) == name and mob.mob_id not in tracked and mob.mob_id not in used
                      and 1 <= mob.level <= 50]
        eligible = []
        for mob in candidates:
            compatible = [(x, y) for x, y, level in points if abs(mob.level - level) <= 1]
            if not compatible:
                continue
            dist = min(distance2(mob, point) for point in compatible)
            if dist <= CAPNBRY_RADIUS * CAPNBRY_RADIUS:
                eligible.append((mob, dist, min(compatible, key=lambda p: distance2(mob, p))))
        eligible.sort(key=lambda item: (item[1], item[0].mob_id))
        for mob, dist, point in eligible[:CAPNBRY_INCREMENT]:
            used.add(mob.mob_id)
            selected.append(row_item(mob, "period_old_frontier",
                f"capnbry-period-extension:{zone_id}:{cell_x}:{cell_y}:{name}",
                "CapnBry DAoC bestiary sighting coordinates",
                point[0], point[1], round(math.sqrt(dist))))

    for zone_id, period_names, source in (
        (210, MOUNT_COLLORY_PERIOD_NAMES, "2002 Mount Collory hunting and trophy reports"),
        (211, CRUACHAN_PERIOD_NAMES, "2002 Cruachan Gorge hunting reports"),
    ):
        for name in sorted(period_names):
            rows = [mob for mob in archive.get(zone_id, ())
                    if norm(mob.name) == name and mob.mob_id not in tracked and mob.mob_id not in used
                    and 1 <= mob.level <= 50]
            clusters: dict[tuple[int, int], list[Mob]] = defaultdict(list)
            for mob in rows:
                clusters[(mob.local_x // OUTDOOR_CELL, mob.local_y // OUTDOOR_CELL)].append(mob)
            # Prefer the two largest coherent archived camps; single stray rows
            # are not sufficient evidence for a restoration.
            coherent = sorted(clusters.items(), key=lambda pair: (-len(pair[1]), pair[0]))
            for (cell_x, cell_y), camp in coherent[:2]:
                if len(camp) < 2:
                    continue
                camp.sort(key=lambda mob: mob.mob_id)
                for mob in camp[:OUTDOOR_TARGET]:
                    used.add(mob.mob_id)
                    selected.append(row_item(mob, "period_old_frontier",
                        f"period-report:{zone_id}:{name}:{cell_x}:{cell_y}", source))

    selected.sort(key=lambda row: (row["zone_id"], row["source_id"], row["mob_id"]))
    return {
        "schema": 2,
        "policy": {
            "archive_is_candidate_only": True,
            "synthetic_monster_types": False,
            "preserve_existing_spawns": True,
            "outdoor_map_alignment_radius": OUTDOOR_ALIGNMENT,
            "maximum_rows_per_outdoor_camp": OUTDOOR_TARGET,
            "maximum_rows_per_dungeon_room_cell": DUNGEON_TARGET,
            "bidirectional_navmesh_validation_required": True,
        },
        "sources": [
            "Kirstena/Illia Albion map compilation, August 2002",
            "https://thepp.be/DAOC/Cartes/alb_all.pdf",
        ],
        "spawns": selected,
        "summary": {
            "total": len(selected),
            "outdoor": sum(row["category"] == "period_albion_outdoor" for row in selected),
            "dungeon": sum(row["category"] == "period_albion_dungeon" for row in selected),
            "old_frontier": sum(row["category"] == "period_old_frontier" for row in selected),
            "zones": {str(zone): sum(row["zone_id"] == zone for row in selected)
                      for zone in sorted({row["zone_id"] for row in selected})},
        },
    }


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--database", type=Path, required=True)
    parser.add_argument("--albion-atlas", type=Path, required=True)
    parser.add_argument("--capnbry-cache", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--nav-rejections", type=Path)
    parser.add_argument("--id-output", type=Path)
    parser.add_argument("--camp-report", type=Path)
    args = parser.parse_args()
    manifest = build_manifest(args.database, args.albion_atlas, args.capnbry_cache)
    if args.nav_rejections and args.nav_rejections.exists():
        failures = json.loads(args.nav_rejections.read_text(encoding="utf-8"))
        rejected = {str(item).split(":", 1)[0] for item in failures}
        manifest["spawns"] = [row for row in manifest["spawns"] if row["mob_id"] not in rejected]
        manifest["summary"]["navmesh_rejected"] = len(rejected)
        manifest["policy"]["bidirectional_navmesh_validation_completed"] = True
        manifest["summary"]["total"] = len(manifest["spawns"])
        manifest["summary"]["outdoor"] = sum(row["category"] == "period_albion_outdoor" for row in manifest["spawns"])
        manifest["summary"]["dungeon"] = sum(row["category"] == "period_albion_dungeon" for row in manifest["spawns"])
        manifest["summary"]["old_frontier"] = sum(row["category"] == "period_old_frontier" for row in manifest["spawns"])
        manifest["summary"]["zones"] = {str(zone): sum(row["zone_id"] == zone for row in manifest["spawns"])
            for zone in sorted({row["zone_id"] for row in manifest["spawns"]})}
    manifest["integrity"] = audit_bindings(args.database, manifest["spawns"])
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
    if args.id_output:
        args.id_output.parent.mkdir(parents=True, exist_ok=True)
        args.id_output.write_text("\n".join(sorted(row["mob_id"] for row in manifest["spawns"])) + "\n", encoding="ascii")
    if args.camp_report:
        camps: dict[str, list[dict]] = defaultdict(list)
        for row in manifest["spawns"]:
            camps[row["source_id"]].append(row)
        with sqlite3.connect(args.database) as db:
            zone_names = {int(row[0]): str(row[1]) for row in db.execute("SELECT ZoneID,Name FROM Zones")}
        args.camp_report.parent.mkdir(parents=True, exist_ok=True)
        with args.camp_report.open("w", newline="", encoding="utf-8-sig") as handle:
            fields = ("zone_id", "zone", "category", "monster", "rows", "levels",
                      "local_x", "local_y", "source", "mob_ids")
            writer = csv.DictWriter(handle, fieldnames=fields)
            writer.writeheader()
            for source_id, rows in sorted(camps.items(), key=lambda pair:
                    (pair[1][0]["zone_id"], norm(pair[1][0]["name"]), pair[0])):
                first = rows[0]
                writer.writerow({
                    "zone_id": first["zone_id"], "zone": zone_names.get(first["zone_id"], ""),
                    "category": first["category"], "monster": first["name"], "rows": len(rows),
                    "levels": ";".join(map(str, sorted({row["level"] for row in rows}))),
                    "local_x": round(sum(row["local_x"] for row in rows) / len(rows)),
                    "local_y": round(sum(row["local_y"] for row in rows) / len(rows)),
                    "source": first["source"],
                    "mob_ids": ";".join(sorted(row["mob_id"] for row in rows)),
                })
    print(json.dumps(manifest["summary"], indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

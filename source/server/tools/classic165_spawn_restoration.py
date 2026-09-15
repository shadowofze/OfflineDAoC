#!/usr/bin/env python3
"""Select a deliberately small, source-corroborated Classic 1.65 spawn restore set.

This tool never writes the database.  Removed database rows supply candidate
NPC records and exact server coordinates, but a row is accepted only when an
independent period source corroborates the same monster and location.  The
output is an immutable migration manifest that can be reviewed, navmesh-tested,
and then embedded in OfflineDaoc.Setup.
"""

from __future__ import annotations

import argparse
import csv
import json
import math
import sqlite3
import xml.etree.ElementTree as ET
from collections import defaultdict
from dataclasses import dataclass
from pathlib import Path


CAMP_CELL = 4200
ALIGN_RADIUS = 1500
TARGET_CAMP_SIZE = 2

OLD_FRONTIER_ZONES = frozenset({11, 12, 14, 15, 111, 112, 113, 115, 210, 212, 214})
ALBION_MAINLAND_ZONES = frozenset({0, 1, 2, 3, 4, 6, 7, 8, 9, 10})

# CapnBry publishes no usable zone-211 coordinates.  These deliberately few
# landmarks were transcribed from Kirstena's 2002 Cruachan Gorge mob-location
# map.  The radius accounts for the map-label width; archive rows outside the
# mapped neighborhood are rejected even when their monster name is historical.
CRUACHAN_LANDMARKS = (
    ("roan stepper", 16_000, 60_000, 5_000),
    ("siog seeker", 10_000, 60_000, 5_000),
    ("squabbler", 21_500, 58_500, 5_000),
    ("graugach", 24_000, 56_000, 5_000),
    ("sett protector", 27_500, 61_500, 5_000),
    ("sett youngling", 29_000, 63_000, 5_000),
    ("curmudgeon puggard", 32_000, 56_000, 5_000),
    ("spectral briton invader", 34_000, 57_500, 5_000),
    ("cruach imp", 37_000, 59_000, 5_000),
)


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
class MobRow:
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


def normalized_name(value: str) -> str:
    return " ".join((value or "").strip().split()).casefold()


def load_zones(connection: sqlite3.Connection) -> dict[int, Zone]:
    rows = connection.execute(
        """SELECT ZoneID,RegionID,Name,OffsetX*8192,OffsetY*8192,
                  Width*8192,Height*8192 FROM Zones""").fetchall()
    return {int(row[0]): Zone(*map(int, row[:2]), row[2], *map(int, row[3:])) for row in rows}


def load_mobs(connection: sqlite3.Connection, table: str,
              zones: dict[int, Zone], zone_ids: set[int] | frozenset[int],
              excluded_ids: set[str] | None = None) -> dict[tuple[int, str], list[MobRow]]:
    result: dict[tuple[int, str], list[MobRow]] = defaultdict(list)
    zones_by_region: dict[int, list[Zone]] = defaultdict(list)
    for zone in zones.values():
        if zone.zone_id in zone_ids:
            zones_by_region[zone.region_id].append(zone)
    rows = connection.execute(
        f"""SELECT Mob_ID,Name,Region,X,Y,Z,Level FROM {table}
             WHERE ClassType='DOL.GS.GameNPC' AND Realm=0""").fetchall()
    for mob_id, name, region_id, x, y, z, level in rows:
        if excluded_ids and str(mob_id) in excluded_ids:
            continue
        zone = next((candidate for candidate in zones_by_region.get(int(region_id), ())
                     if candidate.offset_x <= int(x) < candidate.offset_x + candidate.width
                     and candidate.offset_y <= int(y) < candidate.offset_y + candidate.height), None)
        if zone is None:
            continue
        clean = normalized_name(name)
        result[(zone.zone_id, clean)].append(MobRow(
            str(mob_id), str(name), zone.zone_id, zone.region_id,
            int(x) - zone.offset_x, int(y) - zone.offset_y,
            int(x), int(y), int(z), int(level)))
    return result


def load_capnbry_sightings(cache: Path, zone_ids: set[int] | frozenset[int]) -> dict[tuple[int, str, int, int], list[tuple[int, int, int, int, int]]]:
    result: dict[tuple[int, str, int, int], list[tuple[int, int, int, int, int]]] = defaultdict(list)
    for path in sorted((cache / "mobs").glob("*.xml")):
        root = ET.parse(path).getroot()
        mob = root.find("mob")
        if mob is None:
            continue
        name = (mob.findtext("name") or "").strip()
        if not name or name != name.lower():
            continue
        mob_id = int(mob.findtext("mob_id") or path.stem)
        for seen in mob.findall("mobseen"):
            try:
                zone_id = int(seen.findtext("zone", "-1"))
                x = int(seen.findtext("x", "0"))
                y = int(seen.findtext("y", "0"))
                z = int(seen.findtext("z", "0"))
                level = int(seen.findtext("level", "0"))
            except ValueError:
                continue
            if zone_id in zone_ids and 1 <= level <= 50:
                result[(zone_id, normalized_name(name), x // CAMP_CELL, y // CAMP_CELL)].append(
                    (x, y, z, level, mob_id))
    return result


def squared_distance(ax: int, ay: int, bx: int, by: int) -> int:
    return (ax - bx) ** 2 + (ay - by) ** 2


def choose_rows(candidates: list[MobRow], witnesses: list[tuple[int, int, int, int, int]],
                current: list[MobRow], used_ids: set[str], target_size: int,
                radius: int) -> list[tuple[MobRow, int]]:
    source_levels = {item[3] for item in witnesses}
    radius_squared = radius * radius

    def evidence_distance(row: MobRow) -> int:
        return min(squared_distance(row.local_x, row.local_y, item[0], item[1]) for item in witnesses)

    matching_current = [row for row in current
                        if evidence_distance(row) <= radius_squared
                        and min(abs(row.level - level) for level in source_levels) <= 1]
    needed = max(0, target_size - len(matching_current))
    eligible = [row for row in candidates
                if row.mob_id not in used_ids
                and 1 <= row.level <= 50
                and evidence_distance(row) <= radius_squared
                and min(abs(row.level - level) for level in source_levels) <= 1]
    eligible.sort(key=lambda row: (evidence_distance(row), row.mob_id))
    selected = []
    for row in eligible:
        if len(selected) >= needed:
            break
        used_ids.add(row.mob_id)
        selected.append((row, round(math.sqrt(evidence_distance(row)))))
    return selected


def item_for(row: MobRow, category: str, source: str, source_id: str,
             source_x: int, source_y: int, distance: int) -> dict:
    return {
        "mob_id": row.mob_id,
        "name": row.name,
        "zone_id": row.zone_id,
        "region_id": row.region_id,
        "local_x": row.local_x,
        "local_y": row.local_y,
        "world_x": row.world_x,
        "world_y": row.world_y,
        "z": row.z,
        "level": row.level,
        "category": category,
        "source": source,
        "source_id": source_id,
        "source_x": source_x,
        "source_y": source_y,
        "source_distance": distance,
    }


def build_manifest(database: Path, cache: Path) -> dict:
    all_zones = set(OLD_FRONTIER_ZONES | ALBION_MAINLAND_ZONES | {211})
    with sqlite3.connect(database) as connection:
        zones = load_zones(connection)
        has_tracking = connection.execute(
            "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='offline_classic165_restored_mobs'").fetchone()[0]
        tracked = ({str(row[0]) for row in connection.execute(
            "SELECT Mob_ID FROM offline_classic165_restored_mobs")} if has_tracking else set())
        archive = load_mobs(connection, "offline_classic165_removed_mobs", zones, all_zones)
        # A post-migration audit must reproduce the same manifest. Previously
        # restored rows are removed from the baseline-current count, then
        # selected again from the immutable archive by the same evidence rules.
        live = load_mobs(connection, "Mob", zones, all_zones, tracked)

    sightings = load_capnbry_sightings(cache, OLD_FRONTIER_ZONES | ALBION_MAINLAND_ZONES)
    used_ids: set[str] = set()
    restored: list[dict] = []
    per_source: dict[str, int] = defaultdict(int)

    for key, witnesses in sorted(sightings.items()):
        zone_id, name, cell_x, cell_y = key
        chosen = choose_rows(archive.get((zone_id, name), []), witnesses,
                             live.get((zone_id, name), []), used_ids,
                             TARGET_CAMP_SIZE, ALIGN_RADIUS)
        if not chosen:
            continue
        category = "old_frontier" if zone_id in OLD_FRONTIER_ZONES else "albion_fragment"
        source_mob_id = witnesses[0][4]
        center_x = round(sum(item[0] for item in witnesses) / len(witnesses))
        center_y = round(sum(item[1] for item in witnesses) / len(witnesses))
        source_id = f"capnbry:{zone_id}:{cell_x}:{cell_y}:{name}"
        for row, distance in chosen:
            restored.append(item_for(row, category,
                f"http://capnbry.net/daoc/mobs.php?f=xml&m={source_mob_id}",
                source_id, center_x, center_y, distance))
            per_source[source_id] += 1

    # Cruachan is intentionally independent of the empty CapnBry page.  Each
    # map landmark is a separate tiny camp and gets no more than two rows.
    for name, source_x, source_y, radius in CRUACHAN_LANDMARKS:
        witnesses = [(source_x, source_y, 0, row.level, 0)
                     for row in archive.get((211, name), [])]
        if not witnesses:
            continue
        source_id = f"kirstena-2002:211:{name}"
        chosen = choose_rows(archive.get((211, name), []), witnesses,
                             live.get((211, name), []), used_ids,
                             TARGET_CAMP_SIZE, radius)
        for row, distance in chosen:
            restored.append(item_for(row, "old_frontier",
                "Kirstena's Atlas Cruachan Gorge mob-location map, updated 2002-06-14",
                source_id, source_x, source_y, distance))
            per_source[source_id] += 1

    restored.sort(key=lambda item: (item["zone_id"], item["source_id"], item["mob_id"]))
    return {
        "schema": 1,
        "policy": {
            "capnbry_alignment_radius": ALIGN_RADIUS,
            "maximum_rows_added_per_source_camp": TARGET_CAMP_SIZE,
            "archive_is_candidate_only": True,
            "synthetic_monster_types": False,
            "navmesh_validation_required": True,
        },
        "sources": [
            "http://capnbry.net/daoc/",
            "Kirstena's Atlas Cruachan Gorge mob-location map, updated 2002-06-14",
        ],
        "spawns": restored,
        "summary": {
            "total": len(restored),
            "old_frontier": sum(item["category"] == "old_frontier" for item in restored),
            "albion_fragment": sum(item["category"] == "albion_fragment" for item in restored),
            "source_camps": len(per_source),
        },
    }


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--database", type=Path, required=True)
    parser.add_argument("--capnbry-cache", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--nav-rejections", type=Path)
    parser.add_argument("--id-output", type=Path)
    parser.add_argument("--camp-report", type=Path)
    args = parser.parse_args()
    manifest = build_manifest(args.database, args.capnbry_cache)
    rejected_ids: set[str] = set()
    if args.nav_rejections:
        failures = json.loads(args.nav_rejections.read_text(encoding="utf-8"))
        rejected_ids = {str(item).split(":", 1)[0] for item in failures}
        manifest["spawns"] = [item for item in manifest["spawns"] if item["mob_id"] not in rejected_ids]
        manifest["summary"]["total"] = len(manifest["spawns"])
        manifest["summary"]["old_frontier"] = sum(
            item["category"] == "old_frontier" for item in manifest["spawns"])
        manifest["summary"]["albion_fragment"] = sum(
            item["category"] == "albion_fragment" for item in manifest["spawns"])
        manifest["summary"]["source_camps"] = len({item["source_id"] for item in manifest["spawns"]})
        manifest["summary"]["navmesh_rejected"] = len(rejected_ids)
        manifest["policy"]["navmesh_validation_completed"] = True
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(manifest, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    if args.id_output:
        args.id_output.parent.mkdir(parents=True, exist_ok=True)
        ids = sorted(item["mob_id"] for item in manifest["spawns"])
        args.id_output.write_text("\n".join(ids) + "\n", encoding="ascii")
    if args.camp_report:
        with sqlite3.connect(args.database) as connection:
            zone_names = {int(row[0]): str(row[1]) for row in
                          connection.execute("SELECT ZoneID,Name FROM Zones")}
        camps: dict[str, list[dict]] = defaultdict(list)
        for item in manifest["spawns"]:
            camps[item["source_id"]].append(item)
        args.camp_report.parent.mkdir(parents=True, exist_ok=True)
        with args.camp_report.open("w", newline="", encoding="utf-8-sig") as handle:
            fields = ["zone_id", "zone", "monster", "restored_rows", "levels",
                      "local_x", "local_y", "source", "source_distance_max", "mob_ids"]
            writer = csv.DictWriter(handle, fieldnames=fields)
            writer.writeheader()
            for source_id, rows in sorted(camps.items(), key=lambda pair:
                    (pair[1][0]["zone_id"], pair[1][0]["name"].casefold(), pair[0])):
                first = rows[0]
                writer.writerow({
                    "zone_id": first["zone_id"],
                    "zone": zone_names.get(first["zone_id"], ""),
                    "monster": first["name"],
                    "restored_rows": len(rows),
                    "levels": ";".join(map(str, sorted({row["level"] for row in rows}))),
                    "local_x": round(sum(row["local_x"] for row in rows) / len(rows)),
                    "local_y": round(sum(row["local_y"] for row in rows) / len(rows)),
                    "source": first["source"],
                    "source_distance_max": max(row["source_distance"] for row in rows),
                    "mob_ids": ";".join(sorted(row["mob_id"] for row in rows)),
                })
    print(json.dumps(manifest["summary"], indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

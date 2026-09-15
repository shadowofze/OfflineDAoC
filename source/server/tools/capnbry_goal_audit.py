#!/usr/bin/env python3
"""Build and audit the Offline DAoC PvE goal catalog against CapnBry.

CapnBry supplies the authoritative monster names, levels, zone-local positions,
and sightings.  The local database is used only to determine whether this
particular server build has a compatible live spawn near that authoritative
position.  New Frontiers data is deliberately reported but not imported: this
1.65 server runs Old Frontiers, whose zone IDs and geography are different.
"""

from __future__ import annotations

import argparse
import csv
import json
import math
import re
import sqlite3
import time
import urllib.error
import urllib.request
import xml.etree.ElementTree as ET
from collections import Counter, defaultdict
from concurrent.futures import ThreadPoolExecutor, as_completed
from dataclasses import dataclass
from pathlib import Path


BASE_URL = "http://capnbry.net/daoc"
CAMP_CELL = 4200
TARGET_RADIUS = 2600
ALIGNED_RADIUS = 1500

# Classic, Shrouded Isles, their dungeons, and Darkness Falls.  Capital cities,
# housing, ToA, later expansions, battlegrounds, and CapnBry's New Frontiers
# copies are intentionally not bot grinding destinations.
SUPPORTED_ZONE_IDS = frozenset(
    # Albion Classic / SI.
    [0, 1, 2, 3, 4, 6, 7, 8, 9, 10, 11, 12, 14, 15, 19, 21, 22, 23, 24,
     50, 51, 52, 53, 54, 55, 56, 57, 60, 61, 62]
    # Midgard Classic / SI.
    + [100, 101, 102, 103, 104, 105, 106, 107, 108, 111, 112, 113, 115,
       116, 125, 126, 127, 128, 129, 150, 151, 152, 153, 154, 155, 156,
       158, 160, 161]
    # Hibernia Classic / SI.
    + [180, 181, 182, 183, 184, 185, 186, 187, 190, 191, 200, 201, 202,
       203, 204, 205, 206, 207, 208, 210, 211, 212, 214, 216, 220, 221,
       222, 223, 224]
    # Shared Classic dungeon.
    + [249]
)

CAPITAL_ZONE_IDS = frozenset([26, 120, 209])
NEW_FRONTIER_ZONE_IDS = frozenset(range(163, 179))


@dataclass(frozen=True)
class Zone:
    zone_id: int
    region_id: int
    name: str
    offset_x: int
    offset_y: int
    width: int
    height: int
    expansion: int


@dataclass(frozen=True)
class Sighting:
    mob_id: int
    name: str
    zone_id: int
    x: int
    y: int
    z: int
    level: int


def normalized_name(value: str) -> str:
    return " ".join((value or "").strip().split()).casefold()


def is_strict_lowercase_regular_name(value: str) -> bool:
    value = " ".join((value or "").strip().split())
    return bool(value and any(ch.isalpha() for ch in value) and value == value.lower())


def fetch(url: str, cache_path: Path, retries: int = 4) -> bytes:
    if cache_path.exists() and cache_path.stat().st_size:
        return cache_path.read_bytes()
    cache_path.parent.mkdir(parents=True, exist_ok=True)
    last_error = None
    for attempt in range(retries):
        try:
            request = urllib.request.Request(url, headers={"User-Agent": "OfflineDAoC-CapnBry-Audit/1.0"})
            with urllib.request.urlopen(request, timeout=35) as response:
                data = response.read()
            cache_path.write_bytes(data)
            return data
        except (OSError, urllib.error.URLError) as exc:
            last_error = exc
            time.sleep(0.5 * (attempt + 1))
    raise RuntimeError(f"Could not fetch {url}: {last_error}")


def parse_zone_indexes(cache_dir: Path) -> dict[int, str]:
    zones: dict[int, str] = {}
    pattern = re.compile(r"<td>(\d+)</td>\s*<td>([^<]+)</td>", re.I)
    for realm in range(1, 6):
        data = fetch(f"{BASE_URL}/mobs.php?a=zones&r={realm}", cache_dir / f"zones-r{realm}.html")
        text = data.decode("latin1", "replace")
        for zone_id, name in pattern.findall(text):
            zones[int(zone_id)] = re.sub(r"\s+", " ", name).strip()
    return zones


def parse_mob_indexes(cache_dir: Path) -> dict[int, str]:
    mobs: dict[int, str] = {}
    for realm in range(1, 6):
        data = fetch(f"{BASE_URL}/mobs.php?f=xml&r={realm}", cache_dir / f"mobs-r{realm}.xml")
        root = ET.fromstring(data)
        for element in root.findall("mob"):
            mob_id = int(element.findtext("mob_id", "0"))
            name = element.findtext("mobname", "").strip()
            if mob_id and is_strict_lowercase_regular_name(name):
                mobs[mob_id] = name
    return mobs


def parse_mob_sightings(mob_id: int, indexed_name: str, cache_dir: Path) -> list[Sighting]:
    data = fetch(f"{BASE_URL}/mobs.php?f=xml&m={mob_id}", cache_dir / "mobs" / f"{mob_id}.xml")
    root = ET.fromstring(data)
    mob = root.find("mob")
    if mob is None:
        return []
    name = (mob.findtext("name") or indexed_name).strip()
    if not is_strict_lowercase_regular_name(name):
        return []
    result = []
    for seen in mob.findall("mobseen"):
        try:
            result.append(Sighting(
                mob_id, name, int(seen.findtext("zone", "-1")),
                int(seen.findtext("x", "0")), int(seen.findtext("y", "0")),
                int(seen.findtext("z", "0")), int(seen.findtext("level", "0"))))
        except ValueError:
            continue
    return result


def load_zones(connection: sqlite3.Connection) -> dict[int, Zone]:
    rows = connection.execute(
        """select z.ZoneID,z.RegionID,z.Name,z.OffsetX,z.OffsetY,z.Width,z.Height,r.Expansion
             from Zones z join Regions r on r.RegionID=z.RegionID""").fetchall()
    return {
        int(row[0]): Zone(int(row[0]), int(row[1]), row[2], int(row[3]) * 8192,
                          int(row[4]) * 8192, int(row[5]) * 8192,
                          int(row[6]) * 8192, int(row[7]))
        for row in rows
    }


def local_spawn_rows(connection: sqlite3.Connection, zones: dict[int, Zone]) -> list[dict]:
    by_region: dict[int, list[Zone]] = defaultdict(list)
    for zone in zones.values():
        if zone.zone_id in SUPPORTED_ZONE_IDS:
            by_region[zone.region_id].append(zone)
    rows = connection.execute(
        """select Mob_ID,Name,Suffix,Region,X,Y,Z,Level,ClassType,Realm,Flags
             from Mob where ClassType='DOL.GS.GameNPC' and Realm=0""").fetchall()
    result = []
    for mob_id, name, suffix, region_id, x, y, z, level, class_type, realm, flags in rows:
        if not is_strict_lowercase_regular_name(name):
            continue
        zone = next((candidate for candidate in by_region.get(int(region_id), [])
                     if candidate.offset_x <= int(x) < candidate.offset_x + candidate.width
                     and candidate.offset_y <= int(y) < candidate.offset_y + candidate.height), None)
        if zone is None:
            continue
        result.append({
            "mob_id": mob_id, "name": name, "normalized_name": normalized_name(name),
            "suffix": suffix or "", "zone_id": zone.zone_id, "region_id": zone.region_id,
            "local_x": int(x) - zone.offset_x, "local_y": int(y) - zone.offset_y,
            "z": int(z), "level": int(level), "flags": int(flags),
        })
    return result


def cluster_sightings(sightings: list[Sighting]) -> list[dict]:
    groups: dict[tuple[int, str, int, int], list[Sighting]] = defaultdict(list)
    for seen in sightings:
        groups[(seen.zone_id, normalized_name(seen.name), seen.x // CAMP_CELL, seen.y // CAMP_CELL)].append(seen)
    clusters = []
    for (zone_id, key_name, cell_x, cell_y), members in sorted(groups.items()):
        levels = sorted({member.level for member in members if 1 <= member.level <= 50})
        if not levels:
            continue
        clusters.append({
            "id": f"capnbry:{zone_id}:{cell_x}:{cell_y}:{key_name}",
            "mob_id": members[0].mob_id,
            "name": members[0].name,
            "normalized_name": key_name,
            "zone_id": zone_id,
            "local_x": round(sum(member.x for member in members) / len(members)),
            "local_y": round(sum(member.y for member in members) / len(members)),
            "z": round(sum(member.z for member in members) / len(members)),
            "levels": levels,
            "sightings": len(members),
            "source": f"{BASE_URL}/mobs.php?f=xml&m={members[0].mob_id}",
        })
    return clusters


def cluster_local(rows: list[dict]) -> list[dict]:
    groups: dict[tuple[int, str, int, int], list[dict]] = defaultdict(list)
    for row in rows:
        groups[(row["zone_id"], row["normalized_name"],
                row["local_x"] // CAMP_CELL, row["local_y"] // CAMP_CELL)].append(row)
    clusters = []
    for (zone_id, name, cell_x, cell_y), members in sorted(groups.items()):
        clusters.append({
            "id": f"local:{zone_id}:{cell_x}:{cell_y}:{name}",
            "name": members[0]["name"], "normalized_name": name, "zone_id": zone_id,
            "local_x": round(sum(member["local_x"] for member in members) / len(members)),
            "local_y": round(sum(member["local_y"] for member in members) / len(members)),
            "z": round(sum(member["z"] for member in members) / len(members)),
            "levels": sorted({member["level"] for member in members if member["level"] > 0}),
            "spawn_count": len(members),
        })
    return clusters


def distance(a: dict, b: dict) -> float:
    return math.hypot(a["local_x"] - b["local_x"], a["local_y"] - b["local_y"])


def compare(authoritative: list[dict], local: list[dict], zone_names: dict[int, str]) -> tuple[list[dict], list[dict]]:
    authoritative_by_key: dict[tuple[int, str], list[dict]] = defaultdict(list)
    local_by_key: dict[tuple[int, str], list[dict]] = defaultdict(list)
    for cluster in authoritative:
        authoritative_by_key[(cluster["zone_id"], cluster["normalized_name"])].append(cluster)
    for cluster in local:
        local_by_key[(cluster["zone_id"], cluster["normalized_name"])].append(cluster)

    audit = []
    runtime = []
    for cluster in authoritative:
        candidates = local_by_key.get((cluster["zone_id"], cluster["normalized_name"]), [])
        nearest = min(candidates, key=lambda item: distance(cluster, item)) if candidates else None
        gap = round(distance(cluster, nearest)) if nearest else None
        if nearest is None:
            status = "missing_local_spawn"
        elif gap <= ALIGNED_RADIUS:
            status = "aligned"
        elif gap <= TARGET_RADIUS:
            status = "nearby_compatible"
        elif gap <= CAMP_CELL:
            status = "coordinate_drift"
        else:
            status = "misplaced"
        level_overlap = bool(nearest and (not nearest["levels"] or set(cluster["levels"]) & set(nearest["levels"])))
        audit.append({
            "direction": "capnbry_to_local", "status": status, "zone_id": cluster["zone_id"],
            "zone": zone_names.get(cluster["zone_id"], ""), "name": cluster["name"],
            "capnbry_x": cluster["local_x"], "capnbry_y": cluster["local_y"],
            "capnbry_levels": ";".join(map(str, cluster["levels"])),
            "local_x": nearest["local_x"] if nearest else "",
            "local_y": nearest["local_y"] if nearest else "",
            "local_levels": ";".join(map(str, nearest["levels"])) if nearest else "",
            "distance": gap if gap is not None else "", "level_overlap": level_overlap,
            "source": cluster["source"],
        })
        # The authoritative coordinate is used only when a killable local spawn
        # actually exists within the controller's acquisition radius.
        if nearest is not None and gap <= TARGET_RADIUS:
            item = dict(cluster)
            item.update({
                "zone": zone_names.get(cluster["zone_id"], ""),
                "region_id": None,
                "local_spawn_count": nearest["spawn_count"],
                "local_distance": gap,
            })
            runtime.append(item)

    for cluster in local:
        candidates = authoritative_by_key.get((cluster["zone_id"], cluster["normalized_name"]), [])
        nearest = min(candidates, key=lambda item: distance(cluster, item)) if candidates else None
        gap = round(distance(cluster, nearest)) if nearest else None
        if nearest is None or gap > TARGET_RADIUS:
            audit.append({
                "direction": "local_to_capnbry", "status": "unverified_current_goal",
                "zone_id": cluster["zone_id"], "zone": zone_names.get(cluster["zone_id"], ""),
                "name": cluster["name"], "capnbry_x": nearest["local_x"] if nearest else "",
                "capnbry_y": nearest["local_y"] if nearest else "",
                "capnbry_levels": ";".join(map(str, nearest["levels"])) if nearest else "",
                "local_x": cluster["local_x"], "local_y": cluster["local_y"],
                "local_levels": ";".join(map(str, cluster["levels"])),
                "distance": gap if gap is not None else "", "level_overlap": "",
                "source": nearest["source"] if nearest else "",
            })
    return audit, runtime


def write_outputs(output_dir: Path, generated_path: Path, zones: dict[int, Zone],
                  zone_names: dict[int, str], authoritative: list[dict], local: list[dict],
                  audit: list[dict], runtime: list[dict], excluded_nf_sightings: int) -> None:
    output_dir.mkdir(parents=True, exist_ok=True)
    generated_path.parent.mkdir(parents=True, exist_ok=True)
    region_by_zone = {zone_id: zone.region_id for zone_id, zone in zones.items()}
    for item in runtime:
        item["region_id"] = region_by_zone[item["zone_id"]]
    authoritative_zone_counts = Counter(item["zone_id"] for item in authoritative)
    supported_site_zones = sorted(SUPPORTED_ZONE_IDS & zone_names.keys())
    source_empty_zones = [zone_id for zone_id in supported_site_zones if authoritative_zone_counts[zone_id] == 0]
    payload = {
        "schema": 1,
        "authority": "CapnBry DAoC Bestiary",
        "source": BASE_URL + "/",
        "camp_cell_size": CAMP_CELL,
        "target_match_radius": TARGET_RADIUS,
        "covered_zone_ids": sorted(authoritative_zone_counts),
        "source_empty_supported_zone_ids": source_empty_zones,
        "goals": runtime,
    }
    generated_path.write_text(json.dumps(payload, separators=(",", ":"), ensure_ascii=False), encoding="utf-8")

    fields = ["direction", "status", "zone_id", "zone", "name", "capnbry_x", "capnbry_y",
              "capnbry_levels", "local_x", "local_y", "local_levels", "distance",
              "level_overlap", "source"]
    with (output_dir / "capnbry_goal_discrepancies.csv").open("w", newline="", encoding="utf-8-sig") as handle:
        writer = csv.DictWriter(handle, fieldnames=fields)
        writer.writeheader()
        writer.writerows(audit)

    counts = Counter(item["status"] for item in audit)
    level_counts = Counter(level for item in runtime for level in item["levels"])
    zone_counts = Counter(item["zone"] for item in runtime)
    local_zone_counts = Counter(item["zone_id"] for item in local)
    runtime_zone_counts = Counter(item["zone_id"] for item in runtime)
    report = [
        "# CapnBry authoritative goal audit", "",
        f"- CapnBry lowercase Classic/SI/DF clusters: **{len(authoritative):,}**",
        f"- Current local lowercase spawn clusters: **{len(local):,}**",
        f"- Runtime-safe authoritative goals (local target within {TARGET_RADIUS} units): **{len(runtime):,}**",
        f"- CapnBry New Frontiers sightings quarantined as incompatible with this Old Frontiers runtime: **{excluded_nf_sightings:,}**",
        f"- Supported zones whose CapnBry page contains no lowercase level 1-50 sightings: **{len(source_empty_zones):,}**",
        "", "## Discrepancy classifications", "",
    ]
    report.extend(f"- {key}: **{value:,}**" for key, value in sorted(counts.items()))
    report += ["", "## Runtime-safe goals by monster level", "",
               "| Level | Goal clusters |", "|---:|---:|"]
    report.extend(f"| {level} | {level_counts.get(level, 0):,} |" for level in range(1, 51))
    report += ["", "## Runtime-safe goals by zone", "", "| Zone | Goal clusters |", "|---|---:|"]
    report.extend(f"| {name} | {count:,} |" for name, count in sorted(zone_counts.items()))
    report += ["", "## Full supported-zone coverage", "",
               "| Zone ID | Zone | CapnBry clusters | Current local clusters | Runtime-safe goals |",
               "|---:|---|---:|---:|---:|"]
    report.extend(
        f"| {zone_id} | {zone_names.get(zone_id, zones[zone_id].name)} | "
        f"{authoritative_zone_counts[zone_id]:,} | {local_zone_counts[zone_id]:,} | {runtime_zone_counts[zone_id]:,} |"
        for zone_id in supported_site_zones)
    if source_empty_zones:
        report += ["", "## CapnBry source coverage gaps", "",
                   "These compatible zones are present in the local client/server, but their CapnBry zone pages publish no lowercase level 1-50 sightings. No authoritative coordinates were invented for them.", ""]
        report.extend(f"- {zone_id}: {zone_names.get(zone_id, zones[zone_id].name)}" for zone_id in source_empty_zones)
    (output_dir / "capnbry_goal_audit.md").write_text("\n".join(report) + "\n", encoding="utf-8")
    (output_dir / "capnbry_goal_audit.json").write_text(json.dumps({
        "summary": dict(counts), "authoritative_clusters": len(authoritative),
        "local_clusters": len(local), "runtime_safe_goals": len(runtime),
        "new_frontiers_sightings_quarantined": excluded_nf_sightings,
        "source_empty_supported_zones": source_empty_zones,
        "levels": {str(level): level_counts.get(level, 0) for level in range(1, 51)},
    }, indent=2), encoding="utf-8")


def download_maps(zone_names: dict[int, str], cache_dir: Path) -> None:
    for zone_id in sorted(SUPPORTED_ZONE_IDS & zone_names.keys()):
        try:
            fetch(f"{BASE_URL}/maps/zone{zone_id:03}.jpg", cache_dir / "maps" / f"zone{zone_id:03}.jpg", retries=2)
        except RuntimeError:
            # Not every dungeon has a raster map; the coordinate XML remains
            # authoritative and the missing map is visible in the audit cache.
            continue


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--database", type=Path, required=True)
    parser.add_argument("--cache", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--generated", type=Path, required=True)
    parser.add_argument("--workers", type=int, default=8)
    parser.add_argument("--download-maps", action="store_true")
    args = parser.parse_args()

    args.cache.mkdir(parents=True, exist_ok=True)
    zone_names = parse_zone_indexes(args.cache)
    mob_index = parse_mob_indexes(args.cache)
    print(f"CapnBry lowercase mob index: {len(mob_index):,}", flush=True)

    all_sightings: list[Sighting] = []
    errors = []
    with ThreadPoolExecutor(max_workers=max(1, args.workers)) as pool:
        futures = {pool.submit(parse_mob_sightings, mob_id, name, args.cache): mob_id
                   for mob_id, name in mob_index.items()}
        completed = 0
        for future in as_completed(futures):
            try:
                all_sightings.extend(future.result())
            except Exception as exc:  # keep a complete bounded failure list
                errors.append((futures[future], str(exc)))
            completed += 1
            if completed % 250 == 0:
                print(f"Fetched {completed:,}/{len(futures):,} monster records", flush=True)
    if errors:
        raise RuntimeError(f"{len(errors)} CapnBry records failed; first failures: {errors[:10]}")

    compatible = [seen for seen in all_sightings
                  if seen.zone_id in SUPPORTED_ZONE_IDS and 1 <= seen.level <= 50]
    excluded_nf = sum(1 for seen in all_sightings if seen.zone_id in NEW_FRONTIER_ZONE_IDS)
    authoritative = cluster_sightings(compatible)

    with sqlite3.connect(args.database) as connection:
        zones = load_zones(connection)
        missing_zones = sorted(SUPPORTED_ZONE_IDS - zones.keys())
        if missing_zones:
            raise RuntimeError(f"Supported CapnBry zones missing from this client/server DB: {missing_zones}")
        local = cluster_local(local_spawn_rows(connection, zones))

    audit, runtime = compare(authoritative, local, zone_names)
    write_outputs(args.output, args.generated, zones, zone_names, authoritative, local, audit, runtime, excluded_nf)
    if args.download_maps:
        download_maps(zone_names, args.cache)
    print(f"Authoritative clusters: {len(authoritative):,}", flush=True)
    print(f"Runtime-safe goals: {len(runtime):,}", flush=True)
    print(f"Audit rows: {len(audit):,}", flush=True)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

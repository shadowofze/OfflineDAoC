"""Build the small, static SQLite overlay used by the optional Sluaghbinder patch.

The source database is read-only.  Only rows owned by class 63, the named
Sluaghbinder lines/spells/styles/pet templates, Muirenn's two world rows, and
the feature's own NPC equipment templates are copied. Accounts, characters,
inventory, bots, settings, logs, and any other save data never enter the overlay.

Usage:
    python build_overlay.py --source-db <new-class-test-db> --output <overlay.json>
"""

from __future__ import annotations

import argparse
import json
import sqlite3
from pathlib import Path


LINE_KEYS = (
    "Sluagh Host",
    "Abhartach's Rot",
    "Cairn Oath",
    "Dullahan's Bulwark",
    "Abhartach's Bane",
    "Sluagh Covenant",
    "Epic Spells",
    "Sluaghbinder's Legacy",
)
SPEC_KEYS = (
    "SluaghbinderCareer",
    *LINE_KEYS,
)
PET_TEMPLATE_IDS = tuple(range(60170001, 60170008))
MOB_IDS = (
    "sluaghbinder_trainer_tir_na_nog",
    "sluaghbinder_bound_wisp_tir_na_nog",
)
EQUIPMENT_TEMPLATE_IDS = (
    "sluagh_zombie_magician_staff",
    "sluagh_zombie_guardian_mace_shield",
    "sluagh_zombie_priest_mace_buckler",
    "sluagh_cairn_dullahan_flail_shield",
    "SluaghbinderMuirennBlack",
)


def table_columns(db: sqlite3.Connection, table: str) -> list[str]:
    return [row[1] for row in db.execute(f'PRAGMA table_info("{table}")')]


def table_rows(db: sqlite3.Connection, table: str) -> list[dict[str, object]]:
    columns = table_columns(db, table)
    if not columns:
        raise RuntimeError(f"Required table is missing: {table}")
    rows = db.execute(f'SELECT * FROM "{table}"').fetchall()
    return [dict(zip(columns, row)) for row in rows]


def select_rows(db: sqlite3.Connection, table: str) -> list[dict[str, object]]:
    rows = table_rows(db, table)
    if table == "Specialization":
        return [r for r in rows if r.get("KeyName") in SPEC_KEYS]
    if table == "SpellLine":
        return [r for r in rows if r.get("KeyName") in LINE_KEYS]
    if table == "LineXSpell":
        return [r for r in rows if r.get("LineName") in LINE_KEYS]
    if table == "ClassXSpecialization":
        return [r for r in rows if r.get("ClassID") == 63]
    if table == "SpecXAbility":
        return [r for r in rows if r.get("Spec") in SPEC_KEYS]
    if table == "Style":
        return [r for r in rows if r.get("ClassId") == 63]
    if table == "Spell":
        return [
            r for r in rows
            if str(r.get("Spell_ID") or "").startswith("Sluaghbinder_")
            or 59000 <= int(r.get("SpellID") or -1) <= 59084
        ]
    if table == "NpcTemplate":
        return [r for r in rows if r.get("TemplateId") in PET_TEMPLATE_IDS]
    if table == "NPCEquipment":
        return [r for r in rows if r.get("TemplateID") in EQUIPMENT_TEMPLATE_IDS]
    if table == "Mob":
        return [r for r in rows if r.get("Mob_ID") in MOB_IDS]
    raise RuntimeError(f"No selector is defined for {table}")


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--source-db", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)
    args = parser.parse_args()
    source = args.source_db.resolve()
    output = args.output.resolve()
    if not source.exists():
        raise SystemExit(f"Source database not found: {source}")
    output.parent.mkdir(parents=True, exist_ok=True)
    with sqlite3.connect(f"file:{source.as_posix()}?mode=ro", uri=True) as db:
        payload = {
            "format": 1,
            "classId": 63,
            "feature": "Sluaghbinder",
            "tables": {
                table: {
                    "columns": table_columns(db, table),
                    "rows": select_rows(db, table),
                }
                for table in (
                    "Specialization",
                    "SpellLine",
                    "LineXSpell",
                    "ClassXSpecialization",
                    "SpecXAbility",
                    "Style",
                    "Spell",
                    "NpcTemplate",
                    "NPCEquipment",
                    "Mob",
                )
            },
        }
    for table, value in payload["tables"].items():
        if not value["rows"]:
            raise SystemExit(f"No Sluaghbinder rows selected from {table}")
    # Write deterministic LF bytes on Windows too. Text-mode newline conversion
    # would otherwise rewrite the whole tracked overlay as CRLF on each build.
    output.write_bytes((json.dumps(payload, ensure_ascii=False, indent=2) + "\n").encode("utf-8"))
    print(f"Wrote {output}")
    for table, value in payload["tables"].items():
        print(f"{table}: {len(value['rows'])} rows")


if __name__ == "__main__":
    main()

"""Report class-scoped differences between a packaged and freshly exported overlay."""

from __future__ import annotations

import argparse
import json
from collections import Counter
from pathlib import Path


def row_id(row: dict[str, object]) -> str:
    for keys in (
        ("Mob_ID",), ("TemplateId",), ("TemplateID", "Slot"),
        ("SpellID",), ("ID",), ("KeyName",), ("LineName", "SpellID", "Level"),
        ("ClassID", "SpecKeyName"), ("Spec", "AbilityKey", "SpecLevel"),
    ):
        if all(key in row for key in keys):
            return "/".join(str(row[key]) for key in keys)
    return json.dumps(row, sort_keys=True, ensure_ascii=False)[:90]


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--packaged", required=True, type=Path)
    parser.add_argument("--isolated", required=True, type=Path)
    args = parser.parse_args()
    packaged = json.loads(args.packaged.read_text(encoding="utf-8"))
    isolated = json.loads(args.isolated.read_text(encoding="utf-8"))
    for table in sorted(set(packaged["tables"]) | set(isolated["tables"])):
        old = packaged["tables"].get(table, {})
        new = isolated["tables"].get(table, {})
        old_rows = old.get("rows", [])
        new_rows = new.get("rows", [])
        old_set = Counter(json.dumps(row, sort_keys=True, ensure_ascii=False) for row in old_rows)
        new_set = Counter(json.dumps(row, sort_keys=True, ensure_ascii=False) for row in new_rows)
        removed = list((old_set - new_set).elements())
        added = list((new_set - old_set).elements())
        changed = bool(removed or added or old.get("columns") != new.get("columns"))
        print(f"{table}: packaged={len(old_rows)} isolated={len(new_rows)} "
              f"removed={len(removed)} added={len(added)} "
              f"columns_equal={old.get('columns') == new.get('columns')}")
        if not changed:
            continue
        for label, values in (("packaged only", removed), ("isolated only", added)):
            for value in values[:15]:
                row = json.loads(value)
                print(f"  {label}: {row_id(row)} {json.dumps(row, ensure_ascii=False, sort_keys=True)}")
            if len(values) > 15:
                print(f"  {label}: ... {len(values) - 15} more")


if __name__ == "__main__":
    main()

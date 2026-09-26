"""Read-only release gate for the normal v0.32 world used by v0.32b."""

from __future__ import annotations

import argparse
import sqlite3
from pathlib import Path


ORO_ID = "504d573f-deab-4cb2-9dbc-d9d053d7af2f"


def validate_public_base(database: Path) -> None:
    if not database.is_file():
        raise ValueError(f"Normal v0.32 world database is missing: {database}")
    with sqlite3.connect(f"{database.resolve().as_uri()}?mode=ro", uri=True) as db:
        if db.execute("PRAGMA quick_check").fetchone()[0] != "ok":
            raise ValueError("Normal v0.32 world database failed SQLite quick_check")
        tables = {
            row[0]
            for row in db.execute("SELECT name FROM sqlite_master WHERE type='table'")
        }
        for table in ("Account", "DOLCharacters", "Inventory", "offline_world_bots"):
            if table not in tables:
                raise ValueError(f"Normal v0.32 world database is missing {table}")
            if db.execute(f'SELECT COUNT(*) FROM "{table}"').fetchone()[0] != 0:
                raise ValueError(f"Normal v0.32 world database contains private {table} rows")
        boss = db.execute(
            "SELECT Name, Region, ClassType, Level FROM Mob WHERE Mob_ID=?",
            (ORO_ID,),
        ).fetchall()
        if boss != [("High Lord Oro", 249, "DOL.GS.HighLordOro", 69)]:
            raise ValueError("Normal v0.32 world must contain public High Lord Oro at level 69")


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--database", required=True, type=Path)
    args = parser.parse_args()
    validate_public_base(args.database)
    print("Clean v0.32 world and level-69 High Lord Oro verified read-only.")


if __name__ == "__main__":
    main()

"""Final narrow preflight for a clean public v0.32 Darkness Falls world.

Run only on a disposable release-staging database after the preserved v0.31
Shannon patch and the two guarded Darkness Falls migrations. This script will
not accept a database containing player accounts, characters, or saved bots.
It enables always-open Darkness Falls and verifies the exact raid exclusions.
"""

import argparse
import json
import sqlite3
from pathlib import Path


ORO_ID = "504d573f-deab-4cb2-9dbc-d9d053d7af2f"
SAVED_TABLES = ("Account", "DOLCharacters", "DOLCharactersBackup",
                "Inventory", "offline_world_bots", "bot_profiles")


def check(connection: sqlite3.Connection, manifest: dict) -> None:
    if connection.execute("PRAGMA quick_check").fetchone()[0] != "ok":
        raise RuntimeError("SQLite quick_check failed")
    for table in SAVED_TABLES:
        if connection.execute(f'SELECT COUNT(*) FROM "{table}"').fetchone()[0]:
            raise RuntimeError(f"Refusing to package saved data from {table}")
    if connection.execute("SELECT COUNT(*) FROM ClassXSpecialization WHERE ClassID=63").fetchone()[0]:
        raise RuntimeError("Sluaghbinder specialization leaked into normal world")
    if connection.execute("SELECT COUNT(*) FROM Mob WHERE Region=249").fetchone()[0] != 2492:
        raise RuntimeError("Unexpected Darkness Falls spawn count")
    if connection.execute("SELECT COUNT(*) FROM Mob WHERE Region=249 AND Realm=0 AND Level>0").fetchone()[0] != 2460:
        raise RuntimeError("Unexpected Darkness Falls combat row count")
    if connection.execute("SELECT COUNT(*) FROM Mob WHERE Region=249 AND Name='Mystemas'").fetchone()[0]:
        raise RuntimeError("Atlas-only Mystemas remains in Darkness Falls")
    if connection.execute("SELECT COUNT(*) FROM Mob WHERE Name='beach rat' AND Region=200 "
                          "AND Level IN (1,2) AND X BETWEEN 306640 AND 307890 "
                          "AND Y BETWEEN 626640 AND 627760").fetchone()[0] < 11:
        raise RuntimeError("v0.31 Shannon Estuary camp is missing")
    if connection.execute("SELECT COUNT(*) FROM NpcTemplate WHERE NpcTemplate_ID LIKE 'df165-familiar-%'").fetchone()[0] != 19:
        raise RuntimeError("Darkness Falls familiar templates are incomplete")
    if connection.execute("SELECT COUNT(*) FROM ServerProperty WHERE Key='allow_all_realms_df'").fetchone()[0] != 1:
        raise RuntimeError("Darkness Falls access setting is absent or ambiguous")

    rows = manifest["rows"]
    if len(rows) != 85 or manifest["baselineCombatRows"] != 2460:
        raise RuntimeError("Unexpected Darkness Falls raid manifest")
    for expected in rows:
        actual = connection.execute(
            "SELECT Name,Level,ClassType,Realm,Region,X,Y,Z FROM Mob WHERE Mob_ID=?",
            (expected["id"],)).fetchall()
        wanted = (expected["name"], expected["level"], expected["classType"],
                  0, 249, *expected["spawn"])
        if actual != [wanted]:
            raise RuntimeError(f"Raid exclusion mismatch: {expected['id']}")
    oro = connection.execute("SELECT Name,Level,ClassType FROM Mob WHERE Mob_ID=?", (ORO_ID,)).fetchone()
    if oro != ("High Lord Oro", 69, "DOL.GS.HighLordOro"):
        raise RuntimeError("Clean public High Lord Oro baseline changed")


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--database", required=True, type=Path)
    parser.add_argument("--manifest", required=True, type=Path)
    parser.add_argument("--apply", action="store_true")
    args = parser.parse_args()
    database = args.database.resolve(strict=True)
    if (database.name != "opendaoc.sqlite3.db" or
            database.parent.name.casefold() != "data" or
            database.parent.parent.name.casefold() != "runtime"):
        raise RuntimeError("Expected a disposable runtime/data/opendaoc.sqlite3.db")
    manifest = json.loads(args.manifest.read_text(encoding="utf-8"))
    with sqlite3.connect(f"file:{database}?mode={'rw' if args.apply else 'ro'}", uri=True) as connection:
        check(connection, manifest)
        current = connection.execute("SELECT Value FROM ServerProperty WHERE Key='allow_all_realms_df'").fetchone()[0]
        if args.apply:
            if current not in ("False", "True"):
                raise RuntimeError("Unexpected Darkness Falls access value")
            connection.execute("BEGIN IMMEDIATE")
            connection.execute("UPDATE ServerProperty SET Value='True' WHERE Key='allow_all_realms_df'")
            connection.commit()
            check(connection, manifest)
            current = connection.execute("SELECT Value FROM ServerProperty WHERE Key='allow_all_realms_df'").fetchone()[0]
        if current != "True":
            raise RuntimeError("Always-open Darkness Falls has not been enabled; use --apply")
        print("Clean public v0.32 world verified: 2492 DF spawns, 85 reserved raid rows, all realms admitted, no saved users.")


if __name__ == "__main__":
    main()

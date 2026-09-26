"""Narrow, repeatable 1.65 Darkness Falls world-data repair.

Only operates on the explicitly supplied SQLite database. It takes an online
SQLite backup before touching the database. Atlas's DF weekly quest NPCs are
disabled in their script registrations, not in this migration, because those
NPCs are normally spawned in memory by the scripts.
"""

import argparse
import datetime as dt
import sqlite3
import uuid
from pathlib import Path


MYSTEMAS_MOB_ID = "21099"
MYSTEMAS_TEMPLATE_ID = 60164347
IONO_LIST_ID = "baced768-cbb8-4371-ad19-115563af9b01"
ALBION_CLOAK_ID = "exquisite_infernal_pyre_walkers_cloak"
MIDGARD_CLOAK_ID = "exquisite_infernal_pyre_walkers_cloak_m"
HIBERNIA_DIAMOND_ID = "Exquisite_Infernal_Black_Diamond3"
YOJO_LIST_ID = "067eddfd-b27f-4560-b5d2-761420943c53"


def count(db, query, params=()):
    return db.execute(query, params).fetchone()[0]


def verify_before(db):
    if count(db, "SELECT count(*) FROM Mob WHERE Region=249 AND Name='Mystemas' AND Mob_ID=?", (MYSTEMAS_MOB_ID,)) not in (0, 1):
        raise RuntimeError("Mystemas Mob identity is ambiguous")
    if count(db, "SELECT count(*) FROM Mob WHERE NPCTemplateID=? AND Mob_ID<>?", (MYSTEMAS_TEMPLATE_ID, MYSTEMAS_MOB_ID)):
        raise RuntimeError("Mystemas template is referenced by another Mob")
    if count(db, "SELECT count(*) FROM NpcTemplate WHERE TemplateId=? AND Name='Mystemas'", (MYSTEMAS_TEMPLATE_ID,)) not in (0, 1):
        raise RuntimeError("Mystemas template identity is ambiguous")
    if count(db, "SELECT count(*) FROM Mob WHERE Region=249 AND Realm=2 AND Name='Iono' AND ItemsListTemplateID=?", (IONO_LIST_ID,)) != 1:
        raise RuntimeError("Iono merchant/list mismatch")
    if count(db, "SELECT count(*) FROM Mob WHERE Region=249 AND Realm=3 AND Name='Yojo' AND ItemsListTemplateID=?", (YOJO_LIST_ID,)) != 1:
        raise RuntimeError("Yojo merchant/list mismatch")
    if count(db, "SELECT count(*) FROM ItemTemplate WHERE Id_nb=? AND Realm=1", (ALBION_CLOAK_ID,)) != 1:
        raise RuntimeError("Albion cloak source is absent or modified")
    if count(db, "SELECT count(*) FROM MerchantItem WHERE ItemListID=? AND ItemTemplateID IN (?,?)", (IONO_LIST_ID, ALBION_CLOAK_ID, MIDGARD_CLOAK_ID)) != 1:
        raise RuntimeError("Iono cloak listing is absent or duplicated")
    if count(db, "SELECT count(*) FROM MerchantItem WHERE ItemListID=? AND ItemTemplateID=?", (YOJO_LIST_ID, HIBERNIA_DIAMOND_ID)) != 1:
        raise RuntimeError("Yojo diamond listing is absent or duplicated")
    if count(db, "SELECT count(*) FROM MerchantItem WHERE ItemTemplateID=? AND ItemListID<>?", (HIBERNIA_DIAMOND_ID, YOJO_LIST_ID)):
        raise RuntimeError("Hibernian diamond is unexpectedly sold elsewhere")


def clone_midgard_cloak(db):
    if count(db, "SELECT count(*) FROM ItemTemplate WHERE Id_nb=?", (MIDGARD_CLOAK_ID,)):
        if count(db, "SELECT count(*) FROM ItemTemplate WHERE Id_nb=? AND Realm=2", (MIDGARD_CLOAK_ID,)) != 1:
            raise RuntimeError("Existing Midgard cloak has an unexpected realm")
        return False

    db.row_factory = sqlite3.Row
    source = db.execute("SELECT * FROM ItemTemplate WHERE Id_nb=?", (ALBION_CLOAK_ID,)).fetchone()
    item = dict(source)
    item["Id_nb"] = MIDGARD_CLOAK_ID
    item["Realm"] = 2
    item["ItemTemplate_ID"] = str(uuid.uuid4())
    item["LastTimeRowUpdated"] = dt.datetime.now(dt.timezone.utc).strftime("%Y-%m-%d %H:%M:%S")
    columns = list(item)
    names = ",".join('"' + name + '"' for name in columns)
    placeholders = ",".join("?" for _ in columns)
    db.execute(f"INSERT INTO ItemTemplate ({names}) VALUES ({placeholders})", tuple(item.values()))
    db.row_factory = None
    return True


def apply(db):
    verify_before(db)
    db.execute("BEGIN IMMEDIATE")
    try:
        cloned = clone_midgard_cloak(db)
        db.execute(
            "UPDATE MerchantItem SET ItemTemplateID=? WHERE ItemListID=? AND ItemTemplateID=?",
            (MIDGARD_CLOAK_ID, IONO_LIST_ID, ALBION_CLOAK_ID),
        )
        db.execute(
            "UPDATE ItemTemplate SET Realm=3 WHERE Id_nb=? AND Realm=1",
            (HIBERNIA_DIAMOND_ID,),
        )
        db.execute(
            "DELETE FROM Mob WHERE Mob_ID=? AND Region=249 AND Name='Mystemas'",
            (MYSTEMAS_MOB_ID,),
        )
        db.execute(
            "DELETE FROM NpcTemplate WHERE TemplateId=? AND Name='Mystemas'",
            (MYSTEMAS_TEMPLATE_ID,),
        )
        if count(db, "SELECT count(*) FROM MerchantItem WHERE ItemListID=? AND ItemTemplateID=?", (IONO_LIST_ID, MIDGARD_CLOAK_ID)) != 1:
            raise RuntimeError("Iono cloak repair failed")
        if count(db, "SELECT count(*) FROM ItemTemplate WHERE Id_nb=? AND Realm=3", (HIBERNIA_DIAMOND_ID,)) != 1:
            raise RuntimeError("Yojo diamond repair failed")
        if count(db, "SELECT count(*) FROM Mob WHERE Region=249 AND Name='Mystemas'"):
            raise RuntimeError("Mystemas spawn remains")
        db.commit()
        return cloned
    except Exception:
        db.rollback()
        raise


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--db", type=Path, required=True)
    parser.add_argument("--backup-dir", type=Path, required=True)
    parser.add_argument("--apply", action="store_true", help="Required to mutate the database")
    args = parser.parse_args()
    db_path = args.db.resolve(strict=True)
    with sqlite3.connect(f"file:{db_path}?mode=ro", uri=True) as check:
        verify_before(check)
        print("Preflight passed: Mystemas and both merchant items match expected identities.")
    if not args.apply:
        print("Dry run only. No data changed.")
        return

    backup_dir = args.backup_dir.resolve(strict=True)
    backup_name = f"df165-world-pre-{dt.datetime.now(dt.timezone.utc):%Y%m%d-%H%M%S}-{uuid.uuid4().hex[:8]}.db"
    backup_path = backup_dir / backup_name
    with sqlite3.connect(str(db_path)) as db:
        db.execute("PRAGMA busy_timeout=5000")
        with sqlite3.connect(str(backup_path)) as backup:
            db.backup(backup)
        if count(db, "PRAGMA integrity_check") != "ok":
            raise RuntimeError("Database failed integrity check before migration")
        cloned = apply(db)
        if count(db, "PRAGMA integrity_check") != "ok":
            raise RuntimeError("Database failed integrity check after migration")
        print(f"Applied: Mystemas removed; Midgard cloak {'cloned' if cloned else 'already present'}; Yojo realm repaired.")
        print(f"Backup: {backup_path}")


if __name__ == "__main__":
    main()

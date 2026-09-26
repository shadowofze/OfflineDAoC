"""Guarded 1.65 Darkness Falls creature taxonomy migration.

Only run while the target server is stopped. The script takes a SQLite online
backup outside the runtime tree before its single transaction.
It deliberately does not infer that every DF "Monster" is a charmable demon.
"""

import argparse
import datetime as dt
import sqlite3
from pathlib import Path


REGION = 249
FAMILIAR_TYPE = {
    568: 1,  # rat
    587: 7,  # ant
    104: 1,  # cat
    103: 1,  # boar
    640: 7,  # fiery scorpion
    641: 7,  # fiery spider
    649: 1,  # hound
    134: 1,  # lynx/cat
}
FAMILIAR_SOURCE_IDS = {50019, 50020, 60159887, 60266090, 602660999}

# These names/body types are explicitly identified in the contemporary
# Darkness Falls bestiary. Type 0 means generic Monster, not an Animal.
SPECIES_TYPE = {
    "apprentice necyomancer": 6,
    "young necyomancer": 6,
    "necyomancer": 6,
    "experienced necyomancer": 6,
    "avernal quasit": 2,
    "molochian tempter": 2,
    "essence shredder": 2,
    "deamhaness": 11,
    "soultorn hibernian cosantoir": 11,
    "soultorn norse isen vakten": 11,
    "soultorn albion eagle knight": 11,
    "cambion": 11,
    "lilispawn": 0,
    "rocot": 0,
    "cursed necyomancer": 0,
    "condemned necyomancer": 0,
    "tormented necyomancer": 0,
    "chaosian": 0,
}


def verify_runtime_database(path: Path, connection: sqlite3.Connection) -> None:
    if path.name != "opendaoc.sqlite3.db":
        raise RuntimeError("Expected a runtime/data/opendaoc.sqlite3.db file")
    if path.parent.name.casefold() != "data" or path.parent.parent.name.casefold() != "runtime":
        raise RuntimeError("Unexpected runtime database location")
    if connection.execute("SELECT COUNT(*) FROM Mob WHERE Region=?", (REGION,)).fetchone()[0] < 2000:
        raise RuntimeError("This database is missing the expected Darkness Falls world data")
    for source_id in FAMILIAR_SOURCE_IDS:
        if connection.execute("SELECT COUNT(*) FROM NpcTemplate WHERE TemplateId=?", (source_id,)).fetchone()[0] != 1:
            raise RuntimeError(f"Missing or ambiguous familiar source template {source_id}")


def migrate(connection: sqlite3.Connection) -> dict[str, int]:
    changed = {"familiar_templates": 0, "familiar_mobs": 0,
               "species_mobs": 0, "species_templates": 0}
    columns = [row[1] for row in connection.execute("PRAGMA table_info(NpcTemplate)")]
    insert = ("INSERT INTO NpcTemplate (" + ",".join(f'`{col}`' for col in columns) + ") VALUES (" +
              ",".join("?" for _ in columns) + ")")

    pairs = list(connection.execute(
        "SELECT DISTINCT NPCTemplateID, Model FROM Mob "
        "WHERE Region=? AND Realm=0 AND Name='demoniac familiar' "
        "AND NPCTemplateID IN (50019,50020,60159887,60266090,602660999) "
        "ORDER BY NPCTemplateID,Model", (REGION,)))
    if len(pairs) > 40:
        raise RuntimeError("Unexpected familiar template/model count")

    for index, (source_id, model) in enumerate(pairs):
        if model not in FAMILIAR_TYPE:
            raise RuntimeError(f"Unknown familiar model {model}; do not guess its creature type")
        source = connection.execute("SELECT * FROM NpcTemplate WHERE TemplateId=?", (source_id,)).fetchone()
        if source is None:
            raise RuntimeError(f"Missing familiar template {source_id}")
        new_id = 700249000 + index
        # The source/template/model string is the durable idempotency key; do
        # not let the arithmetic identifier collide with unrelated content.
        stable_key = f"df165-familiar-{source_id}-{model}"
        existing = connection.execute("SELECT TemplateId FROM NpcTemplate WHERE NpcTemplate_ID=?", (stable_key,)).fetchone()
        if existing is not None:
            new_id = existing[0]
        else:
            if connection.execute("SELECT 1 FROM NpcTemplate WHERE TemplateId=?", (new_id,)).fetchone():
                raise RuntimeError(f"Familiar template ID collision: {new_id}")
            values = dict(zip(columns, source))
            values.update(TemplateId=new_id, NpcTemplate_ID=stable_key,
                          Model=str(model), BodyType=FAMILIAR_TYPE[model])
            connection.execute(insert, tuple(values[col] for col in columns))
            changed["familiar_templates"] += 1
        changed["familiar_mobs"] += connection.execute(
            "UPDATE Mob SET NPCTemplateID=?,BodyType=? WHERE Region=? AND Realm=0 "
            "AND Name='demoniac familiar' AND NPCTemplateID=? AND Model=?",
            (new_id, FAMILIAR_TYPE[model], REGION, source_id, model)).rowcount

    # Only the DF instances of these species are changed. The affected source
    # templates are not referenced by any other region; verify before editing.
    for name, body_type in SPECIES_TYPE.items():
        template_ids = [row[0] for row in connection.execute(
            "SELECT DISTINCT NPCTemplateID FROM Mob WHERE Region=? AND Realm=0 AND lower(Name)=? "
            "AND NPCTemplateID>0", (REGION, name))]
        for template_id in template_ids:
            if connection.execute("SELECT COUNT(*) FROM Mob WHERE NPCTemplateID=? AND Region<>?",
                                  (template_id, REGION)).fetchone()[0]:
                raise RuntimeError(f"Template {template_id} is shared outside Darkness Falls")
            changed["species_templates"] += connection.execute(
                "UPDATE NpcTemplate SET BodyType=? WHERE TemplateId=? AND BodyType<>?",
                (body_type, template_id, body_type)).rowcount
        changed["species_mobs"] += connection.execute(
            "UPDATE Mob SET BodyType=? WHERE Region=? AND Realm=0 AND lower(Name)=? AND BodyType<>?",
            (body_type, REGION, name, body_type)).rowcount

    # Every copied familiar template must now be single-model and have the
    # same body type as the corresponding live spawn record.
    invalid = connection.execute(
        "SELECT COUNT(*) FROM Mob m JOIN NpcTemplate t ON t.TemplateId=m.NPCTemplateID "
        "WHERE m.Region=? AND m.Realm=0 AND m.Name='demoniac familiar' "
        "AND (m.BodyType<>t.BodyType OR CAST(t.Model AS INTEGER)<>m.Model)", (REGION,)).fetchone()[0]
    if invalid:
        raise RuntimeError(f"{invalid} familiar rows still have mismatched model/type templates")
    return changed


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--database", required=True, type=Path)
    parser.add_argument("--backup-dir", required=True, type=Path)
    parser.add_argument("--apply", action="store_true", help="write changes; default is validation only")
    arguments = parser.parse_args()
    database = arguments.database.resolve()
    if not database.is_file():
        raise RuntimeError(f"Database does not exist: {database}")
    mode = "rw" if arguments.apply else "ro"
    connection = sqlite3.connect(f"file:{database}?mode={mode}", uri=True)
    try:
        verify_runtime_database(database, connection)
        print("Verified Darkness Falls runtime database:", database)
        if not arguments.apply:
            print("Dry-run validation only; use --apply while the server is stopped")
            return
        backup_dir = arguments.backup_dir.resolve()
        runtime_dir = database.parent.parent
        if backup_dir == runtime_dir or runtime_dir in backup_dir.parents:
            raise RuntimeError("Backups must be outside the runtime tree")
        backup_dir.mkdir(parents=True, exist_ok=True)
        stamp = dt.datetime.now(dt.timezone.utc).strftime("%Y%m%d-%H%M%S")
        backup_path = backup_dir / f"before-df-charm-{stamp}.sqlite3.db"
        if backup_path.exists():
            raise RuntimeError("Backup path already exists")
        backup = sqlite3.connect(str(backup_path))
        try:
            connection.backup(backup)
            print("SQLite backup:", backup_path)
        finally:
            backup.close()
        connection.execute("BEGIN IMMEDIATE")
        result = migrate(connection)
        connection.commit()
        print("Applied:", result)
        print("Integrity:", connection.execute("PRAGMA quick_check").fetchone()[0])
    except BaseException:
        connection.rollback()
        raise
    finally:
        connection.close()


if __name__ == "__main__":
    main()

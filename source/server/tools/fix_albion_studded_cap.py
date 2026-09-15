"""Narrow, restart-required cosmetic repair. Run only with CoreServer stopped."""
import argparse
import json
import sqlite3
import subprocess
from pathlib import Path

parser = argparse.ArgumentParser()
parser.add_argument("database", type=Path)
parser.add_argument("backup", type=Path)
args = parser.parse_args()
check = subprocess.run(["powershell.exe", "-NoProfile", "-Command",
                        "if (Get-Process CoreServer -ErrorAction SilentlyContinue) { exit 9 } else { exit 0 }"], check=False)
if check.returncode != 0:
    raise SystemExit("CoreServer is running or its status could not be checked; no changes made")
db = sqlite3.connect(args.database.resolve().as_uri() + "?mode=rw", uri=True)
db.row_factory = sqlite3.Row
before = db.execute("SELECT * FROM ItemTemplate WHERE Id_nb = 'Bronze_Torn_Cap_alb'").fetchone()
assert before is not None
assert (before['Realm'], before['Level'], before['Object_Type'], before['Item_Type']) == (1, 1, 34, 21)
if before['Model'] == 824:
    print("Already corrected; no changes made")
    raise SystemExit(0)
assert before['Model'] == 829, "Unexpected item model; manual review required"
assert not args.backup.exists(), "Never overwrite the original database backup"
with sqlite3.connect(args.backup) as backup:
    db.backup(backup)
db.execute("BEGIN IMMEDIATE")
try:
    db.execute("UPDATE ItemTemplate SET Model = 824 WHERE Id_nb = 'Bronze_Torn_Cap_alb' AND Model = 829")
    assert db.total_changes == 1
    after = db.execute("SELECT * FROM ItemTemplate WHERE Id_nb = 'Bronze_Torn_Cap_alb'").fetchone()
    changed = [key for key in before.keys() if before[key] != after[key]]
    assert changed == ['Model'], changed
    assert db.execute("PRAGMA quick_check").fetchone()[0] == 'ok'
    db.commit()
except Exception:
    db.rollback()
    raise
print(json.dumps({'item': before['Id_nb'], 'oldModel': 829, 'newModel': 824,
                  'rowsChanged': 1, 'fieldsChanged': changed,
                  'backup': str(args.backup), 'integrity': 'ok'}))

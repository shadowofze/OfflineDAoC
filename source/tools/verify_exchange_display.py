"""Read-only verification: pipe MainForm.cs on stdin; pass the live SQLite path."""
import re
import sqlite3
import sys
from pathlib import Path

source = sys.stdin.read()
query = re.search(
    r"SELECT COALESCE\(NULLIF\(u.Name.*?ORDER BY RealmName, ItemLevel DESC, ItemName",
    source, re.S,
).group().replace("{listedUtcColumn}", "COALESCE(i.RealmExchangeListedUtc, '')")
with sqlite3.connect(Path(sys.argv[1]).resolve().as_uri() + "?mode=ro", uri=True) as database:
    rows = database.execute(query).fetchall()
    assert rows, "No real listings available for verification"
    assert all(row[0] != "Unknown item" and not row[0].startswith("Unique_") and row[2] > 0 for row in rows)
    print(f"Verified {len(rows)} real listings using the launcher's actual query:")
    for row in rows:
        print(row[0], "level", row[2], "price(copper)", row[4], "realm", row[5])

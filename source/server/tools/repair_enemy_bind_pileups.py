"""Stopped-server, row-backed-up repair of bots at opposing classic bindstones."""
import argparse
import collections
import datetime
import json
import pathlib
import sqlite3
import subprocess

p = argparse.ArgumentParser()
p.add_argument('--db', required=True)
p.add_argument('--backup', required=True)
p.add_argument('--apply', action='store_true')
a = p.parse_args()
db = pathlib.Path(a.db).resolve()
if a.apply:
    running = subprocess.run(['powershell', '-NoProfile', '-Command',
        'if (Get-Process CoreServer -ErrorAction SilentlyContinue) { exit 1 }'], capture_output=True)
    if running.returncode != 0:
        raise SystemExit('CoreServer is running; refusing repair')
c = sqlite3.connect(db.as_uri() + ('?mode=rw' if a.apply else '?mode=ro'), uri=True)
c.row_factory = sqlite3.Row
owners = {1: 1, 100: 2, 200: 3}
capitals = {1: (10, 26, 'City of Camelot', 35990, 30298, 8005),
            2: (101, 120, 'Jordheim', 32020, 28294, 8803),
            3: (201, 209, 'Tir na Nog', 33197, 31200, 8000)}
stones = [dict(r) for r in c.execute('SELECT Region,X,Y,Z,Radius,Realm FROM BindPoint WHERE Region IN (1,100,200)')]
def enemy_stone(realm, region, x, y, z):
    return any(s['Region'] == region and (s['Realm'] or owners[region]) != realm
        and abs(z - s['Z']) <= 256
        and (x-s['X'])**2 + (y-s['Y'])**2 <= s['Radius']**2 for s in stones)

c.execute('BEGIN IMMEDIATE' if a.apply else 'BEGIN')
repairs = []
for row in c.execute('SELECT * FROM offline_world_bots WHERE IsRetired=0'):
    r = dict(row)
    if r['Realm'] not in capitals:
        continue
    move = enemy_stone(r['Realm'], r['RegionId'], r['X'], r['Y'], r['Z'])
    bind = enemy_stone(r['Realm'], r['BindRegionId'], r['BindX'], r['BindY'], r['BindZ'])
    if move or bind:
        repairs.append({'before': r, 'move': move, 'bind': bind})
print(json.dumps({'rows': len(repairs), 'relocations_by_realm': dict(collections.Counter(
    r['before']['Realm'] for r in repairs if r['move'])), 'saved_bind_repairs': sum(r['bind'] for r in repairs),
    'old_clusters': dict(collections.Counter(str((r['before']['RegionId'],r['before']['X'],r['before']['Y']))
        for r in repairs if r['move']))}, indent=2))
if not a.apply:
    c.rollback()
    raise SystemExit(0)
backup = pathlib.Path(a.backup)
with backup.open('x', encoding='utf-8') as f:
    json.dump({'db': str(db), 'utc': datetime.datetime.now(datetime.timezone.utc).isoformat(), 'repairs': repairs}, f, indent=2)
for change in repairs:
    r = change['before']
    region, zone, name, x, y, z = capitals[r['Realm']]
    # Only position and obsolete route-display fields change. Resources,
    # objective tenure, inventory, builds, equipment and money are untouched.
    updates = dict(BindRegionId=region, BindX=x, BindY=y, BindZ=z)
    if change['move']:
        updates.update(RegionId=region, ZoneId=zone, ZoneName=name, X=x, Y=y, Z=z,
            CurrentCampId='', ItineraryJson='', Activity='Relocated from enemy bindstone to own capital',
            TravelDestination=name, ObjectiveProgress='Resume assigned task from own capital')
    c.execute('UPDATE offline_world_bots SET ' + ','.join(k+'=?' for k in updates) + ' WHERE BotId=?',
        [*updates.values(), r['BotId']])
    after = dict(c.execute('SELECT * FROM offline_world_bots WHERE BotId=?', (r['BotId'],)).fetchone())
    assert all(after[k] == v for k,v in r.items() if k not in updates), 'Unexpected field changed'
c.commit()
print('APPLIED; all non-target fields verified unchanged. Backup:', backup)

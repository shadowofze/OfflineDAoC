"""Apply only the positions certified by the read-only installed-mesh NUnit probe.
Run with the server stopped. Default is read-only; --apply requires a new backup path.
"""
import argparse
import collections
import hashlib
import json
import pathlib
import sqlite3
import xml.etree.ElementTree as ET

p = argparse.ArgumentParser()
p.add_argument('--db', required=True)
p.add_argument('--trx', required=True)
p.add_argument('--backup')
p.add_argument('--report')
p.add_argument('--apply', action='store_true')
args = p.parse_args()
ns = {'t': 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010'}
test = ET.parse(args.trx)
counters = test.find('.//t:Counters', ns).attrib
assert counters['failed'] == '0' and counters['passed'] == '1', 'Mesh probe must pass first'
output = test.find('.//t:UnitTestResult/t:Output/t:StdOut', ns).text
assert 'ROSTER_AUDIT inspected=4500 skipped=0' in output, 'Expected full roster mesh audit'
repairs = [json.loads(line.split('ROSTER_REPAIR ', 1)[1]) for line in output.splitlines()
           if line.startswith('ROSTER_REPAIR ')]
assert len({r['id'] for r in repairs}) == len(repairs)
db_path = pathlib.Path(args.db).resolve()
db = sqlite3.connect(db_path.as_uri() + ('?mode=rw' if args.apply else '?mode=ro'), uri=True)
db.row_factory = sqlite3.Row
assert db.execute('PRAGMA quick_check').fetchone()[0] == 'ok'
for r in repairs:
    row = db.execute('SELECT * FROM offline_world_bots WHERE BotId=? AND IsRetired=0', (r['id'],)).fetchone()
    assert row and tuple(row[k] for k in ('RegionId', 'X', 'Y', 'Z')) == tuple(r[k] for k in ('region','x','y','z')), f"Stale probe: {r['name']}"
print('Certified repairs:', len(repairs), dict(collections.Counter(r['reason'] for r in repairs)))
if not args.apply:
    raise SystemExit(0)

backup = pathlib.Path(args.backup).resolve()
assert not backup.exists() and backup != db_path
backup.parent.mkdir(parents=True, exist_ok=True)
with sqlite3.connect(backup) as snapshot:
    db.backup(snapshot)

changed_columns = {'RegionId','X','Y','Z','ZoneId','ZoneName','CurrentCampId','ItineraryJson',
                   'Activity','CurrentGoal','TargetName','TravelDestination','ObjectiveProgress',
                   'ObjectiveAssignmentId','ObjectiveAssignedUtc','ObjectiveExpiresUtc','ObjectivePhase',
                   'ObjectivePveMode','ObjectivePveKillTarget','ObjectivePveKills'}
def protected_hash():
    digest = hashlib.sha256()
    for row in db.execute('SELECT * FROM offline_world_bots ORDER BY BotId'):
        digest.update(json.dumps({k:row[k] for k in row.keys() if k not in changed_columns}, sort_keys=True).encode())
    return digest.hexdigest()

db.execute('BEGIN IMMEDIATE')
try:
    before = protected_hash()
    for r in repairs:
        cursor = db.execute('''UPDATE offline_world_bots
          SET RegionId=?,X=?,Y=?,Z=?,ZoneId=?,ZoneName=?,CurrentCampId='',ItineraryJson='',
          Activity='Offline; repaired ground position', CurrentGoal='Choose a fresh reachable goal after position repair',
          TargetName='',TravelDestination='',ObjectiveProgress=?,ObjectiveAssignmentId='',
          ObjectiveAssignedUtc='',ObjectiveExpiresUtc='',ObjectivePhase='',ObjectivePveMode='',
          ObjectivePveKillTarget=0,ObjectivePveKills=0
          WHERE BotId=? AND RegionId=? AND X=? AND Y=? AND Z=? AND IsRetired=0''',
          (r['toRegion'],r['toX'],r['toY'],r['toZ'],r['toZone'],r['toZoneName'],
           'Offline mesh-verified recovery: '+r['reason'],r['id'],r['region'],r['x'],r['y'],r['z']))
        assert cursor.rowcount == 1, f"Record changed: {r['name']}"
    assert protected_hash() == before, 'Protected character progress changed'
    assert db.execute('PRAGMA quick_check').fetchone()[0] == 'ok'
    db.commit()
except BaseException:
    db.rollback()
    raise
if args.report:
    pathlib.Path(args.report).write_text(json.dumps({'backup':str(backup),'protected_bot_state_sha256':before,
        'count':len(repairs),'repairs':repairs}, indent=2), encoding='utf-8')
print('Applied:', len(repairs), 'All protected bot columns unchanged; database quick_check OK.')

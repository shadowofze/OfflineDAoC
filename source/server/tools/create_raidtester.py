"""Create only the requested test character; dry-run unless --apply is given."""
import argparse
import datetime
import json
import sqlite3
import subprocess
import uuid
from pathlib import Path

parser = argparse.ArgumentParser()
parser.add_argument('--apply', action='store_true')
args = parser.parse_args()
database = Path(r'C:\Users\thedo\Desktop\Offline DAoC\runtime\data\opendaoc.sqlite3.db')
processes = subprocess.check_output(['powershell', '-NoProfile', '-Command',
    "Get-Process CoreServer,game,camelot -ErrorAction SilentlyContinue | Select-Object -ExpandProperty ProcessName; exit 0"], text=True)
assert not processes.strip(), 'Stop the server and game before provisioning.'
with sqlite3.connect(database) as connection:
    connection.row_factory = sqlite3.Row
    connection.execute('BEGIN IMMEDIATE')
    assert not connection.execute("SELECT 1 FROM DOLCharacters WHERE Name='Raidtester' COLLATE NOCASE").fetchone(), 'Raidtester already exists; refusing to overwrite.'
    accounts = connection.execute('SELECT DISTINCT AccountName FROM DOLCharacters').fetchall()
    assert len(accounts) == 1, 'Select the account explicitly if there is more than one.'
    account = accounts[0][0]
    before = [tuple(row) for row in connection.execute('SELECT * FROM DOLCharacters ORDER BY DOLCharacters_ID')]
    slots = {row[0] for row in connection.execute('SELECT AccountSlot FROM DOLCharacters WHERE AccountName=?', (account,))}
    slot = next(value for value in range(100, 110) if value not in slots)
    now = datetime.datetime.now(datetime.timezone.utc).isoformat()
    ident = uuid.uuid4().hex
    character = dict(DOLCharacters_ID=ident, AccountName=account, AccountSlot=slot,
        Name='Raidtester', Realm=1, Class=11, Race=1, Gender=0, Level=50,
        CreationModel=32, CurrentModel=32, Experience=169999999950,
        Strength=115, Dexterity=93, Constitution=85, Quickness=60,
        Intelligence=60, Piety=60, Empathy=60, Charisma=60,
        Health=2000, Mana=0, Endurance=100, MaxEndurance=100, Concentration=100,
        MaxSpeed=191, ActiveWeaponSlot=1, GainXP=1, GainRP=1, Autoloot=1,
        Platinum=10, Region=10, Xpos=36200, Ypos=30450, Zpos=8000,
        BindRegion=10, BindXpos=36200, BindYpos=30450, BindZpos=8000,
        SerializedSpecs='Slash|50;Dual Wield|50;Parry|28',
        CreationDate=now, LastLevelUp=now, LastTimeRowUpdated=now, RespecAmountAllSkill=1)
    def insert(table, values):
        connection.execute('INSERT INTO '+table+' ('+','.join('"'+key+'"' for key in values)+') VALUES ('+
                           ','.join('?' for _ in values)+')', list(values.values()))
    insert('DOLCharacters', character)
    equipment = connection.execute('SELECT SlotPosition, TemplateId FROM offline_level50_loadouts WHERE ClassId=11').fetchall()
    assert len(equipment) >= 14, 'Missing Mercenary equipment.'
    for position, template in equipment:
        item = connection.execute('SELECT Object_Type, AllowedClasses FROM ItemTemplate WHERE Id_nb=?', (template,)).fetchone()
        assert item and (not item['AllowedClasses'] or '11' in item['AllowedClasses'].split(';'))
        insert('Inventory', dict(Inventory_ID=uuid.uuid4().hex, OwnerID=ident,
            ITemplate_Id=template, SlotPosition=position, Count=1,
            Condition=100000, Durability=100000, LastTimeRowUpdated=now))
    after = [tuple(row) for row in connection.execute('SELECT * FROM DOLCharacters WHERE DOLCharacters_ID!=? ORDER BY DOLCharacters_ID', (ident,))]
    assert before == after, 'Existing characters changed.'
    assert connection.execute('PRAGMA quick_check').fetchone()[0] == 'ok'
    if args.apply:
        connection.commit()
        print(json.dumps(dict(name='Raidtester', account=account, characterId=ident,
                              classId=11, level=50, equipmentItems=len(equipment), status='created')))
    else:
        connection.rollback()
        print('Validated Raidtester and equipment; dry run rolled back.')

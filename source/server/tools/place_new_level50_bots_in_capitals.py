"""Move only the never-loaded September 4 level-50 cohort to realm capitals."""
import sqlite3

db = r'C:\Users\thedo\Desktop\new class test\runtime\data\opendaoc.sqlite3.db'
with sqlite3.connect(db) as connection:
    connection.execute('BEGIN IMMEDIATE')
    where = "BotId BETWEEN 48128 AND 53627 AND Level=50 AND IsOnline=0 AND COALESCE(LastSavedUtc,'')=''"
    assert connection.execute('SELECT COUNT(*) FROM offline_world_bots WHERE '+where).fetchone()[0] == 5500
    for realm, region, zone, name, x, y, z in [
        (1, 10, 26, 'City of Camelot', 35990, 30298, 8000),
        (2, 101, 120, 'Jordheim', 32020, 28294, 8819),
        (3, 201, 209, 'Tir na Nog', 33197, 31200, 8000),
    ]:
        result = connection.execute('UPDATE offline_world_bots SET RegionId=?,ZoneId=?,ZoneName=?,X=?,Y=?,Z=?,BindRegionId=?,BindX=?,BindY=?,BindZ=? WHERE '+where+' AND Realm=?',
                                    (region,zone,name,x,y,z,region,x,y,z,realm))
        print(name, result.rowcount)
    assert connection.execute('PRAGMA quick_check').fetchone()[0] == 'ok'
print('Capital placements committed; inventory, currency, tasks and login ramp unchanged.')

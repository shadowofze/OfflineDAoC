"""Offline-only realm-boundary repair. Default: read-only preview."""
import argparse, sqlite3, datetime, json, subprocess
from pathlib import Path

p=argparse.ArgumentParser();p.add_argument('--apply',action='store_true');a=p.parse_args()
db=Path('C:/Users/thedo/Desktop/Offline DaOC/runtime/data/opendaoc.sqlite3.db')
running=subprocess.run(['powershell','-NoProfile','-Command','@(Get-Process CoreServer -ErrorAction SilentlyContinue).Count'],capture_output=True,text=True,check=True)
if running.stdout.strip()!='0': raise SystemExit('CoreServer is running; refusing repair')
c=sqlite3.connect(db.as_uri()+'?mode='+('rw' if a.apply else 'ro'),uri=True,timeout=2);c.row_factory=sqlite3.Row
zones=[dict(r) for r in c.execute('select ZoneID,Name,RegionID,OffsetX,OffsetY,Width,Height from Zones')]
def zone_at(region,x,y):
    return next((z for z in zones if z['RegionID']==region and z['OffsetX']*8192<=x<(z['OffsetX']+z['Width'])*8192 and z['OffsetY']*8192<=y<(z['OffsetY']+z['Height'])*8192),None)
def owner(region,zone):
    classic={1:(1,{11,12,14,15}),100:(2,{111,112,113,115}),200:(3,{210,211,212,214})}
    if region in classic:
        realm,frontier=classic[region];return 0 if zone in frontier else realm
    for realm,rs in ((1,{10,20,21,22,23,24,50,51,60,61,62}),(2,{101,125,126,127,128,129,150,151,160,161}),(3,{180,181,190,191,192,193,194,201,220,221,222,223,224})):
        if region in rs:return realm
    return 0
capitals={1:(10,35990,30298,8000),2:(101,32020,28294,8819),3:(201,33197,31200,8000)}
repairs=[]
for row in c.execute('select * from offline_world_bots where IsRetired=0'):
    r=dict(row);z=zone_at(r['RegionId'],r['X'],r['Y']);b=zone_at(r['BindRegionId'],r['BindX'],r['BindY'])
    wrong=z is not None and owner(r['RegionId'],z['ZoneID']) not in (0,r['Realm'])
    bad_bind=b is not None and owner(r['BindRegionId'],b['ZoneID']) not in (0,r['Realm'])
    if wrong or bad_bind:repairs.append((r,wrong,bad_bind))
print(json.dumps([{'id':r['BotId'],'name':r['Name'],'realm':r['Realm'],'region':r['RegionId'],'position':[r['X'],r['Y'],r['Z']],'move':w,'repairBind':b} for r,w,b in repairs]))
print('REPAIRS',len(repairs),'MOVE',sum(w for _,w,_ in repairs),'BIND',sum(b for _,_,b in repairs))
if a.apply:
    stamp=datetime.datetime.now().strftime('%Y%m%d-%H%M%S')
    backup=db.with_name('opendaoc.before-realm-boundary-'+stamp+'.sqlite3.db')
    with sqlite3.connect(backup) as target:c.backup(target)
    before=tuple(c.execute('select count(*),sum(MoneyCopper),sum(Experience),sum(InventoryRevision) from offline_world_bots').fetchone())
    with c:
        for r,wrong,bad_bind in repairs:
            region,x,y,z=capitals[r['Realm']];zone=zone_at(region,x,y)
            if wrong:
                c.execute("update offline_world_bots set RegionId=?,X=?,Y=?,Z=?,ZoneId=?,ZoneName=?,Activity='Recovered to own capital: enemy homeland boundary',CurrentCampId='',ItineraryJson='',TargetName='',TravelDestination='',CurrentGoal='',ObjectiveProgress='Replan from own capital after realm boundary repair',IsOnline=0 where BotId=?",(region,x,y,z,zone['ZoneID'],zone['Name'],r['BotId']))
            if bad_bind:c.execute('update offline_world_bots set BindRegionId=?,BindX=?,BindY=?,BindZ=? where BotId=?',(region,x,y,z,r['BotId']))
        after=tuple(c.execute('select count(*),sum(MoneyCopper),sum(Experience),sum(InventoryRevision) from offline_world_bots').fetchone())
        assert before==after,'Sacred accounting changed; roll back'
    print('APPLIED; backup',backup,'accounting unchanged',before)
    print('quick_check',c.execute('pragma quick_check').fetchone()[0])

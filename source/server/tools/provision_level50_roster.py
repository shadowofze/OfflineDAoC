"""One-shot, stopped-server provisioning. Default is rollback-only validation."""
import sqlite3, re, random, uuid, datetime, argparse
from pathlib import Path

p=argparse.ArgumentParser(); p.add_argument('--apply',action='store_true'); a=p.parse_args()
root=Path(__file__).resolve().parents[1]
db=Path(r'C:\Users\thedo\Desktop\new class test\runtime\data\opendaoc.sqlite3.db')
c=sqlite3.connect(db); c.row_factory=sqlite3.Row
c.execute('pragma foreign_keys=on'); c.execute('begin immediate')
assert c.execute('select count(*) from offline_world_bots where IsRetired=0').fetchone()[0]==4500
assert not c.execute("select 1 from DOLCharacters where Name='OfflineDaOC' collate nocase").fetchone()
oldbots=[tuple(r) for r in c.execute('select * from offline_world_bots order by BotId')]
oldplayers=[tuple(r) for r in c.execute('select * from DOLCharacters order by Name')]
reserved={r[0].lower() for r in c.execute('select Name from offline_world_bots union select Name from DOLCharacters')}
source=(root.parent/'tools/OfflineDaoc.Launcher/BotCharacterGenerator.cs').read_text()
classes=[]
for m in re.finditer(r'new\((\d+), "([^"]+)", ([^\n]+)\)',source):
 cid=int(m[1]); races=[(int(x),y) for x,y in re.findall(r'\((\d+), "([^"]+)"\)',m[3])]
 if races: classes.append((cid,m[2],races))
assert len(classes)==40
armor={1:36,2:36,3:34,4:35,5:32,6:35,7:32,8:32,9:33,10:33,11:35,12:32,13:32,19:35,
21:35,22:35,23:33,24:35,25:34,26:35,27:32,28:35,29:32,30:32,31:34,32:34,
40:32,41:32,42:32,43:37,44:38,45:38,46:38,47:38,48:37,49:33,50:37,55:32,56:32,63:38}
weapons={1:[3,2,4,6],2:[3,2,4,6,7],3:[3,4],4:[3,4],5:[8],6:[2],7:[8],8:[8],9:[3,4],10:[8],11:[3,2,4],12:[8],13:[8],19:[24,3,2,4],
21:[11,12,13],22:[11,12,13],23:[11,13],24:[11,12,13],25:[14,11],26:[12],27:[8],28:[12],29:[8],30:[8],31:[13,11],32:[25,11,12,13],
40:[8],41:[8],42:[8],43:[19,20,21],44:[19,20,21,22,23],45:[19,20,21,22],46:[19,20],47:[19,20],48:[19,20],49:[19,21],50:[19,21],55:[8],56:[26],63:[20,26]}
shield={1:3,2:3,3:2,6:2,11:2,19:3,21:3,22:3,26:1,28:1,43:2,44:3,45:3,46:2,47:1,48:1,63:3}
dual={9,11,23,31,32,43,49,50}
bow={3:9,25:15,50:18}
wear=[21,22,23,25,27,28,24,26,29,32,33,34,35,36]
now=datetime.datetime.now(datetime.timezone.utc).isoformat()
cache={}; loadouts={}
def insert(table,row):
 c.execute('insert into '+table+' ('+','.join('"'+x+'"' for x in row)+') values ('+','.join('?' for _ in row)+')',list(row.values()))
def template(cid,realm,typ,slot,bonuses=()):
 key=(cid,typ,slot)
 if key in cache:return cache[key]
 real_slot=slot%1000
 lookupslot={34:33,36:35}.get(real_slot,real_slot)
 rows=c.execute('select * from ItemTemplate where Object_Type=? and Item_Type=? and Level between 45 and 51 and Model>0 and MaxCount=1 and Realm in (0,?) order by Level desc,Quality desc',(typ,lookupslot,realm)).fetchall()
 rows=[r for r in rows if not r['AllowedClasses'] or str(cid) in re.findall(r'\d+',r['AllowedClasses'])]
 if typ==45:
  rows=c.execute('select * from ItemTemplate where Object_Type=45 and Model=? and Realm in (0,?) and MaxCount=1 order by Level desc',({1:228,2:227,3:325}[slot//1000],realm)).fetchall()
 if not rows and slot==11 and typ!=42:
  rows=c.execute('select * from ItemTemplate where Object_Type=? and Item_Type=10 and Level between 45 and 51 and Model>0 and MaxCount=1 and Realm in (0,?) order by Level desc',(typ,realm)).fetchall()
 assert rows,(cid,typ,slot,'missing real appearance/template')
 r=dict(rows[0]); ident=f'offline50_20260904_c{cid}_s{slot}_t{typ}'
 for field in ['SpellID','SpellID1','ProcSpellID','ProcSpellID1','PoisonSpellID','Charges','Charges1','MaxCharges','MaxCharges1','ExtraBonus','ExtraBonusType']:
  r[field]=0
 for n in range(1,11): r['Bonus'+str(n)]=r['Bonus'+str(n)+'Type']=0
 r.update(Id_nb=ident,ItemTemplate_ID=uuid.uuid4().hex,Name=f'Offline level 50 {next(x[1] for x in classes if x[0]==cid)} {typ}/{slot}',
   Level=50,LevelRequirement=50,BonusLevel=50,Quality=100,Bonus=35,Condition=100000,MaxCondition=100000,Durability=100000,MaxDurability=100000,
   Item_Type=real_slot,Realm=realm,AllowedClasses=str(cid),IsPickable=1,IsDropable=1,IsTradable=1,CanDropAsLoot=1,
   IsIndestructible=0,IsNotLosingDur=0,MaxCount=1,PackSize=1,Price=100000,Flags=0,ClassType=None,PackageID='Offline50-20260904',Description='Classic level 50 class template',LastTimeRowUpdated=now)
 if typ in range(2,29):r['DPS_AF']=165; r['Hand']=1 if slot==12 else 2 if slot==11 else 0; r['SPD_ABS']=max(25,r['SPD_ABS'])
 if typ in range(32,39):r['DPS_AF']=50 if typ==32 else 100; r['SPD_ABS']={32:0,33:10,34:19,35:27,36:34,37:19,38:27}[typ]
 if typ==42:r['Type_Damage']=shield[cid];r['Hand']=0;r['DPS_AF']=165
 if typ==45:r['Type_Damage']=slot//1000
 for n,(prop,value) in enumerate(bonuses,1): r['Bonus'+str(n)+'Type']=prop;r['Bonus'+str(n)]=value
 if typ==8 and cid!=10:r['ExtraBonusType']=165;r['ExtraBonus']=50
 insert('ItemTemplate',r);cache[key]=ident;return ident
for cid,name,races in classes:
 realm=1 if cid<20 else 2 if cid<40 else 3
 casting={5:5,6:6,7:5,8:5,12:5,13:5,19:6,21:6,26:6,27:6,28:6,29:6,30:6,40:5,41:5,42:5,45:5,46:7,47:7,48:8,55:5,56:5,4:8,24:8,10:6}
 stats=[1,2,3,4] if cid not in casting else [3,2,casting[cid],1 if armor[cid]!=32 else 4]
 bonuses=[(s,25) for s in stats for _ in range(3)]+[(s,13) for s in range(11,20) for _ in range(2)]+[(10,50)]*4+[(9,13)]*2+[(163,11),(164,11)]
 gear=[]
 for i,slot in enumerate(wear): gear.append((slot,template(cid,realm,armor[cid] if slot in [21,22,23,25,27,28] else 41,slot,bonuses[i::len(wear)])))
 occupied=set(wear); bag=40
 for typ in weapons[cid]:
  native=12 if typ in [6,7,8,14,22,23,26] else 10
  slot=native if native not in occupied else bag
  if slot==bag:bag+=1
  gear.append((slot,template(cid,realm,typ,native)));occupied.add(slot)
 if cid in shield:gear.append((11,template(cid,realm,42,11)))
 elif cid in dual:gear.append((11,template(cid,realm,17 if cid==31 else weapons[cid][0],11)))
 if cid in bow:gear.append((13,template(cid,realm,bow[cid],13)))
 if cid in [4,48]:
  for subtype in [1,2,3]:
   item=template(cid,realm,45,12+subtype*1000)
   slot=12 if subtype==1 and 12 not in occupied else bag
   if slot==bag:bag+=1
   gear.append((slot,item));occupied.add(slot)
 loadouts[cid]=gear

c.execute('create table if not exists offline_level50_loadouts (ClassId integer not null, SlotPosition integer not null, TemplateId text not null, primary key(ClassId,SlotPosition))')
assert not c.execute('select 1 from offline_level50_loadouts limit 1').fetchone()
for cid,gear in loadouts.items():
 c.executemany('insert into offline_level50_loadouts values (?,?,?)',[(cid,slot,item) for slot,item in gear])

def equip(owner,cid):
 for slot,item in loadouts[cid]:insert('Inventory',dict(Inventory_ID=uuid.uuid4().hex,OwnerID=owner,ITemplate_Id=item,SlotPosition=slot,Count=1,Condition=100000,Durability=100000,LastTimeRowUpdated=now))
def name(realm):
 starts={1:['Ald','Ber','Ced','Ed','Gar','Har','Leof','Ren'],2:['Arn','Bjorn','Dag','Egil','Finn','Hald','Ivar','Sig'],3:['Aed','Bran','Cael','Ciar','Con','Eog','Lorc','Nial']}[realm]
 while True:
  n=random.choice(starts)+''.join(random.choice(['a','en','or','il','un','ar']) for _ in range(3))+random.choice(['ric','var','wen','ren','dan','mund'])
  n=n[:19]
  if n.lower() not in reserved:reserved.add(n.lower());return n
for realm,count in [(1,1834),(2,1833),(3,1833)]:
 choices=[v for v in classes if (1 if v[0]<20 else 2 if v[0]<40 else 3)==realm]
 for i in range(count):
  cid,cn,races=choices[i%len(choices)];race,rn=random.choice(races)
  locations=c.execute('select * from StartupLocation where RealmID=? and RaceID=? and ClassID in (?,0) order by case when ClassID=? then 0 else 1 end',(realm,race,cid,cid)).fetchall()
  if not locations:locations=c.execute('select * from StartupLocation where RealmID=? and ClassID=?',(realm,cid)).fetchall()
  assert locations,(realm,cid,race)
  loc=locations[0]
  insert('offline_world_bots',dict(Name=name(realm),Realm=realm,ClassId=cid,ClassName=cn,RaceId=race,RaceName=rn,Gender=random.randint(1,2),Level=50,Experience=169999999950,MoneyCopper=100000000,
    IsAlive=1,IsOnline=0,Health=2000,Mana=2000,Endurance=100,RegionId=loc['Region'],X=loc['XPos'],Y=loc['YPos'],Z=loc['ZPos'],BindRegionId=loc['Region'],BindX=loc['XPos'],BindY=loc['YPos'],BindZ=loc['ZPos'],
    LastUpdateUtc=now,Activity='Queued: level 50 class template; normal training and task selection',SerializedAbilities='trained-level|1',InventoryRevision=1))
  bid=c.execute('select last_insert_rowid()').fetchone()[0];equip('offlinebot:'+str(bid),cid)
slots={r[0] for r in c.execute("select AccountSlot from DOLCharacters where AccountName='thedo'")}; slot=next(s for s in range(200,210) if s not in slots)
loc=c.execute('select * from StartupLocation where RealmID=2 and RaceID=14 and ClassID in (30,36) order by ClassID limit 1').fetchone()
assert loc
pid=uuid.uuid4().hex
insert('DOLCharacters',dict(DOLCharacters_ID=pid,Name='OfflineDaOC',AccountName='thedo',AccountSlot=slot,Realm=2,Class=30,Race=14,Gender=0,Level=50,Experience=169999999950,
 CreationModel=773,CurrentModel=773,Strength=55,Constitution=45,Dexterity=94,Quickness=93,Intelligence=60,Piety=115,Empathy=60,Charisma=60,
 MaxEndurance=100,Health=1000,Mana=1000,Endurance=100,Concentration=100,MaxSpeed=191,Platinum=10,ActiveWeaponSlot=1,GainXP=1,GainRP=1,Autoloot=1,
 Region=loc['Region'],Xpos=loc['XPos'],Ypos=loc['YPos'],Zpos=loc['ZPos'],BindRegion=loc['Region'],BindXpos=loc['XPos'],BindYpos=loc['YPos'],BindZpos=loc['ZPos'],
 CreationDate=now,LastLevelUp=now,LastTimeRowUpdated=now,RespecAmountAllSkill=1))
equip(pid,30)
for table,keys in [('offline_population_settings',('PopulationEnabled','ActiveTarget','HardActiveCap')),('ServerProperty',('population_enabled','active_target','hard_active_cap'))]:
 for key,value in zip(keys,['true','10000','0']):c.execute('update '+table+' set Value=? where Key=?',(value,key))
assert [tuple(r) for r in c.execute('select * from offline_world_bots where BotId<=? order by BotId',(max(r[0] for r in oldbots),))]==oldbots
assert [tuple(r) for r in c.execute("select * from DOLCharacters where Name!='OfflineDaOC' order by Name")]==oldplayers
assert c.execute('select count(*) from offline_world_bots where IsRetired=0').fetchone()[0]==10000
assert c.execute('pragma quick_check').fetchone()[0]=='ok'
print('Validated: 5500 new level-50 bots; 10000 total; 10 platinum each; OfflineDaOC on thedo; '+str(len(cache))+' class equipment templates.')
if a.apply:c.commit();print('COMMITTED')
else:c.rollback();print('DRY RUN rolled back')

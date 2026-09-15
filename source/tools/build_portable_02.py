"""Build separate clean Offline DAoC 0.2 staging; never edit live installation."""
import hashlib, json, pathlib, secrets, shutil, sqlite3, sys, time
import xml.etree.ElementTree as ET

base=pathlib.Path('C:/Users/thedo/Desktop')
live=base/'Offline DaOC/runtime'
extra=base/'Offline DAOC extra files'
release=extra/'releases/Offline DAoC v0.2'
tools=extra/'development source/tools'
policy=json.loads((tools/'OfflineDaoc.ProgressImport/progress-policy.json').read_text())
if release.exists(): raise SystemExit('Staging already exists; refusing overwrite')
release.mkdir(parents=True)
source_hashes={}
def digest(p):
    h=hashlib.sha256()
    with p.open('rb') as f:
        for b in iter(lambda:f.read(1024*1024),b''):h.update(b)
    return h.hexdigest()
def copytree(src,dst,filter=None,track=False):
    count=size=0
    for p in src.rglob('*'):
        if not p.is_file():continue
        rel=p.relative_to(src)
        if filter and not filter(rel):continue
        target=dst/rel;target.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(p,target)
        if track:source_hashes[str(p)]=digest(p)
        count+=1;size+=p.stat().st_size
    print(f'Copied {src.name}: {count} files, {size/1024**3:.2f} GiB',flush=True)
def include(rel):
    parts=[x.lower() for x in rel.parts];name=parts[-1]
    if any(x in {'logs','backups','deployment-backups','screenshots','.git'} or 'before-' in x or 'pre-route-' in x for x in parts):return False
    if parts[0]=='data':return False
    if name in {'account.txt','rvr-world.json','errorlog.txt','debug.log','chat.log','user.dat','unins000.exe','unins000.dat','uninstdaoc.exe'}:return False
    if name.endswith(('.log','.dmp','.bak','.sqlite3.db','.sqlite3.db-wal','.sqlite3.db-shm')):return False
    return True
copytree(live,release/'runtime',include,True)
runtime=release/'runtime';(runtime/'data').mkdir(exist_ok=True)
src=sqlite3.connect((live/'data/opendaoc.sqlite3.db').as_uri()+'?mode=ro',uri=True)
out=sqlite3.connect(runtime/'data/opendaoc.sqlite3.db');src.backup(out);src.close()
out.execute('pragma secure_delete=on');out.execute('pragma foreign_keys=off')
tables={r[0] for r in out.execute("select name from sqlite_master where type='table'")}
# Starting guild definitions/ranks are world data, but reset their earned state.
for table in policy['ProgressTables']+policy['ClearTables']:
    if table not in {'Guild','GuildRank'} and table in tables:out.execute('delete from "'+table+'"')
out.execute("update Guild set RealmPoints=0,BountyPoints=0,Bank=0,MeritPoints=0,Webpage='',Email='',Motd='',oMotd='',HaveGuildHouse=0,GuildHouseNumber=0")
out.execute("update DBHouse set OwnerID='',GuildName='',GuildHouse=0,HasConsignment=0,KeptMoney=0,Model=0,Name='' where coalesce(OwnerID,'')<>''")
out.execute("update Keep set Realm=OriginalRealm,ClaimedGuildName='' where OriginalRealm in (1,2,3)")
out.execute("update ServerProperty set Value='1' where lower(Key) in ('xp_rate','bot_xp_rate')")
out.execute("insert or replace into offline_local_options(Key,Value) values('MakeMeGM','false')")
out.execute("update offline_population_settings set Value='0' where Key='ActiveTarget'")
out.execute("update offline_population_settings set Value='true' where Key='PopulationEnabled'")
out.execute("update offline_population_settings set Value='15',Description='First third within five minutes; full requested roster by fifteen minutes.' where Key='StartupRampMinutes'")
for table in policy['ProgressTables']+policy['ClearTables']:
    if table not in {'Guild','GuildRank'}:out.execute('delete from sqlite_sequence where name=?',(table,))
out.execute('delete from sqlite_stat1');out.execute('delete from sqlite_stat4');out.commit()
out.execute('pragma journal_mode=delete');out.execute('vacuum');out.execute('analyze');out.commit()
assert out.execute('pragma quick_check').fetchone()[0]=='ok'
for table in policy['ProgressTables']+policy['ClearTables']:
    if table not in {'Guild','GuildRank','offline_local_options'}:assert out.execute('select count(*) from "'+table+'"').fetchone()[0]==0,table
out.close()
print('Clean database verified',flush=True)
(runtime/'account.txt').write_text('Account: offline\nPassword: '+secrets.token_hex(16)+'\n',encoding='utf-8')
config=runtime/'server/config/serverconfig.xml';tree=ET.parse(config);tree.find('./Server/AutoAccountCreation').text='True';tree.write(config,encoding='utf-8',xml_declaration=True)
# Portable framework host: no global modern .NET installation required.
dotnet=pathlib.Path('C:/Program Files/dotnet');bundled=release/'tools/dotnet'
for sub in ['host/fxr/10.0.11','shared/Microsoft.NETCore.App/10.0.11','shared/Microsoft.WindowsDesktop.App/10.0.11','shared/Microsoft.AspNetCore.App/10.0.11']:
    copytree(dotnet/sub,bundled/sub)
for name in ['dotnet.exe','LICENSE.txt','ThirdPartyNotices.txt']:
    if (dotnet/name).exists():shutil.copy2(dotnet/name,bundled/name)
copytree(tools/'OfflineDaoc.ProgressImport/bin/Release/net10.0-windows',release/'tools/ProgressImporter')
builder=extra/'development tools/OpenDAoC-BuildNav'
copytree(builder/'bin/Release/net10.0-windows7.0',release/'tools/NavmeshBuilder')
copytree(builder,release/'tools/NavmeshBuilder/source',lambda p: not any(x.lower() in {'.git','bin','obj'} for x in p.parts))
for name in ['ignorelist.txt','log4net.xml']:
    if (builder/name).exists():shutil.copy2(builder/name,release/'tools/NavmeshBuilder'/name)
for p in (release/'tools/NavmeshBuilder').rglob('cem.json'):
    p.write_text(json.dumps({'CEM':{'GamePath':'../../runtime/client-opendaoc/app'}},indent=2))
# Built importer source is included for transparent offline maintenance.
copytree(tools/'OfflineDaoc.ProgressImport',release/'tools/ProgressImporter/source',lambda p:not any(x in {'bin','obj'} for x in p.parts))
(extra/'releases/source-runtime-hashes.json').write_text(json.dumps(source_hashes,indent=2))
print('STAGED '+str(release),flush=True)

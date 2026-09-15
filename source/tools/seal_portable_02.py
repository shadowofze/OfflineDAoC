import pathlib,hashlib,json,sqlite3,xml.etree.ElementTree as ET,zipfile,time,sys
b=pathlib.Path('C:/Users/thedo/Desktop/Offline DAOC extra files')
stage=b/'releases/Offline DAoC v0.2';live=pathlib.Path('C:/Users/thedo/Desktop/Offline DaOC/runtime')
archive=b/'releases/Offline DAoC v0.2 - Complete Portable.zip'
def sha(p):
 h=hashlib.sha256()
 with p.open('rb') as f:
  for chunk in iter(lambda:f.read(1024*1024),b''):h.update(chunk)
 return h.hexdigest()
source=json.loads((b/'releases/source-runtime-hashes.json').read_text())
checked=0;changed=[];missing=[]
for sourcePath,originalHash in source.items():
 p=pathlib.Path(sourcePath)
 if sha(p)!=originalHash:changed.append(sourcePath)
 rel=p.relative_to(live);target=stage/'runtime'/rel
 if str(rel).replace('\\','/')!='server/config/serverconfig.xml' and (not target.exists() or sha(target)!=originalHash):missing.append(str(rel))
 checked+=1
 if checked%6000==0:print('Source/runtime hash checks',checked,flush=True)
assert not changed,changed
assert not missing,missing
print('All selected live source files unchanged; packaged runtime matches except intended auto-account config',flush=True)
with sqlite3.connect((stage/'runtime/data/opendaoc.sqlite3.db').as_uri()+'?mode=ro',uri=True) as c:
 for t in ['Account','DOLCharacters','DOLCharactersBackup','offline_world_bots','Inventory','ItemUnique','SinglePermission','offline_bot_commands','offline_runtime_status','realm_exchange_sales']:
  assert c.execute('select count(*) from '+t).fetchone()[0]==0,t
 assert c.execute('pragma quick_check').fetchone()[0]=='ok'
 assert dict(c.execute("select Key,Value from ServerProperty where Key in ('xp_rate','bot_xp_rate')"))=={'xp_rate':'1','bot_xp_rate':'1'}
 assert c.execute("select Value from offline_local_options where Key='MakeMeGM'").fetchone()[0]=='false'
 assert c.execute("select Value from ServerProperty where lower(Key)='allow_auto_account_creation'").fetchone()[0].lower()=='true'
assert ET.parse(stage/'runtime/server/config/serverconfig.xml').findtext('./Server/AutoAccountCreation').lower()=='true'
for extension in ['*.cmd','*.ps1']:
 for p in stage.rglob(extension):
  data=p.read_text(encoding='utf-8-sig');p.write_bytes(data.replace('\r\n','\n').replace('\n','\r\n').encode('utf-8'))
files=sorted(p for p in stage.rglob('*') if p.is_file() and p.name!='PACKAGE MANIFEST.sha256')
assert not any(p.suffix.lower() in {'.log','.dmp'} or any(part.lower() in {'progress-backups','logs'} for part in p.relative_to(stage).parts) for p in files)
manifest={p.relative_to(stage).as_posix():sha(p) for p in files}
(stage/'PACKAGE MANIFEST.sha256').write_text(''.join(f'{h}  {p}\n' for p,h in manifest.items()),encoding='utf-8')
files.append(stage/'PACKAGE MANIFEST.sha256')
total=sum(p.stat().st_size for p in files);print(f'Packing {len(files)} files, {total/1024**3:.2f} GiB',flush=True)
if archive.exists():raise SystemExit('Archive exists; refusing overwrite')
with zipfile.ZipFile(archive,'x',compression=zipfile.ZIP_DEFLATED,compresslevel=1,allowZip64=True) as z:
 done=0;last=time.monotonic()
 for p in files:
  z.write(p,'Offline DAoC v0.2/'+p.relative_to(stage).as_posix());done+=p.stat().st_size
  if time.monotonic()-last>20:
   print(f'ZIP progress {done/total:.0%}',flush=True);last=time.monotonic()
print(f'Archive built: {archive.stat().st_size/1024**3:.2f} GiB; verifying every entry',flush=True)
with zipfile.ZipFile(archive) as z:
 assert len(z.infolist())==len(files)
 for i,item in enumerate(z.infolist()):
  name=item.filename.removeprefix('Offline DAoC v0.2/')
  h=hashlib.sha256()
  with z.open(item) as stream:
   for chunk in iter(lambda:stream.read(1024*1024),b''):h.update(chunk)
  if name in manifest:assert h.hexdigest()==manifest[name],name
  if i and i%6000==0:print('ZIP CRC/SHA verified entries',i,flush=True)
 checksum=sha(archive)
(archive.with_suffix('.zip.sha256.txt')).write_text(checksum+'  '+archive.name+'\n')
result={'archive':str(archive),'zip_bytes':archive.stat().st_size,'unpacked_bytes':total,'file_count':len(files),'sha256':checksum,'runtime_source_verified':checked,'navmeshes':len(list((stage/'runtime/server/navmesh').glob('*.nav'))),'clean_seed':True,'all_zip_entries_crc_and_sha_verified':True}
(b/'releases/portable-v02-verification.json').write_text(json.dumps(result,indent=2))
print(json.dumps(result,indent=2),flush=True)

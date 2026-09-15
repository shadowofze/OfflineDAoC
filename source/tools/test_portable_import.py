import pathlib,sqlite3,subprocess,os,hashlib,json,shutil
b=pathlib.Path('C:/Users/thedo/Desktop/Offline DAOC extra files')
stage=b/'releases/Offline DAoC v0.2';test=b/'validation/portable-v02-import'
exe=stage/'tools/ProgressImporter/OfflineDaoc.ProgressImport.exe'
env=os.environ.copy();env['DOTNET_ROOT']=str(stage/'tools/dotnet');env['DOTNET_ROOT_X64']=env['DOTNET_ROOT'];env['DOTNET_MULTILEVEL_LOOKUP']='0'
def sha(p):
 h=hashlib.sha256()
 with p.open('rb') as f:
  for chunk in iter(lambda:f.read(1024*1024),b''):h.update(chunk)
 return h.hexdigest()
def db(folder):return test/folder/'runtime/data/opendaoc.sqlite3.db'
def run(source,target,label,success):
 report=test/(label+'.txt')
 result=subprocess.run([str(exe),'--import',str(test/source),str(test/target),'--replace-progress',str(report)],env=env,timeout=240)
 assert (result.returncode==0)==success,report.read_text()
 print(label, 'PASS',flush=True)
oldhash=sha(db('old'));targethash=sha(db('new'))
run('old','old','same-folder-rejected',False)
assert sha(db('old'))==oldhash
(test/'bad/runtime/data').mkdir(parents=True)
shutil.copy2(db('old'),db('bad'));shutil.copy2(test/'old/runtime/account.txt',test/'bad/runtime/account.txt')
c=sqlite3.connect(db('bad'));c.execute("alter table Account add column UnsupportedFutureField text default 'must not lose'");c.commit();c.close()
run('bad','new','unknown-schema-rejected',False)
assert sha(db('new'))==targethash
# A committed WAL must be included, not lost by copying only the main DB file.
c=sqlite3.connect(db('old'));c.execute('pragma journal_mode=wal');c.execute('pragma wal_autocheckpoint=0')
bot,money=c.execute('select BotId,MoneyCopper from offline_world_bots limit 1').fetchone()
c.execute('update offline_world_bots set MoneyCopper=? where BotId=?',(money+1,bot));c.commit()
run('old','new','wal-and-repeat-import',True)
d=sqlite3.connect(db('new'))
assert d.execute('select MoneyCopper from offline_world_bots where BotId=?',(bot,)).fetchone()[0]==money+1
assert d.execute('select count(*) from offline_world_bots').fetchone()[0]==10600
assert d.execute('select count(*) from DOLCharacters').fetchone()[0]==2
assert d.execute('select count(*) from Account where PrivLevel<>1').fetchone()[0]==0
assert d.execute('select count(*) from offline_world_bots where IsOnline<>0').fetchone()[0]==0
assert dict(d.execute("select Key,Value from ServerProperty where Key in ('xp_rate','bot_xp_rate')"))=={'xp_rate':'1','bot_xp_rate':'1'}
for table in ['Inventory','ItemUnique','AccountXMoney','realm_exchange_sales']:
 assert d.execute('select count(*) from '+table).fetchone()==c.execute('select count(*) from '+table).fetchone()
assert d.execute('pragma quick_check').fetchone()[0]=='ok'
c.close();d.close()
# The distributed seed was never used as the import destination.
seed=sqlite3.connect((stage/'runtime/data/opendaoc.sqlite3.db').as_uri()+'?mode=ro',uri=True)
for table in ['Account','DOLCharacters','offline_world_bots','Inventory','ItemUnique','realm_exchange_sales','SinglePermission']:
 assert seed.execute('select count(*) from '+table).fetchone()[0]==0,table
print('ALL IMPORT TESTS PASSED; shipped database is still clean',flush=True)

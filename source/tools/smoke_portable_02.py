import pathlib,shutil,subprocess,os,time,socket,sqlite3,json
def processes():
 data=json.loads(subprocess.check_output(['powershell','-NoProfile','-Command',
  'Get-CimInstance Win32_Process | Select-Object ProcessId,Name,ExecutablePath | ConvertTo-Json -Compress'],text=True))
 return data if isinstance(data,list) else [data]
b=pathlib.Path('C:/Users/thedo/Desktop/Offline DAOC extra files')
stage=b/'releases/Offline DAoC v0.2';test=b/'validation/portable-v02-smoke'
if any(p['Name'].lower()=='coreserver.exe' for p in processes()):raise SystemExit('Another server is running')
with socket.socket() as probe:
 if probe.connect_ex(('127.0.0.1',10300))==0:raise SystemExit('Port 10300 is in use')
test.mkdir(exist_ok=False)
shutil.copytree(stage/'runtime/server',test/'server')
shutil.copytree(stage/'runtime/data',test/'data')
shutil.copy2(stage/'runtime/account.txt',test/'account.txt')
env=os.environ.copy();env['DOTNET_ROOT']=str(stage/'tools/dotnet');env['DOTNET_ROOT_X64']=env['DOTNET_ROOT'];env['DOTNET_MULTILEVEL_LOOKUP']='0'
log=test/'console.log';clients=[]
with log.open('w',encoding='utf-8') as output:
 server=subprocess.Popen([str(test/'server/CoreServer.exe')],cwd=test/'server',env=env,stdin=subprocess.PIPE,stdout=output,stderr=subprocess.STDOUT,text=True,creationflags=subprocess.CREATE_NO_WINDOW)
 print('Isolated zero-bot server PID',server.pid,flush=True)
 try:
  deadline=time.monotonic()+240
  while time.monotonic()<deadline:
   if server.poll() is not None:raise RuntimeError('Server exited during startup')
   text=log.read_text(encoding='utf-8',errors='replace')
   if 'GameServer startup completed' in text:break
   time.sleep(1)
  else:raise RuntimeError('Startup timeout')
  print('Server startup passed using bundled .NET',flush=True)
  # Exercise the same connector launch as ENTER REALM, using only newly
  # generated package credentials. No user account or character is accessed.
  creds=dict(line.split(':',1) for line in (test/'account.txt').read_text().splitlines() if ':' in line)
  app=stage/'runtime/client-opendaoc/app'
  before={p['ProcessId'] for p in processes()}
  connector=subprocess.Popen([str(app/'connect.exe'),'game.dll','127.0.0.1',creds['Account'].strip(),creds['Password'].strip()],cwd=app,env=env,creationflags=subprocess.CREATE_NO_WINDOW)
  deadline=time.monotonic()+120
  while time.monotonic()<deadline:
   for p in processes():
    if p['ProcessId'] not in before and p['ExecutablePath'] and pathlib.Path(p['ExecutablePath']).parent==app and p['ProcessId'] not in clients:clients.append(p['ProcessId'])
   with sqlite3.connect((test/'data/opendaoc.sqlite3.db').as_uri()+'?mode=ro',uri=True) as db:
    found=db.execute("select count(*) from Account where Name='offline' and PrivLevel=1").fetchone()[0]
   if found:break
   time.sleep(1)
  else:raise RuntimeError('Client did not automatically create the account; inspect isolated logs')
  print('PASS: ENTER REALM connector created fresh offline account automatically, privilege 1',flush=True)
 finally:
  # Only processes created for this disposable, characterless login test.
  for pid in clients:
   subprocess.run(['taskkill','/PID',str(pid),'/T','/F'],capture_output=True)
  if server.poll() is None:
   server.stdin.write('exit\n');server.stdin.flush()
   try:server.wait(timeout=120)
   except subprocess.TimeoutExpired:server.terminate();raise RuntimeError('Smoke server required forced shutdown')
  print('Isolated server stopped, exit',server.returncode,flush=True)

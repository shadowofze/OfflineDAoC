import pathlib,re,json,shutil
b=pathlib.Path('C:/Users/thedo/Desktop/Offline DAOC extra files')
stage=b/'releases/Offline DAoC v0.2'
server=b/'development source/server'
common='''OFFLINE DAOC 0.2 — SLASH COMMANDS

COMMON PLAYER COMMANDS (NO GM REQUIRED)

/classes                 List your realm's classes and companion roles.
/spawn                   Open the companion class-selection menu.
/spawn <class name>      Add a level-scaled temporary companion to your party.
/pull                    Tell party bots and pets to engage your selected target.
/grind                   Start stationary automatic pulling with your companions.
/grind stop              Stop automatic grind pulling.
/mobs                    List accessible monsters at your current level.
/mobs <level> [page]     List exact-level monsters; optional page number.
/tele <playerbot name>   Teleport to a named autonomous playerbot.
/tele mob <name>         Go to a monster camp; dungeon targets use an outside approach.
/tc                      Teleport to your capital's Realm Exchange.
/faction <message>       Talk to your realm's players and playerbots.
/who                     Query players/playerbots.
/help                    Show available in-game help.
/quit                    Log out normally.

GM OBSERVER COMMAND

/fly                     Toggle invisible, invulnerable observer flight. Requires
                         GM access. Space ascends while enabled. Toggling off
                         returns you to the starting location. Use the game's
                         configurable Hide Interface keybinding to toggle the UI;
                         F12 is not assigned to flight by this package.

Make Me a GM defaults OFF. Create your local account by entering the game once,
then log out and stop the server before changing the launcher GM checkbox.
GM commands can change the world: back up progress before using unfamiliar ones.

LAUNCHER SHORTCUTS (NOT SLASH COMMANDS)

+ Lv.1 and + Lv.50 create bots for the selected realm. Shift-click: 10 bots;
Ctrl-click: 100 bots. Level 50 bots receive templated class gear and 10 platinum.
Right-click a bot in the list and select Teleport to for the in-game character.

REGISTERED COMMAND REFERENCE

The following names and privilege levels were extracted from the current server
source matching this release. Some legacy help text uses localization keys or
requires context; consult in-game help for syntax. GM=privilege 2; Admin=3.
An admin-only command is not unlocked by the ordinary Make Me a GM checkbox.
Client-side actions/keybindings are not all server slash commands.

'''
commands={}
for file in (server/'GameServer').rglob('*.cs'):
 if any(x in {'obj','bin'} for x in file.parts):continue
 text=file.read_text(encoding='utf-8-sig',errors='replace')
 for match in re.finditer(r'\[Cmd(?:Attribute)?\((.*?)\)\]',text,re.S):
  body=match.group(1); privilege=re.search(r'ePrivLevel\.(\w+)',body)
  if not privilege:continue
  initial=body[:privilege.start()]
  strings=re.findall(r'"((?:\\.|[^"\\])*)"',initial)
  if not strings:continue
  name=strings[0].lstrip('&')
  if name.casefold() in {'bot','go','travel'}:continue
  commands[name]=privilege.group(1)
  # Only ampersand-prefixed aliases, not header localization keys.
  for alias in strings[1:]:
   if alias.startswith('&') and alias[1:].casefold() not in {'bot','go','travel'}:commands[alias[1:]]=privilege.group(1)
assert all(x in commands for x in ['spawn','classes','grind','pull','mobs','tele','tc','fly'])
(stage/'SLASH COMMANDS.txt').write_text(common+'\n'.join('/'+name+' — '+commands[name] for name in sorted(commands,key=str.lower))+'\n',encoding='utf-8')
# Corresponding server/launcher source, excluding builds, logs, private saves,
# repository history and operational repair reports.
target=stage/'source'
allowed={'.cs','.csproj','.sln','.slnx','.props','.targets','.resx','.config','.cpp','.c','.h','.hpp','.natvis','.rc','.def','.cmake','.ico','.png','.json','.yml','.yaml'}
count=0
for part in ['CoreBase','CoreDatabase','CoreServer','GameServer','Pathing','Tests']:
 for p in (server/part).rglob('*'):
  if not p.is_file() or any(x.lower() in {'bin','obj','build','release','debug','testresults','.git','logs','backups','reports'} for x in p.relative_to(server).parts):continue
  if p.suffix.lower() not in allowed:continue
  dest=target/'server'/p.relative_to(server);dest.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(p,dest);count+=1
for p in server.iterdir():
 if p.is_file() and (p.suffix in {'.sln','.props','.targets','.config'} or p.name in {'LICENSE','README.md','.editorconfig'}):
  dest=target/'server'/p.name;dest.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(p,dest)
launcher=b/'development source/tools/OfflineDaoc.Launcher'
for p in launcher.rglob('*'):
 if p.is_file() and not any(x in {'bin','obj'} for x in p.relative_to(launcher).parts):
  dest=target/'launcher'/p.relative_to(launcher);dest.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(p,dest)
shutil.copy2(server/'LICENSE',stage/'SERVER LICENSE.txt')
print('Commands:',len(commands),'source files:',count)

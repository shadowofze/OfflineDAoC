"""Offline DAoC safe texture workflow. No AI, gameplay or database changes."""
import argparse
import hashlib
import io
import json
import math
import os
from pathlib import Path
import shutil
import struct
import subprocess
import uuid
from datetime import datetime, timezone

from PIL import Image
from archive import Entry, read, write, verify_memory_image, require

HOME = Path(__file__).resolve().parent
CLIENT = HOME.parent / 'runtime/client-opendaoc/app'
Image.MAX_IMAGE_PIXELS = 36_000_000

def sha(data):
    return hashlib.sha256(data).hexdigest()

def load(path):
    return json.loads(Path(path).read_text(encoding='utf-8'))

def save(path, value):
    Path(path).write_text(json.dumps(value, indent=2), encoding='utf-8')

def safe(base, relative):
    require(isinstance(relative, str) and relative and not Path(relative).is_absolute(), 'Expected relative path')
    path = (base / relative).resolve()
    require(path.is_relative_to(base.resolve()) and path != base.resolve(), 'Path escapes its folder')
    return path

def profiles():
    return load(HOME/'assets.json')

def stopped():
    if os.name == 'nt':
        running = subprocess.check_output(['tasklist', '/FO', 'CSV'], text=True, creationflags=0x08000000).lower()
        require(not any('"'+name+'"' in running for name in
                        ['coreserver.exe','game.dll','game.exe','camelot.exe','connect.exe','offlinedaoc.exe','opendaoclauncher.exe']),
                'Close the game, server AND launcher before installing or restoring. Build/export can be done while playing.')

def profile_files(profile):
    return [profile['texture_path']] + profile.get('fallback_paths', [])

def check_profile(profile):
    require(profile.get('private_asset') is True, 'Only explicitly registered private assets can be installed')
    # Profiles are local administrator-managed metadata, not supplied by external artwork.
    for path, expected in profile['guards'].items():
        require(sha(safe(CLIENT,path).read_bytes()) == expected, f'Protected model/source changed: {path}. Review the asset profile.')
    _, entries = read((CLIENT/'gamedata.mpk').read_bytes())
    tables = {e.name:e.data for e in entries}
    for table, identity, expected in profile['bindings']:
        matches = [row for row in tables[table].splitlines() if row.startswith(str(identity).encode()+b',')]
        require(len(matches) == 1 and sha(matches[0]) == expected, f'Client binding changed: {table} #{identity}')
    for table, identity in profile.get('contiguous_registration', []):
        rows=tables[table].splitlines()
        index=next(i for i,row in enumerate(rows) if row.startswith(str(identity).encode()+b','))
        require(all(row.split(b',',1)[0].strip().isdigit() for row in rows[2:index+1]),
                f'{table} #{identity} is behind a blank/invalid registration row; repair the binding before importing')
    for p in profile_files(profile):
        safe(CLIENT,p)
        require(p not in profile['guards'] and p != profile['mesh_path'] and p != 'gamedata.mpk',
                'Texture installation cannot overwrite a protected model, source or mapping table')

def texture_bytes(profile):
    data = safe(CLIENT,profile['texture_path']).read_bytes()
    if profile.get('archive_entry'):
        _, entries = read(data)
        matches = [e for e in entries if e.name == profile['archive_entry']]
        require(len(matches)==1, 'Private texture not found in archive')
        return matches[0].data
    return data

def export_project(key, destination=None):
    profile = profiles()[key]
    check_profile(profile)
    folder = Path(destination) if destination else HOME/'projects'/f'{key}-{datetime.now():%Y%m%d-%H%M%S}-{uuid.uuid4().hex[:6]}'
    require(not folder.exists(), 'Export destination already exists; choose a new folder')
    raw = texture_bytes(profile)
    image = Image.open(io.BytesIO(raw)).convert('RGBA')
    folder.mkdir(parents=True)
    image.save(folder/'EDIT THIS TEXTURE.png')
    (folder/'reference-original.dds').write_bytes(raw)
    mesh = safe(CLIENT,profile['mesh_path']).read_bytes()
    (folder/'reference-mesh.nif').write_bytes(mesh)
    reference_hashes = {f:sha((folder/f).read_bytes()) for f in ['reference-original.dds','reference-mesh.nif']}
    info = dict(version=1, asset=key, width=image.width, height=image.height,
                profile_hash=sha(json.dumps(profile,sort_keys=True).encode()), references=reference_hashes,
                baseline={p:sha(safe(CLIENT,p).read_bytes()) for p in profile_files(profile)})
    save(folder/'asset-project.json',info)
    (folder/'EDITING INSTRUCTIONS.txt').write_text(
        'Edit/upscale EDIT THIS TEXTURE.png, then save the result as a NEW PNG.\n'
        'Keep the entire atlas in the same orientation and proportions. Do not crop, move,\n'
        'rearrange or paint over separate UV islands. A full character portrait is NOT a texture.\n'
        'Do not edit reference-original.dds, reference-mesh.nif or asset-project.json.\n'
        'The importer preserves the original alpha mask. Preview carefully before installing.\n'
        'AI cannot be assumed to preserve UV island placement even when dimensions match.\n'
        'NIF is supplied for reference; new geometry/rigging is not automatically converted.\n',encoding='utf-8')
    return folder

def dds(image):
    alpha = image.getchannel('A').getextrema() != (255,255)
    codec = 'DXT5' if alpha else 'DXT1'
    working = image.convert('RGBA' if alpha else 'RGB')
    blocks, header = [], None
    while True:
        output=io.BytesIO()
        working.save(output,format='DDS',pixel_format=codec)
        data=output.getvalue()
        require(data[84:88] == codec.encode(), 'Unexpected DDS compression')
        header = bytearray(data[:128]) if header is None else header
        blocks.append(data[128:])
        if working.size == (1,1):
            break
        working=working.resize((max(1,working.width//2),max(1,working.height//2)),Image.Resampling.LANCZOS)
    struct.pack_into('<I',header,8,(struct.unpack_from('<I',header,8)[0]|0xA0000)&~0x8)
    # Pillow's compressed DDS header may contain a row-pitch value here. With
    # DDSD_LINEARSIZE this field must describe the entire top compressed mip.
    struct.pack_into('<I',header,20,len(blocks[0]))
    struct.pack_into('<I',header,88,0)
    struct.pack_into('<I',header,28,len(blocks))
    struct.pack_into('<I',header,108,0x1000|0x8|0x400000)
    result=bytes(header)+b''.join(blocks)
    validate_dds(result)
    return result

def validate_dds(data):
    require(len(data)>=128, 'Truncated DDS header')
    require(data[:4] == b'DDS ' and struct.unpack_from('<I',data,4)[0] == 124, 'Not legacy DDS')
    h,w=struct.unpack_from('<II',data,12)
    levels=struct.unpack_from('<I',data,28)[0]
    codec=data[84:88]
    require(codec in (b'DXT1',b'DXT5'), 'Unsupported DDS codec/DX10 header')
    require(1<=w<=2048 and 1<=h<=2048 and w&(w-1)==0 and h&(h-1)==0, 'Invalid texture dimensions')
    require(levels==int(math.log2(max(w,h)))+1, 'Incomplete mip chain')
    block=8 if codec==b'DXT1' else 16
    require(struct.unpack_from('<I',data,8)[0]&0x80000 != 0, 'DDS lacks linear-size flag')
    require(struct.unpack_from('<I',data,20)[0] == max(1,(w+3)//4)*max(1,(h+3)//4)*block,
            'DDS top-level compressed byte count is incorrect')
    offset=128
    for i in range(levels):
        width,height=max(1,w>>i),max(1,h>>i)
        size=max(1,(width+3)//4)*max(1,(height+3)//4)*block
        require(len(data)>=offset+size, 'Truncated DDS mip data')
        offset+=size
    require(offset==len(data), 'DDS payload length mismatch')
    image=Image.open(io.BytesIO(data));image.load()
    return w,h,codec.decode(),levels

def build(project, artwork, maximum=2048, mesh=None):
    folder=Path(project).resolve()
    meta=load(folder/'asset-project.json')
    profile=profiles()[meta['asset']]
    check_profile(profile)
    require(meta['profile_hash']==sha(json.dumps(profile,sort_keys=True).encode()), 'Profile changed; export a new project')
    for filename,digest in meta['references'].items():
        require(sha(safe(folder,filename).read_bytes())==digest,'Reference file changed; export a new project')
    if mesh:
        require(Path(mesh).suffix.lower()=='.nif' and sha(Path(mesh).read_bytes())==meta['references']['reference-mesh.nif'],
                'New/modified meshes require a separate NIF, skeleton, bone-weight and UV conversion review. No mesh installed.')
    require(maximum in (256,512,1024,2048),'Maximum texture size must be 256, 512, 1024 or 2048')
    artwork=Path(artwork)
    require(artwork.suffix.lower() in ('.png','.tga','.dds','.bmp','.jpg','.jpeg','.webp'),
            'Import a flat texture atlas image, not an OBJ/FBX/GLB mesh or a character portrait.')
    with Image.open(artwork) as opened:
        require(getattr(opened,'n_frames',1)==1,'Animated artwork is unsupported')
        require(opened.getexif().get(274,1)==1,'Rotated EXIF artwork: export a normally oriented PNG first')
        require(16<=opened.width<=8192 and 16<=opened.height<=8192,'Artwork dimensions outside 16..8192')
        image=opened.convert('RGBA')
    reference_bytes=(folder/'reference-original.dds').read_bytes()
    validate_dds(reference_bytes)
    reference=Image.open(io.BytesIO(reference_bytes)).convert('RGBA')
    ratio=reference.width/reference.height
    require(abs((image.width/image.height)/ratio-1)<=0.01,
            'Aspect ratio differs from original atlas. Do not crop or add borders; correct the image externally.')
    largest=min(maximum,2**math.ceil(math.log2(max(image.size))))
    size=(largest,max(1,round(largest/ratio))) if ratio>=1 else (max(1,round(largest*ratio)),largest)
    require(all(x&(x-1)==0 for x in size),'Original atlas ratio cannot map safely to power-of-two dimensions')
    prepared=image.resize(size,Image.Resampling.LANCZOS)
    prepared.putalpha(reference.getchannel('A').resize(size,Image.Resampling.LANCZOS))
    encoded=dds(prepared)
    require(encoded[84:88]==reference_bytes[84:88], 'Compression/alpha mode changed; this asset needs manual review')
    # Follow the now-tested game texture pipeline: retain all legacy header
    # properties from the working texture except dimensions, size and mip count.
    header=bytearray(reference_bytes[:128])
    for offset in (12,16,20,28):
        header[offset:offset+4]=encoded[offset:offset+4]
    encoded=bytes(header)+encoded[128:]
    validate_dds(encoded)
    staged=folder/'builds'/uuid.uuid4().hex
    staged.mkdir(parents=True)
    outputs={}
    target=profile['texture_path']
    baseline=meta['baseline']
    require(set(baseline)==set(profile_files(profile)),'Unexpected baseline target set')
    for path,digest in baseline.items():
        require(sha(safe(CLIENT,path).read_bytes())==digest,'Live texture changed; export a fresh project before building')
    if profile.get('archive_entry'):
        original=safe(CLIENT,target).read_bytes()
        name,entries=read(original)
        replaced=[]
        for entry in entries:
            replaced.append(Entry(entry.name,encoded,entry.timestamp,entry.flags) if entry.name==profile['archive_entry'] else entry)
        outputs[target]=write(name,replaced)
        verify_memory_image(outputs[target])
        # Every other archive entry must be byte-identical.
        for old,new in zip(entries,read(outputs[target])[1]):
            if old.name!=profile['archive_entry']:
                require(old==new,'Unrelated archive entry changed')
    else:
        outputs[target]=encoded
    for fallback in profile.get('fallback_paths',[]):
        stream=io.BytesIO()
        prepared.convert('RGB' if prepared.getchannel('A').getextrema()==(255,255) else 'RGBA').save(stream,format='TGA')
        outputs[fallback]=stream.getvalue()
    for path,data in outputs.items():
        destination=safe(staged/'payload',path);destination.parent.mkdir(parents=True,exist_ok=True);destination.write_bytes(data)
    prepared.save(staged/'converted-preview.png')
    # Side-by-side preview at matching scales exposes layout differences for human review.
    preview=Image.new('RGBA',(1024,512),(40,40,40,255))
    preview.paste(reference.resize((512,512)),(0,0));preview.paste(Image.open(io.BytesIO(encoded)).convert('RGBA').resize((512,512)),(512,0))
    preview.convert('RGB').save(staged/'COMPARE ORIGINAL LEFT - IMPORT RIGHT.png')
    report=dict(asset=meta['asset'],input=str(artwork),dimensions=size,dds=validate_dds(encoded),
                alpha='Original alpha mask preserved',mesh='Original NIF and normalized UVs unchanged',
                warning='Inspect preview and test a fresh summon in game. Dimensions cannot prove AI preserved UV island artwork.',
                profile_hash=meta['profile_hash'],baseline=baseline,
                outputs={path:sha(data) for path,data in outputs.items()},installed=False)
    save(staged/'build.json',report)
    return staged

class ToolLock:
    def __enter__(self):
        self.path=HOME/'INSTALL IN PROGRESS.lock'
        try:
            self.handle=self.path.open('x')
        except FileExistsError:
            raise ValueError('Another installation is active, or an interrupted install needs review. See backups and INSTALL IN PROGRESS.lock.')
        self.handle.write(str(os.getpid()));self.handle.close()
        return self
    def __exit__(self,*_):
        self.path.unlink()

def install(staged, reviewed=False):
    require(reviewed,'Preview confirmation required: review the comparison image before installation')
    staged=Path(staged).resolve();report=load(staged/'build.json');profile=profiles()[report['asset']]
    with ToolLock():
        stopped();check_profile(profile)
        require(report['profile_hash']==sha(json.dumps(profile,sort_keys=True).encode()),'Profile changed')
        require(set(report['outputs'])==set(profile_files(profile))==set(report['baseline']),'Unapproved installation targets')
        require(not report['installed'],'Build already installed; export a new project for another edit')
        prepared={}
        for relative,expected in report['outputs'].items():
            live=safe(CLIENT,relative);data=safe(staged/'payload',relative).read_bytes()
            require(sha(data)==expected,'Staged file changed')
            require(sha(live.read_bytes())==report['baseline'][relative],'Live files changed since export')
            if relative.endswith('.mpk'):
                archive_name, new_entries=read(data);verify_memory_image(data)
                old_name, old_entries=read(live.read_bytes())
                require(archive_name==old_name and len(old_entries)==len(new_entries), 'Texture archive layout changed')
                found=0
                for old,new in zip(old_entries,new_entries):
                    require((old.name,old.timestamp,old.flags)==(new.name,new.timestamp,new.flags), 'Archive entry metadata changed')
                    if old.name==profile.get('archive_entry'):
                        validate_dds(new.data);found+=1
                    else:
                        require(old.data==new.data,'Unrelated archive texture changed')
                require(found==1,'Private texture archive entry missing')
            elif relative.endswith('.dds'):
                validate_dds(data)
            else:
                Image.open(io.BytesIO(data)).load()
            prepared[relative]=data
        backup=HOME/'backups'/f'{datetime.now():%Y%m%d-%H%M%S}-{uuid.uuid4().hex[:8]}'
        backup.mkdir(parents=True)
        receipt=dict(state='prepared',asset=report['asset'],before=report['baseline'],after=report['outputs'])
        for relative in prepared:
            dest=safe(backup/'files',relative);dest.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(safe(CLIENT,relative),dest)
        save(backup/'receipt.json',receipt)
        try:
            for relative,data in prepared.items():
                stopped()
                live=safe(CLIENT,relative);temp=live.with_name(live.name+'.assettool-tmp')
                require(not temp.exists(),'Leftover temporary file needs review')
                temp.write_bytes(data)
                try:
                    os.replace(temp,live)
                finally:
                    if temp.exists():temp.unlink()
            require(all(sha(safe(CLIENT,p).read_bytes())==h for p,h in report['outputs'].items()),'Installed hash mismatch')
            check_profile(profile)
        except Exception:
            for relative in prepared:shutil.copy2(safe(backup/'files',relative),safe(CLIENT,relative))
            receipt['state']='failed-restored';save(backup/'receipt.json',receipt)
            raise
        receipt['state']='installed';save(backup/'receipt.json',receipt)
        report['installed']=True;report['backup']=str(backup);save(staged/'build.json',report)
    return backup

def rollback(backup):
    backup=Path(backup).resolve()
    require(backup.is_relative_to((HOME/'backups').resolve()),'Select a backup made by this tool')
    receipt=load(backup/'receipt.json');profile=profiles()[receipt['asset']]
    with ToolLock():
        stopped()
        require(receipt['state'] in ('installed','prepared'),'Backup is not pending or installed')
        require(set(receipt['before'])==set(receipt['after'])==set(profile_files(profile)),'Unexpected rollback targets')
        for p,expected in receipt['before'].items():
            require(sha(safe(backup/'files',p).read_bytes())==expected,'Backup checksum mismatch')
            current=sha(safe(CLIENT,p).read_bytes())
            require(current in (expected,receipt['after'][p]),'Later change detected; refusing to overwrite it')
        # Retain current bytes in memory so an I/O failure during restoration
        # cannot intentionally leave a half-restored sword DDS/TGA pair.
        current={p:safe(CLIENT,p).read_bytes() for p in receipt['before']}
        try:
            for p in receipt['before']:
                stopped()
                shutil.copy2(safe(backup/'files',p),safe(CLIENT,p))
            require(all(sha(safe(CLIENT,p).read_bytes())==h for p,h in receipt['before'].items()),'Rollback verification failed')
        except Exception:
            for p,data in current.items():safe(CLIENT,p).write_bytes(data)
            raise
        receipt['state']='restored';save(backup/'receipt.json',receipt)
    return backup

def main():
    parser=argparse.ArgumentParser(description=__doc__)
    commands=parser.add_subparsers(dest='command',required=True)
    commands.add_parser('list')
    exp=commands.add_parser('export');exp.add_argument('asset');exp.add_argument('--output')
    imp=commands.add_parser('build');imp.add_argument('project');imp.add_argument('artwork');imp.add_argument('--maximum',type=int,default=2048);imp.add_argument('--mesh')
    ins=commands.add_parser('install');ins.add_argument('build');ins.add_argument('--preview-reviewed',action='store_true')
    rb=commands.add_parser('rollback');rb.add_argument('backup')
    chk=commands.add_parser('validate');chk.add_argument('file')
    args=parser.parse_args()
    if args.command=='list':print('\n'.join(f'{k}: {v["label"]}' for k,v in profiles().items()))
    elif args.command=='export':print(export_project(args.asset,args.output))
    elif args.command=='build':print(build(args.project,args.artwork,args.maximum,args.mesh))
    elif args.command=='install':print(install(args.build,args.preview_reviewed))
    elif args.command=='rollback':print(rollback(args.backup))
    elif args.command=='validate':
        data=Path(args.file).read_bytes()
        if data[:4]==b'MPAK':read(data);print(f'PASS: {verify_memory_image(data)} MPK entries, both offset paths checked')
        else:print(validate_dds(data))

if __name__=='__main__':
    try:main()
    except Exception as error:
        raise SystemExit(f'STOPPED: {error}')

"""Stage rebuilt lair tiles while preserving every other installed nav tile byte-for-byte."""
from pathlib import Path
import struct
import hashlib

ROOT = Path(__file__).resolve().parents[1]
LIVE = Path('C:/Users/thedo/Desktop/Offline DAoC/runtime/server/navmesh')
NEW = ROOT.parent/'OpenDAoC-BuildNav/bin/Release/net10.0-windows7.0/base/zones'
OUT = ROOT/'build/dragon-geometry/staged/navmesh'

def read(path):
    data=path.read_bytes()
    magic,version,count=struct.unpack_from('<3i',data)
    assert magic==0x4d534554 and version==1
    tiles={};offset=40
    for _ in range(count):
        ref,size=struct.unpack_from('<Qi',data,offset)
        header=data[offset:offset+16];offset+=16
        blob=data[offset:offset+size];offset+=size
        assert len(blob)==size and struct.unpack_from('<i',blob)[0]==0x444e4156
        key=struct.unpack_from('<3i',blob,8)
        assert key not in tiles
        tiles[key]=(header,blob)
    assert offset==len(data)
    return data[:40],tiles

if __name__=='__main__':
    OUT.mkdir(parents=True,exist_ok=True)
    for zone,x,y in [(4,391326,755351),(116,708811,1021459),(216,408646,706432)]:
        name=f'zone{zone:03}.nav'
        oldheader,old=read(LIVE/name);newheader,new=read(NEW/name)
        # Detour tile lookup uses horizontal origin only. Vertices and each tile's
        # BV tree carry absolute heights; a different global minimum Y is harmless.
        assert oldheader[12:16]==newheader[12:16] and oldheader[20:]==newheader[20:], 'Grid/limits differ; do not merge incompatible meshes'
        changed=[];parts=[]
        for key,(tileheader,blob) in old.items():
            bounds=struct.unpack_from('<6f',blob,72)
            # Entire lair plus apron. No other towns/camps/zone approaches are rebuilt.
            overlaps=(bounds[0]*32<=x+2200 and bounds[3]*32>=x-2200 and
                      bounds[2]*32<=y+2200 and bounds[5]*32>=y-2200)
            if overlaps:
                assert key in new, f'Missing lair tile {key}'
                replacement=new[key][1]
                # Keep installed tile references; Detour recreates links on load.
                tileheader=tileheader[:8]+struct.pack('<i',len(replacement))+tileheader[12:16]
                blob=replacement;changed.append(key)
            parts.extend((tileheader,blob))
        assert changed and len(changed)<400
        result=oldheader+b''.join(parts)
        (OUT/name).write_bytes(result)
        _,check=read(OUT/name)
        assert len(check)==len(old)
        for key in old:
            if key not in changed: assert check[key]==old[key]
        print(name,'lair tiles',len(changed),'unchanged tiles',len(old)-len(changed),
              'source sha256',hashlib.sha256((LIVE/name).read_bytes()).hexdigest(),
              'staged sha256',hashlib.sha256(result).hexdigest())

"""Read-only, bounded x86 client inspection for the native raid extension."""
import argparse
import struct
import pefile
import capstone
from pathlib import Path
p=argparse.ArgumentParser()
p.add_argument('--address',type=lambda v:int(v,0))
p.add_argument('--size',type=int,default=180)
p.add_argument('--xref',type=lambda v:int(v,0))
p.add_argument('--string')
p.add_argument('--calls',type=lambda v:int(v,0))
a=p.parse_args()
pe=pefile.PE(r'C:/Users/thedo/Desktop/Offline DAoC/runtime/client-opendaoc/app/game.dll')
base=pe.OPTIONAL_HEADER.ImageBase;data=pe.__data__
if a.string:
    pos=0
    while True:
        pos=data.find(a.string.encode()+b'\0',pos)
        if pos<0:break
        print('STRING',hex(base+pe.get_rva_from_offset(pos)))
        a.xref=base+pe.get_rva_from_offset(pos)
        pos+=1
if a.xref:
    pos=0
    while True:
        pos=data.find(struct.pack('<I',a.xref),pos)
        if pos<0:break
        print('XREF',hex(base+pe.get_rva_from_offset(pos)));pos+=4
if a.calls:
    section=next(s for s in pe.sections if s.Name.startswith(b'.text'))
    code=section.get_data();lo=base+section.VirtualAddress
    for i in range(len(code)-5):
        if code[i]==0xe8 and lo+i+5+struct.unpack_from('<i',code,i+1)[0]==a.calls:
            print('CALL',hex(lo+i))
if a.address:
    md=capstone.Cs(capstone.CS_ARCH_X86,capstone.CS_MODE_32)
    for i in md.disasm(pe.get_data(a.address-base,a.size),a.address):
        print(hex(i.address),i.mnemonic,i.op_str)

"""Execute the native hooks in x86 emulation, never against a running client."""
import struct
import sys
import xml.etree.ElementTree as ET
import build_native_raid_client as build
from unicorn import Uc, UC_ARCH_X86, UC_MODE_32, UC_HOOK_CODE
from unicorn.x86_const import *
import pefile

image,report=build.build();pe=pefile.PE(data=image);base=pe.OPTIONAL_HEADER.ImageBase
uc=Uc(UC_ARCH_X86,UC_MODE_32);uc.mem_map(base,0x2100000)
for section in pe.sections:
    if section.SizeOfRawData:uc.mem_write(base+section.VirtualAddress,section.get_data())
uc.mem_map(0x5000000,0x10000);uc.mem_map(0x6000000,0x20000)
sp=0x5008000;frame=sp+0x100;body=0x6010000;sentinel=0x601f000
allocations=[];texts={};selected=[];windows=[]
def read32(address):return struct.unpack('<I',uc.mem_read(address,4))[0]
def string(address):return bytes(uc.mem_read(address,128)).split(b'\0')[0].decode('ascii')
def ret(cleanup=0):
    esp=uc.reg_read(UC_X86_REG_ESP);target=read32(esp)
    uc.reg_write(UC_X86_REG_ESP,esp+4+cleanup);uc.reg_write(UC_X86_REG_EIP,target)
def stub(machine,address,size,user):
    esp=machine.reg_read(UC_X86_REG_ESP)
    if address in (0x4b6bc0,0x4b6c84):
        out=read32(esp+8);label=string(read32(esp+12));obj=0x6000000+64*len(allocations)
        machine.mem_write(out,struct.pack('<I',obj))
        if address==0x4b6bc0:machine.mem_write(obj+8,struct.pack('<3f',100,0,-3.402823e38))
        allocations.append(label);ret(20 if address==0x4b6bc0 else 12)
    elif address==0x524436:
        texts[machine.reg_read(UC_X86_REG_EBX)]=string(read32(esp+4));ret(4)
    elif address==0x77156e:
        machine.reg_write(UC_X86_REG_EAX,0 if string(read32(esp+4))==string(read32(esp+8)) else 1);ret()
    elif address==0x41ab40:
        selected.append(read32(machine.reg_read(UC_X86_REG_EAX)));ret()
    elif address==0x601e000:
        windows.append((read32(esp+4),read32(esp+8)));ret(8)
uc.hook_add(UC_HOOK_CODE,stub)
def setup():
    uc.reg_write(UC_X86_REG_ESP,sp);uc.reg_write(UC_X86_REG_EBP,frame)
    uc.reg_write(UC_X86_REG_EBX,body);uc.reg_write(UC_X86_REG_EDI,0x123456)
uc.mem_write(0x104c2bc,struct.pack('<I',0x601d000))
uc.mem_write(0x601d000,struct.pack('<I',0x601d100))
uc.mem_write(0x601d108,struct.pack('<I',0x601e000))
setup();uc.emu_start(report['blocks']['init']['address'],0x4da940,count=100000)
assert len(allocations)==120 and len(set(allocations))==120
def packet(op,slot=0,hp=0,power=0,id=0,name='',length=128,version=1):
    setup();data=bytearray(128);data[1:8]=bytes([0x52,version,op,slot,hp,power,0])
    struct.pack_into('<H',data,8,id);data[12:12+len(name)]=name.encode()
    uc.mem_write(body,bytes(data));uc.mem_write(frame+0x18,struct.pack('<I',length))
    end=0x411209 if length<128 or version!=1 else 0x4113ad
    uc.emu_start(report['blocks']['packet']['address'],end,count=100000)
    assert uc.reg_read(UC_X86_REG_ESP)==sp
for i in range(40):packet(1,i,100-i,60-i,200+i,f'Member{i:02}')
for i in range(40):
    h=read32(report['hp']+i*4);p=read32(report['power']+i*4);n=read32(report['names']+i*4)
    assert struct.unpack('<f',uc.mem_read(h+12,4))[0]==100-i
    assert struct.unpack('<f',uc.mem_read(p+12,4))[0]==60-i
    assert texts[n]==f'Member{i:02}'
    assert read32(report['ids']+i*4)==200+i
packet(1,255,100,100,65535,'Invalid')
packet(1,0,101,0,65535,'Invalid')
packet(1,0,50,50,65535,'Short',length=2)
packet(1,0,50,50,65535,'Version',version=2)
assert read32(report['ids'])==200
packet(2);assert windows[-1]==(0x76,1)
for i in range(40):
    setup();uc.mem_write(sp,struct.pack('<I',sentinel));uc.mem_write(body,f'RaidMember{i:02}'.encode()+b'\0')
    uc.reg_write(UC_X86_REG_ESI,body)
    uc.emu_start(report['blocks']['eventNames']['address'],sentinel,count=100000)
    assert uc.reg_read(UC_X86_REG_EAX)==0x600+i
    setup();uc.mem_write(sp,struct.pack('<I',sentinel));uc.mem_write(body+8,struct.pack('<I',0x600+i))
    uc.reg_write(UC_X86_REG_EAX,body)
    uc.emu_start(report['blocks']['eventHandler']['address'],sentinel,count=100000)
    assert selected[-1]==200+i
packet(3);assert windows[-1]==(0x76,0)
setup();uc.mem_write(sp,struct.pack('<I',sentinel));uc.reg_write(UC_X86_REG_EAX,body)
uc.mem_write(body+8,struct.pack('<I',0x600));uc.emu_start(report['blocks']['eventHandler']['address'],sentinel,count=10000)
assert len(selected)==40
root=ET.fromstring(build.window())
assert len(root.findall('.//InvisibleButtonDef'))==40
assert len(root.findall('.//StatusBarDef'))==80
print('PASS: 120 independent native adapters; all 40 live names/health/power/IDs.')
print('PASS: bounded packets reject short/version/index/health errors.')
print('PASS: all 40 click actions select the matching ID; inactive raid ignores clicks.')
print('PASS: native show/hide and 40 clickable XML rows. Game rendering still requires live verification.')

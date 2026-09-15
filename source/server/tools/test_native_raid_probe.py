"""Emulate probe machine code with a stubbed UI allocator; no game process."""
import importlib.util
import struct
import sys
import xml.etree.ElementTree as ET
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parents[1]/'build/native-raid-probe/deps'))
from unicorn import Uc, UC_ARCH_X86, UC_MODE_32, UC_HOOK_CODE
from unicorn.x86_const import *
import pefile
import native_raid_probe as probe

original=(probe.CLIENT/'game.dll').read_bytes()
patched,report=probe.build(original)
pe=pefile.PE(data=patched)
base=pe.OPTIONAL_HEADER.ImageBase
start=int(report['codeAddress'],16)
slots=int(report['slots'],16)
for failed_slot in (-1,0,19,39):
    uc=Uc(UC_ARCH_X86,UC_MODE_32)
    uc.mem_map(base,0x2100000)
    for section in pe.sections:
        if section.SizeOfRawData: uc.mem_write(base+section.VirtualAddress,section.get_data())
    stack=0x5000000; heap=0x6000000
    uc.mem_map(stack,0x10000);uc.mem_map(heap,0x10000)
    sp=stack+0x8000
    regs={UC_X86_REG_EAX:0x1234,UC_X86_REG_EBX:0x5678,UC_X86_REG_ECX:0x11223344,
          UC_X86_REG_EDX:0x4321,UC_X86_REG_ESI:0x6789,UC_X86_REG_EDI:0x778899,
          UC_X86_REG_EBP:sp+128}
    for reg,value in regs.items():uc.reg_write(reg,value)
    uc.reg_write(UC_X86_REG_ESP,sp)
    calls=[]
    def read32(address): return struct.unpack('<I',uc.mem_read(address,4))[0]
    def hook(machine,address,size,unused):
        if address != 0x4B6BC0:return
        esp=machine.reg_read(UC_X86_REG_ESP)
        ret,store,out,name,maximum,minimum=struct.unpack('<6I',machine.mem_read(esp,24))
        i=len(calls)
        assert store==regs[UC_X86_REG_EDI]
        assert out==slots+4*i
        assert maximum==0x42C80000 and minimum==0
        text=bytes(machine.mem_read(name,40)).split(b'\0')[0]
        assert text==f'raid_probe_health{i}'.encode()
        allocated=0 if i==failed_slot else heap+32*i
        machine.mem_write(out,struct.pack('<I',allocated))
        machine.reg_write(UC_X86_REG_EAX,0xDEAD)
        machine.reg_write(UC_X86_REG_ECX,0xDEAD)
        machine.reg_write(UC_X86_REG_EDX,0xDEAD)
        machine.reg_write(UC_X86_REG_ESP,esp+24)
        machine.reg_write(UC_X86_REG_EIP,ret)
        calls.append(allocated)
    uc.hook_add(UC_HOOK_CODE,hook)
    uc.emu_start(start,probe.HOOK+8,count=10000)
    assert len(calls)==40
    for reg,value in regs.items():assert uc.reg_read(reg)==value,(reg,value)
    assert uc.reg_read(UC_X86_REG_ESP)==sp-8
    assert read32(sp-8)==regs[UC_X86_REG_ECX] and read32(sp-4)==0
    for i,address in enumerate(calls):
        if not address:continue
        assert struct.unpack('<f',uc.mem_read(address+16,4))[0]==20+i*2
        assert uc.mem_read(address+4,1)==b'\x01'
    print('PASS: 40 independent adapters, registers/stack preserved, null slot',failed_slot)
root=ET.fromstring(probe.window())
assert len(root.findall('.//StatusBarDef'))==40
assert len({x.text for x in root.findall('.//AdapterName')})==40
assert root.find('.//Width').text=='620'
print('PASS: XML contains forty unique raid adapters in a compact native window.')

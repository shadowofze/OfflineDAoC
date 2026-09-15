import pefile, capstone, collections
p=pefile.PE(r'C:\Users\thedo\Desktop\Offline DAoC\runtime\client-opendaoc\app\game.dll')
d=p.__data__; base=p.OPTIONAL_HEADER.ImageBase
md=capstone.Cs(capstone.CS_ARCH_X86,capstone.CS_MODE_32)
md.skipdata=True
s=next(s for s in p.sections if s.Name.startswith(b'.text'))
recent=collections.deque(maxlen=8)
for i in md.disasm(s.get_data(),base+s.VirtualAddress):
    recent.append(f'{i.address:x} {i.mnemonic} {i.op_str}')
    if i.mnemonic=='call' and i.op_str=='0x40ab77' and any('push 0x20' in line for line in recent):
        print('\n'.join(recent))
        off=p.get_offset_from_rva(i.address-base)
        print('\nSITE',hex(i.address))
        for j in md.disasm(d[off:off+45],i.address): print(hex(j.address),j.mnemonic,j.op_str)

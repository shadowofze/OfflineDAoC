"""Read-only inspection of the installed client's keyboard action mapping."""
import struct
import pefile
import capstone

p = pefile.PE(r'C:\Users\thedo\Desktop\Offline DAoC\runtime\client-opendaoc\app\game.dll')
d = p.__data__
md = capstone.Cs(capstone.CS_ARCH_X86, capstone.CS_MODE_32)
sec = next(s for s in p.sections if s.Name.startswith(b'.text'))
lo, hi = sec.PointerToRawData, sec.PointerToRawData + sec.SizeOfRawData
base = p.OPTIONAL_HEADER.ImageBase
for label in [b'Debug=%d']:
    off = d.find(label)
    va = base + p.get_rva_from_offset(off)
    print(label, hex(va))
    needle = struct.pack('<I', va)
    pos = d.find(needle)
    while pos >= 0:
        print('REF', hex(base + p.get_rva_from_offset(pos)))
        if lo <= pos < hi:
            start = max(lo, pos - 40)
            for i in md.disasm(d[start:pos+40], base + p.get_rva_from_offset(start)):
                print(hex(i.address), i.mnemonic, i.op_str)
        else:
            print(d[max(0,pos-24):pos+24].hex(' '))
        pos = d.find(needle, pos+4)

import pefile,struct,capstone
p=pefile.PE(r'C:\Users\thedo\Desktop\Offline DAoC\runtime\client-opendaoc\app\game.dll')
d=p.__data__; base=p.OPTIONAL_HEADER.ImageBase
s=next(s for s in p.sections if s.Name.startswith(b'.text'))
low=base+s.VirtualAddress; high=low+s.Misc_VirtualSize
m=capstone.Cs(capstone.CS_ARCH_X86,capstone.CS_MODE_32)
for sec in p.sections:
 if sec.Name.startswith(b'.text'): continue
 start=None; run=[]
 for off in range(sec.PointerToRawData,sec.PointerToRawData+sec.SizeOfRawData-4,4):
  value=struct.unpack_from('<I',d,off)[0]
  if low <= value < high:
   if start is None:start=off
   run.append(value)
  else:
   if len(run)>=64:
    print('TABLE',hex(base+p.get_rva_from_offset(start)),len(run),'entry33',hex(run[33]))
   start=None;run=[]

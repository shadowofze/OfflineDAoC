"""Add opt-in native observer flight handling to this exact offline client.

Debug packet 0x21 byte 0 remains GM permission. Reserved byte 1:
0 unchanged legacy behavior, 1 enter flight, 2 leave flight.
The native flight flag is the same flag set by the client's GM keyboard action.
"""
from pathlib import Path
import struct
import pefile

path = Path(r'C:\Users\thedo\Desktop\Offline DAoC\runtime\client-opendaoc\app\game.dll')
data = bytearray(path.read_bytes())
pe = pefile.PE(data=data)
base = pe.OPTIONAL_HEADER.ImageBase
assert base == 0x400000
assert not any(s.Name.rstrip(b'\0') == b'.ofly' for s in pe.sections), 'Already patched'
hook = 0x411209
offset = pe.get_offset_from_rva(hook-base)
assert data[offset:offset+5] == bytes.fromhex('b8 9a 00 00 00'), 'Unexpected client handler'
before = pe.get_offset_from_rva(0x411201-base)
assert data[before:before+8] == bytes.fromhex('0f be 03 a3 a0 6f f9 00'), 'Unexpected debug permission handler'
align = lambda n,a: (n+a-1)//a*a
last = pe.sections[-1]
rva = align(last.VirtualAddress+max(last.Misc_VirtualSize,last.SizeOfRawData),pe.OPTIONAL_HEADER.SectionAlignment)
raw = align(len(data),pe.OPTIONAL_HEADER.FileAlignment)
section_header = pe.sections[0].get_file_offset()+40*pe.FILE_HEADER.NumberOfSections
assert section_header+40 <= min(s.PointerToRawData for s in pe.sections if s.PointerToRawData), 'No spare section header'
assert not any(data[section_header:section_header+40]), 'Section header space occupied'
# Preserve registers and flags. Only observer-tagged packets touch the flight flag.
# The original displaced mov eax,0x9a is always replayed before returning.
code = bytes.fromhex(
    '9c 83 3d a0 6f f9 00 00 74 10 '
    '80 7b 01 01 75 0a c7 05 90 98 04 01 01 00 00 00 '
    '80 7b 01 02 75 0a c7 05 90 98 04 01 00 00 00 00 '
    '9d b8 9a 00 00 00 c3')
size = align(len(code),pe.OPTIONAL_HEADER.FileAlignment)
data.extend(b'\0'*(raw+size-len(data)))
data[raw:raw+len(code)] = code
data[section_header:section_header+40] = struct.pack('<8sIIIIIIHHI',b'.ofly\0\0\0',len(code),rva,size,raw,0,0,0,0,0x60000020)
struct.pack_into('<H',data,pe.FILE_HEADER.get_field_absolute_offset('NumberOfSections'),pe.FILE_HEADER.NumberOfSections+1)
struct.pack_into('<I',data,pe.OPTIONAL_HEADER.get_field_absolute_offset('SizeOfImage'),align(rva+len(code),pe.OPTIONAL_HEADER.SectionAlignment))
struct.pack_into('<I',data,pe.OPTIONAL_HEADER.get_field_absolute_offset('SizeOfCode'),pe.OPTIONAL_HEADER.SizeOfCode+size)
data[offset:offset+5] = b'\xe8'+struct.pack('<i',base+rva-(hook+5))
patched = pefile.PE(data=data)
struct.pack_into('<I',data,pe.OPTIONAL_HEADER.get_field_absolute_offset('CheckSum'),patched.generate_checksum())
path.write_bytes(data)
print('Installed native observer flight packet handling; existing client patches preserved.')

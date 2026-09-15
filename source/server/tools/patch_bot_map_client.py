"""Offline DAoC 1.127: correct cropped-map zone lookup for bot markers only.

The stock relic-marker lookup tests (world - zone origin) against cropped map
width/height, but omits the crop origin. The renderer DOES subtract that origin.
Consequently a valid interior marker resolves to zone -1 and stays at the UI
origin. Preserve the packet and renderer (including floor/Z selection); correct
the bounds lookup only for the server's high-bit autonomous-bot marker IDs.

Requires keystone-engine to assemble; fails closed on another client build.
Writes a NEW output file. Pass that verified file to deployment separately.
"""
import argparse
from pathlib import Path
import struct
from keystone import Ks, KS_ARCH_X86, KS_MODE_32

HOOK = 0x530BC8
ORIGINAL_LOOKUP = 0x4F878A
IMAGE_BASE = 0x400000


def u32(data, offset):
    return struct.unpack_from('<I', data, offset)[0]


def aligned(value, alignment):
    return (value + alignment - 1) // alignment * alignment


def build_patch(original):
    data = bytearray(original)
    pe = u32(data, 0x3C)
    assert data[pe:pe + 4] == b'PE\0\0', 'Not a PE file'
    assert struct.unpack_from('<H', data, pe + 4)[0] == 0x14C, 'Not x86'
    count = struct.unpack_from('<H', data, pe + 6)[0]
    opt = pe + 24
    assert u32(data, opt + 28) == IMAGE_BASE, 'Unexpected image base'
    table = opt + struct.unpack_from('<H', data, pe + 20)[0]
    sections = [struct.unpack_from('<8sIIIIIIHHI', data, table + i * 40) for i in range(count)]
    assert not any(s[0].rstrip(b'\0') == b'.botmap' for s in sections), 'Already patched'

    def offset(address):
        rva = address - IMAGE_BASE
        for s in sections:
            if s[2] <= rva < s[2] + s[3]:
                return s[4] + rva - s[2]
        raise ValueError('Address outside file')

    hook = offset(HOOK)
    expected = b'\xe8' + struct.pack('<i', ORIGINAL_LOOKUP - HOOK - 5)
    assert data[hook:hook + 5] == expected, 'Unexpected marker call; refusing patch'
    # The caller's frame must contain the marker ID at EBP+8.
    assert data[offset(0x530B77):offset(0x530B77) + 6] == bytes.fromhex('55 8b ec 8b 45 0c')
    assert data[offset(ORIGINAL_LOOKUP):offset(ORIGINAL_LOOKUP) + 10] == bytes.fromhex('53 56 8b f0 33 db 57 83 c6 24')
    section_alignment, file_alignment = struct.unpack_from('<II', data, opt + 32)
    rva = aligned(max(s[2] + max(s[1], s[3]) for s in sections), section_alignment)
    address = IMAGE_BASE + rva
    asm = '''
        test dword ptr [ebp+8], 0x80000000
        jz 0x4f878a
        push ebx
        push esi
        mov esi, eax
        xor ebx, ebx
        push edi
        add esi, 0x24
    next_zone:
        mov eax, [esi-0x1c]
        cmp eax, [esp+0x10]
        jne skip_zone
        fild dword ptr [esi]
        fiadd dword ptr [esi-8]
        fsubr dword ptr [esp+0x14]
        call 0x77734c
        fild dword ptr [esi+4]
        fiadd dword ptr [esi-4]
        mov edi, eax
        fsubr dword ptr [esp+0x18]
        call 0x77734c
        test edi, edi
        jl skip_zone
        cmp edi, [esi-0x10]
        jge skip_zone
        test eax, eax
        jl skip_zone
        cmp eax, [esi-0xc]
        jl found_zone
    skip_zone:
        inc ebx
        add esi, 0x68
        cmp ebx, 0x200
        jl next_zone
        or eax, -1
        jmp done
    found_zone:
        mov eax, ebx
    done:
        pop edi
        pop esi
        pop ebx
        ret 0xc
    '''
    code = bytes(Ks(KS_ARCH_X86, KS_MODE_32).asm(asm, addr=address)[0])
    new_header = table + count * 40
    assert new_header + 40 <= min(s[4] for s in sections if s[4]), 'No PE header room'
    assert not any(data[new_header:new_header + 40]), 'Nonempty PE header slack'
    raw = aligned(len(data), file_alignment)
    raw_size = aligned(len(code), file_alignment)
    data.extend(bytes(raw + raw_size - len(data)))
    data[raw:raw + len(code)] = code
    struct.pack_into('<8sIIIIIIHHI', data, new_header,
                     b'.botmap\0', len(code), rva, raw_size, raw, 0, 0, 0, 0, 0x60000020)
    struct.pack_into('<H', data, pe + 6, count + 1)
    struct.pack_into('<I', data, opt + 4, u32(data, opt + 4) + raw_size)
    struct.pack_into('<I', data, opt + 56, aligned(rva + len(code), section_alignment))
    struct.pack_into('<I', data, opt + 64, 0)  # Recalculate PE checksum below.
    data[hook:hook + 5] = b'\xe8' + struct.pack('<i', address - HOOK - 5)
    checksum = 0
    for i in range(0, len(data), 2):
        checksum += int.from_bytes(data[i:i + 2], 'little')
        checksum = (checksum & 0xffff) + (checksum >> 16)
    checksum = (checksum & 0xffff) + (checksum >> 16)
    struct.pack_into('<I', data, opt + 64, checksum + len(data))
    return bytes(data), address


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('source', type=Path)
    parser.add_argument('output', type=Path)
    args = parser.parse_args()
    assert args.source.resolve() != args.output.resolve(), 'Output must be a separate file'
    assert not args.output.exists(), 'Output already exists'
    patched, address = build_patch(args.source.read_bytes())
    args.output.write_bytes(patched)
    print(f'Patched bot-marker lookup at {address:#x}; output {args.output}')

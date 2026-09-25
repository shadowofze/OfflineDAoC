"""Prepare an optional red bounty marker for the isolated DAoC 1.127 client.

This writes a new file and never replaces game.dll. The existing map renderer
uses group_location (green) for server-only marker IDs. Only 0xFFFE0001 is
redirected to the existing quest_waypoint (red dot) template; every other ID keeps
the old path. Review and test the output before any separate deployment.
"""

import argparse
import hashlib
from pathlib import Path
import struct

IMAGE_BASE = 0x400000
HOOK = 0x530D71
CONTINUE = 0x530D76
GREEN_TEMPLATE = 0x953570
RED_TEMPLATE = 0x953580
BOUNTY_MARKER_ID = 0xFFFE0001
WAYPOINT_TEMPLATE = b'quest_waypoint\0'
# The preserved v0.3 client, also used as the v0.31 seed. Never apply this
# native hook to the optional-class DLL or a different client build.
EXPECTED_SHA256 = '67dcf68a37b95a93946a943b99d5e19b4a03e08cd6469275e25c7b909de21e99'


def u32(data, offset):
    return struct.unpack_from('<I', data, offset)[0]


def aligned(value, alignment):
    return (value + alignment - 1) // alignment * alignment


def build_patch(original):
    actual_sha256 = hashlib.sha256(original).hexdigest()
    if actual_sha256 != EXPECTED_SHA256:
        raise ValueError(
            f'Unsupported client DLL SHA-256 {actual_sha256}; expected {EXPECTED_SHA256}'
        )
    data = bytearray(original)
    pe = u32(data, 0x3C)
    assert data[pe:pe + 4] == b'PE\0\0', 'Not a PE file'
    assert struct.unpack_from('<H', data, pe + 4)[0] == 0x14C, 'Not x86'
    count = struct.unpack_from('<H', data, pe + 6)[0]
    opt = pe + 24
    assert u32(data, opt + 28) == IMAGE_BASE, 'Unexpected image base'
    table = opt + struct.unpack_from('<H', data, pe + 20)[0]
    sections = [struct.unpack_from('<8sIIIIIIHHI', data, table + i * 40)
                for i in range(count)]
    names = {section[0].rstrip(b'\0') for section in sections}
    assert b'.botmap' in names, 'Dungeon map correction is missing'
    assert b'.bounty' not in names, 'Already patched'

    def offset(address):
        rva = address - IMAGE_BASE
        for section in sections:
            if section[2] <= rva < section[2] + section[3]:
                return section[4] + rva - section[2]
        raise ValueError(f'Address {address:#x} outside file')

    assert data[offset(HOOK):offset(HOOK) + 5] == b'\x68' + struct.pack('<I', GREEN_TEMPLATE), \
        'Unexpected map marker fallback; refusing patch'
    assert data[offset(GREEN_TEMPLATE):offset(GREEN_TEMPLATE) + 15] == b'group_location\0', \
        'Unexpected green template'
    assert data[offset(RED_TEMPLATE):offset(RED_TEMPLATE) + 13] == b'mino_relic_3\0', \
        'Unexpected red template'
    # The marker-rendering function saves the incoming ID in EBX before this
    # fallback; the surrounding original byte signatures pin that contract.
    assert data[offset(0x530C5B):offset(0x530C5B) + 3] == bytes.fromhex('8b 75 08')
    assert data[offset(0x530C5E):offset(0x530C5E) + 3] == bytes.fromhex('8b 5d 0c')
    assert data[offset(0x530D39):offset(0x530D39) + 7] == bytes.fromhex('56 53 8b f0 e8 51 00')

    section_alignment, file_alignment = struct.unpack_from('<II', data, opt + 32)
    rva = aligned(max(s[2] + max(s[1], s[3]) for s in sections), section_alignment)
    address = IMAGE_BASE + rva
    # cmp ebx, bounty ID; jne default; push red-dot template; jmp continuation;
    # default: push original green; jmp continuation.
    code = (b'\x81\xfb' + struct.pack('<I', BOUNTY_MARKER_ID) +
            b'\x75\x0a' +
            b'\x68' + struct.pack('<I', address + 28) +
            b'\xe9' + struct.pack('<i', CONTINUE - (address + 18)) +
            b'\x68' + struct.pack('<I', GREEN_TEMPLATE) +
            b'\xe9' + struct.pack('<i', CONTINUE - (address + 28)) +
            WAYPOINT_TEMPLATE)
    assert len(code) == 28 + len(WAYPOINT_TEMPLATE)

    new_header = table + count * 40
    assert new_header + 40 <= min(s[4] for s in sections if s[4]), 'No PE header room'
    assert not any(data[new_header:new_header + 40]), 'Nonempty PE header slack'
    raw = aligned(len(data), file_alignment)
    raw_size = aligned(len(code), file_alignment)
    data.extend(bytes(raw + raw_size - len(data)))
    data[raw:raw + len(code)] = code
    struct.pack_into('<8sIIIIIIHHI', data, new_header,
                     b'.bounty\0', len(code), rva, raw_size, raw, 0, 0, 0, 0, 0x60000020)
    struct.pack_into('<H', data, pe + 6, count + 1)
    struct.pack_into('<I', data, opt + 4, u32(data, opt + 4) + raw_size)
    struct.pack_into('<I', data, opt + 56, aligned(rva + len(code), section_alignment))
    struct.pack_into('<I', data, opt + 64, 0)
    data[offset(HOOK):offset(HOOK) + 5] = b'\xe9' + struct.pack('<i', address - CONTINUE)
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
    patched, hook_target = build_patch(args.source.read_bytes())
    args.output.write_bytes(patched)
    print(f'Prepared optional bounty marker hook at {hook_target:#x}: {args.output}')

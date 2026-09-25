"""Read-only static validation of the optional DAoC 1.127 bounty map hook."""

import argparse
from pathlib import Path
import struct

from patch_bounty_map_client import (
    BOUNTY_MARKER_ID, CONTINUE, GREEN_TEMPLATE, HOOK, IMAGE_BASE,
    RED_TEMPLATE, WAYPOINT_TEMPLATE, build_patch, u32,
)


def offset(data, address):
    pe = u32(data, 0x3C)
    count = struct.unpack_from('<H', data, pe + 6)[0]
    table = pe + 24 + struct.unpack_from('<H', data, pe + 20)[0]
    for i in range(count):
        section = struct.unpack_from('<8sIIIIIIHHI', data, table + i * 40)
        rva = address - IMAGE_BASE
        if section[2] <= rva < section[2] + section[3]:
            return section[4] + rva - section[2]
    raise ValueError(f'Address {address:#x} outside file')


def validate(source):
    original = source.read_bytes()
    candidate, cave = build_patch(original)
    code = candidate[offset(candidate, cave):offset(candidate, cave) + 28 + len(WAYPOINT_TEMPLATE)]

    assert code[:2] == b'\x81\xfb'  # cmp ebx, imm32
    assert u32(code, 2) == BOUNTY_MARKER_ID
    assert code[6:8] == b'\x75\x0a'  # only non-bounty falls through to green
    assert code[8] == code[18] == 0x68  # push template address
    assert u32(code, 9) == cave + 28
    assert u32(code, 19) == GREEN_TEMPLATE
    assert code[28:] == WAYPOINT_TEMPLATE
    assert code[13] == code[23] == 0xE9
    assert cave + 18 + struct.unpack_from('<i', code, 14)[0] == CONTINUE
    assert cave + 28 + struct.unpack_from('<i', code, 24)[0] == CONTINUE
    assert candidate[offset(candidate, HOOK)] == 0xE9
    assert HOOK + 5 + struct.unpack_from('<i', candidate, offset(candidate, HOOK) + 1)[0] == cave

    # The renderer before its fallback, the normal relic templates, and the
    # client's already-deployed map-crop hook remain byte-for-byte unchanged.
    for start, end in ((0x530B77, HOOK), (CONTINUE, 0x530D93),
                       (GREEN_TEMPLATE, RED_TEMPLATE + 13), (0x530BC8, 0x530BCD)):
        a, b = offset(original, start), offset(original, end - 1) + 1
        assert candidate[a:b] == original[a:b], (hex(start), hex(end))
    print(f'PASS: bounty {BOUNTY_MARKER_ID:#x} selects red; all other fallback IDs select green.')
    print(f'PASS: both branches resume at {CONTINUE:#x}; existing renderer and bot-map hook are unchanged.')
    print(f'Candidate size: {len(candidate)} bytes; source size: {len(original)} bytes. No file was written.')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('source', type=Path)
    args = parser.parse_args()
    validate(args.source)

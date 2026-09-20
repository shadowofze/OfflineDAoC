"""Restrict the Sluaghbinder race buttons in the isolated client.

The Hibernian Sluaghbinder reuses the native Mauler character-creation slot
(client class id 0x3e).  The custom client registers four race links for that
slot: Celt (9), Lurikeen (0xc), Minotaur (0x15), and Firbolg (0xa).  The
server already accepts only Celt and Firbolg.  This small, build-identified
patch removes the two stale native links so the character-creation page and
the server agree.

This script always writes a separate output file and refuses an unexpected
client build.  It does not touch the main Offline DAoC installation.
"""

from __future__ import annotations

import argparse
import hashlib
import struct
from pathlib import Path


IMAGE_BASE = 0x400000
SLUAGHBINDER_LURIKEEN_CALL = 0x5B3ADC
SLUAGHBINDER_MINOTAUR_CALL = 0x5B3AEE


def _pe_offset(data: bytes, address: int) -> int:
    pe = struct.unpack_from("<I", data, 0x3C)[0]
    assert data[pe : pe + 4] == b"PE\0\0", "not a PE image"
    machine = struct.unpack_from("<H", data, pe + 4)[0]
    assert machine == 0x14C, f"expected x86 client, got machine {machine:#x}"
    sections = struct.unpack_from("<H", data, pe + 6)[0]
    optional = pe + 24
    assert struct.unpack_from("<I", data, optional + 28)[0] == IMAGE_BASE
    table = optional + struct.unpack_from("<H", data, pe + 20)[0]
    rva = address - IMAGE_BASE
    for index in range(sections):
        name, _virtual_size, virtual_address, raw_size, raw_offset, *_ = struct.unpack_from(
            "<8sIIIIIIHHI", data, table + index * 40
        )
        if virtual_address <= rva < virtual_address + max(_virtual_size, raw_size):
            return raw_offset + rva - virtual_address
    raise AssertionError(f"address {address:#x} is outside image sections")


def _call_bytes(address: int, target: int) -> bytes:
    return b"\xE8" + struct.pack("<i", target - address - 5)


def patch_bytes(original: bytes) -> bytes:
    data = bytearray(original)
    # Both call sites are in the Sluaghbinder constructor immediately after
    # loading the Lurikeen and Minotaur race IDs.  Keep the constructor layout
    # intact and suppress only those two vector insertions.
    expected_target = 0x5B4A65
    sites = (SLUAGHBINDER_LURIKEEN_CALL, SLUAGHBINDER_MINOTAUR_CALL)
    for site in sites:
        offset = _pe_offset(data, site)
        expected = _call_bytes(site, expected_target)
        assert bytes(data[offset : offset + 5]) == expected, (
            f"unexpected bytes at {site:#x}; refusing to patch this client build"
        )
    for site in sites:
        offset = _pe_offset(data, site)
        # The original call consumes the argument pushed immediately before
        # it.  A plain NOP would leave that argument on the stack and corrupt
        # the following class-constructor call.  Pop it while preserving the
        # original five-byte instruction footprint.
        data[offset : offset + 5] = b"\x83\xC4\x04\x90\x90"
    # PE checksum is informational for this client, but keep it correct for
    # launchers that validate it.
    pe = struct.unpack_from("<I", data, 0x3C)[0]
    optional = pe + 24
    checksum_offset = optional + 64
    struct.pack_into("<I", data, checksum_offset, 0)
    checksum = 0
    for offset in range(0, len(data), 2):
        word = int.from_bytes(data[offset : offset + 2], "little")
        checksum = (checksum + word) & 0xFFFFFFFF
        checksum = (checksum & 0xFFFF) + (checksum >> 16)
    checksum = (checksum & 0xFFFF) + (checksum >> 16)
    struct.pack_into("<I", data, checksum_offset, checksum + len(data))
    return bytes(data)


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("source", type=Path)
    parser.add_argument("output", type=Path)
    args = parser.parse_args()
    source = args.source.resolve()
    output = args.output.resolve()
    assert source != output, "output must be separate from source"
    assert source.exists(), source
    assert not output.exists(), f"refusing to overwrite {output}"
    original = source.read_bytes()
    patched = patch_bytes(original)
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_bytes(patched)
    print(f"source_sha256={hashlib.sha256(original).hexdigest()}")
    print(f"patched_sha256={hashlib.sha256(patched).hexdigest()}")
    print("removed Sluaghbinder client race links: Lurikeen (0xc), Minotaur (0x15)")


if __name__ == "__main__":
    main()

"""Patch the isolated client's Hibernian class-label resource.

The experimental client uses the native Hibernian Mauler packet slot for the
server-side Sluaghbinder class.  Its class-name resource is loaded by one
specific initializer.  Redirecting that one load to the already embedded
Sluaghbinder string fixes the character-select label without changing the
native Midgard or Albion class resources, packet layout, or class IDs.

This is deliberately build-identified.  It writes a new file and refuses to
overwrite an input or an existing output.
"""

from __future__ import annotations

import argparse
import hashlib
import struct
from pathlib import Path


IMAGE_BASE = 0x400000
LABEL_LOAD_ADDRESS = 0x44F3D1
OLD_STRING_ADDRESS = 0x00940ABC
SLUAGHBINDER_STRING_ADDRESS = 0x00922B8B


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
        _name, virtual_size, virtual_address, raw_size, raw_offset, *_ = struct.unpack_from(
            "<8sIIIIIIHHI", data, table + index * 40
        )
        if virtual_address <= rva < virtual_address + max(virtual_size, raw_size):
            return raw_offset + rva - virtual_address
    raise AssertionError(f"address {address:#x} is outside image sections")


def _write_checksum(data: bytearray) -> None:
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


def patch_bytes(original: bytes) -> bytes:
    data = bytearray(original)
    offset = _pe_offset(data, LABEL_LOAD_ADDRESS)
    expected = b"\xBE" + struct.pack("<I", OLD_STRING_ADDRESS)
    actual = bytes(data[offset : offset + len(expected)])
    assert actual == expected, (
        f"unexpected class-label initializer at {LABEL_LOAD_ADDRESS:#x}: "
        f"{actual.hex()} (expected {expected.hex()})"
    )
    data[offset + 1 : offset + 5] = struct.pack("<I", SLUAGHBINDER_STRING_ADDRESS)
    assert b"Sluaghbinder\0" in data, "embedded Sluaghbinder label is missing"
    _write_checksum(data)
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
    print("redirected only the Hibernian Sluaghbinder class-label load")


if __name__ == "__main__":
    main()

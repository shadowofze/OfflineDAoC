"""DAoC MPAK v2 codec. Both compressed and expanded-memory offsets are validated."""
import struct
import zlib
from dataclasses import dataclass

MAX_EXPANDED = 128 * 1024 * 1024

def require(ok, message):
    if not ok:
        raise ValueError(message)

def inflate(data, limit=MAX_EXPANDED):
    decoder = zlib.decompressobj()
    result = decoder.decompress(data, limit + 1)
    require(len(result) <= limit and decoder.eof and not decoder.unused_data,
            'Invalid, oversized or truncated compressed stream')
    return result

@dataclass
class Entry:
    name: str
    data: bytes
    timestamp: int = 0
    flags: int = 4

def read(blob):
    require(blob[:5] == b'MPAK\x02' and len(blob) >= 21, 'Not an MPAK v2 archive')
    checksum, ds, ns, count = struct.unpack('<4I', bytes(v ^ i for i, v in enumerate(blob[5:21])))
    require(0 < count <= 100000 and ds > 0 and ns > 0 and 21 + ns + ds <= len(blob), 'Invalid MPK header')
    directory = blob[21+ns:21+ns+ds]
    require(zlib.crc32(directory) & 0xffffffff == checksum, 'MPK directory CRC mismatch')
    name = inflate(blob[21:21+ns], 4096)
    rows = inflate(directory)
    require(len(rows) == count * 284, 'MPK directory size mismatch')
    payload = blob[21+ns+ds:]
    entries, names = [], set()
    expanded_offset = compressed_offset = 0
    for i in range(count):
        row = rows[i*284:(i+1)*284]
        filename = row[:256].split(b'\0')[0].decode('latin1')
        require(filename and filename.lower() not in names, 'Duplicate/empty MPK filename')
        names.add(filename.lower())
        stamp, flags, memory, size, offset, length, crc = struct.unpack('<7I', row[256:])
        require(memory == expanded_offset, f'{filename}: invalid expanded-memory offset {memory}, expected {expanded_offset}')
        require(offset == compressed_offset and offset+length <= len(payload), f'{filename}: invalid compressed offset')
        compressed = payload[offset:offset+length]
        require(zlib.crc32(compressed) & 0xffffffff == crc, f'{filename}: compressed CRC mismatch')
        data = inflate(compressed)
        require(len(data) == size, f'{filename}: expanded size mismatch')
        entries.append(Entry(filename, data, stamp, flags))
        expanded_offset += size
        compressed_offset += length
        require(expanded_offset <= MAX_EXPANDED, 'Archive exceeds safe expanded-size limit')
    require(compressed_offset == len(payload), 'Unexpected trailing MPK payload')
    return name, entries

def write(name, entries):
    directory, payload = bytearray(), bytearray()
    memory = 0
    for entry in entries:
        filename = entry.name.encode('latin1')
        require(0 < len(filename) < 256 and b'\0' not in filename, 'Invalid MPK filename')
        compressed = zlib.compress(entry.data, 9)
        directory += filename.ljust(256, b'\0')
        directory += struct.pack('<7I', entry.timestamp, entry.flags, memory, len(entry.data),
                                 len(payload), len(compressed), zlib.crc32(compressed) & 0xffffffff)
        memory += len(entry.data)
        payload += compressed
    d, n = zlib.compress(directory, 9), zlib.compress(name, 9)
    header = struct.pack('<4I', zlib.crc32(d) & 0xffffffff, len(d), len(n), len(entries))
    result = b'MPAK\x02' + bytes(v ^ i for i, v in enumerate(header)) + n + d + payload
    actual_name, actual_entries = read(result)
    require(actual_name == name and actual_entries == entries, 'MPK round-trip verification failed')
    return result

def verify_memory_image(blob):
    """Separate check: reconstruct expanded image, then access each file by its memory pointer."""
    _, ds, ns, count = struct.unpack('<4I', bytes(v ^ i for i, v in enumerate(blob[5:21])))
    directory = zlib.decompress(blob[21+ns:21+ns+ds])
    stream = blob[21+ns+ds:]
    records, data = [], []
    for i in range(count):
        fields = struct.unpack_from('<7I', directory, i*284+256)
        _, _, pointer, size, offset, length, _ = fields
        content = zlib.decompress(stream[offset:offset+length])
        records.append((pointer, size, content))
        data.append(content)
    image = b''.join(data)
    for pointer, size, content in records:
        require(image[pointer:pointer+size] == content, 'Expanded-memory lookup differs from compressed-file lookup')
    return len(records)

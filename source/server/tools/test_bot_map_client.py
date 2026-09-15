"""Execute the original and corrected x86 lookup in Unicorn (no game needed).

Uses the installed client's real regions.dat crop data, not hard-coded offsets.
Requires unicorn and keystone-engine. Run with the client app directory.
"""
import configparser
from pathlib import Path
import struct
import sys
from unicorn import Uc, UC_ARCH_X86, UC_MODE_32, UC_HOOK_CODE
from unicorn.x86_const import *
from patch_bot_map_client import build_patch, u32, ORIGINAL_LOOKUP, IMAGE_BASE

app = Path(sys.argv[1])
original = (app / 'game.dll').read_bytes()
source_pe = u32(original, 0x3c)
source_table = source_pe + 24 + struct.unpack_from('<H', original, source_pe + 20)[0]
existing_patch = next((source_table + i * 40
    for i in range(struct.unpack_from('<H', original, source_pe + 6)[0])
    if original[source_table + i * 40:source_table + i * 40 + 8].rstrip(b'\0') == b'.botmap'), None)
if existing_patch is None:
    patched, entry = build_patch(original)
else:
    patched, entry = original, IMAGE_BASE + u32(original, existing_patch + 12)
    assert original[0x130bc8] == 0xe8
    assert 0x530bcd + struct.unpack_from('<i', original, 0x130bc9)[0] == entry
pe = u32(patched, 0x3c)
opt = pe + 24
table = opt + struct.unpack_from('<H', patched, pe + 20)[0]
uc = Uc(UC_ARCH_X86, UC_MODE_32)
uc.mem_map(IMAGE_BASE, u32(patched, opt + 56))
for i in range(struct.unpack_from('<H', patched, pe + 6)[0]):
    s = struct.unpack_from('<8sIIIIIIHHI', patched, table + i * 40)
    if s[3]:
        uc.mem_write(IMAGE_BASE + s[2], patched[s[4]:s[4] + s[3]])
STACK, MANAGER, FRAME, STOP = 0x5000000, 0x5100000, 0x5200000, 0x5300000
for base in (STACK, MANAGER, FRAME, STOP):
    uc.mem_map(base, 0x20000)
uc.mem_write(STOP, b'\x90')


def lookup(address, marker, region, zone, ox, oy, cropx, cropy, width, height, x, y):
    uc.mem_write(MANAGER, bytes(0x20000))
    record = MANAGER + 4 + zone * 0x68
    uc.mem_write(record + 4, struct.pack('<I', region))
    uc.mem_write(record + 0x10, struct.pack('<6i', width, height, cropx, cropy, ox, oy))
    stack = STACK + 0x10000
    uc.mem_write(stack, struct.pack('<IIff', STOP, region, x, y))
    uc.mem_write(FRAME + 8, struct.pack('<I', marker))
    for register, value in ((UC_X86_REG_ESP, stack), (UC_X86_REG_EBP, FRAME),
                            (UC_X86_REG_EAX, MANAGER), (UC_X86_REG_EBX, 0x12345678),
                            (UC_X86_REG_ESI, 0x23456789), (UC_X86_REG_EDI, 0x3456789a),
                            (UC_X86_REG_FPCW, 0x37f), (UC_X86_REG_FPSW, 0),
                            (UC_X86_REG_FPTAG, 0xffff)):
        uc.reg_write(register, value)
    uc.emu_start(address, STOP, count=100000)
    assert uc.reg_read(UC_X86_REG_EIP) == STOP
    assert uc.reg_read(UC_X86_REG_ESP) == stack + 16, 'Callee stack cleanup changed'
    assert uc.reg_read(UC_X86_REG_EBP) == FRAME
    assert uc.reg_read(UC_X86_REG_EBX) == 0x12345678
    assert uc.reg_read(UC_X86_REG_ESI) == 0x23456789
    assert uc.reg_read(UC_X86_REG_EDI) == 0x3456789a
    return uc.reg_read(UC_X86_REG_EAX)


# Actual Nisse example from the server. Old lookup returns -1 (UI-origin pip).
args = (129, 129, 8192, 8192, 17244, 19507, 14282, 14282, 33486, 32889)
assert lookup(ORIGINAL_LOOKUP, 0x80000001, *args) == 0xffffffff
assert lookup(entry, 0x80000001, *args) == 129
assert lookup(entry, 1, *args) == 0xffffffff, 'Real relic behavior changed'

maps = configparser.ConfigParser(strict=False, interpolation=None, inline_comment_prefixes=(';',))
maps.read_string('\n'.join(line.split(';', 1)[0] for line in
    (app / 'ui/maps/regions.dat').read_text(encoding='cp1252').splitlines()))
cases = corrected = 0
for section in maps.values():
    if 'zone_count' not in section:
        continue
    region = int(section.name.removeprefix('region'))
    for i in range(section.getint('zone_count')):
        prefix = f'zone{i}_'
        if prefix + 'number' not in section:
            continue
        zone = section.getint(prefix + 'number')
        if zone >= 512:
            continue  # The installed client's lookup has 512 entries.
        cx, cy = section.getint(prefix + 'offsetx', 0), section.getint(prefix + 'offsety', 0)
        width, height = section.getint(prefix + 'width', 65536), section.getint(prefix + 'height', 65536)
        # Exercise nonzero physical zone origins too. Map crop is zone-local.
        ox, oy = 8192, 16384
        for lx, ly in ((cx, cy), (cx + width // 2, cy + height // 2),
                       (cx + width - 1, cy + height - 1)):
            args = (region, zone, ox, oy, cx, cy, width, height, ox + lx, oy + ly)
            old = lookup(ORIGINAL_LOOKUP, 1, *args)
            assert lookup(entry, 1, *args) == old, 'Ordinary relic lookup changed'
            assert lookup(entry, 0x80000001, *args) == zone, (zone, lx, ly)
            corrected += old != zone
            cases += 1
        # Invalid coordinates stay invalid; no unconditional forced zone.
        for lx, ly in ((cx - 1, cy), (cx, cy - 1), (cx + width, cy), (cx, cy + height)):
            args = (region, zone, ox, oy, cx, cy, width, height, ox + lx, oy + ly)
            assert lookup(entry, 0x80000001, *args) == 0xffffffff
            cases += 1

# Hook and new section only: the normal projection/floor routines are identical.
assert patched[0x130328:0x1304dc] == original[0x130328:0x1304dc]
assert patched[0x130a31:0x130b20] == original[0x130a31:0x130b20]
print(f'PASS: Nisse reproduction, {cases} real-map bounds cases; {corrected} previously rejected positions corrected.')
print('PASS: ordinary relics, original projection/floor code, callee registers and stack unchanged.')

# Run the actual client area/floor selector too. Only the map-data container
# accessors are stubbed; rectangle tests and height selection execute as x86.
areas = configparser.ConfigParser(strict=False, interpolation=None, allow_no_value=True)
areas.read_string('\n'.join(line.split(';', 1)[0] for line in
    (app / 'ui/maps/areas.dat').read_text(encoding='cp1252').splitlines()))
active_areas = []


def area_accessor(cpu, address, size, unused):
    stack = cpu.reg_read(UC_X86_REG_ESP)
    if address == 0x4f8703:
        value, consumed = len(active_areas), 8
    elif address == 0x4f873f:
        index = u32(cpu.mem_read(stack, 16), 12)
        value, consumed = FRAME + 0x1000 + index * 24, 12
    else:
        return
    ret = u32(cpu.mem_read(stack, 4), 0)
    cpu.reg_write(UC_X86_REG_EAX, value)
    cpu.reg_write(UC_X86_REG_ESP, stack + 4 + consumed)
    cpu.reg_write(UC_X86_REG_EIP, ret)


hook = uc.hook_add(UC_HOOK_CODE, area_accessor, begin=0x4f8703, end=0x4f873f)
floor_cases = 0
for section in areas.values():
    if 'area_count' not in section:
        continue
    zone = int(section.name.removeprefix('zone'))
    active_areas = []
    for i in range(section.getint('area_count')):
        p = f'area{i}_'
        if p + 'left' not in section:
            continue
        active_areas.append(tuple(section.getint(p + key, -1 if key in ('z', 'depth') else 0)
                                  for key in ('left', 'top', 'z', 'width', 'height', 'depth')))
    for i, area in enumerate(active_areas):
        uc.mem_write(FRAME + 0x1000 + i * 24, struct.pack('<6i', *area))
    for left, top, bottom, width, height, depth in active_areas:
        if width <= 0 or height <= 0:
            continue
        x, y = left + width // 2, top + height // 2
        for z in (bottom, bottom + 1, bottom + max(1, depth // 2), bottom + depth + 1):
            expected = next((i for i, (a, b, c, w, h, d) in enumerate(active_areas)
                             if a < x <= a + w and b < y <= b + h and
                             (c == -1 or c + d == -1 or c < z <= c + d)), 0xffffffff)
            stack = STACK + 0x10000
            uc.mem_write(stack, struct.pack('<IIiii', STOP, zone, x, y, z))
            uc.reg_write(UC_X86_REG_ESP, stack)
            uc.reg_write(UC_X86_REG_ESI, MANAGER)
            uc.reg_write(UC_X86_REG_EBP, FRAME)
            uc.emu_start(0x530a31, STOP, count=100000)
            assert uc.reg_read(UC_X86_REG_EAX) == expected, (zone, x, y, z)
            assert uc.reg_read(UC_X86_REG_ESP) == stack + 20
            floor_cases += 1
uc.hook_del(hook)
print(f'PASS: {floor_cases} real area/floor height-boundary cases in the original client selector.')

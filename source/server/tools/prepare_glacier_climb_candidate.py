"""Stage, never deploy, a conservative subdivision of two exported TG climb hops.

The original client geometry and existing links remain unchanged. New landing
pads follow the already-exported climb002 line, using the original builder's
96-unit pad dimensions. Run the installed-native navigation proof afterward.
"""
import argparse
import hashlib
import json
import math
from pathlib import Path
import shutil
import struct


def prepare(source: Path, destination: Path):
    source = source.resolve(strict=True)
    destination = destination.resolve()
    if destination.exists():
        raise ValueError("Destination must be a new staging directory; nothing is overwritten")
    geometry = source / "zone160.obj"
    setting = source / "zone160.gset"
    ladder_data = json.loads((source / "zone160.ladders.json").read_text())
    ladder = next(l for l in ladder_data["Ladders"] if l["Name"].startswith("climb002@"))
    lines = setting.read_text().splitlines()
    links = [line for line in lines if line.startswith("c ")]
    with (source / "zone160.nav").open("rb") as nav:
        header = struct.unpack("<IIIfffffII", nav.read(40))
    if header[0] != 1297302868 or header[1] != 1:
        raise ValueError("Unexpected native mesh header")
    # Detour links only the same tile and immediate neighbors. TG's tiles
    # are much smaller than outdoor tiles; derive the limit from this mesh.
    max_step = min(header[6], header[7]) / 2
    if not 1 <= max_step <= 64:
        raise ValueError("Unexpected tile width; refusing guessed link spacing")
    if len(links) != 75:
        raise ValueError("Expected the audited 75-link export, not a different mesh input")
    pads = []
    rewritten = []
    ordinal = 0
    for line in lines:
        if line.startswith("f "):
            rewritten.append("f zones/zone160.obj")
            continue
        if not line.startswith("c "):
            rewritten.append(line)
            continue
        ordinal += 1
        if ordinal not in (41, 59):
            rewritten.append(line)
            continue
        parts = line.split()
        a, b = [float(v) for v in parts[1:4]], [float(v) for v in parts[4:7]]
        for endpoint in (a, b):
            height = endpoint[1] * 32
            fraction = (height - ladder["BottomZ"]) / (ladder["TopZ"] - ladder["BottomZ"])
            if not 0 <= fraction <= 1:
                raise ValueError("Climb endpoint outside the exported ladder")
            x = ladder["BottomX"] + fraction * (ladder["TopX"] - ladder["BottomX"])
            y = ladder["BottomY"] + fraction * (ladder["TopY"] - ladder["BottomY"])
            if math.hypot(x - endpoint[0] * 32, y - endpoint[2] * 32) > 128:
                raise ValueError("Refusing to bridge anything off the real exported ladder")
        segments = math.ceil(math.dist(a, b) / max_step)
        chain = [a]
        for step in range(1, segments):
            p = [a[i] + (b[i] - a[i]) * step / segments for i in range(3)]
            chain.append(p)
            pads.append(p)
        chain.append(b)
        for start, end in zip(chain, chain[1:]):
            rewritten.append("c " + " ".join(f"{v:.6f}" for v in start + end) + " " + " ".join(parts[7:]))
    zones = destination / "zones"
    zones.mkdir(parents=True)
    shutil.copy2(geometry, zones / geometry.name)
    shutil.copy2(source / "zone160.ladders.json", zones / "zone160.ladders.json")
    with geometry.open() as stream:
        vertices = sum(line.startswith("v ") for line in stream)
    tx, ty = ladder["TangentX"], ladder["TangentY"]
    fx, fy = -ty, tx
    with (zones / geometry.name).open("a", newline="\n") as out:
        for number, (x, z, y) in enumerate(pads):
            corners = [(x + (tx * t + fx * f) * 1.5, z, y + (ty * t + fy * f) * 1.5)
                       for t, f in [(-1, -1), (1, -1), (1, 1), (-1, 1)]]
            out.write(f"\ng audited_climb002_subdivision_{number}\n")
            for corner in corners:
                out.write("v " + " ".join(f"{v:.6f}" for v in corner) + "\n")
            out.write(f"f {vertices+1} {vertices+3} {vertices+2}\nf {vertices+1} {vertices+4} {vertices+3}\n")
            vertices += 4
            rewritten.append(f"v 4 5 {z-.25:.6f} {z+.25:.6f}")
            rewritten.extend(f"{cx:.6f} {z-.25:.6f} {cy:.6f}" for cx, _, cy in corners)
    (zones / "zone160.gset").write_text("\n".join(rewritten) + "\n")
    manifest = {"candidate_only": True, "source": str(source), "split_links": [41, 59],
                "new_pads": len(pads), "maximum_step_world_units": max_step * 32,
                "input_sha256": {p.name: hashlib.sha256(p.read_bytes()).hexdigest() for p in (geometry, setting)}}
    (destination / "climb-candidate.json").write_text(json.dumps(manifest, indent=2))
    print(json.dumps(manifest))


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("source", type=Path)
    parser.add_argument("destination", type=Path)
    args = parser.parse_args()
    prepare(args.source, args.destination)

"""Build an isolated candidate from actual client climb triangles, never an average ladder line.

Requires --ladder-audit output from BuildNav. No installed/source asset is modified.
This is a candidate, not deployment or proof that a complete journey is viable.
"""
import argparse
import hashlib
import json
import math
import shutil
from collections import defaultdict
from pathlib import Path
from audit_glacier_ground_geometry import cross, sub, dot, intersection


def prepare(source, destination, clearance=72):
    source = source.resolve(strict=True)
    destination = destination.resolve()
    if destination.exists():
        raise ValueError('Candidate destination must be new')
    geometry = source / 'zone160.obj'
    audit = source / 'zone160.climb-audit.json'
    shapes = json.loads(audit.read_text())
    if len(shapes) != 2 or {s['Name'] for s in shapes} != {'climb001', 'climb002'}:
        raise ValueError('Not the audited Glacier client climb surfaces')
    # Index ONLY original collision geometry near the two real climb surfaces.
    vertices, obstacles = [], defaultdict(list)
    vertex_count = 0
    with geometry.open() as stream:
        for line in stream:
            p = line.split()
            if not p:
                continue
            if p[0] == 'v':
                x, z, y = map(float, p[1:4])
                vertices.append((x*32, y*32, z*32))
                vertex_count += 1
            elif p[0] == 'f':
                face = [vertices[int(v.split('/')[0])-1] for v in p[1:]]
                for j in range(1, len(face)-1):
                    tri = (face[0], face[j], face[j+1])
                    if any(max(v[i] for v in tri) < lo or min(v[i] for v in tri) > hi
                           for i, lo, hi in ((0, 50100, 55900), (1, 33400, 42000), (2, 11000, 19600))):
                        continue
                    for x in range(int(min(v[0] for v in tri)//256), int(max(v[0] for v in tri)//256)+1):
                        for y in range(int(min(v[1] for v in tri)//256), int(max(v[1] for v in tri)//256)+1):
                            obstacles[x, y].append(tri)
    def clear(a, b):
        checked = set()
        for x in range(int(min(a[0], b[0])//256), int(max(a[0], b[0])//256)+1):
            for y in range(int(min(a[1], b[1])//256), int(max(a[1], b[1])//256)+1):
                for tri in obstacles.get((x, y), ()):
                    if tri in checked:
                        continue
                    checked.add(tri)
                    for height in (24, 80):
                        t = intersection((a[0], a[1], a[2]+height), (b[0], b[1], b[2]+height), tri)
                        if t is not None and .001 < t < .999:
                            return False
        return True
    points, links, rejected = {}, set(), 0
    for shape in shapes:
        raw = [tuple(v) for v in shape['Vertices']]
        normals = defaultdict(lambda: [0., 0., 0.])
        edges = set()
        for face in shape['Triangles']:
            a, b, c = [raw[i] for i in face]
            normal = cross(sub(b, a), sub(c, a))
            length = math.sqrt(dot(normal, normal))
            if length < .001:
                continue
            for v in (a, b, c):
                normals[v] = [normals[v][i]+normal[i]/length for i in range(3)]
            for v, w in ((a,b), (b,c), (c,a)):
                if v != w:
                    edges.add(tuple(sorted((v,w))))
        offset = {}
        for v, normal in normals.items():
            length = math.hypot(normal[0], normal[1])
            if length < .01:
                raise ValueError('Ambiguous climb face orientation')
            offset[v] = (v[0]+normal[0]/length*clearance, v[1]+normal[1]/length*clearance, v[2]+8)
        for va, vb in sorted(edges):
            a, b = offset[va], offset[vb]
            count = math.ceil(math.dist(a,b)/128)
            chain = [tuple(round(a[i]+(b[i]-a[i])*j/count, 4) for i in range(3)) for j in range(count+1)]
            for start, end in zip(chain, chain[1:]):
                if not clear(start, end):
                    rejected += 1
                    continue
                for p in (start, end):
                    points.setdefault(p, len(points))
                links.add((points[start], points[end]))
    if len(links) >= 4096:
        raise ValueError('Exceeds native offmesh limit; do not silently truncate the graph')
    zones = destination / 'zones'
    zones.mkdir(parents=True)
    shutil.copy2(geometry, zones / 'zone160.obj')
    positions = list(points)
    lines = ['f zones/zone160.obj']
    with (zones / 'zone160.obj').open('a', newline='\n') as out:
        for index, p in enumerate(positions):
            x, y, z = p[0]/32, p[1]/32, p[2]/32
            corners = [(x+dx, z, y+dy) for dx,dy in ((-1.5,-1.5),(1.5,-1.5),(1.5,1.5),(-1.5,1.5))]
            out.write(f'\ng climb_surface_pad_{index}\n')
            for corner in corners:
                out.write('v '+' '.join(f'{v:.6f}' for v in corner)+'\n')
            out.write(f'f {vertex_count+1} {vertex_count+3} {vertex_count+2}\nf {vertex_count+1} {vertex_count+4} {vertex_count+3}\n')
            vertex_count += 4
            lines.append(f'v 4 5 {z-.25:.6f} {z+.25:.6f}')
            lines.extend(f'{cx:.6f} {z-.25:.6f} {cy:.6f}' for cx,_,cy in corners)
    for a,b in sorted(links):
        coordinates = [v for p in (positions[a], positions[b]) for v in (p[0]/32,p[2]/32,p[1]/32)]
        lines.append('c '+' '.join(f'{v:.6f}' for v in coordinates)+' 4.1 1 5 8')
    (zones/'zone160.gset').write_text('\n'.join(lines)+'\n')
    manifest = {'candidate_only': True, 'clearance': clearance, 'positions': positions, 'links': len(links), 'blocked_segments_omitted': rejected,
                'original_geometry_sha256': hashlib.sha256(geometry.read_bytes()).hexdigest(),
                'original_climb_audit_sha256': hashlib.sha256(audit.read_bytes()).hexdigest()}
    (destination/'climb-surface-candidate.json').write_text(json.dumps(manifest))
    print(f'{len(points)} actual-surface nodes; {len(links)} links; {rejected} collision-blocked segments omitted')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('source', type=Path)
    parser.add_argument('destination', type=Path)
    parser.add_argument('--clearance', type=int, choices=(72, 96, 128), default=72)
    args = parser.parse_args()
    prepare(args.source, args.destination, args.clearance)

"""Read-only OBJ collision/support probe for the confirmed Glacier climb join.

No geometry or installed mesh is written. Coordinates are native game units;
the original Recast OBJ stores x/height/y at 1:32 scale.
"""
import argparse
import json
import math
from pathlib import Path


def sub(a, b):
    return tuple(x-y for x, y in zip(a, b))


def dot(a, b):
    return sum(x*y for x, y in zip(a, b))


def cross(a, b):
    return (a[1]*b[2]-a[2]*b[1], a[2]*b[0]-a[0]*b[2], a[0]*b[1]-a[1]*b[0])


def intersection(start, end, triangle):
    a, b, c = triangle
    direction = sub(end, start)
    edge1, edge2 = sub(b, a), sub(c, a)
    h = cross(direction, edge2)
    determinant = dot(edge1, h)
    if abs(determinant) < 1e-8:
        return None
    inv = 1 / determinant
    s = sub(start, a)
    u = inv * dot(s, h)
    if u < 0 or u > 1:
        return None
    q = cross(s, edge1)
    v = inv * dot(direction, q)
    if v < 0 or u+v > 1:
        return None
    t = inv * dot(edge2, q)
    return t if 0 <= t <= 1 else None


def audit(path, start=None, end=None):
    start = start or (51285.05, 39961.23, 11388.554)
    end = end or (51432.195, 40315.688, 11370.557)
    vertices, triangles = [], []
    group = ''
    with path.open() as stream:
        for line in stream:
            parts = line.split()
            if not parts:
                continue
            if parts[0] == 'g':
                group = ' '.join(parts[1:])
            elif parts[0] == 'v':
                x, height, y = map(float, parts[1:4])
                vertices.append((x*32, y*32, height*32))
            elif parts[0] == 'f':
                face = [vertices[int(p.split('/')[0])-1] for p in parts[1:]]
                for i in range(1, len(face)-1):
                    triangle = (face[0], face[i], face[i+1])
                    if all(max(v[axis] for v in triangle) >= min(start[axis], end[axis])-128 and
                           min(v[axis] for v in triangle) <= max(start[axis], end[axis])+128 for axis in range(3)):
                        triangles.append((group, triangle))
    supports = []
    for step in range(9):
        x, y, z = [a+(b-a)*step/8 for a, b in zip(start, end)]
        hits = []
        for name, triangle in triangles:
            t = intersection((x, y, z+128), (x, y, z-256), triangle)
            if t is not None:
                normal = cross(sub(triangle[1], triangle[0]), sub(triangle[2], triangle[0]))
                slope = abs(normal[2]) / max(1e-12, math.sqrt(dot(normal, normal)))
                hits.append((round(z+128-t*384, 2), name, round(slope, 3)))
        supports.append({'point': [round(v, 2) for v in (x,y,z)], 'surfaces': sorted(hits, reverse=True)[:5]})
    obstructions = []
    for height in (24, 50, 80):
        a, b = (start[0], start[1], start[2]+height), (end[0], end[1], end[2]+height)
        hits = [(t, name) for name, triangle in triangles if (t := intersection(a, b, triangle)) is not None and .001 < t < .999]
        obstructions.append({'height': height, 'crossings': sorted(hits)[:10]})
    print(json.dumps({'triangles': len(triangles), 'supports': supports, 'obstructions': obstructions}, indent=2))


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('obj', type=Path)
    parser.add_argument('--start', nargs=3, type=float)
    parser.add_argument('--end', nargs=3, type=float)
    args = parser.parse_args()
    audit(args.obj, args.start, args.end)

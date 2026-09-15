"""Render a read-only collision cross-section; writes only the requested diagnostic PNG."""
import argparse
from pathlib import Path
from PIL import Image, ImageDraw


def plot(source, output, focus=None):
    vertices, floors, walls, pads = [], [], [], []
    group = ''
    low = (50500, 39300)
    high = (52400, 41700)
    height = 11430
    labels = [('entry-connected floor', 51285.05, 39961.23), ('exported climb base', 51432.195, 40315.688)]
    if focus:
        x, y, z = focus
        low, high, height = (x-1400, y-1400), (x+1400, y+1400), z+48
        labels = [('spawn', x, y)]
    with source.open() as stream:
        for line in stream:
            fields = line.split()
            if not fields:
                continue
            if fields[0] == 'g':
                group = ' '.join(fields[1:])
            elif fields[0] == 'v':
                x, z, y = map(float, fields[1:4])
                vertices.append((x * 32, y * 32, z * 32))
            elif fields[0] == 'f':
                face = [vertices[int(v.split('/')[0]) - 1] for v in fields[1:]]
                for i in range(1, len(face)-1):
                    triangle = [face[0], face[i], face[i+1]]
                    if any(max(v[axis] for v in triangle) < low[axis] or min(v[axis] for v in triangle) > high[axis] for axis in (0, 1)):
                        continue
                    if all(height-140 <= v[2] <= height for v in triangle):
                        (pads if 'pad' in group else floors).append([(v[0], v[1]) for v in triangle])
                    hits = []
                    for a, b in zip(triangle, triangle[1:] + triangle[:1]):
                        if (a[2] <= height < b[2]) or (b[2] <= height < a[2]):
                            t = (height-a[2])/(b[2]-a[2])
                            hits.append((a[0]+t*(b[0]-a[0]), a[1]+t*(b[1]-a[1])))
                    if len(hits) == 2:
                        walls.append(hits)
    picture = Image.new('RGB', (960, 1280), 'white')
    draw = ImageDraw.Draw(picture)
    def screen(point):
        scale = min(840/(high[0]-low[0]), 1080/(high[1]-low[1]))
        return (60 + (point[0]-low[0])*scale, 1190 - (point[1]-low[1])*scale)
    for face in floors:
        draw.polygon([screen(p) for p in face], fill='#dce8ee', outline='#becdd4')
    for face in pads:
        draw.polygon([screen(p) for p in face], fill='#f8d25d', outline='#ac8110')
    for wall in walls:
        draw.line([screen(p) for p in wall], fill='#d64545', width=2)
    for name, x, y in labels:
        sx, sy = screen((x, y))
        draw.ellipse((sx-4, sy-4, sx+4, sy+4), fill='black')
        draw.text((sx+8, sy+8), name, fill='black', font_size=18)
    for x in range(int(low[0]//500)*500+500, int(high[0]), 500):
        sx, _ = screen((x, low[1]))
        draw.text((sx-20, 1210), str(x), fill='black', font_size=16)
    for y in range(int(low[1]//500)*500+500, int(high[1]), 500):
        _, sy = screen((low[0], y))
        draw.text((2, sy), str(y), fill='black', font_size=16)
    draw.text((60, 24), 'Glacier: original collision geometry', fill='black', font_size=23)
    draw.text((60, 58), f'Red: walls at Z{height:g}. Yellow: artificial pads.', fill='black', font_size=20)
    picture.save(output)
    print(f'{len(floors)} floor triangles, {len(pads)} pad triangles, {len(walls)} wall segments: {output}')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('source', type=Path)
    parser.add_argument('output', type=Path)
    parser.add_argument('--focus', nargs=3, type=float, metavar=('X', 'Y', 'Z'))
    args = parser.parse_args()
    if args.output.resolve() == args.source.resolve():
        raise ValueError('Source geometry must never be overwritten')
    plot(args.source, args.output, args.focus)

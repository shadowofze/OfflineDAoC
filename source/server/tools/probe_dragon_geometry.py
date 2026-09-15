"""Read-only vertical intersection of exported client geometry at dragon homes."""
from pathlib import Path
import numpy as np

root = Path(__file__).resolve().parents[2] / 'OpenDAoC-BuildNav/bin/Release/net10.0-windows7.0/base/zones'
for zone, x, y in [(4,391326,755351),(116,708811,1021459),(216,408646,706432)]:
    vertices, faces, groups = [], [], []
    group = ''
    for line in (root / f'zone{zone:03}.obj').open():
        fields=line.split()
        if not fields: continue
        if fields[0]=='g': group=' '.join(fields[1:])
        elif fields[0]=='v': vertices.append([float(fields[1])*32,float(fields[3])*32,float(fields[2])*32])
        elif fields[0]=='f':
            faces.append([int(v.split('/')[0])-1 for v in fields[1:4]])
            groups.append(group)
    triangles=np.asarray(vertices)[np.asarray(faces)]
    a,b,c=triangles[:,0],triangles[:,1],triangles[:,2]
    denominator=(b[:,1]-c[:,1])*(a[:,0]-c[:,0])+(c[:,0]-b[:,0])*(a[:,1]-c[:,1])
    valid=np.abs(denominator)>1e-6
    for dx,dy in [(0,0),(200,0),(-200,0),(0,200),(0,-200),(400,0),(-400,0),(0,400),(0,-400)]:
        u=np.zeros(len(a));v=u.copy()
        u[valid]=((b[valid,1]-c[valid,1])*(x+dx-c[valid,0])+(c[valid,0]-b[valid,0])*(y+dy-c[valid,1]))/denominator[valid]
        v[valid]=((c[valid,1]-a[valid,1])*(x+dx-c[valid,0])+(a[valid,0]-c[valid,0])*(y+dy-c[valid,1]))/denominator[valid]
        hits=np.where(valid & (u>=-1e-6) & (v>=-1e-6) & (u+v<=1.000001))[0]
        result=sorted(set((round(float(u[i]*a[i,2]+v[i]*b[i,2]+(1-u[i]-v[i])*c[i,2]),1),groups[i]) for i in hits))
        print(zone,dx,dy,result)

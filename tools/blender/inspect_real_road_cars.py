"""Inspect the two licensed road-car sources without altering them."""
import json
from pathlib import Path
import bpy
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
for kind in ('bmw-m3-e30', 'lotus-elise'):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    if kind=='bmw-m3-e30':
        bpy.ops.wm.open_mainfile(filepath=str(ROOT/'build/real-models/author/BMW E30_Final01/E30_Final01.blend'),load_ui=False,use_scripts=False)
    else:
        bpy.ops.import_scene.gltf(filepath=str(ROOT / 'build/real-models' / (kind+'-decoded.glb')))
    rows=[]
    for obj in bpy.context.scene.objects:
        if obj.type!='MESH': continue
        points=[obj.matrix_world @ Vector(corner) for corner in obj.bound_box]
        low=[min(p[i] for p in points) for i in range(3)]
        high=[max(p[i] for p in points) for i in range(3)]
        rows.append({'name':obj.name,'low':low,'high':high,'vertices':len(obj.data.vertices),'materials':[m.name for m in obj.data.materials if m],'modifiers':[(m.name,m.type) for m in obj.modifiers],'rotation':list(obj.rotation_euler)})
    (ROOT/'build/real-models'/(kind+'-inspect.json')).write_text(json.dumps(rows,indent=2))
    print(kind, 'objects',len(rows), 'vertices',sum(r['vertices'] for r in rows),flush=True)

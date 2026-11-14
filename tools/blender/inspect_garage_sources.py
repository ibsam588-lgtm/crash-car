import bpy
from pathlib import Path
from mathutils import Vector
ROOT = Path(__file__).resolve().parents[2]
for name in ('pickup','suv','hauler','monster'):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=str(ROOT / 'build/fleet-upgrade/sources' / (name+'.glb')))
    objects=[o for o in bpy.context.scene.objects if o.type=='MESH']
    points=[o.matrix_world @ Vector(v) for o in objects for v in o.bound_box]
    print('SOURCE',name,'bounds',[(min(v[i] for v in points),max(v[i] for v in points)) for i in range(3)])
    for o in objects:
        if any(k in o.name.lower() for k in ['tyre','tire','wheel_fl','headlight','headlamp','front bumper']):
            p=sum((o.matrix_world @ Vector(c) for c in o.bound_box),Vector())/8
            print(o.name, tuple(round(v,3) for v in p),tuple(round(v,3) for v in o.dimensions))

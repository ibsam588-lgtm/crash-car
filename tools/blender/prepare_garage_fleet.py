"""Prepare the CC0 detailed SUV/trucks and a lifted, knobby-tired pickup variant."""
import json, math
from pathlib import Path
import bpy
from mathutils import Vector, Matrix

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT/'unity/CarCrashUnityPrototype/Assets/CarCrash/Models/Garage'
TEX = ROOT/'unity/CarCrashUnityPrototype/Assets/CarCrash/Textures/Garage'
OUT.mkdir(parents=True, exist_ok=True); TEX.mkdir(parents=True, exist_ok=True)

def bounds(obj):
    points=[obj.matrix_world @ vertex.co for vertex in obj.data.vertices]
    return Vector(tuple(min(p[i] for p in points) for i in range(3))), Vector(tuple(max(p[i] for p in points) for i in range(3)))

def role(name):
    n=name.lower()
    if 'paint' in n:return 'Paint'
    if 'glass' in n:return 'Glass'
    if 'rubber' in n:return 'Tire'
    if 'chrome' in n:return 'Alloy'
    if 'redlens' in n:return 'Taillamp'
    if 'clearlens' in n:return 'Headlamp'
    if 'amber' in n:return 'Amber'
    if 'interior' in n:return 'Interior'
    if 'steel' in n:return 'Steel'
    return 'Trim'

def texture_at(socket):
    if not socket or not socket.is_linked:return None
    node=socket.links[0].from_node
    if node.type=='TEX_IMAGE':return node.image
    if node.type=='NORMAL_MAP':return texture_at(node.inputs.get('Color'))
    return None

def cylinder(name,a,b,radius,material):
    bpy.ops.mesh.primitive_cylinder_add(vertices=20, radius=radius, depth=(b-a).length, location=(a+b)/2)
    o=bpy.context.object; o.name=name; o.rotation_euler=(b-a).to_track_quat('Z','Y').to_euler(); o.data.materials.append(material)
    bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
    return o

reports=[]
for kind in ('pickup','suv','hauler','monster'):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    source='pickup' if kind=='monster' else kind
    bpy.ops.import_scene.gltf(filepath=str(ROOT/'build/fleet-upgrade/sources'/f'{source}.glb'))
    objects=[o for o in bpy.context.scene.objects if o.type=='MESH']
    points=[p for o in objects for p in bounds(o)]
    center=Vector(((min(p.x for p in points)+max(p.x for p in points))/2,0,0))
    rotate=Matrix.Rotation(math.pi/2,4,'Z')
    # Bake all source transforms into a common frame: front -Y, up Z.
    for obj in objects:
        obj.data=obj.data.copy()
        obj.data.transform(rotate @ Matrix.Translation(-center) @ obj.matrix_world)
        obj.parent=None; obj.matrix_world=Matrix.Identity(4)
        obj.data.update()
    bpy.context.view_layer.update()
    for obj in list(bpy.context.scene.objects):
        if obj.type!='MESH':bpy.data.objects.remove(obj,do_unlink=True)
    tyres=[o for o in objects if 'Sculpted_tyre' in o.name or 'Commercial tire formed sidewalls' in o.name]
    centers={}
    for obj in tyres:
        low,high=bounds(obj); p=(low+high)/2
        key=('F' if p.y<0 else 'R')+('L' if p.x<0 else 'R')
        centers.setdefault(key,[]).append(p)
    pivots={key:sum(value,Vector())/len(value) for key,value in centers.items()}
    groups={key:[] for key in ('FL','FR','RL','RR','Body','Damage_Front_Bumper','Damage_Rear_Bumper')}
    radius=max((bounds(tyres[0])[1]-bounds(tyres[0])[0]).y,(bounds(tyres[0])[1]-bounds(tyres[0])[0]).z)/2
    material_info=[]; image_names={}
    for i,material in enumerate(list(bpy.data.materials)):
        material_role=role(material.name)
        material.name=f'M_Garage_{material_role}_{source}_{i}'
        node=next((n for n in material.node_tree.nodes if n.type=='BSDF_PRINCIPLED'),None)
        info={'name':material.name,'role':material_role,'color':list(node.inputs['Base Color'].default_value),
              'roughness':node.inputs['Roughness'].default_value,'baseTexture':'','normalTexture':''}
        for socket,key in [('Base Color','baseTexture'),('Normal','normalTexture')]:
            image=texture_at(node.inputs.get(socket))
            if image:
                if image.name not in image_names:
                    image_names[image.name]=f'{source}_{len(image_names):02}.png'
                    factor=min(1,512/max(image.size)); image.scale(round(image.size[0]*factor),round(image.size[1]*factor))
                    image.filepath_raw=str(TEX/image_names[image.name]); image.file_format='PNG'; image.save()
                info[key]=image_names[image.name]
        material_info.append(info)
    for obj in objects:
        low,high=bounds(obj); p=(low+high)/2; nearest=min(pivots,key=lambda k:(p-pivots[k]).length)
        wheel_word=any(w in obj.name.lower() for w in ('tyre','tire','rim','lug','center_cap','valve_stem','wheel hub','rotor'))
        delta=p-pivots[nearest]
        if wheel_word and abs(delta.x)<.42 and abs(delta.y)<radius+.15 and abs(delta.z)<radius+.15:
            groups[nearest].append(obj)
        elif 'front bumper' in obj.name.lower():groups['Damage_Front_Bumper'].append(obj)
        elif 'rear bumper' in obj.name.lower():groups['Damage_Rear_Bumper'].append(obj)
        else:groups['Body'].append(obj)
    if kind=='monster':
        old_pivots=pivots.copy(); radius=.82
        pivots={k:Vector((-1.28 if k.endswith('L') else 1.28,v.y,.82)) for k,v in pivots.items()}
        for key,items in groups.items():
            for obj in items:
                for vertex in obj.data.vertices:
                    if key in pivots:
                        local=vertex.co-old_pivots[key]
                        local.x*=2.35; local.y*=2.1; local.z*=2.1
                        vertex.co=pivots[key]+local
                    else:vertex.co.z+=1.20
        steel=next(m for m in bpy.data.materials if '_Steel_' in m.name)
        for axle in ('F','R'):
            y=pivots[axle+'L'].y
            groups['Body'].append(cylinder('Exposed solid axle',Vector((-1.1,y,.82)),Vector((1.1,y,.82)),.11,steel))
            for x in (-.72,.72):
                groups['Body'].append(cylinder('Long travel shock',Vector((x,y,.9)),Vector((x,y,1.85)),.065,steel))
        tire=next(m for m in bpy.data.materials if '_Tire_' in m.name)
        for key,pivot in pivots.items():
            # Raised chevron lugs make the scaled tires read as off-road tires.
            for n in range(32):
                angle=n*math.tau/32
                for side in (-1,1):
                    bpy.ops.mesh.primitive_cube_add(size=1,location=pivot+Vector((side*.15,math.sin(angle)*.81,math.cos(angle)*.81)))
                    lug=bpy.context.object; lug.name='Chevron tread block'; lug.scale=(.25,.16,.095)
                    lug.rotation_euler=(angle,side*.25,side*.18); lug.data.materials.append(tire)
                    bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
                    groups[key].append(lug)
    root=bpy.data.objects.new(f'Playable_{kind}',None); bpy.context.collection.objects.link(root)
    for group,items in groups.items():
        if not items:continue
        bpy.ops.object.select_all(action='DESELECT')
        for obj in items:obj.select_set(True)
        bpy.context.view_layer.objects.active=items[0]; bpy.ops.object.join(); obj=bpy.context.object
        obj.name='Wheel_'+group if group in pivots else group
        if kind!='monster' or group not in pivots:
            decimate=obj.modifiers.new('Playable mesh budget','DECIMATE'); decimate.ratio=.72
            bpy.ops.object.modifier_apply(modifier=decimate.name)
        if group in pivots:
            bpy.context.scene.cursor.location=pivots[group]; bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
        unique=[]; remap={}
        for i,mat in enumerate(obj.data.materials):
            if mat not in unique:unique.append(mat)
            remap[i]=unique.index(mat)
        indices=[remap[p.material_index] for p in obj.data.polygons]
        obj.data.materials.clear()
        for mat in unique:obj.data.materials.append(mat)
        for p,index in zip(obj.data.polygons,indices):p.material_index=index
        obj.parent=root
    bpy.context.scene.cursor.location=(0,0,0)
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.export_scene.fbx(filepath=str(OUT/f'{kind}.fbx'),use_selection=True,axis_forward='-Z',axis_up='Y',
        object_types={'EMPTY','MESH'},apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',bake_anim=False,add_leaf_bones=False)
    info={'materials':material_info,'wheelRadius':radius,'wheelCenters':[[pivots[k].x,pivots[k].z,-pivots[k].y] for k in ('FL','FR','RL','RR')]}
    (OUT/f'{kind}.json').write_text(json.dumps(info,indent=2))
    # Review the complete converted geometry with its authored materials.
    scene=bpy.context.scene
    body_points=[p for o in root.children if o.type=='MESH' for p in bounds(o)]
    length=max(p.y for p in body_points)-min(p.y for p in body_points)
    height=max(p.z for p in body_points)
    bpy.ops.object.camera_add(location=(length*1.15,-length*1.35,height*.9))
    camera=bpy.context.object; camera.rotation_euler=(Vector((0,0,height*.45))-camera.location).to_track_quat('-Z','Y').to_euler(); camera.data.lens=48; scene.camera=camera
    for position,energy in [((3,-4,7),1300),((-4,2,5),950)]:
        bpy.ops.object.light_add(type='AREA',location=position); bpy.context.object.data.energy=energy; bpy.context.object.data.size=5
    scene.world=bpy.data.worlds.new('Garage studio'); scene.world.color=(.18,.18,.18)
    scene.render.engine='CYCLES'; scene.cycles.samples=12
    scene.render.resolution_x=1000; scene.render.resolution_y=650; scene.render.resolution_percentage=100
    scene.render.filepath=str(ROOT/'build/fleet-upgrade'/f'{kind}-preview.png'); bpy.ops.render.render(write_still=True)
    triangles=sum(len(p.vertices)-2 for obj in root.children if obj.type=='MESH' for p in obj.data.polygons)
    reports.append({'kind':kind,'triangles':triangles,'wheelRadius':radius,'wheelCenters':info['wheelCenters']})
    print('GARAGE_VEHICLE_EXPORTED',reports[-1],flush=True)
(ROOT/'build/fleet-upgrade/fleet-export.json').write_text(json.dumps(reports,indent=2))

"""Convert the licensed real road cars, retaining their authored surfaces."""
import json, math, re
from pathlib import Path
import bpy
from mathutils import Vector, Matrix

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'unity/CarCrashUnityPrototype/Assets/CarCrash/Models/RoadCars'
TEX=ROOT/'unity/CarCrashUnityPrototype/Assets/CarCrash/Textures/RoadCars'
OUT.mkdir(parents=True,exist_ok=True);TEX.mkdir(parents=True,exist_ok=True)

def bounds(obj):
    points=[obj.matrix_world @ v.co for v in obj.data.vertices]
    return Vector(tuple(min(p[i] for p in points) for i in range(3))),Vector(tuple(max(p[i] for p in points) for i in range(3)))

def image_at(socket):
    if not socket or not socket.is_linked:return None
    node=socket.links[0].from_node
    if node.type=='TEX_IMAGE':return node.image
    if node.type=='NORMAL_MAP':return image_at(node.inputs.get('Color'))
    if node.type in ('SEPARATE_COLOR','SEPRGB'):return image_at(node.inputs[0])
    return None

def gamma(value):
    return value*12.92 if value<=.0031308 else 1.055*value**(1/2.4)-.055

def role(name,kind):
    n=name.lower()
    if 'paint' in n or 'car.carros' in n:return 'Paint_1'
    if n.endswith('lens'):return 'Lens'
    if 'windows' in n or 'verre' in n:return 'Glass'
    if 'tire' in n:return 'Tire'
    if 'fabric' in n:return 'Fabric'
    if 'rim' in n or 'chrome' in n:return 'Alloy'
    if 'caliper' in n:return 'Caliper'
    if 'disc' in n:return 'Disc'
    if 'headlight' in n or 'avant' in n:return 'Headlamp'
    if 'taillight' in n or 'arr-rouge' in n or n=='car.feu':return 'Taillamp'
    if 'orange' in n or 'feu-2' in n:return 'Amber'
    if 'mirror' in n or 'mirroir' in n:return 'Mirror'
    return 'Trim'

reports=[]
for kind,length in [('bmw-m3-e30',4.36),('lotus-elise',3.79)]:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    if kind=='bmw-m3-e30':
        bpy.ops.wm.open_mainfile(filepath=str(ROOT/'build/real-models/author/BMW E30_Final01/E30_Final01.blend'),load_ui=False,use_scripts=False)
        for image in bpy.data.images:
            candidates=list((ROOT/'build/real-models/author').rglob(Path(image.filepath.replace('\\','/')).name))
            candidates=[p for p in candidates if p.is_file()]
            if candidates and image.filepath:image.filepath=str(candidates[0]);image.reload()
    else:
        bpy.ops.import_scene.gltf(filepath=str(ROOT/'build/real-models'/(kind+'-decoded.glb')))
    if kind=='bmw-m3-e30':
        for obj in bpy.context.scene.objects:
            if obj.type=='MESH' and any(x in obj.name for x in ('RIM','TIRE','Brake','Brembo','Logo_Plane')) and obj.matrix_world.translation.y<-.5:
                # The source front wheels are posed at 29 degrees. Neutralize
                # only their yaw before establishing physics/animation pivots.
                rotation=obj.rotation_euler.copy();rotation.z=0
                obj.rotation_mode='XYZ';obj.rotation_euler=rotation
        bpy.context.view_layer.update()
    objects=[o for o in bpy.context.scene.objects if o.type=='MESH']
    for obj in list(objects):
        names=' '.join(m.name for m in obj.data.materials if m)
        if not names or names in ('SHADE','material','Ground') or 'Logo_Plane' in names or 'NANA-' in names:
            objects.remove(obj);bpy.data.objects.remove(obj,do_unlink=True)
    points=[p for obj in objects for p in bounds(obj)]
    low=Vector(tuple(min(p[i] for p in points) for i in range(3)))
    high=Vector(tuple(max(p[i] for p in points) for i in range(3)))
    center=(low+high)/2;center.z=low.z
    scale=length/(high.y-low.y)
    rotate=Matrix.Rotation(math.pi if kind=='lotus-elise' else 0,4,'Z')
    normalization=Matrix.Scale(scale,4) @ rotate @ Matrix.Translation(-center)
    groups={key:[] for key in ('FL','FR','RL','RR','Body')}
    for obj in objects:
        original_names=' '.join(m.name for m in obj.data.materials if m)
        wheel=any(w in original_names for w in ('_RIM','_TIRE','Brake_Disc','Brembo_Calipers')) if kind=='bmw-m3-e30' else bool(re.search(r'Layer(?:2|16|17|18)_',obj.name))
        # Bake the author's smoothing modifiers before joining objects.
        if kind=='bmw-m3-e30':
            evaluated=obj.evaluated_get(bpy.context.evaluated_depsgraph_get())
            obj.data=bpy.data.meshes.new_from_object(evaluated)
            obj.modifiers.clear()
        else:obj.data=obj.data.copy()
        obj.data.transform(normalization @ obj.matrix_world)
        obj.parent=None;obj.matrix_world=Matrix.Identity(4);obj.data.update()
        a,b=bounds(obj);p=(a+b)/2
        key=('F' if p.y<0 else 'R')+('L' if p.x<0 else 'R') if wheel else 'Body'
        if kind=='lotus-elise':
            for slot in obj.material_slots:
                if slot.material is None or slot.material.name!='CAR.black':continue
                if wheel or 'Layer10_' in obj.name:
                    original=slot.material;slot.material=original.copy()
                    slot.material.name='Lotus_tire_rubber' if wheel else 'Lotus_softtop_fabric'
                    node=next(n for n in slot.material.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
                    node.inputs['Roughness'].default_value=.87 if wheel else .94
                    node.inputs['Metallic'].default_value=0
                    node.inputs['Base Color'].default_value=(.012,.014,.016,1)
                    coat=node.inputs.get('Coat Weight')
                    if coat:coat.default_value=0
        groups[key].append(obj)
    for obj in list(bpy.context.scene.objects):
        if obj.type!='MESH':bpy.data.objects.remove(obj,do_unlink=True)
    # Separate real lower bumper faces instead of marking the complete imported
    # coachwork detachable. The intact silhouette and material UVs are unchanged.
    for label,sign in [('Front',-1),('Rear',1)]:
        key='Damage_'+label+'_Bumper';groups[key]=[]
        for obj in list(groups['Body']):
            selected=[p.index for p in obj.data.polygons if sign*p.center.y>length*.40 and p.center.z<.53 and p.center.z>.08]
            if not selected or len(selected)==len(obj.data.polygons):continue
            bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);bpy.context.view_layer.objects.active=obj
            for vertex in obj.data.vertices:vertex.select=False
            for edge in obj.data.edges:edge.select=False
            chosen=set(selected)
            for polygon in obj.data.polygons:polygon.select=polygon.index in chosen
            bpy.context.tool_settings.mesh_select_mode=(False,False,True)
            before=set(bpy.context.scene.objects)
            bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.separate(type='SELECTED');bpy.ops.object.mode_set(mode='OBJECT')
            groups[key].extend(o for o in bpy.context.scene.objects if o not in before and o.type=='MESH')
    pivots={};radii=[]
    for key in ('FL','FR','RL','RR'):
        tyres=[o for o in groups[key] if any(('TIRE' in m.name or (kind=='lotus-elise' and 'Lotus_tire_rubber' in m.name)) for m in o.data.materials if m)]
        if not tyres:raise RuntimeError('Missing complete tire '+kind+'/'+key)
        a,b=bounds(tyres[0]);pivots[key]=(a+b)/2;radii.append((b.z-a.z)/2)
    images={};material_info=[]
    for index,material in enumerate([m for m in bpy.data.materials if m.users]):
        original=material.name;usage=role(original,kind)
        material.name=f'M_RoadCar_{kind.replace("-","_")}_{usage}_{index}'
        node=next((n for n in material.node_tree.nodes if n.type=='BSDF_PRINCIPLED'),None)
        # The artist's headlamp lenses use a Glass BSDF. FBX's default material
        # conversion makes those opaque white unless we export that role too.
        if node is None:node=next((n for n in material.node_tree.nodes if n.type in ('BSDF_GLASS','BSDF_TRANSPARENT','BSDF_DIFFUSE','BSDF_GLOSSY')),None)
        color_socket=node.inputs.get('Base Color') or node.inputs.get('Color') if node else None
        color=list(color_socket.default_value) if color_socket else [.3,.3,.3,1]
        rough_socket=node.inputs.get('Roughness') if node else None
        metal_socket=node.inputs.get('Metallic') if node else None
        info={'name':material.name,'role':usage,'color':[gamma(c) for c in color[:3]]+[color[3]],
              'metallic':metal_socket.default_value if metal_socket else 0,'roughness':rough_socket.default_value if rough_socket else .25,
              'baseTexture':'','normalTexture':'','roughTexture':'','metalTexture':'','packedSource':kind!='bmw-m3-e30'}
        for socket,field in [('Base Color','baseTexture'),('Normal','normalTexture'),('Roughness','roughTexture'),('Metallic','metalTexture')]:
            source_socket=(node.inputs.get('Base Color') or node.inputs.get('Color')) if node and socket=='Base Color' else node.inputs.get(socket) if node else None
            image=image_at(source_socket)
            if image is None and usage=='Lens' and field=='baseTexture':
                image=next((n.image for n in material.node_tree.nodes if n.type=='TEX_IMAGE' and n.image and 'LENS_COLOR' in n.image.name),None)
            if not image:continue
            if image.name not in images:
                images[image.name]=f'{kind}_{len(images):02}.png'
                factor=min(1,2048/max(image.size))
                if factor<1:image.scale(round(image.size[0]*factor),round(image.size[1]*factor))
                image.filepath_raw=str(TEX/images[image.name]);image.file_format='PNG';image.save()
            info[field]=images[image.name]
        material_info.append(info)
    root=bpy.data.objects.new('Detailed_'+kind,None);bpy.context.collection.objects.link(root)
    for key,items in groups.items():
        if not items:continue
        bpy.ops.object.select_all(action='DESELECT')
        for obj in items:obj.select_set(True)
        bpy.context.view_layer.objects.active=items[0];bpy.ops.object.join();obj=bpy.context.object
        obj.name='Wheel_'+key if key in pivots else key if key.startswith('Damage_') else 'Detailed_Coachwork'
        if kind=='lotus-elise' and key!='Body':
            # Preserve the sculpted tire and rim profile while reducing repeated
            # tread geometry from the high-resolution source. Small detachable
            # bumper pieces use this same mesh budget.
            modifier=obj.modifiers.new('Repeated wheel detail budget','DECIMATE');modifier.ratio=.60
            bpy.ops.object.modifier_apply(modifier=modifier.name)
        if key in pivots:
            bpy.context.scene.cursor.location=pivots[key];bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
        unique=[];remap={}
        for i,mat in enumerate(obj.data.materials):
            if mat not in unique:unique.append(mat)
            remap[i]=unique.index(mat)
        indices=[remap[p.material_index] for p in obj.data.polygons]
        obj.data.materials.clear()
        for mat in unique:obj.data.materials.append(mat)
        for polygon,index in zip(obj.data.polygons,indices):polygon.material_index=index
        obj.parent=root
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.export_scene.fbx(filepath=str(OUT/(kind+'.fbx')),use_selection=True,object_types={'EMPTY','MESH'},
        axis_forward='-Z',axis_up='Y',apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',
        bake_anim=False,add_leaf_bones=False,mesh_smooth_type='FACE',use_mesh_modifiers=True)
    info={'materials':material_info,'wheelRadius':sum(radii)/4,
          'wheelCenters':[[pivots[k].x,pivots[k].z,-pivots[k].y] for k in ('FL','FR','RL','RR')]}
    (OUT/(kind+'.json')).write_text(json.dumps(info,indent=2))
    scene=bpy.context.scene
    bpy.ops.object.camera_add(location=(length*.95,-length*1.2,length*.5))
    camera=bpy.context.object;camera.rotation_euler=(Vector((0,0,.7))-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.lens=52;scene.camera=camera
    for position,energy in [((3,-4,7),1800),((-4,2,5),1400)]:
        bpy.ops.object.light_add(type='AREA',location=position);light=bpy.context.object;light.data.energy=energy;light.data.shape='DISK';light.data.size=5
    scene.world=bpy.data.worlds.new('Neutral studio');scene.world.color=(.20,.20,.20)
    scene.render.engine='CYCLES';scene.cycles.samples=12
    scene.render.resolution_x=1100;scene.render.resolution_y=700;scene.render.resolution_percentage=100
    scene.render.filepath=str(ROOT/'build/real-models'/(kind+'-preview.png'));bpy.ops.render.render(write_still=True)
    report={'kind':kind,'triangles':sum(len(p.vertices)-2 for o in root.children for p in o.data.polygons),
            'wheelRadius':info['wheelRadius'],'wheelCenters':info['wheelCenters'],'textures':len(images)}
    reports.append(report);print('REAL_ROAD_CAR_EXPORTED',report,flush=True)
(ROOT/'build/real-models/export-report.json').write_text(json.dumps(reports,indent=2))

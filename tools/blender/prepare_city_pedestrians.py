"""Convert CC0 Innerscene/MakeHuman walkers to compact animated Unity FBXs."""
import json
from pathlib import Path
import bpy
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "unity/CarCrashUnityPrototype/Assets/CarCrash/Models/Pedestrians"
TEX = ROOT / "unity/CarCrashUnityPrototype/Assets/CarCrash/Textures/Pedestrians"
OUT.mkdir(parents=True, exist_ok=True)
TEX.mkdir(parents=True, exist_ok=True)

def image_at(socket):
    if not socket or not socket.is_linked:
        return None
    node = socket.links[0].from_node
    if node.type == "TEX_IMAGE":
        return node.image
    if node.type == "NORMAL_MAP":
        return image_at(node.inputs.get("Color"))
    return None

reports = []
for name in ("maya", "sam", "harper"):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.context.scene.render.fps = 30
    bpy.ops.import_scene.gltf(filepath=str(ROOT / "build/qa-refinement/people-source" / (name + ".glb")))
    scene = bpy.context.scene
    meshes = [obj for obj in scene.objects if obj.type == "MESH"]
    materials = []
    image_names = {}
    for index, mat in enumerate(list(bpy.data.materials)):
        original = mat.name
        mat.name = f"Ped_{name}_{index:02}"
        bsdf = next((n for n in mat.node_tree.nodes if n.type == "BSDF_PRINCIPLED"), None)
        if not bsdf:
            continue
        entry = {"name": mat.name, "sourceName": original, "color": list(bsdf.inputs['Base Color'].default_value),
                 "roughness": bsdf.inputs['Roughness'].default_value, "cutout": False, "baseTexture": "", "normalTexture": ""}
        for socket_name, key in (("Base Color", "baseTexture"), ("Normal", "normalTexture")):
            image = image_at(bsdf.inputs.get(socket_name))
            if not image:
                continue
            if image.name not in image_names:
                image_names[image.name] = f"{name}_{len(image_names):02}.png"
                size = image.size[:]
                maximum = 1024 if "body.body" in original else 512
                factor = min(1.0, maximum / max(size))
                image.scale(max(1, round(size[0] * factor)), max(1, round(size[1] * factor)))
                image.filepath_raw = str(TEX / image_names[image.name])
                image.file_format = "PNG"
                image.save()
            entry[key] = image_names[image.name]
        entry['cutout'] = any(part in original.lower() for part in ("eyebrow", "eyelash", "hair roots", "short01", "ponytail"))
        materials.append(entry)
    # Retain the armature and its weights while reducing hidden body/clothing density.
    for obj in meshes:
        if len(obj.data.polygons) > 600:
            bpy.context.view_layer.objects.active = obj
            obj.select_set(True)
            decimate = obj.modifiers.new("Sidewalk mesh budget", "DECIMATE")
            decimate.ratio = 0.24 if len(obj.data.polygons) > 4000 else 0.5
            bpy.ops.object.modifier_move_up(modifier=decimate.name)
            while obj.modifiers.find(decimate.name) > 0:
                bpy.ops.object.modifier_move_up(modifier=decimate.name)
            bpy.ops.object.modifier_apply(modifier=decimate.name)
        for face in obj.data.polygons:
            face.use_smooth = True
    frames = [a.frame_range[1] for a in bpy.data.actions]
    scene.frame_start = 1
    scene.frame_end = int(max(frames)) if frames else 60
    scene.render.fps = 30
    scene.frame_set(1)
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.export_scene.fbx(filepath=str(OUT / (name + "_walker.fbx")), use_selection=True,
        axis_forward='-Z', axis_up='Y', apply_unit_scale=True, apply_scale_options='FBX_SCALE_UNITS',
        object_types={'EMPTY', 'ARMATURE', 'MESH'}, add_leaf_bones=False, use_armature_deform_only=True,
        bake_anim=True, bake_anim_use_nla_strips=False, bake_anim_use_all_actions=False,
        bake_anim_simplify_factor=0.0, path_mode='RELATIVE')
    (OUT / (name + "_materials.json")).write_text(json.dumps({"materials": materials}, indent=2))
    triangles = sum(sum(len(p.vertices)-2 for p in obj.data.polygons) for obj in meshes)
    reports.append({"name": name, "triangles": triangles, "frames": scene.frame_end, "materials": len(materials)})
    # A source preview makes the model/animation conversion visually reviewable.
    bpy.ops.object.camera_add(location=(3.0, -5.2, 1.8))
    camera = bpy.context.object
    camera.rotation_euler = (Vector((0, 0, 1)) - camera.location).to_track_quat('-Z', 'Y').to_euler()
    scene.camera = camera
    camera.data.lens = 60
    bpy.ops.object.light_add(type='AREA', location=(2, -3, 5))
    bpy.context.object.data.energy = 450
    bpy.context.object.data.size = 4
    bpy.ops.object.light_add(type='AREA', location=(-3, 1, 4))
    bpy.context.object.data.energy = 280
    bpy.context.object.data.size = 3
    scene.world = bpy.data.worlds.new('Preview world')
    scene.world.use_nodes = True
    scene.world.node_tree.nodes['Background'].inputs['Color'].default_value = (.15, .18, .20, 1)
    scene.render.engine = 'CYCLES'
    scene.cycles.samples = 12
    scene.render.resolution_x, scene.render.resolution_y, scene.render.resolution_percentage = 500, 700, 100
    scene.render.filepath = str(ROOT / 'build/qa-refinement' / (name + '-preview.png'))
    bpy.ops.render.render(write_still=True)
    print('PEDESTRIAN_EXPORTED', reports[-1], flush=True)
(ROOT / 'build/qa-refinement/pedestrian-export.json').write_text(json.dumps(reports, indent=2))

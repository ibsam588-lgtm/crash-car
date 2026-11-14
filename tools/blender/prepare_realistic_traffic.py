"""Convert Yasutoshi Mori's CC BY 4.0 sports car into an efficient traffic mesh.

Run in Blender from the repository root after downloading the artist's FBX with
tools/fetch_traffic_asset.py. All geometry comes from the credited source.
"""
import json
import math
from pathlib import Path

import bpy
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / "build/traffic-home/mori_sportscar_source.fbx"
DEST = ROOT / "unity/CarCrashUnityPrototype/Assets/CarCrash/Models/traffic_gt_detailed.fbx"


def material(name, color, metal, rough):
    mat = bpy.data.materials.new("M_Fleet_" + name)
    mat.diffuse_color = (*color, 1)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = (*color, 1)
    bsdf.inputs["Metallic"].default_value = metal
    bsdf.inputs["Roughness"].default_value = rough
    return mat


def role(name):
    name = name.lower()
    if name == "bodymat_phong": return "Paint"
    if "tiremat" in name or "gomblack" in name: return "Tire"
    if "windowglass" in name or name == "glassmat_phong": return "Glass"
    if "stoplightred" in name or "stoplightcover" in name: return "Taillamp"
    if "headlight_led" in name or "headlightlens" in name: return "Headlamp"
    if "wheelhub" in name or "suspention_red" in name: return "Caliper"
    if any(part in name for part in ("silver", "wheel2mat", "goldmat", "lightreflec", "mirrormat")): return "Alloy"
    if any(part in name for part in ("interior", "seat")): return "Interior"
    return "Trim"


def main():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    bpy.ops.import_scene.fbx(filepath=str(SOURCE))
    sources = [o for o in bpy.context.scene.objects if o.type == "MESH"]
    mats = {
        "Paint": material("Paint", (.18, .25, .34), .45, .24),
        "Tire": material("Tire", (.025, .028, .032), 0, .72),
        "Glass": material("Glass", (.065, .105, .135), .25, .08),
        "Taillamp": material("Taillamp", (.65, .008, .012), .1, .2),
        "Headlamp": material("Headlamp", (.8, .9, 1), .25, .16),
        "Caliper": material("Caliper", (.55, .025, .015), .2, .3),
        "Alloy": material("Alloy", (.5, .53, .56), .85, .25),
        "Interior": material("Interior", (.05, .045, .04), .05, .65),
        "Trim": material("Trim", (.035, .04, .045), .2, .4),
    }
    bpy.context.view_layer.update()
    root = bpy.data.objects.new("DetailedTrafficGT", None)
    bpy.context.collection.objects.link(root)
    wheel_centres = {"FL": Vector((-.009, -.012, .0032)), "FR": Vector((.009, -.012, .0032)),
                     "RL": Vector((-.0095, .017, .0034)), "RR": Vector((.0095, .017, .0034))}
    groups = {}
    scale = 108.0
    for source in sources:
        points = [source.matrix_world @ Vector(c) for c in source.bound_box]
        if not all(math.isfinite(v) for p in points for v in p): continue
        low = Vector(tuple(min(p[i] for p in points) for i in range(3)))
        high = Vector(tuple(max(p[i] for p in points) for i in range(3)))
        centre = (low + high) * .5
        # Omit invisible engine internals and invalid helper geometry in the source FBX.
        if max(high - low) > .05 or abs(centre.x) > .014 or abs(centre.y) > .026: continue
        if high.z > .0126 or low.z < -.001 or max(high - low) < .00035: continue
        if source.name.startswith(("Blocks", "Cylinder", "Crank", "Piston")): continue
        names = [m.name for m in source.data.materials if m]
        if not names: continue
        main_role = role(names[0])
        wheel = ""
        if any(any(k in n.lower() for k in ("tiremat", "wheel2mat", "brakerotar", "wheelhub")) for n in names):
            nearest = min(wheel_centres, key=lambda key: (centre - wheel_centres[key]).length)
            # Some engine seals share the tire material. Only geometry physically
            # near an axle may rotate with that wheel.
            if (centre - wheel_centres[nearest]).length < .004: wheel = nearest
        # The source is a subdivision cage; refine the visible curved body and glass.
        if main_role in ("Paint", "Glass") and len(source.data.vertices) < 4000:
            sub = source.modifiers.new("Traffic body surface", "SUBSURF")
            sub.levels = sub.render_levels = 1
        evaluated = source.evaluated_get(bpy.context.evaluated_depsgraph_get())
        mesh = bpy.data.meshes.new_from_object(evaluated)
        for vertex in mesh.vertices:
            point = source.matrix_world @ vertex.co
            vertex.co = Vector((point.x * scale, (point.y - .003) * scale, point.z * scale + .015))
        mesh.materials.clear()
        for name in names: mesh.materials.append(mats[role(name)])
        for polygon in mesh.polygons: polygon.use_smooth = True
        obj = bpy.data.objects.new("Traffic_" + source.name, mesh)
        bpy.context.collection.objects.link(obj)
        groups.setdefault(wheel or "Body", []).append(obj)

    for source in sources: bpy.data.objects.remove(source, do_unlink=True)
    # Remove old transform empties and retain the newly baked meshes only.
    for obj in list(bpy.context.scene.objects):
        if obj.type != "MESH" and obj != root: bpy.data.objects.remove(obj, do_unlink=True)
    for group, objects in groups.items():
        bpy.ops.object.select_all(action="DESELECT")
        for obj in objects: obj.select_set(True)
        bpy.context.view_layer.objects.active = objects[0]
        bpy.ops.object.join()
        joined = bpy.context.object
        joined.name = "FleetBody" if group == "Body" else "Wheel_" + group
        reduce = joined.modifiers.new("Traffic mesh budget", "DECIMATE")
        reduce.ratio = .30 if group == "Body" else .48
        bpy.context.view_layer.objects.active = joined
        bpy.ops.object.modifier_apply(modifier=reduce.name)
        if group != "Body":
            centre = wheel_centres[group]
            bpy.context.scene.cursor.location = (centre.x * scale, (centre.y - .003) * scale, centre.z * scale + .015)
            bpy.ops.object.origin_set(type="ORIGIN_CURSOR")
        joined.parent = root
        # Consolidate duplicate material slots after joining the source components.
        slots = []
        remap = {}
        for index, slot in enumerate(joined.data.materials):
            if slot not in slots: slots.append(slot)
            remap[index] = slots.index(slot)
        indices = [remap[p.material_index] for p in joined.data.polygons]
        joined.data.materials.clear()
        for slot in slots: joined.data.materials.append(slot)
        for poly, index in zip(joined.data.polygons, indices): poly.material_index = index
    bpy.context.scene.cursor.location = (0, 0, 0)
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.export_scene.fbx(filepath=str(DEST), use_selection=True, apply_unit_scale=True,
                             apply_scale_options="FBX_SCALE_UNITS", axis_forward="-Z", axis_up="Y",
                             object_types={"EMPTY", "MESH"}, bake_anim=False, add_leaf_bones=False)
    triangles = sum(len(p.vertices) - 2 for o in root.children if o.type == "MESH" for p in o.data.polygons)
    report = {"triangles": triangles, "renderMeshes": len(root.children), "source": str(SOURCE.name)}
    (ROOT / "build/traffic-home/traffic-export.json").write_text(json.dumps(report, indent=2))
    print("DETAILED_TRAFFIC_EXPORTED", report)
    # A studio image makes the conversion reviewable before opening Unity.
    bpy.ops.object.light_add(type="AREA", location=(3, -4, 7))
    bpy.context.object.data.energy = 1400
    bpy.context.object.data.shape = "DISK"
    bpy.context.object.data.size = 5
    bpy.ops.object.light_add(type="AREA", location=(-4, 1, 4))
    bpy.context.object.data.energy = 900
    bpy.context.object.data.size = 5
    bpy.ops.object.camera_add(location=(6.8, -8, 3.9))
    camera = bpy.context.object
    camera.rotation_euler = (Vector((0, 0, .6)) - camera.location).to_track_quat('-Z', 'Y').to_euler()
    camera.data.lens = 48
    scene = bpy.context.scene
    scene.camera = camera
    scene.render.engine = "CYCLES"
    scene.cycles.samples = 16
    scene.world.color = (.18, .18, .18)
    scene.render.resolution_x, scene.render.resolution_y, scene.render.resolution_percentage = 1000, 650, 100
    scene.render.filepath = str(ROOT / "build/traffic-home/traffic-model-preview.png")
    bpy.ops.render.render(write_still=True)


if __name__ == "__main__": main()

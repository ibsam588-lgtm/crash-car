"""Bake low-detail seated drivers from the repository's CC0 walker models."""

from math import radians, sqrt
from pathlib import Path
import re
from uuid import uuid4

import bmesh
import bpy
from mathutils import Matrix, Quaternion, Vector


ROOT = Path(__file__).resolve().parents[2]
ASSETS = ROOT / "unity/CarCrashUnityPrototype/Assets/CarCrash"
WALKERS = ASSETS / "Models/Pedestrians"
OUTPUT = ASSETS / "Resources/CabinDrivers"


def aim(armature, joint, child, direction):
    bone = armature.pose.bones[joint]
    line = child.head - bone.head if child else bone.tail - bone.head
    rotation = line.rotation_difference(Vector(direction))
    bone.matrix = Matrix.Translation(bone.head) @ rotation.to_matrix().to_4x4() @ bone.matrix.to_3x3().to_4x4()
    bpy.context.view_layer.update()


def point(armature, world_position):
    return armature.matrix_world.inverted() @ Vector(world_position)


def pose_driver(armature):
    bones = armature.pose.bones
    armature.animation_data_clear()
    for bone in bones:
        bone.matrix_basis = Matrix.Identity(4)
    bpy.context.view_layer.update()

    # The source faces -Y. Recline the upper body without tilting the gaze up.
    recline = Quaternion((1, 0, 0), radians(-10))
    spine = bones["spine_01"]
    neck = bones["neck_01"]
    aim(armature, spine.name, None, recline @ (spine.tail - spine.head))
    aim(armature, neck.name, None, recline.inverted() @ (neck.tail - neck.head))
    head_center = armature.matrix_world @ (bones["head"].head + (bones["head"].tail - bones["head"].head) * .66)

    for side, sign in (("l", 1), ("r", -1)):
        thigh = bones[f"thigh_{side}"]
        calf = bones[f"calf_{side}"]
        shoulder = bones[f"upperarm_{side}"]
        forearm = bones[f"lowerarm_{side}"]
        hand = bones[f"hand_{side}"]

        # Slightly splayed thighs and knees below seat level fit the footwell.
        aim(armature, thigh.name, calf, (sign * .09, -.96, -.14))
        aim(armature, calf.name, None, (-sign * .12, -.62, -.78))

        # Solve the two arm segments with the elbow below and outside the wrist.
        wrist = point(armature, head_center + Vector((sign * .152, -.285, -.385)))
        upper_length = (forearm.head - shoulder.head).length
        lower_length = (hand.head - forearm.head).length
        reach = wrist - shoulder.head
        distance = reach.length
        direction = reach.normalized()
        pole = Vector((sign * .35, .05, -1))
        bend = (pole - direction * pole.dot(direction)).normalized()
        along = (upper_length ** 2 - lower_length ** 2 + distance ** 2) / (2 * distance)
        elbow = shoulder.head + direction * along + bend * sqrt(max(0, upper_length ** 2 - along ** 2))
        aim(armature, shoulder.name, forearm, elbow - shoulder.head)
        aim(armature, forearm.name, hand, wrist - forearm.head)
        aim(armature, hand.name, bones[f"middle_01_{side}"], (-sign * .34, -.69, .64))
        for finger in ("index", "middle", "ring", "pinky"):
            for joint, angle in ((1, 17), (2, 22), (3, 12)):
                digit = bones[f"{finger}_{joint:02d}_{side}"]
                axis = (digit.tail - digit.head).cross(Vector((-sign, .1, -.18)))
                if axis.length_squared > .0000001:
                    direction = Quaternion(axis, radians(angle)) @ (digit.tail - digit.head)
                    aim(armature, digit.name, None, direction)

    return head_center


def bake_mesh(source, dependency_graph, head_center):
    evaluated = source.evaluated_get(dependency_graph)
    data = bpy.data.meshes.new_from_object(evaluated, depsgraph=dependency_graph)
    data.transform(source.matrix_world)
    data.transform(Matrix.Translation(-head_center))
    data.transform(Matrix.Diagonal((.93, .93, .93, 1)))
    model = bpy.data.objects.new(source.name + " seated", data)
    bpy.context.collection.objects.link(model)
    return model


def trim_mesh(model):
    # The FBX has split triangle vertices along exported normals and UVs.
    # Join coincident positions before simplifying so the silhouette stays connected.
    mesh = bmesh.new()
    mesh.from_mesh(model.data)
    bmesh.ops.remove_doubles(mesh, verts=list(mesh.verts), dist=.000075)
    is_skin = model.name.endswith("anatomical body seated")
    if is_skin:
        covered = [vertex for vertex in mesh.verts if vertex.co.z < -.25 and
                   (vertex.co.y > -.25 or vertex.co.z < -.54)]
        bmesh.ops.delete(mesh, geom=covered, context="VERTS")
    bmesh.ops.bisect_plane(mesh, geom=list(mesh.verts) + list(mesh.edges) + list(mesh.faces), dist=.0001,
                         plane_co=(0, 0, -.79), plane_no=(0, 0, 1), clear_inner=True)
    mesh.to_mesh(model.data)
    mesh.free()
    triangles = sum(len(face.vertices) - 2 for face in model.data.polygons)
    face_budget = 600
    # The source already has dense features baked into the face and hand silhouette.
    # A second collapse pass warps those UV islands, so keep their authored surfaces.
    if is_skin or triangles <= face_budget:
        return
    decimate = model.modifiers.new("Cabin mesh budget", "DECIMATE")
    decimate.ratio = face_budget / triangles
    bpy.context.view_layer.objects.active = model
    model.select_set(True)
    bpy.ops.object.modifier_apply(modifier=decimate.name)
    model.select_set(False)


def importer_settings(target, material_names):
    destination = Path(str(target) + ".meta")
    guid = uuid4().hex
    if destination.exists():
        guid = re.search(r"^guid: ([a-f0-9]+)", destination.read_text(), re.MULTILINE)[1]
    remaps = []
    for name in material_names:
        material = ASSETS / "Materials/Pedestrians" / (name + ".mat.meta")
        material_guid = re.search(r"^guid: ([a-f0-9]+)", material.read_text(), re.MULTILINE)[1]
        remaps.append(f"  - first:\n      type: UnityEngine:Material\n      assembly: UnityEngine.CoreModule\n"
                      f"      name: {name}\n    second: {{fileID: 2100000, guid: {material_guid}, type: 2}}")
    source_settings = (WALKERS / "sam_walker.fbx.meta").read_text()
    settings = re.sub(r"^guid: [a-f0-9]+", "guid: " + guid, source_settings, count=1, flags=re.MULTILINE)
    settings = settings.replace("  externalObjects: {}", "  externalObjects:\n" + "\n".join(remaps))
    settings = settings.replace("  importAnimation: 1", "  importAnimation: 0").replace("  animationType: 1", "  animationType: 0")
    settings = settings.replace("    importBlendShapes: 1", "    importBlendShapes: 0")
    destination.write_text("\n".join(line.rstrip() for line in settings.splitlines()) + "\n", encoding="utf-8")


def bake(character):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(WALKERS / (character.lower() + "_walker.fbx")))
    bpy.ops.object.select_all(action="DESELECT")
    scene = bpy.context.scene
    armature = next(obj for obj in scene.objects if obj.type == "ARMATURE")
    head_center = pose_driver(armature)
    graph = bpy.context.evaluated_depsgraph_get()
    posed = []
    for source in list(scene.objects):
        if source.type != "MESH" or source.name.lower().startswith(("phone", "original skinned ankle", "teeth_base", "shoes")):
            continue
        model = bake_mesh(source, graph, head_center)
        trim_mesh(model)
        posed.append(model)
    for model in posed:
        model.select_set(True)
    bpy.context.view_layer.objects.active = posed[0]
    bpy.ops.object.join()
    driver = bpy.context.object
    driver.name = character
    driver.data.name = "Seated " + character
    output = OUTPUT / (character + ".fbx")
    bpy.ops.export_scene.fbx(filepath=str(output), use_selection=True, object_types={"MESH"},
        axis_forward="-Z", axis_up="Y", apply_unit_scale=True, apply_scale_options="FBX_SCALE_UNITS",
        bake_anim=False, path_mode="STRIP")
    importer_settings(output, sorted({material.name for material in driver.data.materials}))
    print(f"CABIN_DRIVER_BAKED {character} triangles={sum(len(face.vertices) - 2 for face in driver.data.polygons)} "
          f"materials={len(driver.data.materials)} bytes={output.stat().st_size}", flush=True)


OUTPUT.mkdir(parents=True, exist_ok=True)
bake("Sam")

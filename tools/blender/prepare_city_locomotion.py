"""Bake rig-only Unity FBXs from the CC0 originals recorded in Pedestrians/sources.json.

Place maya.glb, sam.glb and harper.glb in build/coastal-motion-source before running
this with Blender's --background --python options. The player reuses the existing
walkers and their textures while these carriers supply the three extra clips.
"""
from pathlib import Path
import bpy


ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / "build/coastal-motion-source"
OUTPUT = ROOT / "unity/CarCrashUnityPrototype/Assets/CarCrash/Resources/PedestrianMotion"
CLIPS = ("Idle_Standing", "Jog", "Run")
OUTPUT.mkdir(parents=True, exist_ok=True)


def bake(character):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    scene.render.fps = 30
    bpy.ops.import_scene.gltf(filepath=str(SOURCE / (character + ".glb")))
    armature = next(obj for obj in scene.objects if obj.type == "ARMATURE")
    for mesh in list(scene.objects):
        if mesh.type == "MESH":
            bpy.data.objects.remove(mesh, do_unlink=True)

    # Export a minimal weighted triangle so the FBX import retains its bone hierarchy.
    mesh = bpy.data.meshes.new("Motion carrier")
    mesh.from_pydata([(0, 0, 0), (.01, 0, 0), (0, .01, 0)], [], [(0, 1, 2)])
    carrier = bpy.data.objects.new("Motion carrier", mesh)
    scene.collection.objects.link(carrier)
    carrier.parent = armature
    carrier.vertex_groups.new(name="pelvis").add([0, 1, 2], 1.0, "REPLACE")
    carrier.modifiers.new("Motion rig", "ARMATURE").object = armature

    animation = armature.animation_data
    for track in list(animation.nla_tracks):
        animation.nla_tracks.remove(track)
    for clip in CLIPS:
        action = bpy.data.actions[clip]
        animation.action = action
        animation.action_slot = action.slots[0]
        scene.frame_start = 0
        scene.frame_end = round(action.frame_range[1])
        scene.frame_set(0)
        bpy.ops.object.select_all(action="SELECT")
        # Match prepare_city_pedestrians.py's axis, unit and rig export options.
        bpy.ops.export_scene.fbx(
            filepath=str(OUTPUT / f"{character}-{clip.lower()}.fbx"), use_selection=True,
            axis_forward="-Z", axis_up="Y", apply_unit_scale=True, apply_scale_options="FBX_SCALE_UNITS",
            object_types={"EMPTY", "ARMATURE", "MESH"}, add_leaf_bones=False,
            use_armature_deform_only=True, bake_anim=True, bake_anim_use_nla_strips=False,
            bake_anim_use_all_actions=False, bake_anim_simplify_factor=1.0, path_mode="STRIP",
        )


for character in ("maya", "sam", "harper"):
    bake(character)

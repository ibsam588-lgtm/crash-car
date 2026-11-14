"""Prepare the licensed CarConcept mesh for Unity, retaining its detailed body and cockpit."""
import json
import math
import struct
from pathlib import Path

import bpy
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
MODEL_DIR = ROOT / "unity/CarCrashUnityPrototype/Assets/CarCrash/Models"
TEXTURE_DIR = MODEL_DIR.parent / "Textures/Roadbreaker"
SOURCE = MODEL_DIR / "car_crash_concept_base.glb"


def extract_materials():
    data = SOURCE.read_bytes()
    size = struct.unpack_from("<I", data, 12)[0]
    gltf = json.loads(data[20:20 + size])
    binary_offset = 20 + size + 8
    TEXTURE_DIR.mkdir(parents=True, exist_ok=True)
    for i, image in enumerate(gltf.get("images", [])):
        view = gltf["bufferViews"][image["bufferView"]]
        start = binary_offset + view.get("byteOffset", 0)
        (TEXTURE_DIR / f"source_{i:02}.png").write_bytes(data[start:start + view["byteLength"]])

    def texture_index(info):
        return -1 if not info else gltf["textures"][info["index"]]["source"]

    materials = []
    for i, material in enumerate(gltf.get("materials", [])):
        pbr = material.get("pbrMetallicRoughness", {})
        materials.append({
            "name": material.get("name", "Material"),
            "color": pbr.get("baseColorFactor", [1, 1, 1, 1]),
            "metallic": pbr.get("metallicFactor", 1),
            "roughness": pbr.get("roughnessFactor", 1),
            "albedo": texture_index(pbr.get("baseColorTexture")),
            "normal": texture_index(material.get("normalTexture")),
            "normalStrength": material.get("normalTexture", {}).get("scale", 1),
            "normalTiling": material.get("normalTexture", {}).get("extensions", {}).get("KHR_texture_transform", {}).get("scale", [1, 1]),
            "metalRough": texture_index(pbr.get("metallicRoughnessTexture")),
            "occlusion": texture_index(material.get("occlusionTexture")),
            "occlusionStrength": material.get("occlusionTexture", {}).get("strength", 1) if material.get("occlusionTexture", {}).get("texCoord", 0) == 0 else 0,
            "emission": texture_index(material.get("emissiveTexture")),
            "emissionColor": material.get("emissiveFactor", [0, 0, 0]),
        })
    (TEXTURE_DIR / "materials.json").write_text(json.dumps({"materials": materials}, indent=2))


def main():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    bpy.ops.import_scene.gltf(filepath=str(SOURCE))
    # Remove source-brand markings while keeping all body, tire, brake and cockpit geometry.
    for name in ("License Plate", "InteriorSteeringEmblem"):
        obj = bpy.data.objects.get(name)
        if obj:
            bpy.data.objects.remove(obj, do_unlink=True)
    wheel_names = {"WheelFrontR": "Wheel_FL", "WheelFrontL": "Wheel_FR",
                   "WheelRearR": "Wheel_RL", "WheelRearL": "Wheel_RR"}
    for source_name, target_name in wheel_names.items():
        obj = bpy.data.objects.get(source_name)
        if obj is None:
            raise RuntimeError(f"Missing wheel root: {source_name}")
        obj.name = target_name
        if "Front" in source_name:
            rotation = obj.rotation_quaternion.to_euler("XYZ")
            # Steering is local Y inside the imported glTF parent. Keep the
            # source axle orientation and wheel spin when returning it to centre.
            rotation.y = 0
            obj.rotation_mode = "XYZ"
            obj.rotation_euler = rotation

    # Tag detachable body panels without destroying the source shape.
    for name in ("BodyHood", "BodyDoorLColor1", "BodyDoorRColor1", "BodyRearPanelsColor1"):
        obj = bpy.data.objects.get(name)
        if obj:
            obj.name = "Damage_" + name

    extract_materials()
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.export_scene.fbx(
        filepath=str(MODEL_DIR / "roadbreaker_realistic.fbx"), use_selection=True,
        object_types={"EMPTY", "MESH"}, use_mesh_modifiers=True, mesh_smooth_type="FACE",
        add_leaf_bones=False, bake_anim=False, axis_forward="-Z", axis_up="Y",
        apply_unit_scale=True, apply_scale_options="FBX_SCALE_UNITS",
        use_space_transform=True, bake_space_transform=False,
        path_mode="STRIP", embed_textures=False,
    )
    report = {"meshObjects": len([o for o in bpy.context.scene.objects if o.type == "MESH"]),
              "wheels": {name: list(bpy.data.objects[name].matrix_world.translation) for name in wheel_names.values()}}
    (ROOT / "build/realism-upgrade/vehicle-export.json").write_text(json.dumps(report, indent=2))
    print("REALISTIC_ROADBREAKER_EXPORTED", report)


if __name__ == "__main__":
    main()

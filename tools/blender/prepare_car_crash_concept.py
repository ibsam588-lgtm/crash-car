import math
from pathlib import Path

import bpy


ROOT = Path(__file__).resolve().parents[2]
MODEL_DIR = ROOT / "unity" / "CarCrashUnityPrototype" / "Assets" / "CarCrash" / "Models"
PREVIEW_DIR = ROOT / "assets" / "images" / "concepts" / "car_crash_gt650"
SOURCE = MODEL_DIR / "car_crash_concept_base.glb"
BLEND_OUT = MODEL_DIR / "car_crash_gt650.blend"
FBX_OUT = MODEL_DIR / "car_crash_gt650.fbx"
PREVIEW_OUT = PREVIEW_DIR / "car_crash_gt650_preview.png"


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)


def tune_principled(material, color, metallic, roughness, emission=None):
    material.use_nodes = True
    nodes = material.node_tree.nodes
    links = material.node_tree.links
    bsdf = nodes.get("Principled BSDF")
    if bsdf is None:
        return

    for name in ("Base Color", "Metallic", "Roughness"):
        socket = bsdf.inputs.get(name)
        if socket is not None:
            for link in list(socket.links):
                links.remove(link)

    bsdf.inputs["Base Color"].default_value = color
    bsdf.inputs["Metallic"].default_value = metallic
    bsdf.inputs["Roughness"].default_value = roughness
    if emission is not None and "Emission Color" in bsdf.inputs:
        bsdf.inputs["Emission Color"].default_value = emission
        bsdf.inputs["Emission Strength"].default_value = 4.0
    material.diffuse_color = color


def new_material(name, color, metallic=0.0, roughness=0.4, emission=None):
    material = bpy.data.materials.new(name)
    tune_principled(material, color, metallic, roughness, emission)
    return material


def add_box(name, location, dimensions, material, bevel=0.025):
    bpy.ops.mesh.primitive_cube_add(size=1, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = dimensions
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(material)
    bevel_modifier = obj.modifiers.new("production_bevel", "BEVEL")
    bevel_modifier.width = bevel
    bevel_modifier.segments = 3
    bpy.ops.object.shade_smooth()
    return obj


def add_curve(name, points, material, width=0.006):
    curve = bpy.data.curves.new(name, "CURVE")
    curve.dimensions = "3D"
    curve.bevel_depth = width
    curve.bevel_resolution = 2
    spline = curve.splines.new("POLY")
    spline.points.add(len(points) - 1)
    for point, coordinates in zip(spline.points, points):
        point.co = (*coordinates, 1.0)
    obj = bpy.data.objects.new(name, curve)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(material)
    return obj


def remove_brand_marks():
    for obj in list(bpy.context.scene.objects):
        if obj.name in {"License Plate", "InteriorSteeringEmblem"}:
            bpy.data.objects.remove(obj, do_unlink=True)


def rename_wheel_roots():
    mapping = {
        "WheelFrontL": "Wheel_FL",
        "WheelFrontR": "Wheel_FR",
        "WheelRearL": "Wheel_RL",
        "WheelRearR": "Wheel_RR",
    }
    for old_name, new_name in mapping.items():
        obj = bpy.data.objects.get(old_name)
        if obj is not None:
            obj.name = new_name


def tune_materials():
    competition_yellow = (0.96, 0.46, 0.015, 1.0)
    carbon_black = (0.008, 0.010, 0.013, 1.0)
    forged_black = (0.012, 0.014, 0.018, 1.0)
    brake_red = (0.86, 0.012, 0.008, 1.0)

    for material in bpy.data.materials:
        name = material.name
        if name.startswith("Paint 1"):
            tune_principled(material, competition_yellow, 0.72, 0.20)
        elif name.startswith("Paint 2") or name in {"Panel Sides", "Hardware"}:
            tune_principled(material, carbon_black, 0.22, 0.30)
        elif name in {"Rim1", "Rim2"}:
            tune_principled(material, forged_black, 0.78, 0.22)
        elif name in {"Tireside", "Tiretread", "Dots Stroke"}:
            tune_principled(material, (0.003, 0.004, 0.005, 1.0), 0.0, 0.82)
        elif name == "Brake":
            tune_principled(material, brake_red, 0.32, 0.20)
        elif name == "Brakelight":
            tune_principled(material, (0.7, 0.002, 0.001, 1.0), 0.0, 0.12, (1.0, 0.01, 0.0, 1.0))


def add_car_crash_identity():
    carbon = new_material("M_CarCrash_Carbon", (0.006, 0.008, 0.010, 1.0), metallic=0.25, roughness=0.28)
    crack = new_material(
        "M_CarCrash_GlassCrack",
        (0.70, 0.90, 1.0, 1.0),
        metallic=0.0,
        roughness=0.18,
        emission=(0.34, 0.72, 1.0, 1.0),
    )

    parts = [
        add_box("Damage_Active_Rear_Wing", (0.0, 1.86, 1.02), (1.82, 0.28, 0.075), carbon, 0.022),
        add_box("Rear_Wing_Post_L", (-0.55, 1.75, 0.82), (0.07, 0.11, 0.40), carbon, 0.016),
        add_box("Rear_Wing_Post_R", (0.55, 1.75, 0.82), (0.07, 0.11, 0.40), carbon, 0.016),
    ]

    body = bpy.data.objects.get("BodyUnderside")
    for part in parts:
        part.parent = body
        part.matrix_parent_inverse = body.matrix_world.inverted()

    crack_lines = [
        [(-0.16, -0.86, 1.02), (-0.07, -0.76, 1.10), (0.02, -0.66, 1.16)],
        [(-0.07, -0.76, 1.10), (-0.28, -0.70, 1.04)],
        [(-0.07, -0.76, 1.10), (0.12, -0.73, 1.08), (0.29, -0.70, 1.03)],
        [(0.02, -0.66, 1.16), (0.17, -0.58, 1.20)],
    ]
    for index, points in enumerate(crack_lines):
        crack_obj = add_curve(f"Damage_Windshield_Crack_{index:02d}", points, crack, 0.004)
        crack_obj.parent = body
        crack_obj.matrix_parent_inverse = body.matrix_world.inverted()
        crack_obj.hide_render = True


def add_preview_scene():
    floor = new_material("M_Preview_Floor", (0.025, 0.028, 0.032, 1.0), metallic=0.0, roughness=0.32)
    bpy.ops.mesh.primitive_plane_add(size=40, location=(0.0, 0.0, -0.78))
    plane = bpy.context.object
    plane.name = "Preview_Floor"
    plane.data.materials.append(floor)

    bpy.ops.object.light_add(type="AREA", location=(-4.5, -3.8, 5.2))
    key = bpy.context.object
    key.data.energy = 1150
    key.data.shape = "DISK"
    key.data.size = 5.5
    key.data.color = (1.0, 0.80, 0.58)

    bpy.ops.object.light_add(type="AREA", location=(4.2, 4.4, 2.4))
    fill = bpy.context.object
    fill.data.energy = 760
    fill.data.size = 4.0
    fill.data.color = (0.40, 0.64, 1.0)

    bpy.ops.object.light_add(type="POINT", location=(0.0, 3.6, 1.4))
    rear = bpy.context.object
    rear.data.energy = 380
    rear.data.color = (1.0, 0.16, 0.04)

    bpy.ops.object.camera_add(location=(4.7, 7.2, 2.15))
    camera = bpy.context.object
    camera.data.lens = 58
    bpy.context.scene.camera = camera

    bpy.ops.object.empty_add(type="PLAIN_AXES", location=(0.0, 0.35, 0.35))
    target = bpy.context.object
    constraint = camera.constraints.new(type="TRACK_TO")
    constraint.track_axis = "TRACK_NEGATIVE_Z"
    constraint.up_axis = "UP_Y"
    constraint.target = target


def convert_curves_for_export():
    for obj in list(bpy.context.scene.objects):
        if obj.type != "CURVE":
            continue
        bpy.ops.object.select_all(action="DESELECT")
        obj.select_set(True)
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.convert(target="MESH")


def export_game_model():
    PREVIEW_DIR.mkdir(parents=True, exist_ok=True)
    MODEL_DIR.mkdir(parents=True, exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(BLEND_OUT))

    convert_curves_for_export()
    bpy.ops.object.select_all(action="DESELECT")
    for obj in bpy.context.scene.objects:
        if obj.name.startswith("Preview_") or obj.type in {"CAMERA", "LIGHT"}:
            continue
        if obj.type in {"EMPTY", "MESH"}:
            obj.select_set(True)

    bpy.ops.export_scene.fbx(
        filepath=str(FBX_OUT),
        use_selection=True,
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_UNITS",
        object_types={"EMPTY", "MESH"},
        add_leaf_bones=False,
        use_mesh_modifiers=True,
        path_mode="COPY",
        embed_textures=True,
    )


def render_preview():
    try:
        bpy.context.scene.render.engine = "BLENDER_EEVEE_NEXT"
    except TypeError:
        bpy.context.scene.render.engine = "BLENDER_EEVEE"
    bpy.context.scene.render.resolution_x = 1600
    bpy.context.scene.render.resolution_y = 1200
    bpy.context.scene.render.resolution_percentage = 100
    bpy.context.scene.render.image_settings.file_format = "PNG"
    bpy.context.scene.render.film_transparent = False
    bpy.context.scene.render.filepath = str(PREVIEW_OUT)
    bpy.context.scene.world.color = (0.012, 0.016, 0.022)
    bpy.context.scene.view_settings.look = "AgX - Medium High Contrast"
    bpy.ops.render.render(write_still=True)


def main():
    clear_scene()
    bpy.ops.import_scene.gltf(filepath=str(SOURCE))
    remove_brand_marks()
    rename_wheel_roots()
    tune_materials()
    add_car_crash_identity()
    add_preview_scene()
    export_game_model()
    render_preview()


if __name__ == "__main__":
    main()

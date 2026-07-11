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
    for data_collection in (bpy.data.meshes, bpy.data.curves, bpy.data.materials, bpy.data.cameras, bpy.data.lights):
        for block in list(data_collection):
            if block.users == 0:
                data_collection.remove(block)


def tune_principled(material, color, metallic=0.0, roughness=0.4, emission=None):
    material.use_nodes = True
    bsdf = material.node_tree.nodes.get("Principled BSDF")
    if bsdf is None:
        return

    for socket_name in ("Base Color", "Metallic", "Roughness"):
        socket = bsdf.inputs.get(socket_name)
        if socket is None:
            continue
        for link in list(socket.links):
            material.node_tree.links.remove(link)

    bsdf.inputs["Base Color"].default_value = color
    bsdf.inputs["Metallic"].default_value = metallic
    bsdf.inputs["Roughness"].default_value = roughness
    if emission is not None and "Emission Color" in bsdf.inputs:
        bsdf.inputs["Emission Color"].default_value = emission
        bsdf.inputs["Emission Strength"].default_value = 3.0
    material.diffuse_color = color


def new_material(name, color, metallic=0.0, roughness=0.4, emission=None):
    material = bpy.data.materials.new(name)
    tune_principled(material, color, metallic, roughness, emission)
    return material


def add_bevel(obj, width=0.025, segments=3):
    if width <= 0.0:
        return
    bevel = obj.modifiers.new("production_bevel", "BEVEL")
    bevel.width = width
    bevel.segments = segments


def add_box(name, location, dimensions, material, bevel=0.025, rotation=(0.0, 0.0, 0.0), smooth=True):
    bpy.ops.mesh.primitive_cube_add(size=1, location=location, rotation=rotation)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = dimensions
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(material)
    add_bevel(obj, bevel)
    if smooth:
        for polygon in obj.data.polygons:
            polygon.use_smooth = True
    return obj


def add_cylinder(name, location, radius, depth, material, rotation=(0.0, 0.0, 0.0), vertices=48, bevel=0.012):
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=vertices,
        radius=radius,
        depth=depth,
        location=location,
        rotation=rotation,
    )
    obj = bpy.context.object
    obj.name = name
    obj.data.materials.append(material)
    add_bevel(obj, bevel)
    for polygon in obj.data.polygons:
        polygon.use_smooth = True
    return obj


def add_torus(name, location, major_radius, minor_radius, material, rotation=(0.0, 0.0, 0.0)):
    bpy.ops.mesh.primitive_torus_add(
        major_radius=major_radius,
        minor_radius=minor_radius,
        major_segments=64,
        minor_segments=18,
        location=location,
        rotation=rotation,
    )
    obj = bpy.context.object
    obj.name = name
    obj.data.materials.append(material)
    for polygon in obj.data.polygons:
        polygon.use_smooth = True
    return obj


def add_curve(name, points, material, width=0.006, cyclic=False):
    curve = bpy.data.curves.new(name, "CURVE")
    curve.dimensions = "3D"
    curve.bevel_depth = width
    curve.bevel_resolution = 3
    spline = curve.splines.new("POLY")
    spline.points.add(len(points) - 1)
    for point, coordinates in zip(spline.points, points):
        point.co = (*coordinates, 1.0)
    spline.use_cyclic_u = cyclic
    obj = bpy.data.objects.new(name, curve)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(material)
    return obj


def add_mesh(name, vertices, faces, material, bevel=0.0, smooth=True):
    mesh = bpy.data.meshes.new(f"{name}_Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(material)
    add_bevel(obj, bevel)
    if smooth:
        for polygon in obj.data.polygons:
            polygon.use_smooth = True
    return obj


def parent_keep_world(obj, parent):
    obj.parent = parent
    obj.matrix_parent_inverse = parent.matrix_world.inverted()


def make_materials():
    return {
        "paint": new_material("M_Muscle_Competition_Orange", (1.0, 0.30, 0.018, 1.0), 0.68, 0.21),
        "paint_dark": new_material("M_Muscle_Orange_Shadow", (0.48, 0.055, 0.006, 1.0), 0.62, 0.25),
        "stripe": new_material("M_Muscle_Satin_Black", (0.006, 0.008, 0.010, 1.0), 0.20, 0.26),
        "rubber": new_material("M_Muscle_Tire", (0.004, 0.005, 0.006, 1.0), 0.0, 0.80),
        "wheel": new_material("M_Muscle_Forged_Wheel", (0.018, 0.021, 0.025, 1.0), 0.88, 0.20),
        "wheel_edge": new_material("M_Muscle_Wheel_Edge", (0.21, 0.23, 0.25, 1.0), 0.92, 0.17),
        "brake": new_material("M_Muscle_Brake_Red", (0.72, 0.006, 0.003, 1.0), 0.30, 0.22),
        "rotor": new_material("M_Muscle_Brake_Rotor", (0.20, 0.22, 0.24, 1.0), 0.86, 0.29),
        "glass": new_material("M_Muscle_Smoked_Glass", (0.012, 0.035, 0.050, 1.0), 0.40, 0.12),
        "chrome": new_material("M_Muscle_Dark_Chrome", (0.18, 0.20, 0.22, 1.0), 0.94, 0.16),
        "grille": new_material("M_Muscle_Grille", (0.005, 0.006, 0.007, 1.0), 0.32, 0.34),
        "interior": new_material("M_Muscle_Interior", (0.015, 0.017, 0.020, 1.0), 0.05, 0.52),
        "plate": new_material("M_Muscle_Plate", (0.58, 0.60, 0.56, 1.0), 0.12, 0.40),
        "headlight": new_material(
            "M_Muscle_Headlight",
            (0.70, 0.86, 1.0, 1.0),
            0.06,
            0.09,
            (0.60, 0.83, 1.0, 1.0),
        ),
        "tail": new_material(
            "M_Muscle_Tail_LED",
            (0.62, 0.002, 0.001, 1.0),
            0.05,
            0.10,
            (1.0, 0.005, 0.001, 1.0),
        ),
        "amber": new_material(
            "M_Muscle_Amber_LED",
            (0.92, 0.17, 0.002, 1.0),
            0.04,
            0.11,
            (1.0, 0.16, 0.001, 1.0),
        ),
        "crack": new_material(
            "M_Muscle_Glass_Crack",
            (0.68, 0.90, 1.0, 1.0),
            0.0,
            0.18,
            (0.34, 0.76, 1.0, 1.0),
        ),
    }


def import_reference_wheels():
    if not SOURCE.exists():
        return {}

    bpy.ops.import_scene.gltf(filepath=str(SOURCE))
    source_names = {
        "WheelFrontL": ("Wheel_FL", (0.985, -1.49, 0.405)),
        "WheelFrontR": ("Wheel_FR", (-0.985, -1.49, 0.405)),
        "WheelRearL": ("Wheel_RL", (0.995, 1.31, 0.405)),
        "WheelRearR": ("Wheel_RR", (-0.995, 1.31, 0.405)),
    }
    roots = {}
    keep = set()
    for source_name, (target_name, target_location) in source_names.items():
        root = bpy.data.objects.get(source_name)
        if root is None:
            continue
        stack = [root]
        while stack:
            current = stack.pop()
            keep.add(current)
            stack.extend(list(current.children))

        world = root.matrix_world.copy()
        root.parent = None
        root.matrix_world = world
        root.name = target_name
        root.location = target_location
        root.rotation_mode = "XYZ"
        root.rotation_euler = (0.0, 0.0, 0.0)
        root.scale = (1.0, 1.0, 1.0)
        roots[target_name] = root

    for obj in list(bpy.context.scene.objects):
        if obj not in keep:
            bpy.data.objects.remove(obj, do_unlink=True)

    for material in bpy.data.materials:
        if material.name in {"Tireside", "Tiretread", "Dots Stroke"}:
            tune_principled(material, (0.003, 0.004, 0.005, 1.0), 0.0, 0.78)
        elif material.name in {"Rim1", "Rim2"}:
            tune_principled(material, (0.015, 0.018, 0.022, 1.0), 0.86, 0.20)
        elif material.name == "Brake":
            tune_principled(material, (0.72, 0.006, 0.003, 1.0), 0.32, 0.20)
    return roots


def create_body_shell(material):
    sections = [
        (-2.23, 0.18, 0.80, 0.86),
        (-2.02, 0.17, 0.90, 0.96),
        (-1.58, 0.16, 0.97, 1.00),
        (-0.88, 0.15, 1.01, 0.99),
        (-0.15, 0.15, 1.03, 0.96),
        (0.70, 0.15, 1.04, 0.97),
        (1.38, 0.16, 1.03, 1.00),
        (1.92, 0.17, 0.94, 0.97),
        (2.20, 0.19, 0.79, 0.88),
    ]
    ring_count = 10
    vertices = []
    for y, z_low, z_high, half_width in sections:
        vertices.extend(
            [
                (-half_width * 0.76, y, z_low),
                (-half_width * 0.94, y, z_low + 0.09),
                (-half_width, y, z_low + 0.29),
                (-half_width * 0.96, y, z_high - 0.09),
                (-half_width * 0.78, y, z_high),
                (half_width * 0.78, y, z_high),
                (half_width * 0.96, y, z_high - 0.09),
                (half_width, y, z_low + 0.29),
                (half_width * 0.94, y, z_low + 0.09),
                (half_width * 0.76, y, z_low),
            ]
        )

    faces = []
    for section_index in range(len(sections) - 1):
        start = section_index * ring_count
        next_start = (section_index + 1) * ring_count
        for ring_index in range(ring_count):
            following = (ring_index + 1) % ring_count
            faces.append((start + ring_index, start + following, next_start + following, next_start + ring_index))
    faces.append(tuple(reversed(range(ring_count))))
    last_start = (len(sections) - 1) * ring_count
    faces.append(tuple(last_start + index for index in range(ring_count)))
    return add_mesh("BodyUnderside", vertices, faces, material, bevel=0.045, smooth=True)


def create_side_glass(name, x, material, flip=False):
    vertices = [
        (x, -0.52, 1.02),
        (x, -0.17, 1.42),
        (x, 0.68, 1.44),
        (x, 1.08, 1.04),
    ]
    face = (3, 2, 1, 0) if flip else (0, 1, 2, 3)
    obj = add_mesh(name, vertices, [face], material, bevel=0.012, smooth=False)
    solidify = obj.modifiers.new("glass_thickness", "SOLIDIFY")
    solidify.thickness = 0.018
    return obj


def create_wheel(name, location, materials, parent, rear=False):
    root = bpy.data.objects.new(name, None)
    root.empty_display_type = "PLAIN_AXES"
    root.empty_display_size = 0.20
    root.location = location
    bpy.context.collection.objects.link(root)
    parent_keep_world(root, parent)

    width = 0.255 if rear else 0.225
    tire = add_torus(
        f"{name}_Tire",
        location,
        major_radius=0.315,
        minor_radius=0.090,
        material=materials["rubber"],
        rotation=(0.0, math.radians(90.0), 0.0),
    )
    rim = add_cylinder(
        f"{name}_Rim",
        location,
        0.260,
        width,
        materials["wheel_edge"],
        rotation=(0.0, math.radians(90.0), 0.0),
        vertices=64,
        bevel=0.010,
    )
    inset = add_cylinder(
        f"{name}_Rim_Inset",
        location,
        0.228,
        width + 0.012,
        materials["wheel"],
        rotation=(0.0, math.radians(90.0), 0.0),
        vertices=64,
        bevel=0.008,
    )
    rotor = add_cylinder(
        f"{name}_Rotor",
        location,
        0.180,
        width + 0.020,
        materials["rotor"],
        rotation=(0.0, math.radians(90.0), 0.0),
        vertices=56,
        bevel=0.004,
    )
    hub = add_cylinder(
        f"{name}_Hub",
        location,
        0.060,
        width + 0.034,
        materials["chrome"],
        rotation=(0.0, math.radians(90.0), 0.0),
        vertices=40,
        bevel=0.006,
    )
    wheel_parts = [tire, rim, inset, rotor, hub]

    for spoke_index in range(10):
        angle = math.radians(spoke_index * 36.0)
        offset_y = math.cos(angle) * 0.104
        offset_z = math.sin(angle) * 0.104
        spoke = add_box(
            f"{name}_Spoke_{spoke_index:02d}",
            (location[0], location[1] + offset_y, location[2] + offset_z),
            (width + 0.045, 0.040, 0.215),
            materials["wheel"],
            bevel=0.010,
            rotation=(angle, 0.0, 0.0),
        )
        wheel_parts.append(spoke)

    for part in wheel_parts:
        parent_keep_world(part, root)

    outside_x = location[0] + (width * 0.50 + 0.018) * (1.0 if location[0] > 0 else -1.0)
    caliper = add_box(
        f"BrakeCaliper_{name}",
        (outside_x, location[1] - 0.115, location[2]),
        (0.055, 0.090, 0.215),
        materials["brake"],
        bevel=0.022,
    )
    parent_keep_world(caliper, parent)
    return root


def create_wheel_well(name, x, y, materials, parent):
    liner = add_cylinder(
        f"{name}_Liner",
        (x, y, 0.405),
        0.445,
        0.050,
        materials["grille"],
        rotation=(0.0, math.radians(90.0), 0.0),
        vertices=64,
        bevel=0.0,
    )
    arch_points = []
    for index in range(15):
        angle = math.radians(12.0 + index * (156.0 / 14.0))
        arch_points.append((x, y + math.cos(angle) * 0.455, 0.405 + math.sin(angle) * 0.455))
    arch = add_curve(f"{name}_Arch_Lip", arch_points, materials["paint"], width=0.034)
    parent_keep_world(liner, parent)
    parent_keep_world(arch, parent)


def create_classic_muscle_car(reference_wheels):
    materials = make_materials()
    body = create_body_shell(materials["paint"])
    parts = []

    # Dark engine bay remains visible when the detachable hood is thrown in a crash.
    parts.extend(
        [
            add_box("Engine_Bay", (0.0, -1.24, 0.955), (1.56, 1.56, 0.055), materials["grille"], 0.025),
            add_box("Engine_Airbox", (0.0, -1.18, 0.990), (0.56, 0.72, 0.095), materials["chrome"], 0.035),
            add_box("Rocker_L", (-0.985, 0.08, 0.245), (0.065, 2.45, 0.16), materials["stripe"], 0.022),
            add_box("Rocker_R", (0.985, 0.08, 0.245), (0.065, 2.45, 0.16), materials["stripe"], 0.022),
        ]
    )

    hood = add_box(
        "Damage_Hood",
        (0.0, -1.29, 1.010),
        (1.66, 1.79, 0.095),
        materials["paint"],
        bevel=0.070,
        rotation=(math.radians(1.1), 0.0, 0.0),
    )
    hood_stripes = []
    for x in (-0.225, 0.225):
        stripe = add_box(
            f"Hood_Stripe_{'L' if x < 0 else 'R'}",
            (x, -1.29, 1.064),
            (0.255, 1.70, 0.018),
            materials["stripe"],
            bevel=0.012,
            rotation=(math.radians(1.1), 0.0, 0.0),
        )
        parent_keep_world(stripe, hood)
        hood_stripes.append(stripe)
    hood_scoop = add_box("Hood_Scoop", (0.0, -1.04, 1.110), (0.52, 0.62, 0.10), materials["stripe"], 0.055)
    parent_keep_world(hood_scoop, hood)
    parts.append(hood)

    trunk = add_box("Trunk_Deck", (0.0, 1.60, 1.015), (1.69, 1.05, 0.105), materials["paint"], 0.065, rotation=(math.radians(-1.0), 0.0, 0.0))
    parts.append(trunk)
    for x in (-0.225, 0.225):
        stripe = add_box(
            f"Trunk_Stripe_{'L' if x < 0 else 'R'}",
            (x, 1.60, 1.074),
            (0.255, 0.96, 0.017),
            materials["stripe"],
            0.012,
            rotation=(math.radians(-1.0), 0.0, 0.0),
        )
        parent_keep_world(stripe, trunk)

    roof = add_box("Cabin_Roof", (0.0, 0.30, 1.475), (1.42, 0.94, 0.105), materials["paint"], 0.080)
    parts.append(roof)
    for x in (-0.225, 0.225):
        roof_stripe = add_box(
            f"Roof_Stripe_{'L' if x < 0 else 'R'}",
            (x, 0.30, 1.534),
            (0.255, 0.88, 0.017),
            materials["stripe"],
            0.012,
        )
        parent_keep_world(roof_stripe, roof)

    windshield = add_box(
        "Windshield_Glass",
        (0.0, -0.390, 1.235),
        (1.48, 0.035, 0.550),
        materials["glass"],
        0.045,
        rotation=(math.radians(-27.5), 0.0, 0.0),
    )
    rear_glass = add_box(
        "Rear_Glass",
        (0.0, 0.975, 1.235),
        (1.47, 0.035, 0.530),
        materials["glass"],
        0.045,
        rotation=(math.radians(31.5), 0.0, 0.0),
    )
    side_glass_l = create_side_glass("Side_Glass_L", -0.725, materials["glass"], flip=True)
    side_glass_r = create_side_glass("Side_Glass_R", 0.725, materials["glass"], flip=False)
    parts.extend([windshield, rear_glass, side_glass_l, side_glass_r])

    for x in (-0.755, 0.755):
        side = "L" if x < 0 else "R"
        parts.extend(
            [
                add_box(f"A_Pillar_{side}", (x, -0.390, 1.235), (0.072, 0.078, 0.57), materials["paint"], 0.020, rotation=(math.radians(-27.5), 0.0, 0.0)),
                add_box(f"B_Pillar_{side}", (x, 0.33, 1.235), (0.070, 0.075, 0.45), materials["stripe"], 0.016),
                add_box(f"C_Pillar_{side}", (x, 0.93, 1.235), (0.080, 0.080, 0.55), materials["paint"], 0.022, rotation=(math.radians(31.5), 0.0, 0.0)),
                add_box(f"Damage_Door_{side}", (x + (-0.235 if x < 0 else 0.235), 0.18, 0.70), (0.055, 1.24, 0.50), materials["paint"], 0.035),
                add_box(f"Door_Handle_{side}", (x + (-0.255 if x < 0 else 0.255), 0.56, 0.91), (0.025, 0.20, 0.045), materials["chrome"], 0.012),
                add_box(f"Damage_Mirror_{side}", (x + (-0.13 if x < 0 else 0.13), -0.41, 1.14), (0.24, 0.17, 0.10), materials["paint"], 0.035),
            ]
        )

    # A small but complete cockpit is visible through the large classic greenhouse.
    parts.extend(
        [
            add_box("Dashboard", (0.0, -0.31, 1.02), (1.30, 0.29, 0.20), materials["interior"], 0.055),
            add_box("Driver_Seat_Base", (-0.39, 0.27, 0.78), (0.48, 0.62, 0.18), materials["interior"], 0.075),
            add_box("Passenger_Seat_Base", (0.39, 0.27, 0.78), (0.48, 0.62, 0.18), materials["interior"], 0.075),
            add_box("Driver_Seat_Back", (-0.39, 0.51, 1.02), (0.48, 0.20, 0.58), materials["interior"], 0.085, rotation=(math.radians(-8.0), 0.0, 0.0)),
            add_box("Passenger_Seat_Back", (0.39, 0.51, 1.02), (0.48, 0.20, 0.58), materials["interior"], 0.085, rotation=(math.radians(-8.0), 0.0, 0.0)),
        ]
    )
    steering = add_torus("Steering_Wheel", (-0.38, -0.30, 1.12), 0.145, 0.022, materials["interior"], rotation=(math.radians(78.0), 0.0, 0.0))
    parts.append(steering)

    # Front fascia: recessed full-width grille, four projector lamps, and heavy bumper.
    parts.extend(
        [
            add_box("Front_Grille", (0.0, -2.245, 0.675), (1.72, 0.055, 0.34), materials["grille"], 0.055),
            add_box("Damage_Front_Bumper", (0.0, -2.315, 0.385), (1.92, 0.14, 0.125), materials["chrome"], 0.028, smooth=False),
            add_box("Damage_Front_Splitter", (0.0, -2.280, 0.205), (1.86, 0.29, 0.065), materials["stripe"], 0.022),
        ]
    )
    for bar_x in (-0.28, -0.14, 0.0, 0.14, 0.28):
        parts.append(add_box(f"Grille_Bar_{bar_x:+.2f}", (bar_x, -2.280, 0.675), (0.025, 0.025, 0.25), materials["chrome"], 0.006))
    for index, x in enumerate((-0.68, -0.43, 0.43, 0.68)):
        bezel = add_cylinder(f"Headlight_Bezel_{index:02d}", (x, -2.283, 0.690), 0.115, 0.080, materials["chrome"], rotation=(math.radians(90.0), 0.0, 0.0), bevel=0.007)
        lamp = add_cylinder(f"BodyHeadlights_{index:02d}", (x, -2.330, 0.690), 0.084, 0.038, materials["headlight"], rotation=(math.radians(90.0), 0.0, 0.0), bevel=0.005)
        parts.extend([bezel, lamp])
    parts.extend(
        [
            add_box("Front_Amber_L", (-0.87, -2.315, 0.505), (0.17, 0.035, 0.060), materials["amber"], 0.016),
            add_box("Front_Amber_R", (0.87, -2.315, 0.505), (0.17, 0.035, 0.060), materials["amber"], 0.016),
        ]
    )

    # Rear fascia carries a recognizable original triple-lamp signature without brand marks.
    parts.extend(
        [
            add_box("Rear_Black_Fascia", (0.0, 2.205, 0.680), (1.73, 0.055, 0.37), materials["grille"], 0.050),
            add_box("Damage_Rear_Bumper", (0.0, 2.295, 0.385), (1.92, 0.14, 0.125), materials["chrome"], 0.028, smooth=False),
            add_box("Damage_Ducktail_Spoiler", (0.0, 2.005, 1.062), (1.54, 0.16, 0.050), materials["paint"], 0.020, rotation=(math.radians(-4.0), 0.0, 0.0), smooth=False),
            add_box("Rear_Plate_Recess", (0.0, 2.240, 0.470), (0.58, 0.030, 0.19), materials["stripe"], 0.020),
            add_box("Rear_Plate", (0.0, 2.262, 0.470), (0.46, 0.020, 0.13), materials["plate"], 0.012),
        ]
    )
    lamp_index = 0
    for side_center in (-0.62, 0.62):
        for offset in (-0.13, 0.0, 0.13):
            parts.append(
                add_box(
                    f"BodyTaillights_Triple_{lamp_index:02d}",
                    (side_center + offset, 2.245, 0.705),
                    (0.090, 0.036, 0.225),
                    materials["tail"],
                    0.022,
                )
            )
            lamp_index += 1
    for index, x in enumerate((-0.64, 0.64)):
        parts.append(add_cylinder(f"Rear_Exhaust_{index:02d}", (x, 2.315, 0.235), 0.075, 0.19, materials["chrome"], rotation=(math.radians(90.0), 0.0, 0.0), bevel=0.009))

    # Wheel wells and detailed wheel assemblies match the existing WheelCollider geometry.
    wheel_locations = {
        "Wheel_FL": (0.985, -1.49, 0.405),
        "Wheel_FR": (-0.985, -1.49, 0.405),
        "Wheel_RL": (0.995, 1.31, 0.405),
        "Wheel_RR": (-0.995, 1.31, 0.405),
    }
    for wheel_name, location in wheel_locations.items():
        create_wheel_well(f"{wheel_name}_Well", location[0], location[1], materials, body)
        wheel_root = reference_wheels.get(wheel_name)
        if wheel_root is not None:
            parent_keep_world(wheel_root, body)
        else:
            create_wheel(wheel_name, location, materials, body, rear=wheel_name.endswith("RL") or wheel_name.endswith("RR"))

    # Subtle side contour and panel gaps keep the broad body readable during motion.
    for sign in (-1.0, 1.0):
        side = "L" if sign < 0 else "R"
        contour = add_curve(
            f"Side_Character_Line_{side}",
            [
                (sign * 0.925, -1.94, 0.84),
                (sign * 0.982, -0.75, 0.91),
                (sign * 0.972, 0.55, 0.90),
                (sign * 0.930, 1.88, 0.83),
            ],
            materials["paint_dark"],
            0.007,
        )
        parts.append(contour)

    crack_lines = [
        [(-0.15, -0.407, 1.18), (-0.07, -0.390, 1.26), (0.02, -0.372, 1.34)],
        [(-0.07, -0.390, 1.26), (-0.27, -0.385, 1.23)],
        [(-0.07, -0.390, 1.26), (0.13, -0.386, 1.25), (0.31, -0.398, 1.20)],
        [(0.02, -0.372, 1.34), (0.17, -0.350, 1.42)],
    ]
    for index, points in enumerate(crack_lines):
        crack = add_curve(f"Damage_Windshield_Crack_{index:02d}", points, materials["crack"], 0.005)
        crack.hide_render = True
        parts.append(crack)

    for part in parts:
        if part.parent is None:
            parent_keep_world(part, body)
    return body


def add_preview_scene():
    floor_material = new_material("M_Preview_Floor", (0.020, 0.023, 0.027, 1.0), 0.0, 0.30)
    wall_material = new_material("M_Preview_Backdrop", (0.035, 0.038, 0.043, 1.0), 0.0, 0.46)
    bpy.ops.mesh.primitive_plane_add(size=40, location=(0.0, 0.0, 0.0))
    floor = bpy.context.object
    floor.name = "Preview_Floor"
    floor.data.materials.append(floor_material)
    bpy.ops.mesh.primitive_plane_add(size=30, location=(0.0, 10.0, 6.0), rotation=(math.radians(90.0), 0.0, 0.0))
    backdrop = bpy.context.object
    backdrop.name = "Preview_Backdrop"
    backdrop.data.materials.append(wall_material)

    bpy.ops.object.light_add(type="AREA", location=(-4.2, -3.8, 5.5))
    key = bpy.context.object
    key.name = "Preview_Key"
    key.data.energy = 1250
    key.data.shape = "DISK"
    key.data.size = 5.0
    key.data.color = (1.0, 0.74, 0.50)

    bpy.ops.object.light_add(type="AREA", location=(4.5, 3.8, 3.2))
    fill = bpy.context.object
    fill.name = "Preview_Fill"
    fill.data.energy = 930
    fill.data.size = 4.2
    fill.data.color = (0.34, 0.56, 1.0)

    bpy.ops.object.light_add(type="AREA", location=(0.0, 3.6, 4.8))
    rim = bpy.context.object
    rim.name = "Preview_Rim"
    rim.data.energy = 820
    rim.data.size = 3.2
    rim.data.color = (1.0, 0.22, 0.06)

    bpy.ops.object.camera_add(location=(5.55, 7.95, 1.82))
    camera = bpy.context.object
    camera.name = "Preview_Camera"
    camera.data.lens = 68
    bpy.context.scene.camera = camera

    bpy.ops.object.empty_add(type="PLAIN_AXES", location=(0.0, 0.20, 0.68))
    target = bpy.context.object
    target.name = "Preview_Target"
    constraint = camera.constraints.new(type="TRACK_TO")
    constraint.track_axis = "TRACK_NEGATIVE_Z"
    constraint.up_axis = "UP_Y"
    constraint.target = target


def convert_curves_for_export():
    for obj in list(bpy.context.scene.objects):
        if obj.type != "CURVE" or obj.name.startswith("Preview_"):
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

    # Match the source vehicle's FBX axis contract: local Y is longitudinal,
    # while Unity receives Y-up/Z-forward through this root rotation.
    body = bpy.data.objects.get("BodyUnderside")
    if body is not None:
        body.rotation_mode = "XYZ"
        body.rotation_euler = (math.radians(-90.0), 0.0, 0.0)

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
    bpy.context.scene.render.image_settings.color_mode = "RGBA"
    bpy.context.scene.world.color = (0.009, 0.012, 0.017)
    bpy.context.scene.view_settings.look = "AgX - Medium High Contrast"
    bpy.ops.render.render(write_still=True)


def main():
    clear_scene()
    reference_wheels = import_reference_wheels()
    create_classic_muscle_car(reference_wheels)
    add_preview_scene()
    render_preview()
    export_game_model()


if __name__ == "__main__":
    main()

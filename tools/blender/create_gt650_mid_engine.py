import math
from pathlib import Path

import bpy


ROOT = Path(__file__).resolve().parents[2]
MODEL_DIR = ROOT / "unity" / "CarCrashUnityPrototype" / "Assets" / "CarCrash" / "Models"
PREVIEW_DIR = ROOT / "assets" / "images" / "concepts" / "gt650_mid_engine"


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()


def material(name, color, metallic=0.0, roughness=0.45, alpha=1.0, emission=None):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    if bsdf is not None:
        if "Base Color" in bsdf.inputs:
            bsdf.inputs["Base Color"].default_value = color
        if "Metallic" in bsdf.inputs:
            bsdf.inputs["Metallic"].default_value = metallic
        if "Roughness" in bsdf.inputs:
            bsdf.inputs["Roughness"].default_value = roughness
        if "Alpha" in bsdf.inputs:
            bsdf.inputs["Alpha"].default_value = alpha
        if emission is not None and "Emission Color" in bsdf.inputs:
            bsdf.inputs["Emission Color"].default_value = emission
            bsdf.inputs["Emission Strength"].default_value = 2.25
    mat.diffuse_color = color
    if alpha < 1:
        mat.blend_method = "BLEND"
        mat.use_screen_refraction = True
        mat.show_transparent_back = True
    return mat


def add_modifier_stack(obj, bevel=0.0, segments=2, weighted=True):
    if bevel > 0:
        mod = obj.modifiers.new("controlled_panel_bevel", "BEVEL")
        mod.width = bevel
        mod.segments = segments
        mod.affect = "EDGES"
    if weighted:
        obj.modifiers.new("weighted_normals", "WEIGHTED_NORMAL")
    try:
        bpy.ops.object.select_all(action="DESELECT")
        obj.select_set(True)
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.shade_smooth()
    except Exception:
        pass


def parent_keep_world(child, parent):
    child.parent = parent
    child.matrix_parent_inverse = parent.matrix_world.inverted()


def box(name, loc, dims, mat, bevel=0.03, rot=(0, 0, 0), segments=2):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc, rotation=rot)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = dims
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(mat)
    add_modifier_stack(obj, bevel, segments)
    return obj


def cylinder(name, loc, radius, depth, mat, vertices=64, rot=(0, 0, 0), bevel=False):
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=vertices,
        radius=radius,
        depth=depth,
        location=loc,
        rotation=rot,
    )
    obj = bpy.context.object
    obj.name = name
    obj.data.materials.append(mat)
    add_modifier_stack(obj, 0.008 if bevel else 0, 2)
    return obj


def torus(name, loc, major_radius, minor_radius, mat, rot=(0, 0, 0), major=120, minor=18):
    bpy.ops.mesh.primitive_torus_add(
        major_radius=major_radius,
        minor_radius=minor_radius,
        major_segments=major,
        minor_segments=minor,
        location=loc,
        rotation=rot,
    )
    obj = bpy.context.object
    obj.name = name
    obj.data.materials.append(mat)
    add_modifier_stack(obj, 0, 1)
    return obj


def make_mesh(name, verts, faces, mat, bevel=0.03, segments=2):
    mesh = bpy.data.meshes.new(f"{name}Mesh")
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(mat)
    add_modifier_stack(obj, bevel, segments)
    return obj


def tire_mesh(name, x, y, radius, width, mat, segments=72):
    """Build a game tire with believable width and a recessed wheel opening."""
    verts = []
    outer = radius
    shoulder = radius * 0.94
    inner = radius * 0.61
    x_positions = (-width * 0.50, -width * 0.43, width * 0.43, width * 0.50)
    radii = (inner, outer, outer, inner)
    for local_x, ring_radius in zip(x_positions, radii):
        for i in range(segments):
            angle = math.tau * i / segments
            verts.append(
                (
                    x + local_x,
                    y + math.cos(angle) * ring_radius,
                    radius + math.sin(angle) * ring_radius,
                )
            )

    faces = []
    for ring in range(3):
        a = ring * segments
        b = (ring + 1) * segments
        for i in range(segments):
            j = (i + 1) % segments
            faces.append((a + i, a + j, b + j, b + i))

    obj = make_mesh(name, verts, faces, mat, bevel=0.006, segments=2)
    bevel = obj.modifiers.get("controlled_panel_bevel")
    if bevel is not None:
        bevel.width = radius * 0.035
    return obj


def cabin_canopy(mat):
    """A single smooth glasshouse keeps the windshield, roof and rear glass coherent."""
    stations = [
        # y, lower width, roof width, base z, roof z
        (-1.02, 0.78, 0.53, 0.80, 1.02),
        (-0.68, 0.73, 0.57, 0.84, 1.23),
        (-0.18, 0.68, 0.60, 0.86, 1.35),
        (0.38, 0.67, 0.58, 0.87, 1.33),
        (0.92, 0.76, 0.49, 0.84, 1.02),
    ]

    verts = []
    for y, lower_w, roof_w, base_z, roof_z in stations:
        verts.extend(
            [
                (-lower_w, y, base_z),
                (-roof_w, y, roof_z),
                (roof_w, y, roof_z),
                (lower_w, y, base_z),
            ]
        )

    faces = []
    for i in range(len(stations) - 1):
        a = i * 4
        b = (i + 1) * 4
        faces.extend(
            [
                (a, b, b + 1, a + 1),
                (a + 1, b + 1, b + 2, a + 2),
                (a + 2, b + 2, b + 3, a + 3),
            ]
        )
    faces.extend([(0, 1, 2, 3), (16, 19, 18, 17)])
    canopy = make_mesh("Cabin_Glasshouse", verts, faces, mat, bevel=0.020, segments=3)
    subdiv = canopy.modifiers.new("glasshouse_surface_refinement", "SUBSURF")
    subdiv.levels = 1
    subdiv.render_levels = 1
    return canopy


def body_shell(mat):
    # Wide rear-drive crash muscle proportions inspired by the approved home mockup.
    # This is the painted metal shell only; the cabin/glass is built separately so
    # the car reads as a real coupe instead of one melted wedge.
    # Axes: X width, Y length, Z height. Nose is negative Y, tail is positive Y.
    stations = [
        # y, floor_w, lower_w, shoulder_w, deck_w, floor_z, lower_z, shoulder_z, deck_z
        (-2.70, 0.52, 0.82, 0.80, 0.34, 0.24, 0.38, 0.58, 0.61),
        (-2.08, 0.88, 1.04, 1.00, 0.58, 0.25, 0.46, 0.70, 0.78),
        (-1.45, 1.08, 1.21, 1.16, 0.78, 0.27, 0.51, 0.77, 0.84),
        (-0.45, 1.12, 1.24, 1.12, 0.66, 0.28, 0.52, 0.75, 0.82),
        (0.45, 1.12, 1.24, 1.12, 0.66, 0.29, 0.52, 0.75, 0.82),
        (1.35, 1.18, 1.30, 1.23, 0.78, 0.30, 0.55, 0.81, 0.88),
        (2.15, 1.12, 1.24, 1.20, 0.70, 0.34, 0.58, 0.82, 0.86),
        (2.54, 0.96, 1.13, 1.16, 0.60, 0.35, 0.60, 0.78, 0.80),
    ]

    verts = []
    for y, floor_w, lower_w, shoulder_w, deck_w, floor_z, lower_z, shoulder_z, deck_z in stations:
        verts.extend(
            [
                (-floor_w, y, floor_z),
                (floor_w, y, floor_z),
                (-lower_w, y, lower_z),
                (lower_w, y, lower_z),
                (-shoulder_w, y, shoulder_z),
                (shoulder_w, y, shoulder_z),
                (-deck_w, y, deck_z),
                (deck_w, y, deck_z),
            ]
        )

    faces = []
    for i in range(len(stations) - 1):
        a = i * 8
        b = (i + 1) * 8
        faces.extend(
            [
                (a, b, b + 1, a + 1),  # lower floor belly
                (a, a + 2, b + 2, b),  # left lower side
                (a + 1, b + 1, b + 3, a + 3),  # right lower side
                (a + 2, a + 4, b + 4, b + 2),  # left slab side
                (a + 3, b + 3, b + 5, a + 5),  # right slab side
                (a + 4, a + 6, b + 6, b + 4),  # left crown
                (a + 5, b + 5, b + 7, a + 7),  # right crown
                (a + 6, a + 7, b + 7, b + 6),  # hood/deck surface
            ]
        )
    faces.extend([(0, 1, 3, 5, 7, 6, 4, 2), (56, 58, 60, 62, 63, 61, 59, 57)])
    obj = make_mesh("Crashhawk_BodyShell", verts, faces, mat, bevel=0.030, segments=4)
    subdiv = obj.modifiers.new("subtle_body_surface_refinement", "SUBSURF")
    subdiv.levels = 1
    subdiv.render_levels = 1
    return obj


def add_spoke(name, loc, length, mat, angle, side):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc, rotation=(angle, 0, 0))
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = (0.035, length, 0.032)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(mat)
    obj.location.x += side * 0.02
    add_modifier_stack(obj, bevel=0.006, segments=1)
    return obj


def add_wheel(name, x, y, radius, width, tire_mat, rim_mat, brake_mat, groove_mat):
    side = 1 if x > 0 else -1
    root = bpy.data.objects.new(name, None)
    root.empty_display_type = "PLAIN_AXES"
    root.location = (x, y, radius)
    bpy.context.collection.objects.link(root)

    tire = tire_mesh(f"{name}_Tire_Sidewall", x, y, radius, width, tire_mat)
    parent_keep_world(tire, root)

    for offset in (-0.34, -0.17, 0.0, 0.17, 0.34):
        groove = torus(
            f"{name}_Tread_Groove_{offset:+.2f}",
            (x + offset * width, y, radius),
            radius * 0.965,
            radius * 0.010,
            groove_mat,
            (0, math.pi / 2, 0),
            major=96,
            minor=8,
        )
        parent_keep_world(groove, root)

    rim_outer = torus(f"{name}_Deep_Dish_Rim", (x + side * width * 0.07, y, radius), radius * 0.47, radius * 0.055, rim_mat, (0, math.pi / 2, 0))
    parent_keep_world(rim_outer, root)
    rim_inner = cylinder(f"{name}_Dark_Inner_Rim", (x + side * width * 0.075, y, radius), radius * 0.38, 0.045, rim_mat, vertices=80, rot=(0, math.pi / 2, 0), bevel=True)
    parent_keep_world(rim_inner, root)
    disc = cylinder(f"{name}_Ventilated_BrakeDisc", (x + side * width * 0.115, y, radius), radius * 0.34, 0.035, brake_mat, vertices=80, rot=(0, math.pi / 2, 0), bevel=True)
    parent_keep_world(disc, root)

    for i in range(12):
        angle = i * math.tau / 12
        center_y = y + math.cos(angle) * radius * 0.18
        center_z = radius + math.sin(angle) * radius * 0.18
        spoke = add_spoke(
            f"{name}_Split_Spoke_{i:02d}",
            (x + side * width * 0.145, center_y, center_z),
            radius * 0.58,
            rim_mat,
            angle,
            side,
        )
        parent_keep_world(spoke, root)

    caliper = box(
        f"{name}_Red_Brake_Caliper",
        (x + side * width * 0.18, y - radius * 0.08, radius + radius * 0.35),
        (0.065, 0.11, 0.20),
        brake_mat,
        bevel=0.014,
    )
    parent_keep_world(caliper, root)
    return root


def add_curve_line(name, points, mat, bevel_depth=0.006):
    curve = bpy.data.curves.new(name, "CURVE")
    curve.dimensions = "3D"
    curve.bevel_depth = bevel_depth
    curve.resolution_u = 3
    spl = curve.splines.new("POLY")
    spl.points.add(len(points) - 1)
    for p, co in zip(spl.points, points):
        p.co = (co[0], co[1], co[2], 1.0)
    obj = bpy.data.objects.new(name, curve)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(mat)
    return obj


def add_light_strip(name, loc, dims, mat, rot=(0, 0, 0)):
    return box(name, loc, dims, mat, bevel=0.012, rot=rot, segments=2)


def add_panel(name, verts, mat, bevel=0.006):
    return make_mesh(name, verts, [(0, 1, 2, 3)], mat, bevel=bevel, segments=1)


def add_wheel_arch(name, side, y, z, radius, mat):
    points = []
    # Side-view arch in the Y/Z plane. It covers the visible upper half only,
    # avoiding the full bicycle-hoop look from the first version.
    for i in range(18):
        t = math.radians(18 + i * (144 / 17))
        points.append((side * 1.245, y + math.cos(t) * radius, z + math.sin(t) * radius))
    return add_curve_line(name, points, mat, bevel_depth=0.007)


def build_car():
    clear_scene()
    bpy.context.scene.unit_settings.system = "METRIC"
    bpy.context.scene.unit_settings.scale_length = 1.0

    yellow = material("M_Crashhawk_Competition_Yellow", (0.95, 0.55, 0.025, 1), metallic=0.32, roughness=0.34)
    yellow_dark = material("M_Crashhawk_Darker_Yellow_Shadow", (0.62, 0.32, 0.010, 1), metallic=0.25, roughness=0.42)
    carbon = material("M_Satin_Black_Carbon", (0.006, 0.006, 0.007, 1), metallic=0.0, roughness=0.36)
    rubber = material("M_Performance_Rubber", (0.004, 0.004, 0.004, 1), metallic=0.0, roughness=0.80)
    tread = material("M_Tire_Groove_Black", (0.0, 0.0, 0.0, 1), metallic=0.0, roughness=0.92)
    rim = material("M_Black_Forged_Rim", (0.02, 0.021, 0.024, 1), metallic=0.7, roughness=0.28)
    brake = material("M_Red_Performance_Brake", (0.92, 0.02, 0.0, 1), metallic=0.18, roughness=0.24)
    glass = material("M_Smoked_Blue_Glass", (0.04, 0.12, 0.14, 0.48), metallic=0.0, roughness=0.07, alpha=0.48)
    glass_crack = material("M_Glass_Crack_Lines", (0.75, 0.95, 1.0, 1), metallic=0.0, roughness=0.18, emission=(0.58, 0.85, 1, 1))
    headlight = material("M_Cool_LED_Headlight", (0.78, 0.94, 1.0, 1), roughness=0.08, emission=(0.70, 0.92, 1.0, 1))
    taillight = material("M_Red_LED_Taillight", (1.0, 0.02, 0.015, 1), roughness=0.1, emission=(1.0, 0.02, 0.015, 1))
    chrome = material("M_Exhaust_Dark_Chrome", (0.11, 0.10, 0.095, 1), metallic=0.8, roughness=0.20)
    proxy_mat = material("M_Collision_Proxy_Transparent", (0.0, 0.7, 1.0, 0.15), alpha=0.15)

    root = bpy.data.objects.new("Crashhawk_GT650_Muscle_Crash_Car", None)
    bpy.context.collection.objects.link(root)

    shell = body_shell(yellow)
    shell.parent = root
    canopy = cabin_canopy(glass)
    canopy.parent = root

    parts = [
        box("Damage_Front_Splitter", (0, -2.75, 0.26), (1.90, 0.18, 0.055), carbon, bevel=0.020),
        box("Front_Lower_Grille", (0, -2.63, 0.45), (1.10, 0.045, 0.18), carbon, bevel=0.014),
        box("Hood_Ram_Scoop", (0, -1.52, 0.86), (0.52, 0.68, 0.07), carbon, bevel=0.024, rot=(math.radians(-3), 0, 0), segments=4),
        box("Left_Side_Skirt", (-1.20, 0.02, 0.31), (0.06, 3.45, 0.07), carbon, bevel=0.014),
        box("Right_Side_Skirt", (1.20, 0.02, 0.31), (0.06, 3.45, 0.07), carbon, bevel=0.014),
        box("Damage_Rear_Diffuser_Block", (0, 2.62, 0.32), (1.70, 0.22, 0.20), carbon, bevel=0.02),
        box("Rear_Black_Tail_Panel", (0, 2.58, 0.67), (1.80, 0.055, 0.30), carbon, bevel=0.014),
        box("Rear_Bumper_Yellow_Lip", (0, 2.66, 0.47), (2.05, 0.18, 0.16), yellow_dark, bevel=0.025),
        box("Damage_Active_Rear_Wing", (0, 2.38, 0.98), (1.92, 0.20, 0.08), carbon, bevel=0.021, rot=(math.radians(-7), 0, 0)),
        box("Rear_Wing_Left_Post", (-0.62, 2.30, 0.83), (0.055, 0.12, 0.28), carbon, bevel=0.010),
        box("Rear_Wing_Right_Post", (0.62, 2.30, 0.83), (0.055, 0.12, 0.28), carbon, bevel=0.010),
        box("Black_Roof_Panel", (0, 0.06, 1.355), (0.94, 0.76, 0.030), carbon, bevel=0.024),
        box("Left_Side_Intake_Shadow", (-1.235, 0.92, 0.60), (0.035, 0.48, 0.18), carbon, bevel=0.018),
        box("Right_Side_Intake_Shadow", (1.235, 0.92, 0.60), (0.035, 0.48, 0.18), carbon, bevel=0.018),
        box("Damage_Left_Compact_Mirror", (-1.13, -0.62, 0.98), (0.18, 0.08, 0.09), carbon, bevel=0.025),
        box("Damage_Right_Compact_Mirror", (1.13, -0.62, 0.98), (0.18, 0.08, 0.09), carbon, bevel=0.025),
    ]
    for obj in parts:
        obj.parent = root

    for side in (-1, 1):
        for z in (0.62, 0.76):
            crease = add_curve_line(
                f"{'Left' if side < 0 else 'Right'}_Body_Crease_{z:.2f}",
                [(side * 1.205, -2.05, z), (side * 1.255, -0.45, z + 0.03), (side * 1.255, 1.85, z + 0.015)],
                yellow_dark,
                bevel_depth=0.006,
            )
            crease.parent = root

    # Double black racing stripes, matching the approved yellow mockup language.
    for x in (-0.14, 0.14):
        for name, y, z, length, width in [
            ("Hood_Stripe", -1.56, 0.89, 1.55, 0.10),
            ("Roof_Stripe", -0.04, 1.335, 0.82, 0.095),
            ("Rear_Deck_Stripe", 1.35, 0.91, 1.30, 0.10),
        ]:
            stripe = box(f"{name}_{x:+.2f}", (x, y, z), (width, length, 0.016), carbon, bevel=0.006)
            stripe.parent = root

    for x in (-0.63, 0.63):
        add_light_strip(f"Front_LED_Eyelid_{'L' if x < 0 else 'R'}", (x, -2.61, 0.61), (0.46, 0.035, 0.052), headlight).parent = root
        for i in range(3):
            add_light_strip(
                f"Rear_Triple_LED_{'L' if x < 0 else 'R'}_{i}",
                (x + (i - 1) * 0.105, 2.60, 0.72),
                (0.065, 0.04, 0.13),
                taillight,
            ).parent = root

    for x in (-0.58, -0.42, 0.42, 0.58):
        exhaust = cylinder(f"Round_Exhaust_Tip_{x:+.2f}", (x, 2.73, 0.34), 0.075, 0.18, chrome, vertices=64, rot=(math.pi / 2, 0, 0), bevel=True)
        exhaust.parent = root

    for x in (-0.48, 0.0, 0.48):
        fin = box(f"Rear_Diffuser_Fin_{x:+.2f}", (x, 2.73, 0.23), (0.045, 0.32, 0.22), carbon, bevel=0.006)
        fin.parent = root

    wheels = [
        add_wheel("Wheel_FL", -1.08, -1.38, 0.365, 0.34, rubber, rim, brake, tread),
        add_wheel("Wheel_FR", 1.08, -1.38, 0.365, 0.34, rubber, rim, brake, tread),
        add_wheel("Wheel_RL", -1.10, 1.38, 0.395, 0.38, rubber, rim, brake, tread),
        add_wheel("Wheel_RR", 1.10, 1.38, 0.395, 0.38, rubber, rim, brake, tread),
    ]
    for obj in wheels:
        obj.parent = root

    # Subtle cracked windshield detail so the model reads like a crash-game car.
    crack_lines = [
        [(-0.18, -0.68, 1.18), (-0.10, -0.67, 1.09), (-0.02, -0.66, 1.00)],
        [(-0.10, -0.67, 1.09), (-0.25, -0.66, 1.03)],
        [(-0.09, -0.67, 1.08), (0.08, -0.66, 1.12), (0.22, -0.65, 1.18)],
        [(-0.02, -0.66, 1.00), (0.14, -0.65, 0.94)],
    ]
    for index, points in enumerate(crack_lines):
        crack = add_curve_line(f"Windshield_Crack_{index:02d}", points, glass_crack, bevel_depth=0.004)
        crack.parent = root

    # Collision proxies kept in the source file and hidden from render/game model import.
    for obj in [
        box("COL_Body_Main", (0, 0.12, 0.56), (2.04, 4.25, 0.78), proxy_mat, bevel=0.01),
        box("COL_Front_Impact", (0, -2.16, 0.48), (1.62, 0.62, 0.48), proxy_mat, bevel=0.01),
        box("COL_Rear_Impact", (0, 2.20, 0.54), (1.78, 0.62, 0.52), proxy_mat, bevel=0.01),
    ]:
        obj.parent = root
        obj.hide_render = True

    # Preview studio lighting.
    bpy.ops.object.light_add(type="AREA", location=(-3.5, -4.8, 5.0))
    key = bpy.context.object
    key.name = "Studio_Key_Area"
    key.data.energy = 760
    key.data.size = 4.8
    bpy.ops.object.light_add(type="POINT", location=(3.2, 2.8, 2.2))
    fill = bpy.context.object
    fill.name = "Warm_Rear_Fill"
    fill.data.energy = 180
    fill.data.color = (1.0, 0.72, 0.42)

    bpy.ops.object.camera_add(location=(4.6, 7.7, 2.25), rotation=(math.radians(78), 0, math.radians(150)))
    bpy.context.scene.camera = bpy.context.object
    bpy.context.scene.camera.data.type = "PERSP"
    bpy.context.scene.camera.data.lens = 62
    bpy.ops.object.empty_add(type="PLAIN_AXES", location=(0, 0.50, 0.67))
    target = bpy.context.object
    target.name = "Camera_Target"
    constraint = bpy.context.scene.camera.constraints.new(type="TRACK_TO")
    constraint.track_axis = "TRACK_NEGATIVE_Z"
    constraint.up_axis = "UP_Y"
    constraint.target = target

    return root


def export_outputs():
    MODEL_DIR.mkdir(parents=True, exist_ok=True)
    PREVIEW_DIR.mkdir(parents=True, exist_ok=True)
    blend_path = MODEL_DIR / "gt650_mid_engine.blend"
    fbx_path = MODEL_DIR / "gt650_mid_engine.fbx"
    glb_path = MODEL_DIR / "gt650_mid_engine.glb"
    preview_path = PREVIEW_DIR / "gt650_mid_engine_model_preview.png"

    bpy.ops.wm.save_as_mainfile(filepath=str(blend_path))

    # FBX cannot export Blender curve objects directly. Preserve them in the
    # source .blend, then convert the export copy in-memory to mesh geometry.
    for obj in list(bpy.context.scene.objects):
        if obj.type != "CURVE":
            continue
        bpy.ops.object.select_all(action="DESELECT")
        obj.select_set(True)
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.convert(target="MESH")

    bpy.ops.object.select_all(action="DESELECT")
    for obj in bpy.context.scene.objects:
        if obj.type in {"EMPTY", "MESH"} and not obj.name.startswith("COL_"):
            obj.select_set(True)
    bpy.ops.export_scene.fbx(
        filepath=str(fbx_path),
        use_selection=True,
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_UNITS",
        object_types={"EMPTY", "MESH"},
        bake_space_transform=False,
        add_leaf_bones=False,
        use_mesh_modifiers=True,
    )
    bpy.ops.export_scene.gltf(
        filepath=str(glb_path),
        export_format="GLB",
        use_selection=True,
        export_apply=True,
    )

    try:
        bpy.context.scene.render.engine = "BLENDER_EEVEE_NEXT"
    except Exception:
        bpy.context.scene.render.engine = "BLENDER_EEVEE"
    if hasattr(bpy.context.scene, "eevee") and hasattr(bpy.context.scene.eevee, "taa_render_samples"):
        bpy.context.scene.eevee.taa_render_samples = 64
    bpy.context.scene.render.resolution_x = 1800
    bpy.context.scene.render.resolution_y = 1100
    bpy.context.scene.render.film_transparent = False
    bpy.context.scene.view_settings.exposure = -0.55
    bpy.context.scene.view_settings.gamma = 1.0
    try:
        bpy.context.scene.view_settings.look = "AgX - Medium High Contrast"
    except TypeError:
        bpy.context.scene.view_settings.look = "Medium High Contrast"
    bpy.context.scene.world.color = (0.56, 0.58, 0.60)
    bpy.context.scene.render.filepath = str(preview_path)
    bpy.ops.render.render(write_still=True)


if __name__ == "__main__":
    build_car()
    export_outputs()

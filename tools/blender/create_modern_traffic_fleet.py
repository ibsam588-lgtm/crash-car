import math
from pathlib import Path

import bpy


ROOT = Path(__file__).resolve().parents[2]
MODEL_DIR = ROOT / "unity" / "CarCrashUnityPrototype" / "Assets" / "CarCrash" / "Models"
PREVIEW_DIR = ROOT / "assets" / "images" / "concepts" / "modern_traffic_fleet"


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for datablocks in (bpy.data.meshes, bpy.data.curves, bpy.data.materials, bpy.data.cameras, bpy.data.lights):
        for datablock in list(datablocks):
            if datablock.users == 0:
                datablocks.remove(datablock)


def material(name, color, metallic=0.0, roughness=0.42, emission=None, emission_strength=0.0):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    mat.diffuse_color = color
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    if bsdf is not None:
        bsdf.inputs["Base Color"].default_value = color
        bsdf.inputs["Metallic"].default_value = metallic
        bsdf.inputs["Roughness"].default_value = roughness
        if emission is not None:
            if "Emission Color" in bsdf.inputs:
                bsdf.inputs["Emission Color"].default_value = emission
            elif "Emission" in bsdf.inputs:
                bsdf.inputs["Emission"].default_value = emission
            if "Emission Strength" in bsdf.inputs:
                bsdf.inputs["Emission Strength"].default_value = emission_strength
    return mat


def parent_keep_world(child, parent):
    child.parent = parent
    child.matrix_parent_inverse = parent.matrix_world.inverted()


def smooth_and_bevel(obj, bevel=0.02, segments=3, weighted=True):
    if bevel > 0:
        modifier = obj.modifiers.new("Panel Bevel", "BEVEL")
        modifier.width = bevel
        modifier.segments = segments
        modifier.affect = "EDGES"
    if weighted:
        try:
            obj.modifiers.new("Weighted Normals", "WEIGHTED_NORMAL")
        except Exception:
            pass
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    try:
        bpy.ops.object.shade_smooth()
    except Exception:
        pass


def new_root(name):
    root = bpy.data.objects.new(name, None)
    bpy.context.collection.objects.link(root)
    root.empty_display_type = "PLAIN_AXES"
    root.empty_display_size = 0.35
    return root


def box(name, location, dimensions, mat, parent=None, bevel=0.025, rotation=(0.0, 0.0, 0.0), segments=3):
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=location, rotation=rotation)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = dimensions
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(mat)
    smooth_and_bevel(obj, bevel, segments)
    if parent is not None:
        parent_keep_world(obj, parent)
    return obj


def cylinder(name, location, radius, depth, mat, parent=None, vertices=48, rotation=(0.0, 0.0, 0.0), bevel=0.01):
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=vertices,
        radius=radius,
        depth=depth,
        location=location,
        rotation=rotation,
    )
    obj = bpy.context.object
    obj.name = name
    obj.data.materials.append(mat)
    smooth_and_bevel(obj, bevel, 2)
    if parent is not None:
        parent_keep_world(obj, parent)
    return obj


def torus(name, location, major_radius, minor_radius, mat, parent=None, rotation=(0.0, 0.0, 0.0)):
    bpy.ops.mesh.primitive_torus_add(
        major_radius=major_radius,
        minor_radius=minor_radius,
        major_segments=56,
        minor_segments=14,
        location=location,
        rotation=rotation,
    )
    obj = bpy.context.object
    obj.name = name
    obj.data.materials.append(mat)
    smooth_and_bevel(obj, 0.0, 1, weighted=False)
    if parent is not None:
        parent_keep_world(obj, parent)
    return obj


def mesh_object(name, vertices, faces, mat, parent=None, bevel=0.015, segments=3, subdivision=0):
    mesh = bpy.data.meshes.new(f"{name}_Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(mat)
    smooth_and_bevel(obj, bevel, segments)
    if subdivision > 0:
        modifier = obj.modifiers.new("Surface Refinement", "SUBSURF")
        modifier.subdivision_type = "CATMULL_CLARK"
        modifier.levels = subdivision
        modifier.render_levels = subdivision
    if parent is not None:
        parent_keep_world(obj, parent)
    return obj


def loft_shell(name, stations, mat, parent, bevel=0.025, subdivision=1):
    """Create a closed, longitudinal body shell from eight-point cross sections."""
    vertices = []
    for station in stations:
        y, floor_w, side_w, shoulder_w, deck_w, floor_z, lower_z, shoulder_z, deck_z = station
        vertices.extend(
            [
                (-floor_w, y, floor_z),
                (floor_w, y, floor_z),
                (-side_w, y, lower_z),
                (side_w, y, lower_z),
                (-shoulder_w, y, shoulder_z),
                (shoulder_w, y, shoulder_z),
                (-deck_w, y, deck_z),
                (deck_w, y, deck_z),
            ]
        )

    faces = []
    for index in range(len(stations) - 1):
        a = index * 8
        b = (index + 1) * 8
        faces.extend(
            [
                (a, b, b + 1, a + 1),
                (a, a + 2, b + 2, b),
                (a + 1, b + 1, b + 3, a + 3),
                (a + 2, a + 4, b + 4, b + 2),
                (a + 3, b + 3, b + 5, a + 5),
                (a + 4, a + 6, b + 6, b + 4),
                (a + 5, b + 5, b + 7, a + 7),
                (a + 6, a + 7, b + 7, b + 6),
            ]
        )
    faces.append((0, 1, 3, 5, 7, 6, 4, 2))
    end = (len(stations) - 1) * 8
    faces.append((end, end + 2, end + 4, end + 6, end + 7, end + 5, end + 3, end + 1))
    return mesh_object(name, vertices, faces, mat, parent, bevel, 3, subdivision)


def canopy(name, stations, glass_mat, parent, subdivision=1):
    """Create a coherent modern glasshouse with a gently crowned roof."""
    vertices = []
    for y, base_w, roof_w, base_z, roof_z in stations:
        vertices.extend(
            [
                (-base_w, y, base_z),
                (-roof_w, y, roof_z),
                (-roof_w * 0.24, y, roof_z + 0.045),
                (roof_w * 0.24, y, roof_z + 0.045),
                (roof_w, y, roof_z),
                (base_w, y, base_z),
            ]
        )

    faces = []
    ring = 6
    for index in range(len(stations) - 1):
        a = index * ring
        b = (index + 1) * ring
        for segment in range(ring):
            nxt = (segment + 1) % ring
            faces.append((a + segment, b + segment, b + nxt, a + nxt))
    faces.append(tuple(range(ring - 1, -1, -1)))
    end = (len(stations) - 1) * ring
    faces.append(tuple(end + index for index in range(ring)))
    return mesh_object(name, vertices, faces, glass_mat, parent, 0.012, 3, subdivision)


def add_wheel(root, name, x, y, radius, width, mats, spoke_count=8, tire_profile=0.19):
    wheel_root = bpy.data.objects.new(name, None)
    bpy.context.collection.objects.link(wheel_root)
    wheel_root.location = (x, y, radius)
    wheel_root.empty_display_type = "CIRCLE"
    wheel_root.empty_display_size = radius * 0.68
    parent_keep_world(wheel_root, root)

    center = (x, y, radius)
    outer = radius
    rim_radius = radius * 0.61
    major_radius = (outer + rim_radius) * 0.5
    minor_radius = (outer - rim_radius) * tire_profile / 0.19
    torus(
        f"{name}_Tire",
        center,
        major_radius,
        minor_radius,
        mats["tire"],
        wheel_root,
        rotation=(0.0, math.radians(90.0), 0.0),
    )
    cylinder(
        f"{name}_Rim",
        center,
        rim_radius,
        width * 0.72,
        mats["rim"],
        wheel_root,
        vertices=48,
        rotation=(0.0, math.radians(90.0), 0.0),
        bevel=0.006,
    )
    cylinder(
        f"{name}_BrakeDisc",
        center,
        radius * 0.43,
        width * 0.76,
        mats["brake"],
        wheel_root,
        vertices=48,
        rotation=(0.0, math.radians(90.0), 0.0),
        bevel=0.002,
    )
    cylinder(
        f"{name}_Hub",
        center,
        radius * 0.105,
        width * 0.84,
        mats["hub"],
        wheel_root,
        vertices=32,
        rotation=(0.0, math.radians(90.0), 0.0),
        bevel=0.004,
    )

    visible_x = x + (width * 0.43 if x > 0 else -width * 0.43)
    spoke_length = radius * 0.38
    spoke_offset = radius * 0.27
    for index in range(spoke_count):
        angle = math.tau * index / spoke_count
        spoke = box(
            f"{name}_Spoke_{index + 1:02d}",
            (
                visible_x,
                y + math.cos(angle) * spoke_offset,
                radius + math.sin(angle) * spoke_offset,
            ),
            (width * 0.055, radius * 0.095, spoke_length),
            mats["spoke"],
            wheel_root,
            bevel=0.008,
            rotation=(angle, 0.0, 0.0),
            segments=2,
        )
        spoke["wheel_detail"] = True

    caliper_x = x + (width * 0.48 if x > 0 else -width * 0.48)
    box(
        f"{name}_Caliper",
        (caliper_x, y - radius * 0.22, radius + radius * 0.02),
        (width * 0.09, radius * 0.16, radius * 0.34),
        mats["caliper"],
        wheel_root,
        bevel=0.012,
    )
    return wheel_root


def add_four_wheels(root, front_y, rear_y, half_track, radius, width, mats, spoke_count=8):
    return [
        add_wheel(root, "Wheel_FL", -half_track, front_y, radius, width, mats, spoke_count),
        add_wheel(root, "Wheel_FR", half_track, front_y, radius, width, mats, spoke_count),
        add_wheel(root, "Wheel_RL", -half_track, rear_y, radius, width, mats, spoke_count),
        add_wheel(root, "Wheel_RR", half_track, rear_y, radius, width, mats, spoke_count),
    ]


def add_wheel_arch(root, name, side, y, center_z, arch_radius, tube_radius, mat):
    """Build only the upper half of a wheel opening so the tire stays visually attached."""
    vertices = []
    faces = []
    arc_segments = 24
    tube_segments = 8
    x = side
    for arc_index in range(arc_segments + 1):
        angle = math.pi * arc_index / arc_segments
        center_y = y + math.cos(angle) * arch_radius
        center_height = center_z + math.sin(angle) * arch_radius
        radial_y = math.cos(angle)
        radial_z = math.sin(angle)
        for tube_index in range(tube_segments):
            ring_angle = math.tau * tube_index / tube_segments
            vertices.append(
                (
                    x + math.cos(ring_angle) * tube_radius,
                    center_y + radial_y * math.sin(ring_angle) * tube_radius,
                    center_height + radial_z * math.sin(ring_angle) * tube_radius,
                )
            )
    for arc_index in range(arc_segments):
        a = arc_index * tube_segments
        b = (arc_index + 1) * tube_segments
        for tube_index in range(tube_segments):
            nxt = (tube_index + 1) % tube_segments
            faces.append((a + tube_index, b + tube_index, b + nxt, a + nxt))
    return mesh_object(name, vertices, faces, mat, root, 0.004, 2, 0)


def add_side_details(root, half_width, y_positions, lower_z, upper_z, trim_mat, handle_mat):
    for side in (-1.0, 1.0):
        x = side * (half_width + 0.012)
        for index, y in enumerate(y_positions):
            box(
                f"Door_Seam_{'L' if side < 0 else 'R'}_{index + 1}",
                (x, y, (lower_z + upper_z) * 0.5),
                (0.016, 0.025, upper_z - lower_z),
                trim_mat,
                root,
                bevel=0.002,
            )
        for index, y in enumerate(y_positions[:-1]):
            box(
                f"Door_Handle_{'L' if side < 0 else 'R'}_{index + 1}",
                (x + side * 0.008, y + 0.42, upper_z - 0.13),
                (0.025, 0.18, 0.035),
                handle_mat,
                root,
                bevel=0.008,
            )


def add_mirror(root, side, location, paint_mat, glass_mat):
    x, y, z = location
    x *= side
    box(
        f"Mirror_{'L' if side < 0 else 'R'}_Arm",
        (x - side * 0.08, y, z - 0.02),
        (0.18, 0.055, 0.055),
        paint_mat,
        root,
        bevel=0.018,
    )
    box(
        f"Mirror_{'L' if side < 0 else 'R'}_Housing",
        (x, y, z),
        (0.24, 0.18, 0.12),
        paint_mat,
        root,
        bevel=0.045,
    )
    box(
        f"Mirror_{'L' if side < 0 else 'R'}_Glass",
        (x + side * 0.122, y + 0.004, z),
        (0.012, 0.14, 0.082),
        glass_mat,
        root,
        bevel=0.01,
    )


def build_sedan(mats):
    root = new_root("TrafficSedanModern")
    stations = [
        (-2.38, 0.44, 0.66, 0.62, 0.26, 0.22, 0.31, 0.52, 0.57),
        (-2.15, 0.72, 0.90, 0.86, 0.49, 0.22, 0.34, 0.61, 0.68),
        (-1.82, 0.84, 0.97, 0.94, 0.65, 0.23, 0.38, 0.70, 0.78),
        (-1.38, 0.88, 1.00, 0.98, 0.72, 0.23, 0.39, 0.75, 0.82),
        (-0.62, 0.89, 1.01, 0.98, 0.73, 0.23, 0.40, 0.77, 0.85),
        (0.24, 0.90, 1.01, 0.98, 0.72, 0.24, 0.40, 0.78, 0.86),
        (0.92, 0.91, 1.02, 0.99, 0.74, 0.24, 0.41, 0.79, 0.86),
        (1.42, 0.90, 1.01, 0.98, 0.72, 0.24, 0.42, 0.79, 0.84),
        (1.86, 0.86, 0.97, 0.94, 0.65, 0.24, 0.43, 0.75, 0.78),
        (2.22, 0.74, 0.90, 0.89, 0.55, 0.25, 0.44, 0.68, 0.70),
        (2.38, 0.51, 0.76, 0.78, 0.35, 0.27, 0.43, 0.60, 0.62),
    ]
    loft_shell("Sedan_BodyShell", stations, mats["sedan_paint"], root, 0.024, 1)
    canopy(
        "Sedan_Glasshouse",
        [
            (-1.24, 0.75, 0.56, 0.78, 1.02),
            (-0.86, 0.77, 0.61, 0.80, 1.25),
            (-0.18, 0.78, 0.63, 0.82, 1.39),
            (0.62, 0.78, 0.62, 0.82, 1.37),
            (1.17, 0.76, 0.55, 0.80, 1.18),
            (1.48, 0.70, 0.43, 0.78, 0.93),
        ],
        mats["glass"],
        root,
        1,
    )
    box("Sedan_PanoramicRoof", (0.0, 0.18, 1.405), (0.86, 1.22, 0.035), mats["roof_glass"], root, 0.025)
    for side in (-1.0, 1.0):
        box(f"Sedan_A_Pillar_{side:+.0f}", (side * 0.68, -0.92, 1.08), (0.065, 0.09, 0.62), mats["trim"], root, 0.012, rotation=(math.radians(-31), 0.0, 0.0))
        box(f"Sedan_B_Pillar_{side:+.0f}", (side * 0.78, 0.22, 1.10), (0.06, 0.09, 0.58), mats["trim"], root, 0.01)
        box(f"Sedan_C_Pillar_{side:+.0f}", (side * 0.70, 1.10, 1.03), (0.075, 0.10, 0.52), mats["trim"], root, 0.014, rotation=(math.radians(24), 0.0, 0.0))
    box("Sedan_FrontGrille", (0.0, -2.405, 0.50), (1.35, 0.055, 0.30), mats["trim"], root, 0.055)
    box("Sedan_LowerIntake", (0.0, -2.415, 0.30), (1.58, 0.05, 0.14), mats["carbon"], root, 0.035)
    for side in (-1.0, 1.0):
        box(f"Sedan_Headlamp_{side:+.0f}", (side * 0.64, -2.40, 0.67), (0.48, 0.045, 0.105), mats["headlight"], root, 0.04, rotation=(0.0, 0.0, side * math.radians(7)))
        box(f"Sedan_AirCurtain_{side:+.0f}", (side * 0.78, -2.39, 0.38), (0.22, 0.045, 0.22), mats["carbon"], root, 0.03)
        box(f"Sedan_TailLamp_{side:+.0f}", (side * 0.57, 2.395, 0.68), (0.52, 0.045, 0.105), mats["tail"], root, 0.035)
    box("Sedan_RearLightBridge", (0.0, 2.398, 0.70), (0.72, 0.035, 0.055), mats["tail"], root, 0.02)
    box("Sedan_RearDiffuser", (0.0, 2.405, 0.32), (1.64, 0.06, 0.22), mats["carbon"], root, 0.035)
    box("Sedan_FrontSplitter", (0.0, -2.42, 0.19), (1.76, 0.24, 0.055), mats["carbon"], root, 0.018)
    box("Sedan_RearDeckLip", (0.0, 2.20, 0.82), (1.30, 0.18, 0.055), mats["carbon"], root, 0.018)
    for side in (-1.0, 1.0):
        add_wheel_arch(root, f"Sedan_FenderFront_{side:+.0f}", side * 1.005, -1.43, 0.37, 0.405, 0.042, mats["sedan_paint"])
        add_wheel_arch(root, f"Sedan_FenderRear_{side:+.0f}", side * 1.005, 1.41, 0.37, 0.405, 0.042, mats["sedan_paint"])
        box(f"Sedan_SideSkirt_{side:+.0f}", (side * 1.01, 0.0, 0.28), (0.055, 2.08, 0.12), mats["carbon"], root, 0.022)
    add_side_details(root, 1.015, (-0.52, 0.78, 1.55), 0.45, 0.80, mats["seam"], mats["chrome"])
    add_mirror(root, -1.0, (1.02, -0.78, 0.96), mats["sedan_paint"], mats["mirror"])
    add_mirror(root, 1.0, (1.02, -0.78, 0.96), mats["sedan_paint"], mats["mirror"])
    add_four_wheels(root, -1.43, 1.41, 0.90, 0.37, 0.265, mats, 10)
    root["vehicle_type"] = "modern_fastback_sedan"
    root["dimensions_m"] = "4.76 x 2.02 x 1.44"
    return root


def build_crossover(mats):
    root = new_root("TrafficCrossoverModern")
    stations = [
        (-2.31, 0.48, 0.72, 0.70, 0.30, 0.27, 0.39, 0.60, 0.64),
        (-2.05, 0.76, 0.96, 0.91, 0.55, 0.27, 0.44, 0.72, 0.80),
        (-1.62, 0.88, 1.04, 1.00, 0.73, 0.28, 0.47, 0.85, 0.94),
        (-1.10, 0.91, 1.06, 1.02, 0.78, 0.28, 0.48, 0.91, 0.99),
        (-0.30, 0.92, 1.06, 1.03, 0.79, 0.29, 0.49, 0.94, 1.02),
        (0.58, 0.93, 1.06, 1.03, 0.80, 0.29, 0.49, 0.95, 1.03),
        (1.20, 0.93, 1.06, 1.03, 0.80, 0.29, 0.50, 0.94, 1.00),
        (1.72, 0.90, 1.03, 1.00, 0.75, 0.30, 0.51, 0.89, 0.93),
        (2.12, 0.80, 0.98, 0.96, 0.65, 0.31, 0.50, 0.80, 0.83),
        (2.31, 0.57, 0.83, 0.84, 0.42, 0.32, 0.48, 0.68, 0.70),
    ]
    loft_shell("Crossover_BodyShell", stations, mats["crossover_paint"], root, 0.026, 1)
    canopy(
        "Crossover_Glasshouse",
        [
            (-1.30, 0.80, 0.63, 0.95, 1.20),
            (-0.90, 0.82, 0.68, 0.98, 1.48),
            (-0.16, 0.83, 0.70, 1.00, 1.64),
            (0.75, 0.83, 0.70, 1.00, 1.63),
            (1.38, 0.81, 0.65, 0.98, 1.48),
            (1.72, 0.77, 0.57, 0.93, 1.18),
        ],
        mats["glass"],
        root,
        1,
    )
    box("Crossover_PanoramicRoof", (0.0, 0.28, 1.675), (1.02, 1.45, 0.04), mats["roof_glass"], root, 0.03)
    for side in (-1.0, 1.0):
        box(f"Crossover_RoofRail_{side:+.0f}", (side * 0.67, 0.30, 1.75), (0.055, 1.75, 0.065), mats["chrome"], root, 0.02)
        box(f"Crossover_WheelArchFront_{side:+.0f}", (side * 1.065, -1.38, 0.48), (0.055, 0.92, 0.28), mats["cladding"], root, 0.10)
        box(f"Crossover_WheelArchRear_{side:+.0f}", (side * 1.065, 1.33, 0.48), (0.055, 0.92, 0.28), mats["cladding"], root, 0.10)
        box(f"Crossover_Headlamp_{side:+.0f}", (side * 0.67, -2.325, 0.79), (0.50, 0.045, 0.11), mats["headlight"], root, 0.038)
        box(f"Crossover_DRL_{side:+.0f}", (side * 0.82, -2.32, 0.61), (0.22, 0.035, 0.065), mats["headlight"], root, 0.025)
        box(f"Crossover_TailLamp_{side:+.0f}", (side * 0.60, 2.325, 0.82), (0.55, 0.045, 0.12), mats["tail"], root, 0.04)
    box("Crossover_Grille", (0.0, -2.335, 0.55), (1.38, 0.05, 0.34), mats["trim"], root, 0.06)
    box("Crossover_SkidPlateFront", (0.0, -2.35, 0.30), (1.50, 0.16, 0.14), mats["silver_trim"], root, 0.035)
    box("Crossover_RearLightBridge", (0.0, 2.33, 0.84), (0.72, 0.035, 0.055), mats["tail"], root, 0.02)
    box("Crossover_RearSkidPlate", (0.0, 2.34, 0.34), (1.55, 0.14, 0.17), mats["silver_trim"], root, 0.04)
    box("Crossover_SideCladding_L", (-1.055, 0.0, 0.37), (0.055, 3.45, 0.17), mats["cladding"], root, 0.035)
    box("Crossover_SideCladding_R", (1.055, 0.0, 0.37), (0.055, 3.45, 0.17), mats["cladding"], root, 0.035)
    for side in (-1.0, 1.0):
        add_wheel_arch(root, f"Crossover_ArchFront_{side:+.0f}", side * 1.075, -1.40, 0.42, 0.47, 0.050, mats["cladding"])
        add_wheel_arch(root, f"Crossover_ArchRear_{side:+.0f}", side * 1.075, 1.34, 0.42, 0.47, 0.050, mats["cladding"])
    add_side_details(root, 1.065, (-0.48, 0.77, 1.58), 0.53, 0.98, mats["seam"], mats["chrome"])
    add_mirror(root, -1.0, (1.06, -0.78, 1.12), mats["crossover_paint"], mats["mirror"])
    add_mirror(root, 1.0, (1.06, -0.78, 1.12), mats["crossover_paint"], mats["mirror"])
    add_four_wheels(root, -1.40, 1.34, 0.94, 0.42, 0.29, mats, 9)
    root["vehicle_type"] = "modern_performance_crossover"
    root["dimensions_m"] = "4.62 x 2.12 x 1.76"
    return root


def build_delivery_truck(mats):
    root = new_root("TrafficDeliveryTruckModern")
    cab_stations = [
        (-3.52, 0.70, 1.03, 0.98, 0.62, 0.25, 0.45, 1.72, 2.38),
        (-3.28, 0.92, 1.16, 1.12, 0.88, 0.25, 0.47, 1.98, 2.73),
        (-2.86, 1.03, 1.20, 1.17, 0.98, 0.25, 0.49, 2.15, 2.91),
        (-2.20, 1.06, 1.21, 1.18, 1.02, 0.26, 0.50, 2.28, 3.02),
        (-1.62, 1.06, 1.20, 1.17, 1.02, 0.27, 0.52, 2.30, 3.04),
        (-1.36, 0.98, 1.14, 1.12, 0.96, 0.29, 0.54, 2.28, 3.00),
    ]
    loft_shell("Truck_AeroCab", cab_stations, mats["truck_paint"], root, 0.04, 1)
    box("Truck_CargoBody", (0.0, 1.03, 1.72), (2.42, 4.72, 2.82), mats["truck_box"], root, 0.085, segments=5)
    box("Truck_CargoRoof", (0.0, 1.03, 3.15), (2.32, 4.62, 0.10), mats["silver_trim"], root, 0.045)
    box("Truck_CargoLowerRail_L", (-1.225, 1.02, 0.61), (0.07, 4.56, 0.20), mats["truck_trim"], root, 0.018)
    box("Truck_CargoLowerRail_R", (1.225, 1.02, 0.61), (0.07, 4.56, 0.20), mats["truck_trim"], root, 0.018)
    for side in (-1.0, 1.0):
        for index, y in enumerate((-0.95, -0.15, 0.65, 1.45, 2.25, 3.05)):
            box(f"Truck_CargoRib_{side:+.0f}_{index + 1}", (side * 1.225, y, 1.78), (0.055, 0.075, 2.45), mats["truck_trim"], root, 0.008)
        box(f"Truck_MirrorArm_{side:+.0f}", (side * 1.31, -2.78, 2.15), (0.25, 0.05, 0.06), mats["truck_trim"], root, 0.015)
        box(f"Truck_Mirror_{side:+.0f}", (side * 1.43, -2.78, 2.17), (0.12, 0.30, 0.48), mats["truck_trim"], root, 0.04)
        box(f"Truck_MirrorGlass_{side:+.0f}", (side * 1.495, -2.80, 2.17), (0.018, 0.24, 0.39), mats["mirror"], root, 0.015)
        box(f"Truck_Headlamp_{side:+.0f}", (side * 0.78, -3.54, 0.80), (0.50, 0.055, 0.22), mats["headlight"], root, 0.045)
        box(f"Truck_DRL_{side:+.0f}", (side * 0.91, -3.55, 1.03), (0.22, 0.04, 0.05), mats["headlight"], root, 0.02)
        box(f"Truck_TailLamp_{side:+.0f}", (side * 0.88, 3.42, 0.68), (0.27, 0.045, 0.36), mats["tail"], root, 0.035)
    box("Truck_Windshield", (0.0, -3.43, 2.13), (1.92, 0.07, 1.10), mats["glass"], root, 0.07, rotation=(math.radians(-11), 0.0, 0.0), segments=4)
    for side in (-1.0, 1.0):
        box(f"Truck_SideWindow_{side:+.0f}", (side * 1.205, -2.58, 2.14), (0.035, 0.88, 0.78), mats["glass"], root, 0.035)
        box(f"Truck_DoorSeam_{side:+.0f}", (side * 1.218, -2.17, 1.32), (0.018, 1.10, 1.68), mats["seam"], root, 0.004)
        box(f"Truck_DoorHandle_{side:+.0f}", (side * 1.232, -1.90, 1.58), (0.025, 0.22, 0.055), mats["chrome"], root, 0.012)
    box("Truck_SunVisor", (0.0, -3.39, 2.78), (1.94, 0.12, 0.12), mats["truck_trim"], root, 0.035)
    box("Truck_Grille", (0.0, -3.56, 1.12), (1.44, 0.055, 0.48), mats["truck_trim"], root, 0.065)
    for index in range(5):
        box(f"Truck_GrilleBar_{index + 1}", (0.0, -3.594, 0.96 + index * 0.08), (1.18, 0.018, 0.025), mats["chrome"], root, 0.006)
    box("Truck_FrontBumper", (0.0, -3.60, 0.42), (2.12, 0.18, 0.20), mats["truck_trim"], root, 0.055)
    box("Truck_Step_L", (-1.06, -2.08, 0.39), (0.27, 0.72, 0.13), mats["truck_trim"], root, 0.025)
    box("Truck_Step_R", (1.06, -2.08, 0.39), (0.27, 0.72, 0.13), mats["truck_trim"], root, 0.025)
    box("Truck_RearDoor", (0.0, 3.43, 1.73), (2.20, 0.055, 2.58), mats["truck_box"], root, 0.025)
    for x in (-0.90, -0.45, 0.0, 0.45, 0.90):
        box(f"Truck_RearDoorRib_{x:+.2f}", (x, 3.47, 1.72), (0.045, 0.03, 2.30), mats["truck_trim"], root, 0.007)
    box("Truck_RearBumper", (0.0, 3.50, 0.38), (2.18, 0.18, 0.18), mats["truck_trim"], root, 0.04)
    add_four_wheels(root, -2.27, 2.18, 1.10, 0.49, 0.32, mats, 8)
    root["vehicle_type"] = "modern_aerodynamic_delivery_truck"
    root["dimensions_m"] = "7.10 x 2.44 x 3.20"
    return root


def build_city_bus(mats):
    root = new_root("TrafficCityBusModern")
    body_stations = [
        (-5.38, 0.94, 1.18, 1.14, 0.86, 0.24, 0.44, 2.55, 3.02),
        (-5.12, 1.10, 1.27, 1.22, 1.05, 0.23, 0.43, 2.78, 3.20),
        (-4.58, 1.15, 1.29, 1.24, 1.10, 0.23, 0.43, 2.90, 3.28),
        (-3.20, 1.16, 1.29, 1.24, 1.11, 0.23, 0.43, 2.92, 3.30),
        (-1.20, 1.16, 1.29, 1.24, 1.11, 0.23, 0.43, 2.92, 3.30),
        (1.20, 1.16, 1.29, 1.24, 1.11, 0.23, 0.43, 2.92, 3.30),
        (3.20, 1.16, 1.29, 1.24, 1.11, 0.23, 0.43, 2.92, 3.30),
        (4.52, 1.15, 1.28, 1.23, 1.08, 0.24, 0.44, 2.88, 3.27),
        (5.20, 1.02, 1.20, 1.18, 0.97, 0.25, 0.45, 2.72, 3.12),
        (5.38, 0.86, 1.08, 1.08, 0.74, 0.26, 0.46, 2.48, 2.86),
    ]
    loft_shell("Bus_MainBody", body_stations, mats["bus_paint"], root, 0.055, 1)
    box("Bus_RoofEquipment", (0.0, 0.45, 3.39), (1.42, 2.60, 0.22), mats["bus_roof"], root, 0.10, segments=5)
    box("Bus_FrontWindshield", (0.0, -5.31, 2.31), (2.10, 0.075, 1.22), mats["bus_glass"], root, 0.085, rotation=(math.radians(-7), 0.0, 0.0), segments=5)
    box("Bus_LowerWindshield", (0.0, -5.39, 1.52), (2.08, 0.055, 0.42), mats["bus_glass"], root, 0.055, rotation=(math.radians(-3), 0.0, 0.0), segments=4)
    box("Bus_DestinationPanel", (0.0, -5.36, 2.96), (1.62, 0.04, 0.25), mats["destination"], root, 0.035)
    box("Bus_LowerFront", (0.0, -5.39, 0.82), (2.20, 0.09, 0.72), mats["bus_trim"], root, 0.08)
    box("Bus_FrontBumper", (0.0, -5.45, 0.35), (2.24, 0.18, 0.22), mats["bus_trim"], root, 0.07)
    box("Bus_RearWindow", (0.0, 5.34, 2.40), (1.86, 0.055, 1.05), mats["bus_glass"], root, 0.07)
    box("Bus_RearVent", (0.0, 5.39, 1.18), (1.76, 0.045, 0.48), mats["bus_trim"], root, 0.055)
    box("Bus_RearBumper", (0.0, 5.45, 0.35), (2.24, 0.17, 0.22), mats["bus_trim"], root, 0.065)

    window_centers = (-4.28, -3.28, -2.28, -1.28, -0.28, 0.72, 1.72, 2.72, 3.72, 4.58)
    for side in (-1.0, 1.0):
        for index, y in enumerate(window_centers):
            width_y = 0.78 if index not in (0, len(window_centers) - 1) else 0.68
            box(
                f"Bus_Window_{'L' if side < 0 else 'R'}_{index + 1:02d}",
                (side * 1.286, y, 2.23),
                (0.035, width_y, 0.92),
                mats["bus_glass"],
                root,
                0.025,
            )
        box(f"Bus_BlueBelt_{side:+.0f}", (side * 1.305, 0.0, 1.48), (0.045, 9.92, 0.15), mats["bus_accent"], root, 0.015)
        box(f"Bus_LowerSkirt_{side:+.0f}", (side * 1.295, 0.0, 0.53), (0.055, 9.72, 0.30), mats["bus_trim"], root, 0.04)

    # A glazed two-leaf passenger door on the curb side gives the bus a true low-floor silhouette.
    for index, y in enumerate((-3.92, -3.48)):
        box(f"Bus_DoorGlass_{index + 1}", (1.322, y, 1.47), (0.035, 0.34, 1.68), mats["bus_glass"], root, 0.018)
        box(f"Bus_DoorFrame_{index + 1}", (1.342, y, 1.47), (0.025, 0.045, 1.82), mats["bus_trim"], root, 0.008)

    for side in (-1.0, 1.0):
        box(f"Bus_Headlamp_{side:+.0f}", (side * 0.78, -5.43, 0.77), (0.46, 0.04, 0.18), mats["headlight"], root, 0.045)
        box(f"Bus_DRL_{side:+.0f}", (side * 0.98, -5.42, 1.05), (0.10, 0.035, 0.40), mats["headlight"], root, 0.025)
        box(f"Bus_TailLamp_{side:+.0f}", (side * 0.98, 5.42, 0.92), (0.14, 0.035, 0.58), mats["tail"], root, 0.03)
        box(f"Bus_MirrorArm_{side:+.0f}", (side * 1.40, -4.76, 2.38), (0.34, 0.06, 0.07), mats["bus_trim"], root, 0.015)
        box(f"Bus_Mirror_{side:+.0f}", (side * 1.55, -4.78, 2.37), (0.14, 0.26, 0.44), mats["bus_trim"], root, 0.045)
        box(f"Bus_MirrorGlass_{side:+.0f}", (side * 1.625, -4.80, 2.37), (0.018, 0.21, 0.35), mats["mirror"], root, 0.012)

    add_four_wheels(root, -3.65, 3.48, 1.18, 0.53, 0.34, mats, 8)
    root["vehicle_type"] = "modern_low_floor_city_bus"
    root["dimensions_m"] = "10.80 x 2.58 x 3.42"
    return root


def create_material_library():
    mats = {
        "sedan_paint": material("M_TrafficPaint_Sedan_Graphite", (0.055, 0.075, 0.095, 1.0), 0.86, 0.20),
        "crossover_paint": material("M_TrafficPaint_Crossover_Ruby", (0.36, 0.012, 0.016, 1.0), 0.82, 0.19),
        "truck_paint": material("M_TrafficPaint_Truck_PearlWhite", (0.72, 0.76, 0.79, 1.0), 0.56, 0.26),
        "truck_box": material("M_Truck_Cargo_Satin", (0.52, 0.57, 0.59, 1.0), 0.30, 0.34),
        "bus_paint": material("M_TrafficPaint_Bus_Silver", (0.48, 0.52, 0.54, 1.0), 0.62, 0.25),
        "bus_accent": material("M_Bus_Accent_Teal", (0.015, 0.23, 0.32, 1.0), 0.70, 0.24),
        "glass": material("M_Traffic_Glass_Smoke", (0.006, 0.024, 0.036, 1.0), 0.22, 0.24),
        "roof_glass": material("M_Traffic_RoofGlass", (0.006, 0.015, 0.024, 1.0), 0.31, 0.20),
        "bus_glass": material("M_Bus_WindowGlass", (0.005, 0.030, 0.042, 1.0), 0.26, 0.23),
        "mirror": material("M_MirrorGlass", (0.10, 0.14, 0.16, 1.0), 0.82, 0.07),
        "trim": material("M_Traffic_Trim_Black", (0.008, 0.011, 0.014, 1.0), 0.22, 0.30),
        "truck_trim": material("M_Truck_Trim", (0.012, 0.017, 0.020, 1.0), 0.25, 0.34),
        "bus_trim": material("M_Bus_Trim", (0.014, 0.020, 0.024, 1.0), 0.27, 0.32),
        "bus_roof": material("M_Bus_RoofEquipment", (0.18, 0.20, 0.21, 1.0), 0.42, 0.42),
        "destination": material("M_Bus_DestinationPanel", (0.008, 0.013, 0.017, 1.0), 0.22, 0.23),
        "carbon": material("M_Traffic_Carbon", (0.009, 0.011, 0.013, 1.0), 0.42, 0.29),
        "cladding": material("M_Crossover_Cladding", (0.018, 0.021, 0.023, 1.0), 0.18, 0.43),
        "chrome": material("M_Traffic_Chrome", (0.46, 0.50, 0.52, 1.0), 0.93, 0.12),
        "silver_trim": material("M_Traffic_SatinAluminum", (0.37, 0.40, 0.42, 1.0), 0.78, 0.25),
        "seam": material("M_Traffic_PanelSeam", (0.005, 0.006, 0.007, 1.0), 0.12, 0.46),
        "tire": material("M_Traffic_Tire", (0.006, 0.007, 0.008, 1.0), 0.02, 0.67),
        "rim": material("M_Traffic_Rim", (0.16, 0.18, 0.20, 1.0), 0.88, 0.18),
        "spoke": material("M_Traffic_Spoke", (0.24, 0.27, 0.29, 1.0), 0.90, 0.16),
        "brake": material("M_Traffic_BrakeDisc", (0.22, 0.24, 0.25, 1.0), 0.82, 0.31),
        "hub": material("M_Traffic_WheelHub", (0.035, 0.039, 0.042, 1.0), 0.88, 0.18),
        "caliper": material("M_Traffic_BrakeCaliper", (0.72, 0.025, 0.012, 1.0), 0.62, 0.22),
    }
    mats["headlight"] = material(
        "M_Traffic_LED_Headlight",
        (0.70, 0.86, 1.0, 1.0),
        0.20,
        0.12,
        emission=(0.58, 0.82, 1.0, 1.0),
        emission_strength=4.0,
    )
    mats["tail"] = material(
        "M_Traffic_LED_Tail",
        (0.65, 0.006, 0.002, 1.0),
        0.14,
        0.16,
        emission=(1.0, 0.004, 0.001, 1.0),
        emission_strength=4.5,
    )
    return mats


def hierarchy(root):
    objects = [root]
    queue = list(root.children)
    while queue:
        current = queue.pop()
        objects.append(current)
        queue.extend(list(current.children))
    return objects


def export_vehicle(root, filename):
    bpy.ops.object.select_all(action="DESELECT")
    for obj in hierarchy(root):
        if obj.type in {"EMPTY", "MESH"}:
            obj.select_set(True)
    bpy.context.view_layer.objects.active = root
    bpy.ops.export_scene.fbx(
        filepath=str(MODEL_DIR / filename),
        use_selection=True,
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_UNITS",
        object_types={"EMPTY", "MESH"},
        bake_space_transform=False,
        add_leaf_bones=False,
        use_mesh_modifiers=True,
        mesh_smooth_type="FACE",
    )


def add_preview_studio(roots):
    floor_mat = material("M_PreviewFloor", (0.055, 0.062, 0.070, 1.0), 0.15, 0.38)
    floor = box("Preview_Floor", (0.0, 1.6, -0.09), (18.0, 20.0, 0.18), floor_mat, bevel=0.08)
    floor["preview_only"] = True

    preview_positions = {
        "TrafficSedanModern": (-2.55, -2.80, 0.0),
        "TrafficCrossoverModern": (2.55, -2.20, 0.0),
        "TrafficDeliveryTruckModern": (-2.75, 3.55, 0.0),
        "TrafficCityBusModern": (2.90, 4.55, 0.0),
    }
    for root in roots:
        root.location = preview_positions[root.name]

    bpy.ops.object.light_add(type="AREA", location=(-5.0, -7.0, 10.5))
    key = bpy.context.object
    key.name = "Preview_Key"
    key.data.energy = 1550
    key.data.shape = "DISK"
    key.data.size = 7.0
    bpy.ops.object.light_add(type="AREA", location=(7.0, 1.5, 7.0))
    fill = bpy.context.object
    fill.name = "Preview_Fill"
    fill.data.energy = 1100
    fill.data.color = (0.54, 0.72, 1.0)
    fill.data.size = 6.0
    bpy.ops.object.light_add(type="AREA", location=(-5.0, 9.0, 5.0))
    rim = bpy.context.object
    rim.name = "Preview_Rim"
    rim.data.energy = 1050
    rim.data.color = (1.0, 0.52, 0.23)
    rim.data.size = 5.0

    bpy.ops.object.camera_add(location=(13.4, -18.8, 10.2))
    camera = bpy.context.object
    camera.name = "Preview_Camera"
    camera.data.lens = 56
    bpy.context.scene.camera = camera
    target = bpy.data.objects.new("Preview_Target", None)
    bpy.context.collection.objects.link(target)
    target.location = (0.0, 1.4, 1.1)
    constraint = camera.constraints.new(type="TRACK_TO")
    constraint.target = target
    constraint.track_axis = "TRACK_NEGATIVE_Z"
    constraint.up_axis = "UP_Y"


def render_preview():
    scene = bpy.context.scene
    try:
        scene.render.engine = "BLENDER_EEVEE_NEXT"
    except Exception:
        scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 2000
    scene.render.resolution_y = 1250
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.film_transparent = False
    scene.render.filepath = str(PREVIEW_DIR / "modern_traffic_fleet_preview.png")
    scene.render.image_settings.color_mode = "RGBA"
    scene.world.color = (0.025, 0.032, 0.045)
    scene.view_settings.exposure = -0.35
    try:
        scene.view_settings.look = "AgX - Medium High Contrast"
    except Exception:
        pass
    bpy.ops.render.render(write_still=True)


def main():
    MODEL_DIR.mkdir(parents=True, exist_ok=True)
    PREVIEW_DIR.mkdir(parents=True, exist_ok=True)
    clear_scene()
    mats = create_material_library()
    roots = [
        build_sedan(mats),
        build_crossover(mats),
        build_delivery_truck(mats),
        build_city_bus(mats),
    ]

    outputs = (
        (roots[0], "traffic_sedan_modern.fbx"),
        (roots[1], "traffic_crossover_modern.fbx"),
        (roots[2], "traffic_delivery_truck_modern.fbx"),
        (roots[3], "traffic_city_bus_modern.fbx"),
    )
    for root, filename in outputs:
        export_vehicle(root, filename)

    add_preview_studio(roots)
    render_preview()
    for root in roots:
        root.location = (0.0, 0.0, 0.0)
    bpy.ops.wm.save_as_mainfile(filepath=str(MODEL_DIR / "modern_traffic_fleet.blend"))
    print("Modern traffic fleet exported successfully.")


if __name__ == "__main__":
    main()

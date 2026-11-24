"""Author the original premium fleet. Run with Blender --background --python."""

import math
import sys
from dataclasses import dataclass
from pathlib import Path

import bpy
from mathutils import Vector

sys.path.insert(0, str(Path(__file__).resolve().parent))
import create_modern_traffic_fleet as art


@dataclass(frozen=True)
class Design:
    key: str
    length: float
    width: float
    wheel_radius: float
    front: float
    rear: float
    deck: tuple
    canopy: tuple
    accent: tuple
    spokes: int


FLEET = (
    Design("velocity", 4.58, 2.08, .354, -1.38, 1.40, (.48, .66, .73, .78, .68),
           ((-.89,.68,.44,.68,.74),(-.49,.72,.55,.71,1.05),(-.01,.73,.56,.73,1.13),
            (.51,.71,.54,.74,1.11),(1.18,.63,.39,.73,.79)), (.76,.12,.033), 10),
    Design("bulwark", 5.44, 2.26, .457, -1.63, 1.62, (.91, 1.14, 1.20, 1.08, 1.04),
           ((-1.25,.78,.66,1.12,1.31),(-.88,.85,.74,1.15,1.87),(-.38,.87,.74,1.15,1.92),
            (.43,.85,.74,1.15,1.91),(.78,.83,.71,1.14,1.74),(.90,.81,.67,1.13,1.15)), (.78,.42,.054), 8),
    Design("solstice", 4.80, 1.98, .359, -1.52, 1.35, (.55, .72, .77, .73, .64),
           ((-.64,.71,.51,.73,.77),(-.29,.73,.55,.74,1.10),(.07,.72,.57,.76,1.24),
            (.62,.72,.55,.74,1.21),(1.17,.69,.47,.73,.98),(1.69,.56,.32,.72,.74)), (.23,.49,.68), 12),
    Design("switchback", 4.12, 1.91, .369, -1.26, 1.19, (.68, .86, .88, .86, .76),
           ((-.89,.67,.51,.82,.94),(-.54,.73,.58,.87,1.31),(-.12,.76,.65,.89,1.43),
            (.71,.76,.65,.89,1.44),(1.26,.73,.63,.87,1.39),(1.54,.70,.57,.83,1.14)), (.95,.31,.023), 15),
    Design("undertow", 4.96, 2.01, .371, -1.49, 1.54, (.58, .74, .84, .78, .67),
           ((-1.28,.73,.54,.76,.80),(-.79,.77,.55,.79,1.05),(-.40,.78,.63,.82,1.33),
            (.38,.78,.65,.83,1.37),(.88,.75,.59,.81,1.27),(1.70,.64,.45,.79,.86)), (.13,.86,.89), 8),
    Design("monarch", 4.99, 2.04, .373, -1.47, 1.51, (.63, .81, .90, .85, .75),
           ((-.89,.74,.52,.83,.87),(-.46,.78,.62,.88,1.30),(-.10,.79,.64,.89,1.40),
            (.64,.80,.67,.89,1.43),(1.27,.77,.62,.88,1.38),(1.75,.74,.53,.86,1.14)), (.76,.61,.31), 14),
)


def materials(design):
    def make(label, rgb, metal=0., roughness=.4):
        return art.material(f"M_Premium_{label}", (*rgb, 1), metal, roughness)

    def lens(label, rgb, strength):
        return art.material(f"M_Premium_{label}", (*rgb, 1), .08, .25, (*rgb, 1), strength)

    return {
        "paint": art.material("M_Garage_Paint_Premium", (.39,.41,.45,1), .54,.22),
        "black": make("Carbon", (.035,.041,.048), .21,.36),
        "accent": make("Accent", design.accent, .62,.32),
        "metal": make("Alloy", (.38,.44,.51), .76,.26),
        "rim": make("Rim", (.20,.24,.29), .83,.30),
        "dark": make("Rubber", (.026,.031,.037), .04,.62),
        "interior": make("Cabin", (.048,.047,.046), .04,.58),
        "glass": make("Glass", (.22,.30,.36), .10,.20),
        "head": lens("Headlamp", (.69,.85,1), 1.3),
        "drl": lens("RunningLight", (.53,.83,1), .85),
        "tail": lens("Taillamp", (.88,.028,.014), .9),
        "amber": lens("Marker", (1,.39,.041), .85),
    }


def carved_body(root, design, mat):
    half_length, half_width = design.length / 2, design.width / 2
    nose, hood, belt, deck, tail = design.deck
    lift = .35 if design.key == "bulwark" else .20
    ring_data = (
        (-1,.79,nose),(-.94,.88,nose+.09),(-.77,.96,hood-.02),(-.59,1,hood),
        (-.34,.98,(hood+belt)*.5),(-.05,.975,belt),(.27,.985,belt-.016),
        (.56,1.,deck),(.77,.98,deck-.018),(.94,.89,tail+.055),(1.,.76,tail),
    )
    rings = []
    for fraction, width, height in ring_data:
        shoulder = height - .034 if abs(fraction) > .90 else max(height-.023, design.wheel_radius * 2 + .131)
        w = half_width * width
        rings.append((fraction * half_length, w*.80,w*.99,w*.97,w*.67,lift,lift+.15,shoulder,height))
    body = art.loft_shell("Sculpted exterior panels", rings, mat, root, .018, 1)
    for modifier in tuple(body.modifiers):
        bpy.ops.object.modifier_apply(modifier=modifier.name)
    for side in (-1, 1):
        for axle in (design.front, design.rear):
            bpy.ops.mesh.primitive_cylinder_add(vertices=48, radius=design.wheel_radius+.045, depth=.49,
                location=(side*(half_width+.077), axle, design.wheel_radius), rotation=(0, math.pi/2, 0))
            cutter = bpy.context.object
            bpy.context.view_layer.objects.active = body
            cut = body.modifiers.new("Radiused wheel opening", "BOOLEAN")
            cut.operation, cut.solver, cut.object = "DIFFERENCE", "EXACT", cutter
            bpy.ops.object.modifier_apply(modifier=cut.name)
            bpy.data.objects.remove(cutter, do_unlink=True)
    return body


def strip(name, points, width, material, root):
    for index, (start, end) in enumerate(zip(points, points[1:])):
        dx, dy, dz = (end[n] - start[n] for n in range(3))
        length = math.sqrt(dx*dx + dy*dy + dz*dz)
        part = art.box(f"{name} {index+1}", tuple((start[n]+end[n])*.5 for n in range(3)),
            (width,width,length+width*.40), material, root, width*.42, segments=2)
        part.rotation_euler = Vector((dx, dy, dz)).to_track_quat("Z", "Y").to_euler()


def wheels(root, design, mats):
    radius = design.wheel_radius
    width = .34 if design.key == "bulwark" else .28
    track = design.width * .50 - width * .40
    sides = (("FL",-1,design.front),("FR",1,design.front),("RL",-1,design.rear),("RR",1,design.rear))
    for name, side, axle in sides:
        center = (side*track, axle, radius)
        item = art.new_root("Wheel_" + name)
        item.location = center
        bpy.context.view_layer.update()
        art.parent_keep_world(item, root)
        rings = ((-.50,.645),(-.50,.80),(-.46,.93),(-.36,.985),(-.23,1),
                 (.23,1),(.36,.985),(.46,.93),(.50,.80),(.50,.645))
        segments = 56
        verts = [(center[0]+fraction*width, axle+ratio*radius*math.cos(math.tau*i/segments),
                  radius+ratio*radius*math.sin(math.tau*i/segments)) for fraction,ratio in rings for i in range(segments)]
        faces = []
        for ring in range(len(rings) - 1):
            for i in range(segments):
                here, nxt = ring*segments+i, ring*segments+(i+1)%segments
                faces.append((here,nxt,nxt+segments,here+segments))
        art.mesh_object("Performance tyre " + name, verts, faces, mats["dark"], item, 0)
        axis = (0,math.pi/2,0)
        outer_x = center[0] + side * width * .487
        art.cylinder("Rim barrel",(outer_x-side*.026,axle,radius),radius*.646,.05,mats["rim"],item,56,axis,.008)
        art.cylinder("Brake rotor",(outer_x+side*.003,axle,radius),radius*.52,.018,mats["black"],item,48,axis,.002)
        art.cylinder("Rim center lock",(outer_x+side*.023,axle,radius),radius*.139,.025,mats["metal"],item,24,axis,.004)
        spokes = []
        for i in range(design.spokes):
            angle=math.tau*i/design.spokes
            spokes.append(art.box("Rim spoke",(outer_x+side*.016,axle-math.sin(angle)*radius*.384,radius+math.cos(angle)*radius*.384),
                (.024, radius*(.25 if design.key == "undertow" else .075),radius*.510),mats["metal"],item,.007,(angle,0,0),2))
        bpy.ops.object.select_all(action="DESELECT")
        for spoke in spokes:
            spoke.select_set(True)
        bpy.context.view_layer.objects.active = spokes[0]
        bpy.ops.object.join()
        spokes[0].name = "Alloy spoke face " + name


def exterior(root, design, mats):
    width = design.width*.5
    half = design.length*.5
    nose, _, belt, _, tail = design.deck
    black, metal, accent = mats["black"], mats["metal"], mats["accent"]
    head, drl, rear = mats["head"], mats["drl"], mats["tail"]
    front_y, rear_y = -half-.012, half+.011
    lamp_z, tail_z = nose-.036, tail-.025
    art.box("Front lower splitter",(0,front_y+.065,nose-.32),(width*1.55,.28,.065),black,root,.025)
    art.box("Rear diffuser",(0,rear_y-.06,tail-.32),(width*1.53,.205,.12),black,root,.03)
    if design.key == "undertow":
        art.box("Closed electric nose panel",(0,front_y+.003,nose-.118),(1.36,.11,.141),mats["paint"],root,.047)
    else:
        grille_height = .24 if design.key in ("bulwark","monarch") else .146
        art.box("Recessed radiator grille",(0,front_y+.008,nose-.147),(.92,.108,grille_height),black,root,.045)
        for rib in range(-6,7):
            if design.key == "velocity":
                break
            art.box("Recessed grille vane",(rib*.054,front_y-.048,nose-.147),(.012,.022,grille_height*.72),metal,root,.006)
    for side in (-1,1):
        def front(name, x, rise, span, height, material=head, back=.0):
            art.box(name,(side*x,front_y+back,lamp_z+rise),(span,.08,height),material,root,min(.025,height*.44))

        def back(name, x, rise, span, height, material=rear):
            art.box(name,(side*x,rear_y,tail_z+rise),(span,.067,height),material,root,min(.024,height*.39))

        art.box("Recessed lamp housing",(side*width*.61,front_y+.026,lamp_z-.007),(.42,.153,.124),black,root,.053)
        art.box("Air curtain",(side*width*.63,front_y+.069,nose-.21),(.233,.118,.13),black,root,.033)
        art.box("Low sill molding",(side*width*.954,.04,.38 if design.key == "bulwark" else .25),
            (.08, abs(design.front)+design.rear-design.wheel_radius*2.-.13,.072),black,root,.021)
        for axle in (design.front,design.rear):
            art.add_wheel_arch(root,"Rolled fender lip",side*width*.969,axle,design.wheel_radius,
                design.wheel_radius+.049,.026 if design.key in ("switchback","bulwark") else .015,
                black if design.key in ("switchback","bulwark") else mats["paint"])
        mirror = design.canopy[1]
        art.add_mirror(root,side,(mirror[1]+.20,mirror[0]+.10,mirror[3]+.15),mats["paint"],metal)
        handle_y = -.03 if design.key in ("velocity","solstice") else -.39
        for handle in ((handle_y,) if design.key in ("velocity","solstice") else (handle_y,.62)):
            art.box("Flush door handle",(side*width*.961,handle,belt-.114),(.023,.147,.027),metal,root,.007)

        if design.key == "velocity":
            front("Blade projector",.59,.009,.39,.054)
            strip("Swept hook daytime light",((side*.44,front_y-.036,lamp_z+.066),
                (side*.70,front_y-.034,lamp_z+.061),(side*.788,front_y+.171,lamp_z+.015)),.023,drl,root)
            back("Stacked rear blade",.565,-.012,.36,.039)
            back("Stacked rear blade",.565,.047,.36,.026)
            art.box("Sidepod intake",(side*.956,.79,.582),(.101,.56,.228),black,root,.056)
        elif design.key == "bulwark":
            front("Stacked work headlamp",.64,.045,.38,.124)
            front("Amber work marker",.66,-.080,.39,.038,mats["amber"])
            back("Vertical truck taillamp",.669,-.088,.096,.30)
        elif design.key == "solstice":
            for x in (.47,.60,.73):
                front("Triad headlamp",x,.015,.090,.067)
                back("Triad taillamp",x,-.019,.085,.076)
            art.box("Bonnet extraction vent",(side*.37,-1.34,.716),(.182,.44,.026),black,root,.018)
        elif design.key == "switchback":
            front("Rally divided beam",.56,.024,.33,.084)
            front("Rally fog projector",.53,-.224,.169,.112)
            back("Upright hatch taillamp",.69,.016,.107,.24)
            art.box("Competition mudflap",(side*.80,design.rear+.34,.20),(.25,.048,.30),black,root,.012)
            art.box("Hood heat extraction",(side*.31,-1.10,.872),(.29,.34,.046),black,root,.022)
        elif design.key == "undertow":
            front("Recessed corner projector",.67,-.070,.231,.051)
            back("Rear light end cap",.64,.023,.22,.065)
            art.box("Battery sill copper inlay",(side*.962,.08,.368),(.038,1.78,.024),accent,root,.009)
        else:
            front("Split beam upper",.60,.039,.38,.031)
            front("Split beam lower",.60,-.030,.38,.044)
            back("Double-layer rear ribbon",.65,.013,.35,.048)
            back("Double-layer rear ribbon",.65,-.048,.35,.033)

    if design.key == "velocity":
        art.box("Velocity RS carbon aerofoil",(0,1.79,1.125),(1.82,.25,.058),black,root,.021)
        for side in (-1,1):
            art.box("Swan-neck rear wing mount",(side*.49,1.78,.953),(.050,.124,.30),metal,root,.011,(.16,0,0))
        art.box("Wing trailing-edge accent",(0,1.915,1.123),(1.64,.037,.031),accent,root,.012)
    elif design.key == "bulwark":
        art.box("Bulwark upper brush guard",(0,front_y-.064,nose+.075),(1.64,.15,.098),black,root,.032)
        art.box("Bed liner",(0,1.65,1.067),(1.58,1.77,.074),black,root,.035)
        for side in (-1,1):
            art.box("Bed guard",(side*.88,1.64,1.198),(.14,1.69,.199),mats["paint"],root,.034)
            strip("Bed cage upright",((side*.78,1.22,1.10),(side*.71,.92,1.76)),.074,black,root)
            strip("Brush guard upright",((side*.57,front_y-.05,nose-.24),(side*.57,front_y-.108,nose+.11)),.086,black,root)
        art.box("Bed cage crossbar",(0,.92,1.76),(1.46,.075,.072),black,root,.023)
        for i in range(-2,3):
            art.box("Amber dust marker",(i*.21,.974,1.764),(.118,.055,.045),mats["amber"],root,.014)
        art.box("Brushed front skid plate",(0,front_y+.10,.51),(1.35,.38,.098),metal,root,.027)
    elif design.key == "switchback":
        art.box("Rally roof scoop",(0,.10,1.474),(.53,.39,.082),black,root,.035)
        art.box("Rally hatch aerofoil",(0,1.59,1.425),(1.51,.25,.055),black,root,.021)
        for side in (-1,1):
            strip("Rally wing mount",((side*.51,1.34,1.323),(side*.51,1.56,1.408)),.066,black,root)
    elif design.key == "undertow":
        art.box("Full-width front luminous belt",(0,front_y-.025,lamp_z+.054),(1.53,.067,.036),drl,root,.013)
        art.box("Full-width rear luminous belt",(0,rear_y+.019,tail_z+.025),(1.57,.052,.032),rear,root,.012)
    elif design.key == "monarch":
        for side in (-1,1):
            strip("Shaped metal roof rail",((side*.53,-.07,1.387),(side*.53,.65,1.448),
                (side*.53,1.20,1.401)),.044,metal,root)
            strip("Metal rail foot",((side*.53,1.16,1.35),(side*.53,1.20,1.40)),.060,black,root)
            strip("Liftgate spoiler mount",((side*.52,1.36,1.303),(side*.52,1.60,1.349)),.074,black,root)
        art.box("Shooting brake liftgate spoiler",(0,1.63,1.365),(1.39,.22,.062),black,root,.023)
    else:
        art.box("Touring crest",(0,front_y-.028,nose-.146),(.094,.035,.077),accent,root,.021)
        art.box("Integrated fastback deck lip",(0,half-.16,tail+.058),(1.42,.17,.058),mats["paint"],root,.025)


def build(design):
    root = art.new_root(design.key)
    mats = materials(design)
    carved_body(root, design, mats["paint"])
    roof_level = glazing(root, design, mats)
    for side in (-1,1):
        for a,b in zip(design.canopy,design.canopy[1:]):
            strip("Belt molding",((side*a[1],a[0],a[3]),(side*b[1],b[0],b[3])),.025,mats["black"],root)
        for station in (design.canopy[1],design.canopy[3],design.canopy[-2]):
            strip("Cabin pillar",((side*station[1],station[0],station[3]),
                (side*station[2],station[0],station[4]-.047)),.044,mats["black"],root)
        art.box("Seat back",(side*.33,.22,roof_level-.32),(.38,.17,.44),mats["interior"],root,.064,(.14,0,0))
    dashboard=design.canopy[1]
    art.box("Dashboard",(0,dashboard[0]+.032,dashboard[3]+.035),
        (dashboard[1]*1.73,.30,.11),mats["interior"],root,.05)
    exterior(root, design, mats)
    wheels(root, design, mats)
    return root


def glazing(root, design, mats):
    """Join windshield, side lights, crowned roof and rear glass at the same seams."""
    stations = design.canopy
    vertices = []
    for y, sill, edge, belt, peak in stations:
        vertices.extend(((-sill,y,belt),(-edge,y,peak-.043),(-edge*.38,y,peak),
            (edge*.38,y,peak),(edge,y,peak-.043),(sill,y,belt)))
    glass, roof = [], []
    last_roof = 3 if design.key in ("switchback","monarch") else 2
    for index in range(len(stations) - 1):
        start, end = index*6, (index+1)*6
        glass.extend(((start,start+1,end+1,end),(start+4,start+5,end+5,end+4)))
        top = roof if 2 <= index <= last_roof else glass
        for strip_index in range(1,4):
            top.append((start+strip_index,start+strip_index+1,end+strip_index+1,end+strip_index))
    glass.extend(((0,5,4,3,2,1),tuple(range(len(vertices)-6,len(vertices)))))
    art.mesh_object("Cabin glazing", vertices, glass, mats["glass"], root, 0, 0)
    art.mesh_object("Crowned roof panel", vertices, roof, mats["black"], root, 0, 0)
    for side in (-1,1):
        edge = 1 if side < 0 else 4
        strip("Sealed roof rail",[vertices[index*6+edge] for index in range(len(stations))],.034,mats["black"],root)
    return max(station[-1] for station in stations)


def main():
    art.MODEL_DIR = art.MODEL_DIR.parent / "Resources" / "PremiumGarage"
    art.MODEL_DIR.mkdir(parents=True, exist_ok=True)
    for design in FLEET:
        art.clear_scene()
        art.export_vehicle(build(design), design.key + ".fbx")


if __name__ == "__main__":
    main()

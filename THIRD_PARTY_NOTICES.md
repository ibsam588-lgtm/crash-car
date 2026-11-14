# Third-Party Notices

## Artist-Built Road Cars

The current Roadbreaker GT uses **[FREE] BMW M3 E30** by **Martin Trafas** under Creative Commons Attribution 4.0: https://sketchfab.com/3d-models/free-bmw-m3-e30-ac3c7013434e403e8faff87948caf422. The original artist's Blend and textures are linked from that model's description: https://drive.google.com/file/d/1Q90hgp1deq4El61YaqiDfI7eSyEpCtjE/view. The conversion retains the full body mesh, authored smoothing, rim/tire/brake maps and lamps, normalizes scale, neutralizes front-wheel steering, and groups four independently animated wheels.

The current Apex GT uses **LOTUS ÉLISE** by **Stéphane Agullo** under Creative Commons Attribution 4.0: https://sketchfab.com/3d-models/75e9045ac1af4cdbb69318108c81edfd. The conversion removes the source display ground and mannequin, normalizes scale, preserves separate coachwork/glazing/lamps and wheel roots, reduces repeated wheel tread geometry, and prepares Unity materials.

License for both models: https://creativecommons.org/licenses/by/4.0/. The compressed GLB mirror is https://github.com/pedrosoandre/carros. Source hashes, primary-source license metadata, conversion notes and attribution are recorded in `Assets/CarCrash/Models/RoadCars/sources.json` and `LICENSE.txt`. The former concept/GT assets remain in the project archive; the two artist-built road cars are now used in the playable garage and street traffic.

## CarConcept 3D Model

The production sports-car mesh is derived from the `CarConcept` sample in the Khronos Group glTF Sample Assets repository:

- Source: https://github.com/KhronosGroup/glTF-Sample-Assets/tree/main/Models/CarConcept
- License: Creative Commons Attribution 4.0 International
- Copyright: 2024 Darmstadt Graphics Group GmbH
- Model and textures: Eric Chadwick
- Additional credit: Ed Mackey

The Car Crash version removes Khronos branding and the license-plate mark, changes paint and material styling, renames and prepares wheel roots, adds a rear wing and detachable damage pieces, and converts the asset to Blender and FBX for the Unity pipeline.

The CC BY 4.0 license is available at https://creativecommons.org/licenses/by/4.0/.

The detailed `roadbreaker_realistic.fbx` conversion retains the source body's curved panels, cockpit, brakes, wheels, and tire geometry. It removes source-brand marks, neutralizes front-wheel steering, renames wheel and damage roots, and remaps the source textures to Unity Standard metallic/smoothness materials. The earlier procedural muscle-car conversion remains in the source archive.

## Detailed Traffic Car

The `traffic_gt_detailed.fbx` model is derived from Yasutoshi Mori's SportsCar subdivision model.

- Artist/source: https://github.com/MirageYM/3DModels/tree/master/SportsCar
- Original FBX: `SportsCar/Subdiv_Car.fbx`
- Copyright: 2015 Yasutoshi Mori
- License: Creative Commons Attribution 4.0 International, https://creativecommons.org/licenses/by/4.0/
- Original license: https://github.com/MirageYM/3DModels/blob/master/LICENSE

The conversion normalizes the model's units and orientation, removes hidden engine/helper geometry, refines the visible curved surfaces, reduces the mesh budget, combines material slots, groups four animated wheels, and remaps paint, glass, rubber, metal, interior, and lamps for Unity. The converted car has approximately 115,000 triangles across five render meshes. The download helper pins the original source SHA-256. The source license is also included beside the converted model.

## Poly Haven Environment Assets

The realistic Unity district uses these assets under CC0:

- Venice Sunset HDRI, Greg Zaal: https://polyhaven.com/a/venice_sunset
- Asphalt 02: https://polyhaven.com/a/asphalt_02
- Brick Wall 003, Dimitrios Savva and Rob Tuytel: https://polyhaven.com/a/brick_wall_003
- Concrete Pavement: https://polyhaven.com/a/concrete_pavement
- Plastered Wall 02: https://polyhaven.com/a/plastered_wall_02
- Leafy Grass, Charlotte Baglioni: https://polyhaven.com/a/leafy_grass
- Wood Planks, Amal Kumar: https://polyhaven.com/a/wood_planks

Asset license: https://polyhaven.com/license. Original download URLs and SHA-256 hashes are stored in `Assets/CarCrash/Textures/Realism/sources.json` within the Unity project. Normal and roughness channels are converted for Unity's material inputs.

The pavement/plaster maps and additional asphalt/brick ambient-occlusion maps are recorded in `city-finishes-sources.json` in the same folder, including source and converted-file SHA-256 hashes. These supply albedo, normal, roughness and occlusion inputs; surface scale, weathering, road-wear shapes, palm geometry and window-room shading are implemented locally.

## UI Fonts

The modern interface uses Inter 4.1, designed by Rasmus Andersson, under the SIL Open Font License 1.1. Regular, Medium and SemiBold static TrueType files come from the official release: https://github.com/rsms/inter/releases/tag/v4.1. The license and download provenance are included in `Assets/CarCrash/Resources/Fonts/Inter-OFL.txt` and `Inter-source.json` within the Unity project.

Barlow Condensed and Archivo Black are distributed through the Google Fonts repository under the SIL Open Font License 1.1. Their license notices are included with the font files in `Assets/CarCrash/Resources/Fonts` within the Unity project.

Earlier interface assets include Atkinson Hyperlegible (regular and bold), distributed by Google Fonts under the SIL Open Font License 1.1. Source: https://github.com/google/fonts/tree/main/ofl/atkinsonhyperlegible. Its OFL notice is included beside the font files.

## Expanded Playable Garage

The detailed pickup, crossover SUV, and cargo truck are original unbranded Innerscene models released under CC0 1.0:

- https://www.innerscene.com/tools/library/3d-parts/crew-cab-pickup-with-open-bed-4f02e0cc
- https://www.innerscene.com/tools/library/3d-parts/charcoal-five-door-crossover-suv-a1996830
- https://www.innerscene.com/tools/library/3d-parts/7-5-t-class-box-truck-with-roll-up-door-12baea66
- License: https://creativecommons.org/publicdomain/zero/1.0/

The conversions normalize orientation, consolidate material slots and meshes, retain four independent wheel groups (including dual rear tires on the cargo truck), reduce mesh density, and prepare Unity materials. Ironclaw Monster derives from the same pickup with a lifted body, enlarged wheels, raised chevron tread, exposed axles and long-travel shocks. Download SHA-256 hashes and source URLs are included in `Assets/CarCrash/Models/Garage/sources.json`. Roadbreaker GT and Apex GT derive from the CarConcept and Yasutoshi Mori assets credited above.

## Animated Human Pedestrians

Maya, Sam, and Harper are Innerscene character adaptations of MakeHuman/MPFB CC0 body, rig, skin, and clothing assets, with original Innerscene walking motion. Each asset page dedicates its model and animation to CC0 1.0:

- Maya: https://www.innerscene.com/tools/library/3d-parts/relaxed-walk-maya-3d-person-animated-2c6669a2
- Sam: https://www.innerscene.com/tools/library/3d-parts/relaxed-walk-sam-3d-person-animated-732f1206
- Harper: https://www.innerscene.com/tools/library/3d-parts/brisk-walk-harper-3d-person-animated-0efea8f3
- License: https://creativecommons.org/publicdomain/zero/1.0/

The local conversion preserves skinning and walk clips, reduces each figure to approximately 25,000–28,000 triangles, downsizes textures, and creates Unity Standard materials. The download helper verifies source SHA-256 hashes; converted assets and provenance are included under `Assets/CarCrash/Models/Pedestrians`.

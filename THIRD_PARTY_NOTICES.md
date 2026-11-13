# Third-Party Notices

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

## Poly Haven Environment Assets

The realistic Unity district uses these assets under CC0:

- Venice Sunset HDRI, Greg Zaal: https://polyhaven.com/a/venice_sunset
- Asphalt 02: https://polyhaven.com/a/asphalt_02
- Brick Wall 003, Dimitrios Savva and Rob Tuytel: https://polyhaven.com/a/brick_wall_003

Asset license: https://polyhaven.com/license. Original download URLs and SHA-256 hashes are stored in `Assets/CarCrash/Textures/Realism/sources.json` within the Unity project. Normal and roughness channels are converted for Unity's material inputs.

## UI Fonts

Barlow Condensed and Archivo Black are distributed through the Google Fonts repository under the SIL Open Font License 1.1. Their license notices are included with the font files in `Assets/CarCrash/Resources/Fonts` within the Unity project.

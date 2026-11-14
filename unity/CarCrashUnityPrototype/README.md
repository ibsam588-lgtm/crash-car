# Car Crash Unity Production Slice

This folder contains the Unity 6 production prototype for Car Crash.

## What Is Included

- `Assets/CarCrash/Models/roadbreaker_realistic.fbx` and the licensed source GLB
- Reproducible Blender conversion and environment-download helpers in the repository's `tools` directory
- Vehicle, traffic, camera, HUD, crash damage, slow-motion, scoring, and run-result scripts
- Unity editor builders at `Car Crash > Build Production Impact Run Scene` and `Car Crash > Build Production WebGL`

## Tooling

- Blender is used to generate the source model, FBX, GLB, and preview render.
- Unity imports the FBX and creates a WheelCollider-based playable prototype.

## Open In Unity

1. Open Unity Hub.
2. Sign in with your Unity account.
3. Add this project folder:
   `C:/Users/ibsam/Documents/Car Crash/unity/CarCrashUnityPrototype`
4. Select Unity `6000.0.43f1`.
5. Open the project.
6. Run `Car Crash > Build Production Impact Run Scene`.
7. Open `Assets/CarCrash/Scenes/ImpactRunPrototype.unity` and press Play, or run the WebGL build command.

## Dynamic Racing UI

The modern Elysian interface uses Inter typography, rounded navy panels, blue accents and subtle unscaled hover, press and menu transitions. Home has a primary driving action; the garage has six selectable vehicle cards and circular paint swatches. Floating driving cards show navigation, speed, gear, integrity and nitro, with a consistent touch control row. The city map uses numbered destination cards and highlights the active route. Layouts adapt to desktop, portrait and compact landscape windows.

The scene opens at Home, with Free Drive, the optional 60 second Impact Run, a driving guide, and garage navigation. Free Drive is untimed and continues through stops and crashes until the player chooses to end the session. The garage has an orbiting camera, drag/arrow camera controls, five paint finishes, vehicle specifications, and a Start Free Drive button. Both modes use a three-second countdown.

Recovery is manual through `R`, the HUD button, or Recover & Resume in the pause menu. It places the car on a nearby street without clearing score or elapsed time. Crossing the former lane/distance bounds does not teleport the player. Home, Garage, and End Session remain available from the pause/results menus.

The open-world district covers 620 by 1,060 metres: seven avenues, thirteen cross streets and a coastal access drive connect approximately 15.5 km of road centerlines. Old interior barriers have been removed. Explore Old Town, Sunset Marina, Vista Beach, Paloma Park, the freight yard and Apex Motor Park. All avenues have textured roads, sidewalks, palms, traffic signals and detailed moving cars; regional building styles and the waterfront break up the grid. Lane traffic brakes for a stopped player and can be nudged gently rather than acting as an immovable wall. The nearby GT traffic uses a credited subdivision-car conversion with approximately 115,000 triangles and five render meshes.

Trees, planters, lamps, benches, bins, hydrants, signals, and buildings have solid collision. Pavements use beveled, drivable edges and chassis collision hulls preserve clearance under loaded suspension. The camera retracts before entering static scenery. Skyline towers sit outside the playable avenues. Ninety-six textured, skinned human pedestrians walk bounded sidewalk routes using three licensed figures and original walk clips. They step aside for nearby cars and use dynamic physical bodies; distant models and shadows are culled.

The responsive uGUI HUD uses compact digital instruments, speed-derived automatic gear/rev indicators, a city minimap driven by the street geometry and nearby traffic transforms, body integrity, rechargeable nitro, animated score, hit count, impact-chain feedback, and a run timer. Chase, Low, and Bonnet cameras use speed-sensitive field of view and impact shake. Paint and personal best scores save locally with PlayerPrefs.

The detailed Roadbreaker model preserves the licensed CarConcept body and cockpit. Automotive paint uses neutral specular highlights, fine normal detail and a separate pigment response; glass uses dielectric reflections and transparent tint. Sports cars, SUVs and pickups share the road, with visible dimensions reflected in their collision chassis. The city uses metre-scaled PBR asphalt, pavement, plaster, masonry, grass and timber, window-room parallax, fitted shop signs, side windows, balconies, air conditioners, manholes, drains and surface wear. Feathery palm fronds sway on one shared mesh and render in spatial instance groups, while their original trunk collisions stay active. Static architecture retains regional material batches and distance culling. A 256-pixel byte cubemap is captured and its rougher mip levels filtered in linear light for stable browser reflections. Half-resolution contact occlusion, quarter-resolution bloom and ACES-style colour grading add depth while the UI stays sharp above the camera image.

To reproduce the visual source assets:

```powershell
python tools/fetch_realism_assets.py
python tools/fetch_city_finish_assets.py
blender --background --python tools/blender/prepare_realistic_roadbreaker.py
python tools/prepare_realism_textures.py
python tools/fetch_traffic_asset.py
blender --background --factory-startup --python tools/blender/prepare_realistic_traffic.py
python tools/fetch_pedestrian_assets.py
blender --background --python tools/blender/prepare_city_pedestrians.py
```

Run these commands from the repository root; the Blender helper uses the existing licensed GLB. The texture conversion helper requires Pillow. Asset sources and font licenses are listed in the repository's `THIRD_PARTY_NOTICES.md`.

Keyboard controls:

- `Enter`: Free Drive from Home or the garage
- `G`: garage from Home
- `W` / up arrow: accelerator
- `S` / down arrow: brake, then reverse when held at rest
- `Q` / REV pedal: reverse; brakes first when moving forward
- `Space`: brake without reversing
- `A/D` / left/right arrows: steer while moving; hold with `W` to turn from rest
- `Left Shift`: handbrake
- `B` / left Ctrl: nitro
- `C`: change driving camera
- `M` / MAP: pause to browse the full city and choose a road route
- `Esc` / `P`: pause or resume
- `R`: recover during driving; retry from results

On touch screens, hold GAS with a steering control to turn. Hold REV to back up; it brakes first when moving forward. BRAKE stops the car and also reverses when held at rest. Releasing a control, leaving its hit area, opening the pause menu, or losing app focus clears held input. Braking overrides throttle. Pause also suspends impact slow motion and the run timer.

The garage also offers Apex GT, Summit AWD, Ridgeback 4x4, Atlas Hauler, and Ironclaw Monster. Selection changes the actual model, wheel radius and wheelbase, suspension, weight, acceleration, top speed, and camera framing, and saves locally. A dedicated Rev pedal and `Q` reverse directly from rest and brake first while moving forward; `S` retains brake-then-reverse behavior. Button labels use Inter Medium, minimum readable pixel sizes, and contrasting panels. Compact landscape layouts use a larger UI scale.

Crashes produce speed-scaled sparks, dust clouds, glass spray and an expanding ground splash. These effects also trigger on solid scenery contacts, pause with the simulation, and expire automatically. Run `Car Crash > Validate Garage Fleet and Crash Effects` for real forward/reverse driving in all six vehicles and an actual scenery-impact particle lifecycle test. It writes `Builds/Validation/fleet-qa.json`.

## UI Verification

Run `Car Crash > Validate Realistic Graphics` to render all six vehicles and downtown, marina and park views. It checks shader support, missing-shader pixels, contrast, actual occlusion response, safe reflection formats, shared foliage geometry, traffic variety and collision dimensions. Frames go to `build/cinematic-city` in the repository root and results to `Builds/Validation/graphics-qa.json`.

Taller traffic vehicles are temporarily hidden only while they intersect the camera's immediate clearance zone, so a following SUV cannot fill the foreground. Their original renderer flags restore after each camera render. Graphics QA exercises this with real camera renders.

Run `Car Crash > Validate Dynamic UI` with Play Mode stopped. This checks Home navigation, the driving guide, traffic dimensions and alignment, car paint/wheels, baked reflections, independent touch input, brake priority, nitro, camera switching, scoring, pause during slow motion, results, retry, and garage/Home return. It runs Free Drive for a real 65 seconds, waits after a crash, crosses the former reset bounds, and checks manual recovery preserves the session. The optional challenge is checked separately. It restores the pre-test paint and personal-best preferences and writes `Builds/Validation/ui-smoke.json`.

Run `Car Crash > Validate Driving and World Physics` for measured left/right heading and travel, reverse steering, human skinning/animation and sidewalk movement, camera wall clearance, and actual 80 km/h car impacts against six kinds of street objects. It also checks shopfront coverage on all three avenues, unobstructed cross streets, and actual driving along both side neighborhoods and their connecting streets. It writes `Builds/Validation/driving-qa.json`. Browser QA should also cover desktop, phone portrait, phone landscape, simultaneous pedal/steering input, and all three cameras.

For a browser preview after building WebGL:

```powershell
python -m http.server 8094 --bind 127.0.0.1 --directory Builds/WebGL
```

Open `http://127.0.0.1:8094/`. Landscape and portrait layouts resize with the canvas and respect the device safe area.

## Open-world and traversal QA

`Car Crash > Validate Open World and Unstuck` checks clearance along the entire road grid, all six destination entrances, route creation and map pause behavior, actual driving beyond the previous world boundaries, gentle forward/reverse traffic pushes, recovery space for every vehicle, and the blocked-car hint. It writes `Builds/Validation/world-qa.json` and renders the landmarks to the repository's `build/open-world` directory.

`Car Crash > Validate Curb Traversal` drives every selectable vehicle onto a real pavement edge and reverses back out. It writes `Builds/Validation/traversal-qa.json`. These supplement the UI, fleet and general physics suites.

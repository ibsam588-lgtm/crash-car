# Car Crash Unity Production Slice

This folder contains the Unity 6 production prototype for Car Crash.

## What Is Included

- Artist-built road cars in `Assets/CarCrash/Models/RoadCars`, plus the earlier licensed concept sources
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

Home offers Impact Run, Free Drive, the garage, and a driving guide. In Impact Run, the course card shows the route, medal targets, and selected car. Change the car in the live garage and return to the same briefing. A three-second countdown leads into the course, where cyan road arrows and the minimap guide the next turn. Free Drive is untimed and continues through stops and crashes until the player ends the session.

### Impact Run courses

| Course | Objective | Bronze / Silver / Gold |
|---|---|---|
| Freight Yard | Follow Harbor West to the depot. Try the wide lane or the loading-bay targets, then hit the staged wreck before the finish. | 180 / 380 / 620 |
| Old Town | Line up small targets and steer through the downtown turns. | 160 / 340 / 520 |
| Sunset Marina | Carry speed along the coast and turn into the waterfront finish. | 170 / 370 / 570 |
| Apex Motor Park | Reach the finish through the outer lanes within 75 seconds. | 160 / 340 / 540 |

Marked cones earn 10 points, crates 25, barriers 50, streetlights 100, and staged wrecks 200. Each prop can score once per run through direct contact or a chain collision with an already-scored prop. The three directional checkpoints must be crossed in order before the finish. Recovery returns the player to the last checkpoint or the start. Breakage and points persist through recovery.

The results screen shows the exact sum, counts by prop, medal, personal best, and retry. A finished course banks the score and unlocks the next course. Earning Gold unlocks the Track Gold paint finish. Completed runs and course bests save locally. A timed-out or ended run shows its current breakdown.

Android supports optional Play Games leaderboards and awards when the linked Play Games project has its credentials and resource IDs configured. The rest of the game is available offline. See the [Play release guide](../../android/playstore/README.md) for test registration.

Recovery is manual through `R`, the HUD button, or Recover & Resume in the pause menu. It places the car on a nearby street without clearing score or elapsed time. Crossing the former lane/distance bounds does not teleport the player. Home, Garage, and End Session remain available from the pause/results menus.

The open-world district covers 620 by 1,060 metres: seven avenues, thirteen cross streets and a coastal access drive connect approximately 15.5 km of road centerlines. Old interior barriers have been removed. Explore Old Town, Sunset Marina, Vista Beach, Paloma Park, the freight yard and Apex Motor Park. All avenues have textured roads, sidewalks, palms, traffic signals and detailed moving cars; regional building styles and the waterfront break up the grid. Lane traffic brakes for a stopped player and can be nudged gently rather than acting as an immovable wall. Roadbreaker GT now uses Martin Trafas's full-resolution BMW E30 body (117,253 triangles); Apex GT uses Stéphane Agullo's Lotus Élise (132,218 triangles after wheel and bumper optimization). Both retain authored details and independent wheel pivots and also appear in traffic alongside SUVs and pickups. The Asset credits link in the browser build provides source/license notices.

Eight architectural families vary the height, silhouette and materials of each block: Art Deco hotels, brick warehouses, glazed apartments, Mediterranean apartments, corner stores, townhouses, offices and service workshops. Nine businesses add fitted storefront signs and awnings. Central districts have furnished interiors with counters, shelving, produce, bakery displays, records, clothing, diner booths and workshop tools/cars. Forty-two posed shop attendants add local activity. Shared materials retain individual façade colors through vertex tinting; the final city uses 671 static material batches within its 2.4 million static-vertex budget.

Trees, planters, benches, hydrants and buildings retain solid collision. Streetlights, traffic signals, parking meters, mailboxes, litter bins and café furniture release into dynamic rigidbody assemblies when struck hard enough; fallen objects stay physical. Intact fixtures share spatial GPU instance batches and broken fixtures render with their moving collision bodies. Pavements use beveled, drivable edges and chassis collision hulls preserve clearance under loaded suspension. The camera retracts before entering static scenery. Skyline towers sit outside the playable avenues. One hundred twenty textured, skinned pedestrians walk bounded sidewalk routes using three licensed figures and original walk clips. They step aside for nearby cars and use dynamic physical bodies; distant models and shadows are culled.

The native Android internal-testing workflow, signing requirements and first-release setup are documented in [the Play release guide](../../android/playstore/README.md). It builds the Unity game, with API 36, ARM64 IL2CPP and signed-bundle/16 KB compatibility checks.

The responsive uGUI HUD uses compact digital instruments, speed-derived automatic gear/rev indicators, a city minimap driven by the street geometry and nearby traffic transforms, body integrity, rechargeable nitro, animated score, hit count, impact-chain feedback, and a run timer. Chase, Low, and Bonnet cameras use speed-sensitive field of view and impact shake. Paint and personal best scores save locally with PlayerPrefs.

The artist-built road cars preserve their source coachwork, glazing, trim, lamps and wheel details. Automotive paint uses neutral specular highlights, fine normal detail and a separate pigment response; glass uses dielectric reflections and transparent tint. Sports cars, SUVs and pickups share the road, with visible dimensions reflected in their collision chassis. The city uses metre-scaled PBR asphalt, pavement, plaster, masonry, grass and timber, window-room parallax, fitted shop signs, side windows, balconies, air conditioners, manholes, drains and surface wear. Feathery palm fronds sway on one shared mesh and render in spatial instance groups, while their original trunk collisions stay active. Static architecture retains regional material batches and distance culling. A 256-pixel byte cubemap is captured and its rougher mip levels filtered in linear light for stable browser reflections. Half-resolution contact occlusion, quarter-resolution bloom and ACES-style colour grading add depth while the UI stays sharp above the camera image.

To reproduce the visual source assets:

```powershell
python tools/fetch_real_road_cars.py
npx --yes @gltf-transform/cli@4.2.1 copy build/real-models/lotus-elise.glb build/real-models/lotus-elise-decoded.glb
blender --background --disable-autoexec --python tools/blender/prepare_real_road_cars.py
python tools/prepare_road_car_textures.py
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

- `Enter`: open Impact Run from Home, start the selected course, or confirm the car in the garage
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

Crashes produce speed-scaled sparks, dust, glass spray, an expanding ground splash, impact audio, and brief Android feedback on strong hits. Hard scenery hits mark paint and can crack the windscreen. Rear lamps respond to braking and reverse. Heavy vehicles have fitted cargo hardware, underbody protection, and suspension details. Pedestrians stumble and slow after glancing contact, and hard collisions leave them down with a jointed ragdoll. Course points come from marked props.

Run `Car Crash > Validate Garage Fleet and Crash Effects` for real forward/reverse driving in all six vehicles and a scenery-impact particle lifecycle test. It writes `Builds/Validation/fleet-qa.json`.

## UI Verification

Run `Car Crash > Validate Realistic Graphics` to render all six vehicles and downtown, marina and park views. It checks shader support, missing-shader pixels, contrast, actual occlusion response, safe reflection formats, shared foliage geometry, traffic variety and collision dimensions. Frames go to `build/cinematic-city` in the repository root and results to `Builds/Validation/graphics-qa.json`.

Taller traffic vehicles are temporarily hidden only while they intersect the camera's immediate clearance zone, so a following SUV cannot fill the foreground. Their original renderer flags restore after each camera render. Graphics QA exercises this with real camera renders.

Run `Car Crash > Validate Dynamic UI` with Play Mode stopped. The suite checks the normal menu and driving controls, a real 65-second Free Drive, and recovery after a crash. It also checks course clearance, real scoring contact, the pedestrian knockdown, ordered checkpoints, the score breakdown, the next-course unlock, and the timed course. It restores the pre-test preferences and writes `Builds/Validation/ui-smoke.json`. Set `CRASH_QA_CAPTURE_DIR` to an absolute local directory to capture the briefing, start, first score, and results with a graphics device.

Run `Car Crash > Validate Driving and World Physics` for measured left/right heading and travel, reverse steering, human skinning/animation and sidewalk movement, camera wall clearance, and actual 80 km/h car impacts against six kinds of street objects. It also checks shopfront coverage on all three avenues, unobstructed cross streets, and actual driving along both side neighborhoods and their connecting streets. It writes `Builds/Validation/driving-qa.json`. Browser QA should also cover desktop, phone portrait, phone landscape, simultaneous pedal/steering input, and all three cameras.

For a browser preview after building WebGL:

```powershell
python -m http.server 8094 --bind 127.0.0.1 --directory Builds/WebGL
```

Open `http://127.0.0.1:8094/`. Landscape and portrait layouts resize with the canvas and respect the device safe area.

## Open-world and traversal QA

`Car Crash > Validate Open World and Unstuck` checks clearance along the entire road grid, all six destination entrances, route creation and map pause behavior, actual driving beyond the previous world boundaries, gentle forward/reverse traffic pushes, recovery space for every vehicle, and the blocked-car hint. It writes `Builds/Validation/world-qa.json` and renders the landmarks to the repository's `build/open-world` directory.

`Car Crash > Validate Curb Traversal` drives every selectable vehicle onto a real pavement edge and reverses back out. It writes `Builds/Validation/traversal-qa.json`. These supplement the UI, fleet and general physics suites.

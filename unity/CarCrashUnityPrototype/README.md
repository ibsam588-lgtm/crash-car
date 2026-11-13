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

The scene opens at Home, with Free Drive, the optional 60 second Impact Run, a driving guide, and garage navigation. Free Drive is untimed and continues through stops and crashes until the player chooses to end the session. The garage has an orbiting camera, drag/arrow camera controls, five paint finishes, vehicle specifications, and a Start Free Drive button. Both modes use a three-second countdown.

Recovery is manual through `R`, the HUD button, or Recover & Resume in the pause menu. It places the car on a nearby street without clearing score or elapsed time. Crossing the former lane/distance bounds does not teleport the player. Home, Garage, and End Session remain available from the pause/results menus.

The avenue ends connect through City Loop cross streets. Visible safety barriers mark the street ends, and the ground covers the full district. Lane traffic brakes for a stopped player or another car; wheel rotation follows its actual cruising speed. The nearby GT traffic uses a credited subdivision-car conversion with approximately 115,000 triangles and five render meshes.

Trees, planters, curbs, lamps, benches, bins, hydrants, signals, and buildings have solid collision. The camera retracts before entering static scenery. Skyline towers sit outside the playable avenues. Twenty-four textured, skinned human pedestrians walk bounded sidewalk routes using three licensed figures and original walk clips. They step aside for nearby cars and use dynamic physical bodies; distant models and shadows are culled.

The responsive uGUI HUD uses compact digital instruments, speed-derived automatic gear/rev indicators, a city minimap driven by the street geometry and nearby traffic transforms, body integrity, rechargeable nitro, animated score, hit count, impact-chain feedback, and a run timer. Chase, Low, and Bonnet cameras use speed-sensitive field of view and impact shake. Paint and personal best scores save locally with PlayerPrefs.

The detailed Roadbreaker model preserves the licensed CarConcept body and cockpit. It uses imported tire/normal maps, reflective metallic paint, transparent glass, separate wheels, and emissive lamps. The Elysian district includes textured asphalt and brick, a sunset HDR sky, road intersections, shop signs, palms, sidewalks, street furniture, and skyline towers. Static scenery is combined into material batches to limit draw calls. A street cubemap is captured in the Editor and saved with the scene for browser-compatible reflections. Camera output uses a small ACES-style tone-mapping shader; the UI stays sharp above the camera image.

To reproduce the visual source assets:

```powershell
python tools/fetch_realism_assets.py
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
- `Space`: brake without reversing
- `A/D` / left/right arrows: steer while moving; hold with `W` to turn from rest
- `Left Shift`: handbrake
- `B` / left Ctrl: nitro
- `C`: change driving camera
- `Esc` / `P`: pause or resume
- `R`: recover during driving; retry from results

On touch screens, hold GAS with a steering control to turn. Hold BRAKE / R to stop, then reverse. Releasing a control, leaving its hit area, opening the pause menu, or losing app focus clears held input. Braking overrides throttle. Pause also suspends impact slow motion and the run timer.

## UI Verification

Run `Car Crash > Validate Dynamic UI` with Play Mode stopped. This checks Home navigation, the driving guide, traffic dimensions and alignment, car paint/wheels, baked reflections, independent touch input, brake priority, nitro, camera switching, scoring, pause during slow motion, results, retry, and garage/Home return. It runs Free Drive for a real 65 seconds, waits after a crash, crosses the former reset bounds, and checks manual recovery preserves the session. The optional challenge is checked separately. It restores the pre-test paint and personal-best preferences and writes `Builds/Validation/ui-smoke.json`.

Run `Car Crash > Validate Driving and World Physics` for measured left/right heading and travel, reverse steering, human skinning/animation and sidewalk movement, camera wall clearance, and actual 80 km/h car impacts against six kinds of street objects. It writes `Builds/Validation/driving-qa.json`. Browser QA should also cover desktop, phone portrait, phone landscape, simultaneous pedal/steering input, and all three cameras.

For a browser preview after building WebGL:

```powershell
python -m http.server 8094 --bind 127.0.0.1 --directory Builds/WebGL
```

Open `http://127.0.0.1:8094/`. Landscape and portrait layouts resize with the canvas and respect the device safe area.

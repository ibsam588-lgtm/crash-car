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

The scene opens in a live 3D garage with an orbiting camera, drag/arrow camera controls, five paint finishes, vehicle specifications, and a Start Engine button. A three-second countdown leads into a 60-second Impact Run. A stopped wreck or full damage can finish the run early.

The responsive uGUI HUD uses compact digital instruments, speed-derived automatic gear/rev indicators, a city minimap driven by the street geometry and nearby traffic transforms, body integrity, rechargeable nitro, animated score, hit count, impact-chain feedback, and a run timer. Chase, Low, and Bonnet cameras use speed-sensitive field of view and impact shake. Paint and personal best scores save locally with PlayerPrefs.

The detailed Roadbreaker model preserves the licensed CarConcept body and cockpit. It uses imported tire/normal maps, reflective metallic paint, transparent glass, separate wheels, and emissive lamps. The Elysian district includes textured asphalt and brick, a sunset HDR sky, road intersections, shop signs, palms, sidewalks, street furniture, and skyline towers. Static scenery is combined into material batches to limit draw calls. A street cubemap is captured in the Editor and saved with the scene for browser-compatible reflections. Camera output uses a small ACES-style tone-mapping shader; the UI stays sharp above the camera image.

To reproduce the visual source assets:

```powershell
python tools/fetch_realism_assets.py
blender --background --python tools/blender/prepare_realistic_roadbreaker.py
python tools/prepare_realism_textures.py
```

Run these commands from the repository root; the Blender helper uses the existing licensed GLB. The texture conversion helper requires Pillow. Asset sources and font licenses are listed in the repository's `THIRD_PARTY_NOTICES.md`.

Keyboard controls:

- `Enter`: start from the garage
- `W` / up arrow: accelerator
- `S` / down arrow / `Space`: brake
- `A/D` / left/right arrows: steer
- `Left Shift`: handbrake
- `B` / left Ctrl: nitro
- `C`: change driving camera
- `Esc` / `P`: pause or resume
- `R`: retry from results

On touch screens, hold the steering and pedal controls. Releasing a control, leaving its hit area, opening the pause menu, or losing app focus clears held input. Braking overrides throttle. Pause also suspends impact slow motion and the run timer.

## UI Verification

Run `Car Crash > Validate Dynamic UI` with Play Mode stopped. This checks the actual scene, car dimensions and wheel alignment, baked reflections, paint bindings, independent touch input, brake priority, nitro charge, camera switching, impact scoring, pause during slow motion, results, retry, and garage return. It restores the pre-test paint and personal-best preferences and writes `Builds/Validation/ui-smoke.json`.

For a browser preview after building WebGL:

```powershell
python -m http.server 8094 --bind 127.0.0.1 --directory Builds/WebGL
```

Open `http://127.0.0.1:8094/`. Landscape and portrait layouts resize with the canvas and respect the device safe area.

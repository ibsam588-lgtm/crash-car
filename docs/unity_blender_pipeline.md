# Unity + Blender Pipeline

This is the first playable-car pipeline for Car Crash.

## Installed Locally

- Blender 5.1.2 installed at `C:/Program Files/Blender Foundation/Blender 5.1/blender.exe`
- Unity Hub 3.18.3 installed at `C:/Program Files/Unity Hub/Unity Hub.exe`
- Unity `6000.0.43f1` installed at `C:/Users/ibsam/Unity/Hub/Editor/6000.0.43f1/Editor/Unity.exe`
- WebGL and Windows build support installed and licensed through Unity Hub

## Regenerate The Car Asset

Run this from the repository root:

```powershell
& 'C:\Program Files\Blender Foundation\Blender 5.1\blender.exe' --background --python 'C:\Users\ibsam\Documents\Car Crash\tools\blender\prepare_car_crash_concept.py'
```

Outputs:

- `unity/CarCrashUnityPrototype/Assets/CarCrash/Models/car_crash_gt650.blend`
- `unity/CarCrashUnityPrototype/Assets/CarCrash/Models/car_crash_gt650.fbx`
- `assets/images/concepts/car_crash_gt650/car_crash_gt650_preview.png`

## Unity Setup

1. Open Unity Hub.
2. Add/open this project:
   `C:/Users/ibsam/Documents/Car Crash/unity/CarCrashUnityPrototype`
3. In Unity, run:
   `Car Crash > Build Production Impact Run Scene`
4. Open:
   `Assets/CarCrash/Scenes/ImpactRunPrototype.unity`
5. Press Play, or run `Car Crash > Build Production WebGL`.

## Prototype Features

- FBX import of the prepared high-detail concept sports car
- Separate named wheel roots for animation
- WheelCollider-based steering, throttle, braking, handbrake, and boost
- Rigidbody crash physics
- Slow-motion crash burst
- Score, hit, and damage tracking
- Follow camera and simple debug/mobile HUD
- Moving car, bus, and truck traffic with animated wheels
- Textured asphalt, concrete facades, puddles, crosswalks, storefronts, trees, signals, and barriers
- Detachable damage panels, windshield crack state, slow-motion scoring, and a result/retry flow

## Next Production Steps

- Install Unity Android Build Support, SDK, NDK, and OpenJDK modules.
- Add Android quality tiers and device profiling.
- Configure package signing and generate an Android App Bundle.
- Complete Play Console testing, content rating, privacy, and store-listing submission.

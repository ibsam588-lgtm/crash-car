# Car Crash Unity Production Slice

This folder contains the Unity 6 production prototype for Car Crash.

## What Is Included

- `Assets/CarCrash/Models/car_crash_gt650.blend`
- `Assets/CarCrash/Models/car_crash_gt650.fbx`
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

Controls:

- `W/S`: throttle and reverse
- `A/D`: steer
- `Space`: brake
- `Left Shift`: handbrake

The runtime HUD exposes steering zones, boost, pause, score, hit count, speed, impact callouts, and a retryable result screen.

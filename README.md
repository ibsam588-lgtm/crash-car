# Crash Car

A realistic arcade crash-driving game with a Flutter/Flame application shell and a playable Unity 6 impact-driving prototype.

Crash Car puts the player in a generated realistic muscle car and sends them through arena roads packed with destructible crates, barrels, cones, barricades, moving cars, trucks, buses, and shop fronts. The playable slice includes arena selection, accelerator and steering controls, collision speed loss, vehicle shatter debris, impact slow motion, per-hit score popups, combos, damage, coins, car unlocks, upgrades, a shop-ready screen, and a score summary.

## Stack

- Flutter 3.44+
- Flame for the game loop, collisions, sprites, and debris
- Riverpod for shared progress state
- Shared Preferences for local saves
- Generated PNG assets under `assets/images`
- Unity 6 for the production 3D driving, traffic, collisions, detachable damage, slow motion, and WebGL build
- Blender for the editable vehicle source and FBX export pipeline

## Unity Playable Build

Open `unity/CarCrashUnityPrototype` in Unity `6000.0.43f1`, then use:

- `Car Crash > Build Production Impact Run Scene` to regenerate the Unity scene
- `Car Crash > Build Production WebGL` to create `Builds/WebGL`

The tested production slice includes a modeled sports car with independent WheelColliders, animated wheels and lights, moving traffic, a textured downtown arena, touch/keyboard steering, boost, high-speed collisions, detachable panels, windshield damage, impact slow motion, scoring, and a retryable result screen.

The Unity home screen offers untimed Free Drive, an optional 60 second Impact Run, a driving guide, and the live 3D garage. Nearby lane traffic uses detailed car meshes with proper bodywork, glass, tires, wheels, and lights. Driving adds compact instruments, a live street minimap, nitro and damage meters, three camera views, impact feedback, pause/resume, and personal-best results. The Elysian city scene includes a sunset HDR sky, textured roads and brickwork, intersections, palms, storefronts, and skyline towers. Press `Enter` for Free Drive, `G` for the garage, `W/A/S/D` to drive, `B` for nitro, `C` for the camera, `R` for manual recovery, and `P` to pause. Free Drive does not end after a minute or a stopped crash, and driving beyond the old camera boundaries never teleports the player. Touch controls work while held. See the [Unity UI and validation guide](unity/CarCrashUnityPrototype/README.md#dynamic-racing-ui).

Hold `W` with `A` or `D` to turn from rest. Hold `Q` or the dedicated `REV` pedal to reverse; while moving forward, it brakes first. `S` also brakes then reverses; `Space` brakes without reversing. Trees and street furniture have solid collision, cameras avoid walls, and 96 textured human pedestrians walk sidewalk routes.

The open world covers 620 by 1,060 metres with seven avenues, thirteen connecting cross streets, a coastal access road and approximately 15.5 km of road centerlines. Explore Old Town, Sunset Marina, Vista Beach, Paloma Park, the freight yard and Apex Motor Park. Press `M` or MAP to browse the city and select a destination for road navigation; browsing pauses the drive. Gentle pavement slopes, suspension-aware chassis clearance and movable traffic at low speed help prevent sticking. `R` / RECOVER finds a clear road position for the selected vehicle and preserves score and elapsed time.

The garage offers six playable vehicles: Roadbreaker GT, Apex GT, Summit AWD, Ridgeback 4x4 pickup, Atlas Hauler cargo truck, and Ironclaw Monster. Each has its own dimensions, weight, wheel setup, suspension, speed and handling. Collisions throw sparks, glass flecks, dust and expanding ground splashes. Menu labels use a larger Atkinson Hyperlegible font with brighter text and darker panels. Validation covers actual forward and reverse travel for every vehicle, collision effects, solid scenery, the full driving session, and readable UI controls; see the Unity guide for the validation commands.

Serve the WebGL output locally from the Unity project:

```powershell
python -m http.server 8094 --bind 127.0.0.1 --directory Builds/WebGL
```

## Run

```powershell
flutter pub get
flutter run
```

## Verify

```powershell
flutter analyze
flutter test
flutter build web --release
flutter build appbundle --release
```

## Play Store Prep

The native Unity game updates the existing Guardian Play app, package
`com.corsairlabs.guardian`. Its installed name is **Crash Car: Elysian**. The
Guardian package is permanent even when its store title changes.

The Android-only GitHub workflow builds the Unity project and uploads only to
internal testing after verifying the existing app's upload key, package, version,
API level and native libraries. See [release setup](android/playstore/README.md)
for signing and the remaining activation steps. The Flutter/Fastlane files are
legacy prototypes and do not produce this game release.

The manual **Android native candidate · Guardian internal** workflow also accepts
an exact-source local Unity 6000.3.29f1 build while the cloud repository connection
is being resolved. It verifies the source and bundle digest, signs inside GitHub
and can upload only to internal testing. See the release guide for the isolated
local export command. Automatic cloud builds remain disabled until verified.

The current local bundle path after a release build is:

```text
build/Android/CrashCar.aab
```

See `THIRD_PARTY_NOTICES.md` for 3D model attribution.

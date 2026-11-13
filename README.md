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

Hold `W` with `A` or `D` to turn from rest. Hold `S` to brake and then reverse out of tight spots; `Space` brakes without reversing. Trees and street furniture have solid collision, cameras avoid walls, and 24 textured human pedestrians walk animated sidewalk routes. The physical QA suite measures actual turning and reverse travel and tests six street objects with 80 km/h car impacts.

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

The Android package is `com.ibsam.crashcar`.

Store metadata and screenshots are in `android/fastlane/metadata/android/en-US/`. Release signing is configured to use `android/key.properties` when present; see `android/playstore/README.md` for keystore and upload steps.

The current local bundle path after a release build is:

```text
build/app/outputs/bundle/release/app-release.aab
```

See `THIRD_PARTY_NOTICES.md` for 3D model attribution.

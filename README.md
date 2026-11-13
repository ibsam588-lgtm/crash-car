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

The Unity UI opens with a live 3D car preview, paint selection, and Start Engine countdown. Driving adds responsive analog gauges, live traffic radar, nitro and damage meters, three camera views, impact feedback, pause/resume, and personal-best results. Press `Enter` to start, `W/A/S/D` to drive, `B` for nitro, `C` for the camera, and `P` to pause. Touch controls work while held. See the [Unity UI and validation guide](unity/CarCrashUnityPrototype/README.md#dynamic-racing-ui).

Serve the WebGL output locally from the Unity project:

```powershell
python -m http.server 8092 --bind 127.0.0.1 --directory Builds/WebGL
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

# Crash Car

A realistic arcade crash-driving game built with Flutter, Flame, and Riverpod.

Crash Car puts the player in a generated realistic muscle car and sends them through arena roads packed with destructible crates, barrels, cones, barricades, moving cars, trucks, buses, and shop fronts. The playable slice includes arena selection, speed and steering controls, impact slow motion, collision scoring, combos, damage, coins, car unlocks, upgrades, a shop-ready screen, and a score summary.

## Stack

- Flutter 3.44+
- Flame for the game loop, collisions, sprites, and debris
- Riverpod for shared progress state
- Shared Preferences for local saves
- Generated PNG assets under `assets/images`

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

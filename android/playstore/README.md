# Crash Car Play Store Setup

Package name: `com.ibsam.crashcar`

## Upload Keystore

Create a private upload keystore locally and keep it out of Git:

```powershell
keytool -genkey -v -keystore android/app/upload-keystore.jks -storetype JKS -keyalg RSA -keysize 2048 -validity 10000 -alias upload
Copy-Item android/key.properties.example android/key.properties
```

Then edit `android/key.properties` with the passwords you entered.

## Build

```powershell
flutter build appbundle --release
```

Upload `build/app/outputs/bundle/release/app-release.aab` in Play Console.

## Store Listing Assets

- App icon: `assets/images/store/app_icon_1024.png`
- Feature graphic: `android/fastlane/metadata/android/en-US/images/featureGraphic.png`
- Title, short description, full description, and changelog are under `android/fastlane/metadata/android/en-US/`.

# Crash Car: Elysian — Unity internal testing

The Android release is the native Unity game in `unity/CarCrashUnityPrototype`.
Package: `com.ibsam.crashcar`. The Flutter app and its Fastlane lane are legacy
prototypes and are not used by the Unity workflow.

## Workflow

`.github/workflows/unity-play-internal.yml` builds a signed ARM64 IL2CPP App Bundle
using Unity 6000.0.60f1, minimum API 26 and target API 36. It uses the committed,
locally verified scene, including baked street reflections. The headless runner
does not regenerate the city or capture reflections.

Before upload, it verifies the actual AAB's signature against the upload
certificate, package, version, target SDK, debug flag, and every native library's
16 KB ELF segment alignment. It saves the AAB and public `release-manifest.json`
as a 14-day Actions artifact. Private signing files and Unity logs are not uploaded.

In Actions → **Unity Android · Play internal** → **Run workflow**, leave **publish**
off for a build-only run. Enable it to upload to **internal**. There is no
production track path. Start with **draft** for a new Play app, then use
**completed** after Play's first-release setup is finished. Version names default
to `1.1.0`; version codes are `GITHUB_RUN_NUMBER * 100 + GITHUB_RUN_ATTEMPT`,
including a new code on reruns. Do not recreate the workflow or manually upload a
higher code without adjusting this sequence.

## GitHub secrets and variables

Use Settings → Secrets and variables → Actions. Never paste keys or account
passwords into issues, pull requests or chat.

| Secret | Value |
| --- | --- |
| `UNITY_LICENSE` | Contents of the activated local Unity Personal `.ulf` |
| `UNITY_EMAIL` | Email of the Unity account that owns that license |
| `UNITY_PASSWORD` | That account's password; enter directly into GitHub |
| `ANDROID_KEYSTORE_BASE64` | Base64 of the private upload JKS |
| `ANDROID_KEYSTORE_PASSWORD` | Upload keystore password |
| `ANDROID_KEY_ALIAS` | Upload signing alias |
| `ANDROID_KEY_PASSWORD` | Upload signing key password |
| `GOOGLE_PLAY_SERVICE_ACCOUNT_JSON` | Dedicated internal-testing publisher's JSON key |

| Variable | Value |
| --- | --- |
| `ANDROID_UPLOAD_CERT_SHA256` | Public SHA-256 fingerprint of the upload certificate |
| `ANDROID_VERSION_NAME` | `1.1.0` initially |
| `UNITY_CI_ENABLED` | `true` after a successful hosted build enables main push builds |
| `PLAY_PUBLISH_ENABLED` | `true` after a successful upload enables main push uploads |
| `PLAY_RELEASE_STATUS` | `draft` initially, `completed` after first-release setup |

GameCI's Personal activation uses the local license plus the Unity account email
and password. A password cannot be generated from the license. For social
sign-in, the account owner must set up their Unity password themselves.

## First Play Console setup

1. Create **Crash Car: Elysian**, game, free, English (United States), with the
   available package `com.ibsam.crashcar` in the intended developer account. The
   owner must authorize the policy and export-law declarations.
2. Enable the Google Play Android Developer API in a dedicated Google Cloud
   project. Create an internal-release service account; no project Owner/Editor
   role is needed.
3. Invite its email in Play Console → Users and permissions. Restrict access to
   this app; grant app visibility and release-to-testing permissions. Do not grant
   production release or access to other apps. Store the JSON key only as the
   GitHub secret above and retain a private recovery copy.
4. Enroll in Play App Signing and upload the first signed AAB to internal testing
   in Play Console. Google may require this manual first upload before API
   publishing. Complete the app declarations based on the shipped game and add
   the intended internal testers.
5. Run the workflow with publish enabled and verify the resulting internal
   release. Enable automatic push publishing only after this succeeds.

The game currently includes no login, ads, analytics SDK, in-app purchases or
online multiplayer. Check the final Android manifest and future dependencies
before privacy, content rating, audience or data-safety declarations. Internal
testing does not make the app public.

## Local release verification

Private signing material belongs in ignored `.private/playstore/` and the
project's ignored `.ci/`, never `Assets/`. Keep an independent secure backup of
the upload JKS and passwords. Replacing these breaks uploads until Play approves
an upload-key reset.

With the four Android signing environment variables set securely:

```powershell
python ci/android_release.py prepare --project unity/CarCrashUnityPrototype --version-name 1.1.0 --version-code 2
# Run Unity with Android support, -batchmode -quit, the project path, and:
# -executeMethod CarCrashEditor.AndroidReleaseBuilder.Build
# Output: build/Android/CrashCar.aab
python ci/android_release.py verify --aab build/Android/CrashCar.aab --bundletool /path/to/bundletool.jar --version-code 2 --certificate-sha256 PUBLIC_CERTIFICATE_SHA256
python -m unittest discover -s ci -v
```

Use Google's bundletool 1.18.3, SHA-256
`a099cfa1543f55593bc2ed16a70a7c67fe54b1747bb7301f37fdfd6d91028e29`.
The workflow pins its checksum and all third-party Actions commits.

References: [GameCI activation](https://game.ci/docs/github/activation/),
[Unity Android dependency versions](https://docs.unity.com/en-us/engine/6000.0/manual/platform-specific/android/getting-started/sdksetup/supported-dependency-versions),
[Play target API requirements](https://developer.android.com/google/play/requirements/target-sdk),
[Play API setup](https://developers.google.com/android-publisher/getting_started),
[16 KB page support](https://developer.android.com/guide/practices/page-sizes).

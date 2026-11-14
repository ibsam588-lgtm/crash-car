# Crash Car: Elysian — Unity internal testing

The Android release is the native Unity game in `unity/CarCrashUnityPrototype`.
Package: `com.ibsam.crashcar`. The Flutter app and its Fastlane lane are legacy
prototypes and are not used by the Unity workflow.

## Workflow

`.github/workflows/unity-play-internal.yml` asks Unity Build Automation to compile
the exact GitHub commit as an ARM64 IL2CPP App Bundle, minimum API 26 and target
API 36. Unity supplies the cloud Editor activation; Google SSO stays unchanged.
GitHub then replaces the temporary cloud signature with the upload key already in
GitHub secrets. No Unity/Google password, upload key or Play publisher key goes to
Unity Cloud. The build uses the committed, locally verified scene, including
baked street reflections; it does not regenerate the city in a headless worker.

Before upload, it verifies the actual AAB's signature against the upload
certificate, package, version, target SDK, debug flag, and every native library's
16 KB ELF segment alignment. It saves the AAB and public `release-manifest.json`
as a 14-day Actions artifact. Private signing files and Unity logs are not uploaded.

In Actions → **Unity Android · Play internal** → **Run workflow**, leave **publish**
off for a build-only run. Enable it to upload to **internal**. There is no
production track path. Start with **draft** for a new Play app, then use
**completed** after Play's first-release setup is finished. Version names default
to `1.1.0`; version codes are `100000 + UCB_BUILD_NUMBER`, including a new code on
reruns. Keep the same Unity Android build target so its build sequence persists.
Do not recreate that target or manually upload a higher code without adjusting
the sequence in both the cloud pre-export method and the GitHub client.

### Platform-specific triggers

This repository currently has an Android release workflow only. Its two jobs are
**Build Android app bundle** and **Upload Android internal test**; neither builds
or uploads iOS. An iOS release needs its own separate workflow and credentials.

Android push builds ignore Unity's `iOS`, `IOS` and `Ios` plugin/editor folders,
and editor C# files beginning with those prefixes. An iOS-only change in those
locations cannot launch Android. Android tooling changes trigger only the Android
release workflow; unrelated future `ci/ios_*` files do not trigger it.

Shared gameplay, scenes, models, packages and shared ProjectSettings files still
affect the Android game and trigger its build once automatic builds are enabled.
When an iOS pipeline is added, shared changes should rebuild both platforms.
Keep platform-only code in the corresponding platform folders to avoid treating
it as a shared change. A manual run targets only the workflow selected in Actions.
The separate **Release tooling checks (no app builds)** workflow can test CI edits
without producing or publishing either app.

## GitHub secrets and variables

Use Settings → Secrets and variables → Actions. Never paste keys or account
passwords into issues, pull requests or chat.

| Secret | Value |
| --- | --- |
| `UNITY_BUILD_AUTH` | `Basic ...` Authorization header of a project-scoped Unity service account |
| `ANDROID_KEYSTORE_BASE64` | Base64 of the private upload JKS |
| `ANDROID_KEYSTORE_PASSWORD` | Upload keystore password |
| `ANDROID_KEY_ALIAS` | Upload signing alias |
| `ANDROID_KEY_PASSWORD` | Upload signing key password |
| `GOOGLE_PLAY_SERVICE_ACCOUNT_JSON` | Dedicated internal-testing publisher's JSON key |

| Variable | Value |
| --- | --- |
| `ANDROID_UPLOAD_CERT_SHA256` | Public SHA-256 fingerprint of the upload certificate |
| `ANDROID_VERSION_NAME` | `1.1.0` initially |
| `UNITY_BUILD_ORG` | Unity organization numeric ID |
| `UNITY_BUILD_PROJECT` | The separate Crash Car Unity Cloud project GUID |
| `UNITY_BUILD_TARGET` | Only the Android internal build target ID; never `_all` |
| `UNITY_CI_ENABLED` | `true` after a successful hosted build enables main push builds |
| `PLAY_PUBLISH_ENABLED` | `true` after a successful upload enables main push uploads |
| `PLAY_RELEASE_STATUS` | `draft` initially, `completed` after first-release setup |

The workflow does not use the older `UNITY_LICENSE`, `UNITY_EMAIL` or
`UNITY_PASSWORD` secrets. Do not enter a Google password into GitHub.

## Passwordless Unity Cloud setup

1. Create a separate **Crash Car Elysian** project in the intended Unity
   organization. Check its free allowance before building. Keep it on the free
   tier unless paid usage is explicitly approved; do not add a payment method.
2. Connect the public repository using manual Git setup and its HTTPS URL.
   Public cloning needs no GitHub token or account-wide repository permission.
3. Create one Android target for branch `main`, subfolder
   `unity/CarCrashUnityPrototype`, Unity **6000.0.60f1** (or a later supported
   6000.0 LTS patch), Android SDK **36**, and an available free-tier machine.
   Choose AAB output, release build, and the committed game scene.
4. Set the pre-export method to
   `CarCrashEditor.AndroidReleaseBuilder.PrepareCloudBuild`. It selects ARM64,
   IL2CPP and the Play package/version. Use Unity's generated debug signing
   credential; the upload key stays in GitHub. For a version-name change, set
   `CRASH_VERSION_NAME` on the Unity target as well as `ANDROID_VERSION_NAME` in
   GitHub.
5. **Disable Unity Auto-build, schedules and automatic Editor upgrades.** GitHub
   alone starts the selected Android target, after its platform path filters
   match. This prevents an iOS-only commit from causing an unfiltered cloud build.
6. Create a service account with access only to this project and the minimum
   Build Automation permissions needed to read, start and cancel its builds and
   download their artifacts. Save its Authorization header in `UNITY_BUILD_AUTH`.
   Do not grant organization administration or access to other projects.
7. Set the three Unity build variables, run a build-only dispatch, inspect the
   cloud log and verified artifact, then perform the first internal upload.
   Keep `UNITY_CI_ENABLED` and `PLAY_PUBLISH_ENABLED` false until verified.

Unity build minutes and artifact storage have free limits. Exhausting them can
block later builds. An Actions timeout or cancellation attempts to stop only its
own unfinished cloud build; an interrupted run's receipt identifies it in the
Unity dashboard. Never publish a different commit's cached artifact.

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

References: [Unity Build Automation API](https://docs.unity.com/en-us/oas-build-automation-client/2.0.0),
[Unity service accounts](https://docs.unity.com/en-us/cloud/accounts/create-service-account),
[Unity free-tier pricing](https://unity.com/products/pricing-updates),
[Unity Android dependency versions](https://docs.unity.com/en-us/engine/6000.0/manual/platform-specific/android/getting-started/sdksetup/supported-dependency-versions),
[Play target API requirements](https://developer.android.com/google/play/requirements/target-sdk),
[Play API setup](https://developers.google.com/android-publisher/getting_started),
[16 KB page support](https://developer.android.com/guide/practices/page-sizes).

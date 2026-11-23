# Crash Car: Elysian — Unity internal testing

The Android release is the native Unity game in `unity/CarCrashUnityPrototype`.
Package: `com.corsairlabs.guardian`, the existing **GuardIan — Parental Controls**
Play record being repurposed for **Crash Car: Elysian**. The package cannot be
renamed. The Flutter app and its Fastlane lane are legacy
prototypes and are not used by the Unity workflow.

## Existing app mapping

- Play developer ID: `6403845194305642986`.
- Play app ID: `4974135516168218399`.
- Existing internal track ID: `4701222303150185899`.
- Highest observed bundle version code: **291** (checked November 22, 2026).
  Local releases reject codes below **292**; the cloud sequence starts at **100001**.
- Registered upload certificate SHA-256:
  `5415E182EC66E1E235D55CBE65E3A1370924812E8389FDDDD209A61D338E326F`.
- Play app-signing certificate SHA-256:
  `77F9C31B76CAC0676A99C297C036F99B9943E284EE87A232DCCCB9B2BD7C3200`.

These are public certificate fingerprints, not signing keys. Keep the existing
Play app-signing key. The newly generated Crash Car upload key is a different
key and cannot update Guardian unless Play first approves an upload-key reset.
The workflow checks the configured keystore against `ANDROID_UPLOAD_CERT_SHA256`
**before** requesting a cloud build, and checks the signed AAB again afterwards.
Do not change that variable merely to bypass a mismatch.

Guardian's original upload signing secrets have been transferred into crash-car
and verified in GitHub. The app-scoped publisher has been verified against
Guardian's internal track, and the old guardian-app uploader has been retired.
The selected internal tester list is **Crash Car Internal Testers**.

Mapping the package does not finish an upload. Automatic cloud builds and publishing
must stay disabled until the Unity repository connection, Android target and a
verified hosted release are ready. Never
publish to the separate unused `com.ibsam.crashcar` Play record from this workflow.

## Locally compiled Android candidates

While the Unity cloud source connection is unavailable, the separate manual-only
**Android native candidate · Guardian internal** workflow can sign and upload a
locally compiled game. It does not run on pushes or start any iOS build. The
Guardian private key remains in GitHub; the local export uses temporary signing.

Use Unity **6000.3.29f1** with Android Build Support, JDK 17, NDK r27c
(`27.2.12479018`) and Android SDK 36. Unity 6000.0.86f1 requires Extended LTS
entitlement and cannot run with this computer's current license. Export from a
clean committed branch that will be merged into main using a merge commit:

```powershell
./ci/build_android_candidate.ps1 -UnityEditor /path/to/Unity.exe -AndroidSdk /path/to/Android/Sdk -AndroidNdk /path/to/ndk/27.2.12479018 -Jdk /path/to/jdk17 -VersionCode 292
```

The script archives the exact Git source into an ignored build directory, leaving
the original WebGL project's Editor state untouched. It exports
`CrashCar-cloud.aab` and `native-source.json` with the source commit, Unity tree,
Editor version, version code and AAB SHA-256. Local codes start at 292 and remain
below 100000, reserved for cloud builds. Always choose a code above every previous
Play upload; failed published-version retries require a new code too.

After local native validation and merging the source into main, attach only these
two files to an **unpublished draft GitHub release**, tagged
`android-native-VERSIONCODE` and targeted at the exact source commit in the receipt.
Do not publish that draft. Run the native-candidate workflow on **main** with its
tag and the receipt's SHA-256. Leave **publish** off for a signing/verification-only
run; select **completed** with publish enabled to update the existing internal
release after validation. Play applies its normal automatic review behavior
when the internal release is committed.

The workflow rejects another app or repository, changed bytes, an unmerged source
commit, a different Unity source tree, a stale Editor, or a non-draft release.
It replaces temporary signing with Guardian's registered key and applies the same
actual-AAB signature, manifest, ARM64 IL2CPP and 16 KB page checks as the cloud
workflow. The signed game goes directly from the runner to Play internal testing.
Actions retains the verification receipts. The repo is public, so the workflow
keeps the AAB out of Actions artifacts. Its ephemeral GitHub token has the
repository contents permission required to read the unpublished draft.

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

In Actions → **Crash Car Android · Guardian internal** → **Run workflow**, leave **publish**
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
| `ANDROID_UPLOAD_CERT_SHA256` | Guardian's registered upload certificate fingerprint shown above; update only after a verified Play key change |
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
   `unity/CarCrashUnityPrototype`, Unity **6000.3.29f1** with Android SDK **36**,
   and an available free-tier machine. Check the project's Editor upgrade in an
   isolated checkout before changing the hosted target.
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
6. Use **Crash Car GitHub Builds**, with permanent **Automation User** and
   **Automation Developer** roles only on Crash Car Elysian, as approved by the
   owner. Do not remove Developer as temporary setup cleanup. It has no
   organization-wide roles. Use the minimum
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

## Reusing the Guardian Play app

1. Use the existing Guardian Play record above. Do not create another app or
   change the Play app-signing key. Keep the closed and production tracks untouched.
2. Guardian's original upload keystore and its passwords are now in the four
   Android secrets in `ibsam588-lgtm/crash-car`. They were transferred using
   GitHub's destination public key, with a matching-certificate/password preflight
   and a successful destination check. A public
   certificate alone cannot sign a bundle. GitHub cannot reveal secret values
   stored in the old `guardian-app` repository through its secrets API.
3. The old Guardian repository's CI is disabled and its Android build/sign/upload
   job has been removed. The one-time encrypted transfer helper and its artifact
   have also been removed. Keep the retired uploader disabled.
4. Use the dedicated Play publisher with permissions only for Guardian: read app
   information and release to testing tracks, plus the dependent read-only app
   quality permission. Remove its previous Crash Car app scope when switching.
   Do not grant production or account-wide access. The existing Google Cloud
   Android Publisher API/key can be retained; no project Owner/Editor role is needed.
5. Select the existing **Crash Car Internal Testers** list on Guardian's internal
   track. Keep private tester addresses out of this public repository. Do not
   modify its closed-testing audience or other apps' tester selections.
6. Prepare the store title, game category, screenshots, privacy URL and declarations
   for the actual game. The old Guardian descriptions and parental-control/data
   declarations do not describe this build. Guardian's old API and billing warnings
   are not cleared simply by renaming it; verify the uploaded game uses API 36 and
   has no obsolete billing dependency.
7. Run a build-only dispatch, inspect its verified native AAB, then upload only to
   internal testing. Enable automatic push builds/publishing after a successful
   verified internal release. Do not apply for production access as part of this setup.

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
python ci/android_release.py check-key --certificate-sha256 GUARDIAN_UPLOAD_CERTIFICATE_SHA256
python ci/android_release.py prepare --project unity/CarCrashUnityPrototype --version-name 1.1.0 --version-code 100001
# Run Unity with Android support, -batchmode -quit, the project path, and:
# -executeMethod CarCrashEditor.AndroidReleaseBuilder.Build
# Output: build/Android/CrashCar.aab
python ci/android_release.py verify --aab build/Android/CrashCar.aab --bundletool /path/to/bundletool.jar --version-code 100001 --certificate-sha256 GUARDIAN_UPLOAD_CERTIFICATE_SHA256
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

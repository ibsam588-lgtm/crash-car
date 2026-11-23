import base64
import json
import os
from pathlib import Path
import struct
import tempfile
import unittest
import zipfile
from unittest.mock import patch

from android_release import PACKAGE, MIN_VERSION_CODE, audit_manifest, check_key, elf_alignment, prepare, release_version, unsigned_copy


class ReleaseSafetyTests(unittest.TestCase):
    def manifest(self, package=PACKAGE, target=36, debug="false", code=100001):
        return f'<manifest xmlns:android="http://schemas.android.com/apk/res/android" package="{package}" android:versionCode="{code}"><uses-sdk android:minSdkVersion="26" android:targetSdkVersion="{target}"/><application android:debuggable="{debug}"/></manifest>'

    def test_release_identity(self):
        self.assertEqual(audit_manifest(self.manifest(), 100001)["targetSdk"], 36)
        for xml in (self.manifest(package="com.ibsam.crashcar"), self.manifest(target=35), self.manifest(debug="true"), self.manifest(code=291)):
            with self.assertRaises(ValueError):
                audit_manifest(xml, 100001)
        with self.assertRaises(ValueError):
            audit_manifest(self.manifest(code=291), 291)

    def test_versions_reject_invalid_or_play_overflow(self):
        self.assertEqual(release_version("1.1.0", "100001"), ("1.1.0", 100001))
        self.assertEqual(release_version("1.1.0", str(MIN_VERSION_CODE)), ("1.1.0", MIN_VERSION_CODE))
        for name, code in (("latest", "100001"), ("1.1.0", "291"), ("1.1.0", "2100000001"), ("1.1.0", "1.5")):
            with self.assertRaises(ValueError):
                release_version(name, code)

    def library(self, alignment=16384, address=16384):
        data = bytearray(120)
        data[:6] = b"\x7fELF\x02\x01"
        struct.pack_into("<H", data, 18, 183)
        struct.pack_into("<Q", data, 32, 64)
        struct.pack_into("<HH", data, 54, 56, 1)
        struct.pack_into("<IIQQQQQQ", data, 64, 1, 5, 0, address, 0, 1, 1, alignment)
        return bytes(data)

    def test_native_page_alignment_checks_segments(self):
        self.assertEqual(elf_alignment(self.library(), "fixture.so"), 1)
        for data in (self.library(4096), self.library(address=4096), b"not an elf", self.library()[:80]):
            with self.assertRaises(ValueError):
                elf_alignment(data, "fixture.so")

    def test_signing_requires_every_secret_and_stays_outside_assets(self):
        with tempfile.TemporaryDirectory() as directory:
            project = Path(directory)
            with patch.dict(os.environ, {}, clear=True), self.assertRaises(ValueError):
                prepare(project, "1.1.0", "100001")
            env = {"ANDROID_KEYSTORE_BASE64": base64.b64encode(b"fixture-keystore" * 20).decode(),
                   "ANDROID_KEYSTORE_PASSWORD": "test-only-password", "ANDROID_KEY_ALIAS": "upload", "ANDROID_KEY_PASSWORD": "test-only-password"}
            with patch.dict(os.environ, env, clear=True):
                prepare(project, "1.1.0", "100001")
            self.assertFalse((project / "Assets").exists())
            private = project / ".ci"
            config = json.loads((private / "android-signing.json").read_text())
            self.assertEqual(config["packageName"], PACKAGE)
            self.assertTrue((project / config["keystore"]).is_file())

    def test_upload_key_preflight_rejects_wrong_key_or_public_only_certificate(self):
        env = {"ANDROID_KEYSTORE_BASE64": base64.b64encode(b"fixture-keystore" * 20).decode(),
               "ANDROID_KEYSTORE_PASSWORD": "test-only-password", "ANDROID_KEY_ALIAS": "upload", "ANDROID_KEY_PASSWORD": "test-only-password"}
        fingerprint = "54" * 32
        certificate = "Entry type: PrivateKeyEntry\nSHA256: " + ":".join(["54"] * 32)
        with patch.dict(os.environ, env, clear=True), patch("android_release.run", return_value=certificate) as run:
            check_key(fingerprint, "keytool")
            command = run.call_args.args[0]
            self.assertIn("-storepass:env", command)
            self.assertNotIn("test-only-password", command)
            self.assertFalse(Path(command[command.index("-keystore") + 1]).exists())
            for bad in (certificate.replace("54", "44"), certificate.replace("PrivateKeyEntry", "trustedCertEntry"), "Owner: CN=Android Debug\n" + certificate):
                run.return_value = bad
                with self.assertRaisesRegex(ValueError, "Guardian"):
                    check_key(fingerprint, "keytool")

    def test_editor_and_publisher_target_the_same_existing_play_app(self):
        root = Path(__file__).resolve().parents[1]
        unity = root / "unity/CarCrashUnityPrototype"
        self.assertEqual(PACKAGE, "com.corsairlabs.guardian")
        self.assertIn(f'PackageName="{PACKAGE}"', (unity / "Assets/CarCrash/Editor/AndroidReleaseBuilder.cs").read_text())
        self.assertIn(f"Android: {PACKAGE}", (unity / "ProjectSettings/ProjectSettings.asset").read_text())
        workflow = (root / ".github/workflows/unity-play-internal.yml").read_text(encoding="utf-8")
        self.assertIn(f"packageName: {PACKAGE}", workflow)
        self.assertLess(workflow.index("android_release.py check-key"), workflow.index("unity_cloud_build.py --commit"))

    def test_cloud_resigning_preserves_payload_and_removes_old_signers(self):
        with tempfile.TemporaryDirectory() as directory:
            source, output = Path(directory) / "cloud.aab", Path(directory) / "release.aab"
            payload = {"BundleConfig.pb": b"bundle config", "base/manifest/AndroidManifest.xml": b"binary manifest",
                       "base/assets/city.bin": bytes(range(256)) * 100, "META-INF/services/game": b"keep",
                       "META-INF/NOTICE": b"keep too"}
            signatures = {name: b"old signing metadata" for name in ("META-INF/MANIFEST.MF", "META-INF/DEBUG.SF", "META-INF/DEBUG.RSA", "META-INF/SIG-OTHER")}
            with zipfile.ZipFile(source, "w", zipfile.ZIP_DEFLATED) as archive:
                for name, value in (payload | signatures).items():
                    archive.writestr(name, value)
            unsigned_copy(source, output)
            with zipfile.ZipFile(output) as archive:
                self.assertEqual({name: archive.read(name) for name in archive.namelist()}, payload)
            with self.assertRaises(ValueError):
                unsigned_copy(source, source)

    def test_cloud_resigning_rejects_non_bundle_input(self):
        with tempfile.TemporaryDirectory() as directory:
            source, output = Path(directory) / "other.zip", Path(directory) / "release.aab"
            with zipfile.ZipFile(source, "w") as archive:
                archive.writestr("other-app.txt", "not a bundle")
            with self.assertRaises(ValueError):
                unsigned_copy(source, output)
            self.assertFalse(output.exists())


if __name__ == "__main__":
    unittest.main()

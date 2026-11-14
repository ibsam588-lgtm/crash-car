import base64
import json
import os
from pathlib import Path
import struct
import tempfile
import unittest
from unittest.mock import patch

from android_release import PACKAGE, audit_manifest, elf_alignment, prepare, release_version


class ReleaseSafetyTests(unittest.TestCase):
    def manifest(self, package=PACKAGE, target=36, debug="false", code=100):
        return f'<manifest xmlns:android="http://schemas.android.com/apk/res/android" package="{package}" android:versionCode="{code}"><uses-sdk android:minSdkVersion="26" android:targetSdkVersion="{target}"/><application android:debuggable="{debug}"/></manifest>'

    def test_release_identity(self):
        self.assertEqual(audit_manifest(self.manifest(), 100)["targetSdk"], 36)
        for xml in (self.manifest(package="other.app"), self.manifest(target=35), self.manifest(debug="true"), self.manifest(code=99)):
            with self.assertRaises(ValueError):
                audit_manifest(xml, 100)

    def test_versions_reject_invalid_or_play_overflow(self):
        self.assertEqual(release_version("1.1.0", "100"), ("1.1.0", 100))
        for name, code in (("latest", "100"), ("1.1.0", "1"), ("1.1.0", "2100000001"), ("1.1.0", "1.5")):
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
                prepare(project, "1.1.0", "100")
            env = {"ANDROID_KEYSTORE_BASE64": base64.b64encode(b"fixture-keystore" * 20).decode(),
                   "ANDROID_KEYSTORE_PASSWORD": "test-only-password", "ANDROID_KEY_ALIAS": "upload", "ANDROID_KEY_PASSWORD": "test-only-password"}
            with patch.dict(os.environ, env, clear=True):
                prepare(project, "1.1.0", "100")
            self.assertFalse((project / "Assets").exists())
            private = project / ".ci"
            config = json.loads((private / "android-signing.json").read_text())
            self.assertEqual(config["packageName"], PACKAGE)
            self.assertTrue((project / config["keystore"]).is_file())


if __name__ == "__main__":
    unittest.main()

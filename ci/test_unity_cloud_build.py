import io
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch
import zipfile

from unity_cloud_build import Client, artifact_link, download_bundle, validate_build


class CloudBuildBoundaryTests(unittest.TestCase):
    def test_only_successful_android_build_of_exact_commit_is_accepted(self):
        commit = "a" * 40
        result = dict(build=7, buildStatus="success", platform="android", buildtargetid="android-internal", lastBuiltRevision=commit)
        self.assertEqual(validate_build(result, commit, "android-internal"), 100007)
        for changes in (dict(platform="ios"), dict(buildStatus="failure"), dict(lastBuiltRevision="b" * 40), dict(buildtargetid="another-target")):
            with self.assertRaises(ValueError):
                validate_build(result | changes, commit, "android-internal")

    def test_only_one_primary_completed_bundle_is_downloaded(self):
        wanted = dict(filename="game.aab", href="https://storage.googleapis.com/fixture/game.aab")
        artifacts = [dict(primary=False, files=[dict(filename="unrelated.aab")]), dict(primary=True, files=[wanted])]
        self.assertEqual(artifact_link(artifacts), wanted)
        for files in ([wanted, wanted], [wanted | dict(resumable=True)], [dict(filename="editor.log")]):
            with self.assertRaises(ValueError):
                artifact_link([dict(primary=True, files=files)])

    def test_authentication_cannot_be_forwarded_to_another_project_or_host(self):
        env = dict(UNITY_BUILD_AUTH="Basic Zml4dHVyZTpmaXh0dXJl", UNITY_BUILD_ORG="123456", UNITY_BUILD_PROJECT="11111111-1111-1111-1111-111111111111", UNITY_BUILD_TARGET="android-internal")
        with patch.dict("os.environ", env, clear=True):
            client = Client()
        for url in ("https://example.com/builds", client.base.replace("123456", "999999") + "/builds", client.base + "-other/builds"):
            with self.assertRaises(ValueError):
                client.api(url)
        with patch.dict("os.environ", env | dict(UNITY_BUILD_TARGET="_all"), clear=True):
            with self.assertRaises(ValueError):
                Client()

    def test_direct_and_archived_bundles_download_without_payload_changes(self):
        raw = io.BytesIO()
        with zipfile.ZipFile(raw, "w", zipfile.ZIP_DEFLATED) as archive:
            archive.writestr("BundleConfig.pb", b"fixture bundle")
            archive.writestr("base/manifest/AndroidManifest.xml", b"fixture manifest")
        bundle = raw.getvalue()
        outer = io.BytesIO()
        with zipfile.ZipFile(outer, "w", zipfile.ZIP_DEFLATED) as archive:
            archive.writestr("nested/game.aab", bundle)
        link = dict(href="https://storage.googleapis.com/fixture/game", redirect=False)
        with tempfile.TemporaryDirectory() as directory:
            output = Path(directory) / "cloud.aab"
            for data in (bundle, outer.getvalue()):
                with patch("unity_cloud_build.urlopen", return_value=io.BytesIO(data)):
                    download_bundle(None, link, output)
                self.assertEqual(output.read_bytes(), bundle)


if __name__ == "__main__":
    unittest.main()

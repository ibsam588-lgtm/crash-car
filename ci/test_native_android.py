import hashlib
from pathlib import Path
import tempfile
import unittest

from android_release import PACKAGE
from native_android import REPOSITORY, validate_candidate


class NativeCandidateSafetyTests(unittest.TestCase):
    def test_candidate_requires_matching_source_identity_and_actual_bytes(self):
        with tempfile.TemporaryDirectory() as directory:
            aab = Path(directory) / "candidate.aab"
            aab.write_bytes(b"local bundle fixture")
            digest = hashlib.sha256(aab.read_bytes()).hexdigest()
            commit, tree = "a" * 40, "b" * 40
            receipt = dict(formatVersion=1, repository=REPOSITORY, packageName=PACKAGE,
                           versionName="1.1.0", versionCode=292, unityVersion="6000.3.29f1",
                           sourceCommit=commit, unityTreeSha=tree, sha256=digest, aabBytes=aab.stat().st_size)
            self.assertEqual(validate_candidate(aab, receipt, digest, tree, tree, True, commit), ("1.1.0", 292))
            for fields in (dict(packageName="com.ibsam.crashcar"), dict(repository="other/repo"),
                           dict(sourceCommit="--help"), dict(unityTreeSha="c" * 40),
                           dict(sha256="0" * 64), dict(aabBytes=1), dict(versionCode=291),
                           dict(versionCode=100001), dict(unityVersion="6000.0.43f1")):
                with self.subTest(fields=fields), self.assertRaises(ValueError):
                    validate_candidate(aab, receipt | fields, digest, tree, tree, True, commit)
            for main, source, ancestor, target in (("c" * 40, tree, True, commit),
                                                    (tree, "c" * 40, True, commit),
                                                    (tree, tree, False, commit),
                                                    (tree, tree, True, "d" * 40)):
                with self.assertRaises(ValueError):
                    validate_candidate(aab, receipt, digest, main, source, ancestor, target)
            aab.write_bytes(b"replaced bundle after receipt")
            with self.assertRaises(ValueError):
                validate_candidate(aab, receipt, digest, tree, tree, True, commit)


if __name__ == "__main__":
    unittest.main()

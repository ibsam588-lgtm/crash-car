"""Accept a locally compiled candidate only when it matches committed main source.

The candidate is a private draft release asset, not a signed Play release. GitHub
applies the registered upload key after these provenance and digest checks.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import subprocess

from android_release import PACKAGE, release_version

REPOSITORY = "ibsam588-lgtm/crash-car"
UNITY_PATH = "unity/CarCrashUnityPrototype"


def checked(command: list[str]) -> str:
    result = subprocess.run(command, text=True, capture_output=True, check=True)
    return result.stdout.strip()


def receipt_identity(receipt: dict) -> tuple[str, int]:
    if not isinstance(receipt, dict) or receipt.get("formatVersion") != 1:
        raise ValueError("Unknown native build receipt format")
    if receipt.get("repository") != REPOSITORY or receipt.get("packageName") != PACKAGE:
        raise ValueError("Candidate belongs to another repository or Play app")
    if not all(isinstance(receipt.get(key), str) and re.fullmatch(r"[a-f0-9]{40}", receipt[key])
               for key in ("sourceCommit", "unityTreeSha")):
        raise ValueError("Candidate is missing its committed Unity source identity")
    if receipt.get("unityVersion") != "6000.3.29f1":
        raise ValueError("Candidate must use the verified Unity 6000.3.29f1 Android toolchain")
    name, code = release_version(str(receipt.get("versionName", "")), str(receipt.get("versionCode", "")))
    if code >= 100000:
        raise ValueError("Local candidate codes must remain below the reserved cloud build sequence")
    return name, code


def validate_candidate(aab: Path, receipt: dict, expected_sha: str, main_tree: str,
                       source_tree: str, source_is_ancestor: bool, release_target: str) -> tuple[str, int]:
    version = receipt_identity(receipt)
    if not re.fullmatch(r"[a-f0-9]{64}", expected_sha):
        raise ValueError("An explicit SHA-256 of the local candidate is required")
    if not source_is_ancestor or release_target != receipt["sourceCommit"]:
        raise ValueError("Candidate source must be committed in main and match the draft release target")
    if receipt["unityTreeSha"] != main_tree or source_tree != main_tree:
        raise ValueError("Candidate Unity source differs from the checked-out main source; rebuild it")
    with aab.open("rb") as handle:
        digest = hashlib.file_digest(handle, "sha256").hexdigest()
    if receipt.get("sha256") != expected_sha or digest != expected_sha or receipt.get("aabBytes") != aab.stat().st_size:
        raise ValueError("Candidate bytes do not match the approved local build receipt and SHA-256")
    return version


def download_candidate(tag: str, digest: str, output: Path) -> tuple[str, int]:
    if not re.fullmatch(r"android-native-[0-9]{3,5}(?:-[a-f0-9]{7,12})?", tag):
        raise ValueError("Use an android-native-VERSIONCODE draft release tag")
    if not re.fullmatch(r"[a-f0-9]{64}", digest):
        raise ValueError("Candidate SHA-256 must contain 64 lowercase hexadecimal characters")
    release = json.loads(checked(["gh", "release", "view", tag, "--repo", REPOSITORY,
                                  "--json", "isDraft,tagName,targetCommitish"]))
    if release.get("isDraft") is not True or release.get("tagName") != tag:
        raise ValueError("Only an unpublished draft candidate is accepted")
    output.mkdir(parents=True, exist_ok=False)
    checked(["gh", "release", "download", tag, "--repo", REPOSITORY, "--dir", str(output),
             "--pattern", "CrashCar-cloud.aab", "--pattern", "native-source.json"])
    receipt_file = output / "native-source.json"
    if receipt_file.stat().st_size > 16384:
        raise ValueError("Native build receipt is too large")
    receipt = json.loads(receipt_file.read_text(encoding="utf-8-sig"))
    receipt_identity(receipt)  # Validate the commit before using it as a Git argument.
    ancestor = subprocess.run(["git", "merge-base", "--is-ancestor", receipt["sourceCommit"], "HEAD"],
                              capture_output=True).returncode == 0
    current_tree = checked(["git", "rev-parse", f"HEAD:{UNITY_PATH}"])
    source_tree = checked(["git", "rev-parse", f"{receipt['sourceCommit']}:{UNITY_PATH}"])
    version = validate_candidate(output / "CrashCar-cloud.aab", receipt, digest, current_tree,
                                 source_tree, ancestor, release.get("targetCommitish", ""))
    if not tag.startswith(f"android-native-{version[1]}") or re.match(r"android-native-([0-9]+)", tag)[1] != str(version[1]):
        raise ValueError("Draft tag and candidate version code differ")
    print(f"Verified local candidate source {receipt['sourceCommit']}, version {version[0]} ({version[1]})")
    if os.environ.get("GITHUB_OUTPUT"):
        with open(os.environ["GITHUB_OUTPUT"], "a", encoding="utf-8") as handle:
            handle.write(f"version_name={version[0]}\nversion_code={version[1]}\n")
    return version


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--tag", required=True)
    parser.add_argument("--sha256", required=True)
    parser.add_argument("--output", type=Path, default=Path("build/Android"))
    args = parser.parse_args()
    try:
        download_candidate(args.tag, args.sha256, args.output)
    except (ValueError, OSError, subprocess.CalledProcessError) as error:
        parser.exit(1, f"Native Android candidate: {error}\n")


if __name__ == "__main__":
    main()

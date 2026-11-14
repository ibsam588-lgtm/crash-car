"""Request one Android build from Unity Build Automation without a user password.

Reference: https://docs.unity.com/en-us/oas-build-automation-client/2.0.0
Only a project-scoped service-account Authorization header is used. Upload signing
and Play publishing stay in GitHub; signed artifact URLs are never logged.
"""
from __future__ import annotations

import argparse
import json
import os
from pathlib import Path
import re
import shutil
import time
from urllib.error import HTTPError, URLError
from urllib.parse import quote, urljoin, urlsplit
from urllib.request import HTTPRedirectHandler, Request, build_opener, urlopen
import zipfile

ORIGIN = "https://build-automation.services.api.unity.com"
PROJECT_PATH = "unity/CarCrashUnityPrototype"
MAX_BUNDLE_BYTES = 2 * 1024 ** 3
VERSION_BASE = 100000


class NoRedirect(HTTPRedirectHandler):
    def redirect_request(self, req, fp, code, msg, headers, newurl):
        return None


def setting(name: str, pattern: str) -> str:
    value = os.environ.get(name, "")
    if not re.fullmatch(pattern, value):
        raise ValueError(f"Configure a valid {name} in GitHub Actions")
    return value


class Client:
    def __init__(self):
        self.auth = setting("UNITY_BUILD_AUTH", r"Basic [A-Za-z0-9+/]+={0,2}")
        org = setting("UNITY_BUILD_ORG", r"[0-9]+")
        project = setting("UNITY_BUILD_PROJECT", r"[a-fA-F0-9-]{36}")
        target = setting("UNITY_BUILD_TARGET", r"[a-zA-Z0-9][a-zA-Z0-9_-]{0,99}")
        self.base = f"{ORIGIN}/v2/orgs/{org}/projects/{project}"
        self.target = target
        self.path = f"/buildtargets/{quote(target, safe='')}"
        self.opener = build_opener(NoRedirect)

    def api(self, path: str, method="GET", body=None):
        url = path if path.startswith("https://") else self.base + path
        if not url.startswith(self.base + "/"):
            raise ValueError("Refusing to send Unity credentials outside this project")
        data = None if body is None else json.dumps(body).encode()
        request = Request(url, data=data, method=method, headers={"Authorization": self.auth, "Accept": "application/json", "Content-Type": "application/json"})
        try:
            with self.opener.open(request, timeout=60) as response:
                data = response.read()
        except HTTPError as error:
            if error.code == 303 and method == "GET":
                data = error.read()  # Unity returns the signed URL as JSON, without Location.
            else:
                raise ValueError(f"Unity Build Automation returned HTTP {error.code}; check the project role and dashboard") from None
        except URLError:
            raise ValueError("Could not reach Unity Build Automation") from None
        return json.loads(data) if data else None

    def cancel(self, number: int):
        if number < 1:
            raise ValueError("Invalid cloud build number")
        self.api(f"{self.path}/builds/{number}", "DELETE")


def validate_build(build: dict, commit: str, target: str) -> int:
    if build.get("buildStatus") != "success" or build.get("platform") != "android" or build.get("buildtargetid") != target:
        raise ValueError("The cloud result is not a successful build of the configured Android target")
    if build.get("lastBuiltRevision", "").lower() != commit.lower():
        raise ValueError("The cloud build did not compile the requested Git commit")
    number = build.get("build")
    if type(number) is not int or not 1 <= number <= 2_000_000_000:
        raise ValueError("Invalid Unity cloud build number")
    return VERSION_BASE + number


def artifact_link(artifacts: list) -> dict:
    files = [file for artifact in artifacts if artifact.get("primary") for file in artifact.get("files", []) if not file.get("resumable")]
    for suffix in (".aab", ".zip"):
        matches = [file for file in files if file.get("filename", "").lower().endswith(suffix)]
        if len(matches) == 1:
            return matches[0]
        if matches:
            raise ValueError("The cloud build returned multiple primary Android bundles")
    raise ValueError("The cloud build has no downloadable primary Android bundle")


def download_bundle(client: Client, link: dict, output: Path):
    href = urljoin(ORIGIN, link["href"])
    if link.get("redirect"):
        href = client.api(href)
    if not isinstance(href, str):
        raise ValueError("Invalid cloud artifact download link")
    parsed = urlsplit(href)
    allowed = parsed.hostname and (parsed.hostname.endswith(".blob.core.windows.net") or parsed.hostname == "storage.googleapis.com" or parsed.hostname.endswith(".storage.googleapis.com"))
    if parsed.scheme != "https" or parsed.username or parsed.password or not allowed:
        raise ValueError("Cloud artifact download must use Unity's HTTPS Azure or Google storage")
    output.parent.mkdir(parents=True, exist_ok=True)
    temporary = output.with_suffix(".download")
    try:
        try:
            with urlopen(Request(href), timeout=120) as response, temporary.open("wb") as target:
                size = 0
                while block := response.read(1024 * 1024):
                    size += len(block)
                    if size > MAX_BUNDLE_BYTES:
                        raise ValueError("The cloud artifact exceeds the Android bundle size limit")
                    target.write(block)
        except (HTTPError, URLError):
            raise ValueError("Could not download the Unity cloud artifact; the signed link may have expired") from None
        with zipfile.ZipFile(temporary) as archive:
            is_bundle = "BundleConfig.pb" in archive.namelist()
            if not is_bundle:
                bundles = [entry for entry in archive.infolist() if entry.filename.lower().endswith(".aab") and not entry.is_dir()]
                if len(bundles) != 1 or bundles[0].file_size > MAX_BUNDLE_BYTES:
                    raise ValueError("Expected exactly one AAB inside the Unity build archive")
                with archive.open(bundles[0]) as reader, output.open("wb") as writer:
                    shutil.copyfileobj(reader, writer, 1024 * 1024)
        if is_bundle:
            temporary.replace(output)
    finally:
        temporary.unlink(missing_ok=True)


def build(client: Client, commit: str, output: Path, minutes: int):
    if not re.fullmatch(r"[a-fA-F0-9]{40}", commit):
        raise ValueError("A full Git commit SHA is required")
    target = client.api(client.path)
    settings = target.get("settings", {})
    if target.get("platform") != "android" or settings.get("scm", {}).get("subdirectory", "").strip("/") != PROJECT_PATH:
        raise ValueError("The cloud target must build this repository's native Unity Android project")
    if settings.get("autoBuild"):
        raise ValueError("Disable Unity auto-build so GitHub's platform path filters control builds")
    result = client.api(client.path + "/builds", "POST", {"commit": commit, "branch": "main", "label": "GitHub Android " + commit[:12]})
    if not isinstance(result, list) or len(result) != 1 or result[0].get("error") or result[0].get("buildtargetid") != client.target:
        raise ValueError("Unity did not start the requested Android build")
    number = result[0].get("build")
    if type(number) is not int or number < 1:
        raise ValueError("Unity did not return a cloud build number")
    receipt = output.with_name("cloud-build.json")
    receipt.parent.mkdir(parents=True, exist_ok=True)
    receipt.write_text(json.dumps({"target": client.target, "build": number, "commit": commit}, indent=2) + "\n", encoding="utf-8")
    print(f"Started Unity Android build {number} for {commit[:12]}", flush=True)
    deadline, previous = time.monotonic() + minutes * 60, None
    try:
        while time.monotonic() < deadline:
            current = client.api(f"{client.path}/builds/{number}")
            state = current.get("buildStatus")
            if state != previous:
                print(f"Unity Android build {number}: {state}", flush=True)
                previous = state
            if state == "success":
                code = validate_build(current, commit, client.target)
                artifacts = client.api(f"{client.path}/builds/{number}/artifacts")
                download_bundle(client, artifact_link(artifacts), output)
                if os.environ.get("GITHUB_OUTPUT"):
                    with open(os.environ["GITHUB_OUTPUT"], "a", encoding="utf-8") as values:
                        values.write(f"version_code={code}\n")
                print(f"Downloaded the Android bundle for version code {code}", flush=True)
                return
            if state in ("failure", "canceled", "unknown"):
                raise ValueError(f"Unity Android build {number} finished with status {state}; inspect its dashboard log")
            time.sleep(20)
        raise ValueError(f"Unity Android build {number} exceeded the wait limit")
    except BaseException:
        try:
            client.cancel(number)
        except (ValueError, OSError):
            print(f"Check the Unity dashboard to ensure build {number} has stopped", flush=True)
        raise


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--commit")
    parser.add_argument("--output", type=Path, default=Path("build/Android/CrashCar-cloud.aab"))
    parser.add_argument("--timeout-minutes", type=int, default=85, choices=range(1, 96), metavar="1..95")
    parser.add_argument("--cancel-receipt", type=Path)
    args = parser.parse_args()
    try:
        client = Client()
        if args.cancel_receipt:
            if args.cancel_receipt.is_file():
                receipt = json.loads(args.cancel_receipt.read_text(encoding="utf-8"))
                if receipt.get("target") != client.target:
                    raise ValueError("The cloud receipt belongs to a different build target")
                client.cancel(receipt["build"])
        elif args.commit:
            build(client, args.commit, args.output, args.timeout_minutes)
        else:
            raise ValueError("Pass --commit or --cancel-receipt")
    except (ValueError, OSError, zipfile.BadZipFile) as error:
        parser.exit(1, f"Unity cloud build: {error}\n")


if __name__ == "__main__":
    main()

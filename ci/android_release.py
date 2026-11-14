"""Prepare private Unity signing inputs, then verify the actual signed Play bundle.

Uses only the Python standard library. Secret values are never command arguments.
"""
from __future__ import annotations

import argparse
import base64
import hashlib
import json
import os
from pathlib import Path
import re
import struct
import subprocess
import xml.etree.ElementTree as ET
import zipfile

PACKAGE = "com.ibsam.crashcar"
ANDROID = "{http://schemas.android.com/apk/res/android}"
SIGNING_NAMES = ("ANDROID_KEYSTORE_BASE64", "ANDROID_KEYSTORE_PASSWORD", "ANDROID_KEY_ALIAS", "ANDROID_KEY_PASSWORD")


def release_version(name: str, code: str) -> tuple[str, int]:
    if not re.fullmatch(r"\d+\.\d+\.\d+(?:[.-][A-Za-z0-9]+)*", name):
        raise ValueError("Version name must be a semantic version, for example 1.1.0")
    if not re.fullmatch(r"[0-9]+", code) or not 2 <= int(code) <= 2_100_000_000:
        raise ValueError("Version code must be between 2 and 2100000000")
    return name, int(code)


def prepare(project: Path, name: str, code: str) -> None:
    name, number = release_version(name, code)
    missing = [key for key in SIGNING_NAMES if not os.environ.get(key)]
    if missing:
        raise ValueError("Missing GitHub signing secrets: " + ", ".join(missing))
    try:
        key = base64.b64decode(os.environ[SIGNING_NAMES[0]], validate=True)
    except ValueError:
        raise ValueError("The upload keystore secret is not valid base64") from None
    if not 100 <= len(key) <= 1_000_000:
        raise ValueError("The upload keystore has an invalid size")
    private = project / ".ci"
    private.mkdir(parents=True, exist_ok=True)
    os.chmod(private, 0o700)
    config = dict(keystore=".ci/upload-keystore.jks", storePassword=os.environ[SIGNING_NAMES[1]],
                  keyAlias=os.environ[SIGNING_NAMES[2]], keyPassword=os.environ[SIGNING_NAMES[3]],
                  versionName=name, versionCode=number, packageName=PACKAGE)
    for path, data in ((private / "upload-keystore.jks", key),
                       (private / "android-signing.json", json.dumps(config).encode())):
        path.write_bytes(data)
        os.chmod(path, 0o600)
    print(f"Prepared private signing inputs for {PACKAGE}, version {name} ({number})")


def elf_alignment(data: bytes, label: str) -> int:
    if len(data) < 64 or data[:6] != b"\x7fELF\x02\x01":
        raise ValueError(f"{label}: expected a little-endian 64-bit ELF library")
    if struct.unpack_from("<H", data, 18)[0] != 183:
        raise ValueError(f"{label}: expected ARM64 machine type")
    start = struct.unpack_from("<Q", data, 32)[0]
    entry_size, count = struct.unpack_from("<HH", data, 54)
    if entry_size < 56 or not count or start + count * entry_size > len(data):
        raise ValueError(f"{label}: malformed ELF program headers")
    loads = 0
    for index in range(count):
        header = start + index * entry_size
        kind, _flags, offset, address, _physical, _files, _memory, align = struct.unpack_from("<IIQQQQQQ", data, header)
        if kind == 1:
            loads += 1
            if align < 16384 or (address - offset) % 16384:
                raise ValueError(f"{label}: native library does not support 16 KB memory pages")
    if not loads:
        raise ValueError(f"{label}: no loadable ELF segments")
    return loads


def audit_manifest(xml: str, expected_code: int) -> dict:
    root = ET.fromstring(xml)
    sdk, app = root.find("uses-sdk"), root.find("application")
    if root.get("package") != PACKAGE:
        raise ValueError("Refusing to publish a bundle for another package")
    if int(root.get(ANDROID + "versionCode", "0")) != expected_code:
        raise ValueError("The bundle version code does not match this release")
    if sdk is None or int(sdk.get(ANDROID + "targetSdkVersion", "0")) < 36:
        raise ValueError("Google Play release requires target SDK 36 or newer")
    if app is None or app.get(ANDROID + "debuggable", "false") != "false":
        raise ValueError("Refusing to publish a debuggable application")
    return dict(package=PACKAGE, versionCode=expected_code, versionName=root.get(ANDROID + "versionName"),
                targetSdk=int(sdk.get(ANDROID + "targetSdkVersion")), minSdk=int(sdk.get(ANDROID + "minSdkVersion", "0")))


def run(command: list[str]) -> str:
    result = subprocess.run(command, capture_output=True, text=True, encoding="utf-8", errors="replace")
    if result.returncode:
        raise ValueError(f"{Path(command[0]).name} validation failed: " + (result.stderr or result.stdout)[-2000:])
    return result.stdout


def verify(aab: Path, bundletool: Path, java: str, jarsigner: str, keytool: str, code: int, fingerprint: str) -> None:
    expected = re.sub(r"[^A-Fa-f0-9]", "", fingerprint).upper()
    if len(expected) != 64:
        raise ValueError("Expected upload certificate SHA-256 is required")
    signature = run([jarsigner, "-J-Duser.language=en", "-verify", str(aab)])
    if "jar verified." not in signature.lower():
        raise ValueError("The bundle is unsigned or has an invalid JAR signature")
    certificate = run([keytool, "-J-Duser.language=en", "-printcert", "-jarfile", str(aab)])
    actual = re.search(r"SHA256:\s*([A-Fa-f0-9:]+)", certificate)
    if "android debug" in certificate.lower() or not actual or actual[1].replace(":", "").upper() != expected:
        raise ValueError("The bundle was not signed by the configured upload key")
    run([java, "-jar", str(bundletool), "validate", "--bundle=" + str(aab)])
    xml = run([java, "-jar", str(bundletool), "dump", "manifest", "--bundle=" + str(aab), "--module=base"])
    report = audit_manifest(xml, code)
    libraries = []
    with zipfile.ZipFile(aab) as archive:
        for entry in archive.infolist():
            lower = entry.filename.lower()
            if lower.endswith((".jks", ".keystore", "android-signing.json")):
                raise ValueError("Signing material was accidentally included in the bundle")
            if lower.endswith(".so") and "/lib/" in lower:
                if "/arm64-v8a/" not in lower:
                    raise ValueError("Only the verified ARM64 native libraries may be shipped")
                elf_alignment(archive.read(entry), entry.filename)
                libraries.append(entry.filename)
    if not libraries or not any(name.endswith("/libil2cpp.so") for name in libraries):
        raise ValueError("Expected native Unity IL2CPP game libraries were not found")
    report.update(sha256=hashlib.sha256(aab.read_bytes()).hexdigest(), uploadCertificateSha256=expected,
                  nativeLibraries=libraries, supports16KbPages=True, track="internal")
    aab.with_name("release-manifest.json").write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(f"Verified signed native Unity bundle: {PACKAGE} ({code}), {len(libraries)} ARM64 libraries, 16 KB pages")


def main() -> None:
    parser = argparse.ArgumentParser()
    sub = parser.add_subparsers(dest="operation", required=True)
    prep = sub.add_parser("prepare")
    prep.add_argument("--project", type=Path, required=True)
    prep.add_argument("--version-name", default="1.1.0")
    prep.add_argument("--version-code", required=True)
    check = sub.add_parser("verify")
    check.add_argument("--aab", type=Path, required=True)
    check.add_argument("--bundletool", type=Path, required=True)
    check.add_argument("--java", default="java")
    check.add_argument("--jarsigner", default="jarsigner")
    check.add_argument("--keytool", default="keytool")
    check.add_argument("--version-code", type=int, required=True)
    check.add_argument("--certificate-sha256", required=True)
    args = parser.parse_args()
    try:
        if args.operation == "prepare":
            prepare(args.project, args.version_name, args.version_code)
        else:
            verify(args.aab, args.bundletool, args.java, args.jarsigner, args.keytool, args.version_code, args.certificate_sha256)
    except (ValueError, OSError, zipfile.BadZipFile, ET.ParseError) as error:
        parser.exit(1, f"Android release: {error}\n")


if __name__ == "__main__":
    main()

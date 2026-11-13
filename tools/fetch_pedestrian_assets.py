"""Fetch the CC0 Innerscene/MakeHuman walking figures and retain provenance."""
import hashlib
import json
from pathlib import Path
from urllib.request import Request, urlopen

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "build/qa-refinement/people-source"
OUT.mkdir(parents=True, exist_ok=True)
ASSETS = {
    "maya": "relaxed-walk-maya-3d-person-animated-2c6669a2",
    "sam": "relaxed-walk-sam-3d-person-animated-732f1206",
    "harper": "brisk-walk-harper-3d-person-animated-0efea8f3",
}
EXPECTED = {
    "maya": "3c22e60a599526d264f7cbb744e5bc1f6b1e93a79daade322942c49e7aac6ab3",
    "sam": "fbf6558ff408b6c5d12c82a5aaa506e07ec796f963a9ac1d4b9c88a39b0fd429",
    "harper": "ab3fa8b1580c6ccf8bfe8bc7d4422bb483f04d4cf4d2f82f215b9bbe713bfcf9",
}
manifest = []
for name, slug in ASSETS.items():
    page = "https://www.innerscene.com/tools/library/3d-parts/" + slug
    target = OUT / (name + ".glb")
    if not target.exists():
        request = Request("https://www.innerscene.com/api/library/" + slug + "/download", headers={"User-Agent": "Mozilla/5.0", "Referer": page})
        with urlopen(request, timeout=60) as response:
            target.write_bytes(response.read())
    data = target.read_bytes()
    if data[:4] != b"glTF":
        raise RuntimeError("Download is not a binary glTF: " + name)
    if hashlib.sha256(data).hexdigest() != EXPECTED[name]:
        raise RuntimeError("Source changed; review the pedestrian asset before conversion: " + name)
    manifest.append({"name": name, "source": page, "license": "CC0-1.0", "sha256": hashlib.sha256(data).hexdigest()})
    print(name, len(data), manifest[-1]["sha256"], flush=True)
(OUT / "manifest.json").write_text(json.dumps(manifest, indent=2), encoding="utf-8")

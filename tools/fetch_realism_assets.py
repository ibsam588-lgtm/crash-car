"""Fetch the explicitly selected CC0 Poly Haven surfaces and lighting into the Unity project."""
import hashlib
import json
import urllib.request
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DESTINATION = ROOT / "unity/CarCrashUnityPrototype/Assets/CarCrash/Textures/Realism"
HEADERS = {"User-Agent": "CrashCar-LocalAssetPreparation/1.0"}


def request(url):
    with urllib.request.urlopen(urllib.request.Request(url, headers=HEADERS), timeout=45) as response:
        return response.read()


def main():
    DESTINATION.mkdir(parents=True, exist_ok=True)
    manifest = []
    selections = {"venice_sunset": ("hdri",), "asphalt_02": ("diff", "nor_gl", "rough"),
                  "brick_wall_003": ("diff", "nor_gl", "rough")}
    for asset, maps in selections.items():
        info = json.loads(request(f"https://api.polyhaven.com/files/{asset}"))
        for kind in maps:
            api_kind = {"diff": "Diffuse", "rough": "Rough"}.get(kind, kind)
            variants = info[api_kind]["2k"]
            file_info = variants["hdr"] if kind == "hdri" else variants.get("png", variants.get("jpg"))
            url = file_info["url"]
            path = DESTINATION / (asset + "_" + kind + Path(url).suffix)
            if not path.exists():
                path.write_bytes(request(url))
            digest = hashlib.sha256(path.read_bytes()).hexdigest()
            manifest.append({"asset": asset, "map": kind, "source": url, "license": "CC0",
                             "file": path.name, "sha256": digest})
            print(path.name, path.stat().st_size, flush=True)
    fonts = DESTINATION.parents[1] / "Resources/Fonts"
    fonts.mkdir(parents=True, exist_ok=True)
    for family, filename in (("barlowcondensed", "BarlowCondensed-Bold.ttf"), ("archivoblack", "ArchivoBlack-Regular.ttf")):
        for name in (filename, "OFL.txt"):
            url = f"https://raw.githubusercontent.com/google/fonts/main/ofl/{family}/{name}"
            target = fonts / (family + "-OFL.txt" if name == "OFL.txt" else name)
            if not target.exists():
                target.write_bytes(request(url))
            manifest.append({"asset": family, "map": "font", "source": url, "license": "OFL-1.1", "file": target.name,
                             "sha256": hashlib.sha256(target.read_bytes()).hexdigest()})
    (DESTINATION / "sources.json").write_text(json.dumps(manifest, indent=2))


if __name__ == "__main__":
    main()

"""Fetch the CC0 vehicle sources used by the expanded playable garage."""
import hashlib
import json
from pathlib import Path
from urllib.request import Request, urlopen

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "build/fleet-upgrade/sources"
OUT.mkdir(parents=True, exist_ok=True)
ASSETS = {
    "pickup": ("crew-cab-pickup-with-open-bed-4f02e0cc", "https://www.innerscene.com/api/library/crew-cab-pickup-with-open-bed-4f02e0cc/download"),
    "suv": ("charcoal-five-door-crossover-suv-a1996830", "https://www.innerscene.com/api/library/charcoal-five-door-crossover-suv-a1996830/download"),
    "hauler": ("7-5-t-class-box-truck-with-roll-up-door-12baea66", "https://www.innerscene.com/api/library/7-5-t-class-box-truck-with-roll-up-door-12baea66/download"),
}
EXPECTED = {
    "pickup": "53d0ae5155f7cc0de03613624f409e45fd3ce49727ea54cc7c3dfb491d39af73",
    "suv": "2b9d026a1592d3254da73e8204e7ceb72f7a45b619ea1f0a5ece35014cb3efe9",
    "hauler": "6b983b515ee6bf044489a88cccab0ba4cbdb8d9b873b0c023098f9ecd15f4115",
}
manifest = []
for name, (slug, url) in ASSETS.items():
    source = "https://www.innerscene.com/tools/library/3d-parts/" + slug
    target = OUT / (name + ".glb")
    if not target.exists():
        with urlopen(Request(url, headers={"User-Agent": "Mozilla/5.0", "Referer": source}), timeout=60) as response:
            target.write_bytes(response.read())
    data = target.read_bytes()
    if data[:4] != b"glTF": raise RuntimeError("Invalid model download: " + name)
    if hashlib.sha256(data).hexdigest()!=EXPECTED[name]: raise RuntimeError("Source changed; review before conversion: "+name)
    manifest.append({"name": name, "source": source, "license": "CC0-1.0", "sha256": hashlib.sha256(data).hexdigest()})
    print(name, len(data), manifest[-1]['sha256'], flush=True)
(OUT / "sources.json").write_text(json.dumps(manifest, indent=2))

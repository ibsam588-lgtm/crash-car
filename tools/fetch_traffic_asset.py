"""Download the original CC BY 4.0 traffic-car source from its artist's repository."""
import hashlib
from pathlib import Path
from urllib.request import urlopen

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "build/traffic-home"
OUT.mkdir(parents=True, exist_ok=True)
BASE = "https://raw.githubusercontent.com/MirageYM/3DModels/master/"
for remote, local, expected in [
    ("SportsCar/Subdiv_Car.fbx", "mori_sportscar_source.fbx", "751c4ec569e3607a4a864ce24a164e61b5ebab694596bfb19a5c8ea32a8022f7"),
    ("LICENSE", "Mori-LICENSE.txt", "4f21a6df8cd2475d645f10327d70497937386826c2df2ee7274f3f0c308ee08e"),
]:
    target = OUT / local
    if not target.exists():
        with urlopen(BASE + remote, timeout=60) as response: target.write_bytes(response.read())
    if hashlib.sha256(target.read_bytes()).hexdigest() != expected:
        raise RuntimeError("Source hash changed; verify the artist's asset before conversion: " + local)
    print("Verified", local)

"""Fetch a small, pinned-by-hash CC0 surface set for the playable city."""
import hashlib
import json
import urllib.request
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DEST = ROOT / 'unity/CarCrashUnityPrototype/Assets/CarCrash/Textures/Realism'


def request(url):
    req = urllib.request.Request(url, headers={'User-Agent': 'Elysian-LocalAssetPreparation/1.0'})
    with urllib.request.urlopen(req, timeout=60) as response:
        return response.read()


def main():
    manifest = []
    manifest_path=DEST/'city-finishes-sources.json'
    previous={item['file']:item for item in json.loads(manifest_path.read_text())} if manifest_path.exists() else {}
    selections = {'concrete_pavement': ('diff', 'nor_gl', 'rough', 'ao'),
                  'plastered_wall_02': ('diff', 'nor_gl', 'rough', 'ao'),
                  'asphalt_02': ('ao',), 'brick_wall_003': ('ao',),
                  'leafy_grass': ('diff','nor_gl','rough','ao'), 'wood_planks': ('diff','nor_gl','rough','ao')}
    for asset, maps in selections.items():
        info = json.loads(request(f'https://api.polyhaven.com/files/{asset}'))
        for kind in maps:
            api_kind = {'diff': 'Diffuse', 'rough': 'Rough', 'ao': 'AO'}.get(kind, kind)
            variants = info[api_kind]['2k']
            selected = variants.get('png', variants.get('jpg'))
            url = selected['url']
            target = DEST / f'{asset}_{kind}.png'
            old=previous.get(target.name)
            if old and target.exists() and old['source']==url and hashlib.sha256(target.read_bytes()).hexdigest()==old['sha256']:
                manifest.append(old)
                print(target.name,'verified cache',flush=True)
                continue
            data = request(url)
            from PIL import Image
            from io import BytesIO
            Image.open(BytesIO(data)).save(target)
            manifest.append({'asset': asset, 'map': kind, 'source': url, 'license': 'CC0',
                             'file': target.name, 'source_sha256': hashlib.sha256(data).hexdigest(),
                             'sha256': hashlib.sha256(target.read_bytes()).hexdigest()})
            print(target.name, target.stat().st_size, flush=True)
    manifest_path.write_text(json.dumps(manifest, indent=2))


if __name__ == '__main__':
    main()

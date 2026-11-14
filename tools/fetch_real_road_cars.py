"""Fetch the selected artist-built CC BY road cars and retain their provenance.

Decode the Lotus GLB with @gltf-transform/cli 4.2.1 `copy` before running
tools/blender/prepare_real_road_cars.py. BMW uses the original artist Blend.
"""
import hashlib
import json
import subprocess
from pathlib import Path
from urllib.request import Request, urlopen
from urllib.parse import urlencode
from html.parser import HTMLParser

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'build/real-models'
DEST = ROOT / 'unity/CarCrashUnityPrototype/Assets/CarCrash/Models/RoadCars'
ASSETS = [
    ('bmw-m3-e30', 'ac3c7013434e403e8faff87948caf422', 'Martin Trafas', '35cad98e89e6f1e54ccd238a3df049d29f20ecec3fc0aa2a85e0ad6c2663d330'),
    ('lotus-elise', '75e9045ac1af4cdbb69318108c81edfd', 'Stéphane Agullo', '4cf5f0d9f51a2e3ed864a3a16c93beff53cd70736e6956204e5061a4cd93d9e5'),
]

def request(url):
    with urlopen(Request(url, headers={'User-Agent':'Elysian-AssetPreparation/1.0'}), timeout=60) as response:
        return response.read()

def main():
    OUT.mkdir(parents=True, exist_ok=True)
    DEST.mkdir(parents=True, exist_ok=True)
    manifest=[]
    for name, uid, author, expected in ASSETS:
        path=OUT/(name+'.glb')
        mirror=f'https://raw.githubusercontent.com/pedrosoandre/carros/main/assets/{name}.glb'
        if not path.exists(): path.write_bytes(request(mirror))
        digest=hashlib.sha256(path.read_bytes()).hexdigest()
        if digest!=expected: raise RuntimeError('Source changed; review before converting '+name)
        metadata=json.loads(request('https://api.sketchfab.com/v3/models/'+uid))
        if metadata['license']['slug']!='by' or metadata['user']['displayName']!=author:
            raise RuntimeError('Source credit/license changed: '+name)
        manifest.append({'id':name,'title':metadata['name'],'author':author,
            'source':'https://sketchfab.com/3d-models/'+uid,'download':mirror,
            'license':'CC-BY-4.0','licenseUrl':'https://creativecommons.org/licenses/by/4.0/',
            'sha256':digest,'changes':'Normalized scale and neutral steering, grouped animated wheels, preserved authored texture maps, converted to FBX and Unity materials.'})
        print('Verified',name,len(path.read_bytes()),flush=True)
    (DEST/'sources.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
    (DEST/'LICENSE.txt').write_text('\n\n'.join(f"{m['title']} by {m['author']}\n{m['source']}\nCC BY 4.0: {m['licenseUrl']}\n{m['changes']}" for m in manifest),encoding='utf-8')
    archive=OUT/'bmw-author-source.rar'
    original='https://drive.google.com/uc?export=download&id=1Q90hgp1deq4El61YaqiDfI7eSyEpCtjE'
    if not archive.exists():
        response=request(original)
        if response.startswith(b'<!DOCTYPE html>'):
            class DownloadForm(HTMLParser):
                def __init__(self):super().__init__();self.url='';self.fields={}
                def handle_starttag(self,tag,attributes):
                    attributes=dict(attributes)
                    if tag=='form':self.url=attributes['action']
                    if tag=='input' and attributes.get('type')=='hidden':self.fields[attributes['name']]=attributes['value']
            form=DownloadForm();form.feed(response.decode())
            if form.url!='https://drive.usercontent.google.com/download':raise RuntimeError('Unexpected author download form')
            response=request(form.url+'?'+urlencode(form.fields))
        archive.write_bytes(response)
    digest=hashlib.sha256(archive.read_bytes()).hexdigest()
    if digest!='5d21fb238d02fb6c9a10f53e9b790f6a9924616071c3eab90c3027505d1f0828':
        raise RuntimeError('Artist archive changed; review before extracting')
    author=(OUT/'author').resolve();author.mkdir(exist_ok=True)
    entries=subprocess.check_output(['tar','-tf',str(archive)],text=True).splitlines()
    for entry in entries:
        if not (author/entry).resolve().is_relative_to(author):raise RuntimeError('Unsafe archive member')
    subprocess.run(['tar','-xf',str(archive),'-C',str(author)],check=True)
    manifest[0].update({'modelSource':original,'modelSourceSha256':digest,
        'modelSourceFile':'BMW E30_Final01/E30_Final01.blend',
        'changes':'Converted the original artist Blend at full body resolution, neutralized wheel steering, retained authored smoothing and PBR maps, grouped four animated wheels and prepared FBX/Unity surfaces.'})
    (DEST/'sources.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')

if __name__=='__main__':main()

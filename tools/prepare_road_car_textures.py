"""Pack each authored roughness/metal map for Unity's Standard input."""
import json
from pathlib import Path
from PIL import Image, ImageOps, ImageChops

ROOT=Path(__file__).resolve().parents[1]
MODELS=ROOT/'unity/CarCrashUnityPrototype/Assets/CarCrash/Models/RoadCars'
TEX=ROOT/'unity/CarCrashUnityPrototype/Assets/CarCrash/Textures/RoadCars'
for kind in ('bmw-m3-e30','lotus-elise'):
    path=MODELS/(kind+'.json');data=json.loads(path.read_text())
    for i,material in enumerate(data['materials']):
        if material['role']=='Lens' and material['baseTexture']:
            # Clear headlamp regions transmit the reflector; dyed red/amber
            # plastic remains opaque enough to retain the authored lamp colors.
            source=material.get('sourceBaseTexture',material['baseTexture'])
            image=Image.open(TEX/source).convert('RGB')
            red,green,blue=image.split()
            maximum=ImageChops.lighter(ImageChops.lighter(red,green),blue)
            minimum=ImageChops.darker(ImageChops.darker(red,green),blue)
            alpha=ImageChops.subtract(maximum,minimum).point(lambda value:round(48+min(1,value/70)*195))
            material['sourceBaseTexture']=source
            material['baseTexture']=f'{kind}_lens_tint.png'
            Image.merge('RGBA',(red,green,blue,alpha)).save(TEX/material['baseTexture'])
        size=(1024,1024)
        rough=Image.new('L',size,round(material['roughness']*255))
        metal=Image.new('L',size,round(material['metallic']*255))
        if material['roughTexture']:
            source=Image.open(TEX/material['roughTexture']).convert('RGB').resize(size,Image.Resampling.LANCZOS)
            rough=source.getchannel('G' if material['packedSource'] else 'R')
        if material['metalTexture']:
            source=Image.open(TEX/material['metalTexture']).convert('RGB').resize(size,Image.Resampling.LANCZOS)
            metal=source.getchannel('B' if material['packedSource'] else 'R')
        material['maskTexture']=f'{kind}_{i:02}_mask.png'
        Image.merge('RGBA',(metal,Image.new('L',size),Image.new('L',size),ImageOps.invert(rough))).save(TEX/material['maskTexture'])
    path.write_text(json.dumps(data,indent=2))
    print('Packed authored surfaces',kind,flush=True)

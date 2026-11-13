"""Convert source PBR data channels into Unity Standard's metallic/smoothness format."""
import json
from pathlib import Path
from PIL import Image, ImageChops, ImageOps

ROOT = Path(__file__).resolve().parents[1]
TEXTURES = ROOT / "unity/CarCrashUnityPrototype/Assets/CarCrash/Textures"


def main():
    car = TEXTURES / "Roadbreaker"
    info = json.loads((car / "materials.json").read_text())
    for material in info["materials"]:
        source = material["metalRough"]
        if source < 0:
            continue
        packed = Image.open(car / f"source_{source:02}.png").convert("RGB")
        roughness = packed.getchannel("G")
        metal = packed.getchannel("B")
        zero = Image.new("L", packed.size, 0)
        Image.merge("RGBA", (metal, zero, zero, ImageOps.invert(roughness))).save(car / f"metal_smooth_{source:02}.png")
    for name in ("asphalt_02", "brick_wall_003"):
        rough = Image.open(TEXTURES / "Realism" / f"{name}_rough.png").convert("L")
        zero = Image.new("L", rough.size, 0)
        smooth = ImageOps.invert(rough)
        Image.merge("RGBA", (zero, zero, zero, smooth)).save(TEXTURES / "Realism" / f"{name}_smooth.png")
    print("Unity PBR texture channels prepared")


if __name__ == "__main__":
    main()

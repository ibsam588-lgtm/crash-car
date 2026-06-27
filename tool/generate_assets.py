from __future__ import annotations

import math
import random
import shutil
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter, ImageFont


ROOT = Path(__file__).resolve().parents[1]
ASSETS = ROOT / "assets" / "images"
KEY_ART_SOURCE = Path(
    r"C:\Users\ibsam\.codex\generated_images\019f073b-cc92-7943-8a6c-bac939243d82\ig_0cf86991fb05c983016a3f4bb676a481959ebd9496d838019e.png"
)


def ensure_dirs() -> None:
    for folder in [
        ASSETS,
        ASSETS / "cars",
        ASSETS / "obstacles",
        ASSETS / "debris",
        ASSETS / "icons",
        ASSETS / "traffic",
        ASSETS / "ui",
        ASSETS / "store",
    ]:
        folder.mkdir(parents=True, exist_ok=True)


def save(img: Image.Image, path: Path) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    img.save(path)


def rounded_rectangle_mask(size: tuple[int, int], radius: int) -> Image.Image:
    mask = Image.new("L", size, 0)
    draw = ImageDraw.Draw(mask)
    draw.rounded_rectangle((0, 0, size[0] - 1, size[1] - 1), radius=radius, fill=255)
    return mask


def radial_gradient(size: tuple[int, int], inner: tuple[int, int, int], outer: tuple[int, int, int]) -> Image.Image:
    w, h = size
    img = Image.new("RGB", size, outer)
    px = img.load()
    cx, cy = w * 0.5, h * 0.42
    max_dist = math.hypot(max(cx, w - cx), max(cy, h - cy))
    for y in range(h):
        for x in range(w):
            d = min(1.0, math.hypot(x - cx, y - cy) / max_dist)
            ease = d * d * (3 - 2 * d)
            px[x, y] = tuple(int(inner[i] * (1 - ease) + outer[i] * ease) for i in range(3))
    return img


def draw_shadow(base: Image.Image, bbox: tuple[int, int, int, int], blur: int = 14, alpha: int = 90) -> None:
    shadow = Image.new("RGBA", base.size, (0, 0, 0, 0))
    draw = ImageDraw.Draw(shadow)
    draw.ellipse(bbox, fill=(0, 0, 0, alpha))
    shadow = shadow.filter(ImageFilter.GaussianBlur(blur))
    base.alpha_composite(shadow)


def make_car(
    body: tuple[int, int, int],
    stripe: tuple[int, int, int],
    accent: tuple[int, int, int],
    name: str,
) -> None:
    w, h = 180, 300
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    draw_shadow(img, (24, 62, w - 24, h - 22), blur=12, alpha=120)
    draw = ImageDraw.Draw(img)

    # Tires.
    tire = (12, 16, 20, 255)
    for x0, x1 in [(16, 43), (w - 43, w - 16)]:
        draw.rounded_rectangle((x0, 72, x1, 136), radius=9, fill=tire)
        draw.rounded_rectangle((x0, 178, x1, 248), radius=9, fill=tire)
        draw.rectangle((x0 + 5, 84, x1 - 5, 124), fill=(38, 43, 48, 255))
        draw.rectangle((x0 + 5, 194, x1 - 5, 236), fill=(38, 43, 48, 255))

    # Main body.
    draw.rounded_rectangle((36, 22, w - 36, h - 20), radius=42, fill=body + (255,))
    draw.rounded_rectangle((44, 34, w - 44, h - 32), radius=34, outline=(255, 255, 255, 70), width=3)

    # Hood and trunk panels.
    draw.rounded_rectangle((48, 38, w - 48, 99), radius=22, fill=tuple(min(255, c + 18) for c in body) + (255,))
    draw.rounded_rectangle((50, h - 92, w - 50, h - 34), radius=20, fill=tuple(max(0, c - 25) for c in body) + (255,))

    # Windshield and rear glass.
    glass_top = (22, 41, 57, 238)
    glass_bottom = (71, 123, 154, 220)
    draw.rounded_rectangle((53, 111, w - 53, 159), radius=18, fill=glass_top)
    draw.rounded_rectangle((57, 116, w - 57, 143), radius=13, fill=glass_bottom)
    draw.rounded_rectangle((55, 181, w - 55, 221), radius=17, fill=glass_top)
    draw.rounded_rectangle((60, 185, w - 60, 207), radius=12, fill=glass_bottom)

    # Racing stripes.
    for x in (w // 2 - 24, w // 2 + 10):
        draw.rounded_rectangle((x, 28, x + 14, h - 31), radius=6, fill=stripe + (245,))
    draw.rectangle((w // 2 - 5, 28, w // 2 + 5, h - 31), fill=accent + (180,))

    # Lights and bumper details.
    draw.rounded_rectangle((55, 14, 78, 28), radius=5, fill=(255, 232, 126, 255))
    draw.rounded_rectangle((w - 78, 14, w - 55, 28), radius=5, fill=(255, 232, 126, 255))
    draw.rounded_rectangle((54, h - 25, 82, h - 15), radius=4, fill=(255, 56, 42, 255))
    draw.rounded_rectangle((w - 82, h - 25, w - 54, h - 15), radius=4, fill=(255, 56, 42, 255))
    draw.rounded_rectangle((66, h - 19, w - 66, h - 11), radius=3, fill=(36, 40, 45, 255))

    # Gloss.
    gloss = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    gdraw = ImageDraw.Draw(gloss)
    gdraw.polygon([(49, 44), (78, 34), (67, 252), (43, 236)], fill=(255, 255, 255, 38))
    gdraw.polygon([(108, 39), (131, 49), (118, 229), (98, 245)], fill=(255, 255, 255, 24))
    img.alpha_composite(gloss)
    save(img, ASSETS / "cars" / f"{name}.png")


def make_crate() -> None:
    size = 128
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    draw_shadow(img, (18, 90, 110, 124), blur=10, alpha=85)
    draw = ImageDraw.Draw(img)
    draw.rounded_rectangle((19, 20, 109, 110), radius=7, fill=(129, 82, 42, 255), outline=(70, 42, 24, 255), width=4)
    for inset in (27, 96):
        draw.line((inset, 23, inset, 108), fill=(81, 48, 25, 255), width=6)
    draw.line((23, 32, 105, 32), fill=(177, 113, 58, 255), width=4)
    draw.line((24, 102, 104, 102), fill=(77, 43, 22, 255), width=5)
    draw.line((29, 27, 99, 104), fill=(77, 43, 22, 255), width=7)
    draw.line((99, 27, 29, 104), fill=(77, 43, 22, 255), width=7)
    save(img, ASSETS / "obstacles" / "crate.png")


def make_barrel(color: tuple[int, int, int], name: str) -> None:
    w, h = 108, 140
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    draw_shadow(img, (15, 100, w - 15, h - 4), blur=9, alpha=95)
    draw = ImageDraw.Draw(img)
    dark = tuple(max(0, c - 70) for c in color)
    light = tuple(min(255, c + 42) for c in color)
    draw.ellipse((22, 7, w - 22, 37), fill=light + (255,), outline=dark + (255,), width=4)
    draw.rounded_rectangle((22, 22, w - 22, h - 18), radius=18, fill=color + (255,), outline=dark + (255,), width=4)
    draw.ellipse((22, h - 40, w - 22, h - 10), fill=dark + (255,), outline=dark + (255,), width=4)
    for y in (48, 92):
        draw.rectangle((23, y, w - 23, y + 9), fill=(222, 225, 210, 230))
    draw.rectangle((34, 25, 50, h - 24), fill=(255, 255, 255, 24))
    save(img, ASSETS / "obstacles" / f"{name}.png")


def make_cone() -> None:
    w, h = 108, 132
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    draw_shadow(img, (18, 106, w - 18, h - 8), blur=8, alpha=85)
    draw = ImageDraw.Draw(img)
    draw.rounded_rectangle((18, 101, w - 18, 121), radius=6, fill=(38, 42, 45, 255))
    draw.polygon([(54, 8), (25, 105), (83, 105)], fill=(238, 99, 32, 255), outline=(135, 52, 16, 255))
    draw.polygon([(54, 8), (43, 105), (25, 105)], fill=(255, 135, 48, 255))
    draw.rectangle((34, 72, 74, 84), fill=(242, 245, 226, 255))
    draw.rectangle((42, 42, 66, 51), fill=(242, 245, 226, 255))
    save(img, ASSETS / "obstacles" / "cone.png")


def make_barricade() -> None:
    w, h = 168, 118
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    draw_shadow(img, (20, 85, w - 20, h - 2), blur=10, alpha=90)
    draw = ImageDraw.Draw(img)
    draw.rounded_rectangle((20, 35, w - 20, 72), radius=7, fill=(230, 168, 51, 255), outline=(79, 49, 18, 255), width=4)
    for x in range(28, w - 36, 34):
        draw.polygon([(x, 35), (x + 19, 35), (x + 1, 72), (x - 18, 72)], fill=(29, 35, 41, 255))
    for x in (37, w - 51):
        draw.rounded_rectangle((x, 69, x + 16, 108), radius=5, fill=(48, 52, 56, 255))
    save(img, ASSETS / "obstacles" / "barricade.png")


def make_truck(color: tuple[int, int, int], name: str, cargo: tuple[int, int, int]) -> None:
    w, h = 190, 330
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    draw_shadow(img, (28, 96, w - 28, h - 24), blur=15, alpha=115)
    draw = ImageDraw.Draw(img)
    tire = (12, 15, 17, 255)
    for x0, x1 in [(18, 46), (w - 46, w - 18)]:
        for y0, y1 in [(68, 126), (176, 238), (246, 306)]:
            draw.rounded_rectangle((x0, y0, x1, y1), radius=8, fill=tire)
            draw.rectangle((x0 + 6, y0 + 10, x1 - 6, y1 - 10), fill=(42, 45, 47, 255))

    draw.rounded_rectangle((47, 24, w - 47, 127), radius=24, fill=color + (255,), outline=(18, 24, 28, 255), width=4)
    draw.rounded_rectangle((58, 57, w - 58, 105), radius=12, fill=(32, 61, 75, 238), outline=(168, 206, 214, 160), width=3)
    draw.rounded_rectangle((41, 122, w - 41, h - 23), radius=14, fill=cargo + (255,), outline=(42, 48, 50, 255), width=5)
    draw.rectangle((53, 144, w - 53, h - 43), fill=tuple(min(255, c + 24) for c in cargo) + (255,))
    for y in range(152, h - 60, 38):
        draw.line((51, y, w - 51, y), fill=(255, 255, 255, 34), width=3)
    draw.rounded_rectangle((62, 14, 84, 28), radius=4, fill=(255, 228, 116, 255))
    draw.rounded_rectangle((w - 84, 14, w - 62, 28), radius=4, fill=(255, 228, 116, 255))
    draw.rectangle((61, 134, 75, h - 32), fill=(255, 255, 255, 34))
    save(img, ASSETS / "traffic" / f"{name}.png")


def make_bus() -> None:
    w, h = 184, 340
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    draw_shadow(img, (25, 90, w - 25, h - 18), blur=16, alpha=110)
    draw = ImageDraw.Draw(img)
    for x0, x1 in [(18, 42), (w - 42, w - 18)]:
        draw.rounded_rectangle((x0, 74, x1, 132), radius=8, fill=(13, 16, 18, 255))
        draw.rounded_rectangle((x0, 230, x1, 296), radius=8, fill=(13, 16, 18, 255))
    draw.rounded_rectangle((42, 18, w - 42, h - 18), radius=28, fill=(232, 182, 50, 255), outline=(66, 50, 16, 255), width=5)
    draw.rounded_rectangle((53, 40, w - 53, 96), radius=14, fill=(28, 57, 72, 245), outline=(255, 255, 255, 85), width=3)
    for y in range(118, 260, 44):
        draw.rounded_rectangle((54, y, w - 54, y + 30), radius=8, fill=(31, 65, 81, 238))
    draw.rectangle((60, h - 66, w - 60, h - 42), fill=(34, 39, 42, 255))
    draw.line((w // 2, 24, w // 2, h - 24), fill=(255, 255, 255, 60), width=3)
    save(img, ASSETS / "traffic" / "city_bus.png")


def make_shop(name: str, wall: tuple[int, int, int], awning: tuple[int, int, int], sign: tuple[int, int, int]) -> None:
    w, h = 230, 150
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    draw_shadow(img, (18, 106, w - 18, h - 6), blur=10, alpha=105)
    draw = ImageDraw.Draw(img)
    draw.rounded_rectangle((16, 30, w - 16, h - 18), radius=8, fill=wall + (255,), outline=(28, 34, 36, 255), width=4)
    draw.rounded_rectangle((31, 43, w - 31, 73), radius=6, fill=sign + (255,))
    draw.polygon([(12, 30), (w - 12, 30), (w - 31, 6), (31, 6)], fill=awning + (255,), outline=(39, 35, 25, 255))
    stripe_w = 24
    for x in range(32, w - 31, stripe_w * 2):
        draw.polygon([(x, 7), (x + stripe_w, 7), (x + stripe_w - 9, 30), (x - 9, 30)], fill=(248, 244, 224, 255))
    draw.rectangle((48, 86, 96, h - 22), fill=(28, 53, 62, 238))
    draw.rectangle((125, 88, w - 48, h - 30), fill=(36, 69, 82, 230))
    draw.line((125, 113, w - 48, 113), fill=(255, 255, 255, 55), width=3)
    save(img, ASSETS / "traffic" / f"{name}.png")


def make_debris() -> None:
    pieces = [
        ("wood_1", [(9, 12), (71, 27), (58, 45), (4, 30)], (141, 81, 35)),
        ("wood_2", [(18, 7), (53, 14), (44, 77), (8, 68)], (115, 67, 32)),
        ("metal_1", [(12, 22), (63, 11), (73, 37), (18, 59)], (114, 128, 137)),
        ("glass_1", [(19, 8), (60, 22), (42, 65)], (98, 183, 218)),
    ]
    for name, poly, color in pieces:
        img = Image.new("RGBA", (86, 86), (0, 0, 0, 0))
        draw = ImageDraw.Draw(img)
        draw.polygon(poly, fill=color + (245,), outline=tuple(max(0, c - 54) for c in color) + (255,))
        draw.line(poly[:2], fill=(255, 255, 255, 60), width=3)
        save(img, ASSETS / "debris" / f"{name}.png")


def make_crash_fragments() -> None:
    rng = random.Random(42)
    sources = [
        "realistic_muscle_orange.png",
        "realistic_interceptor_blue.png",
        "realistic_rally_green.png",
        "realistic_stunt_red.png",
    ]
    index = 1
    for source_name in sources:
        source_path = ASSETS / "cars" / source_name
        if not source_path.exists():
            continue
        src = Image.open(source_path).convert("RGBA")
        bbox = src.getchannel("A").getbbox()
        if bbox is None:
            continue
        for _ in range(5):
            for _attempt in range(40):
                w = rng.randint(68, 130)
                h = rng.randint(46, 110)
                x = rng.randint(bbox[0], max(bbox[0], bbox[2] - w))
                y = rng.randint(bbox[1], max(bbox[1], bbox[3] - h))
                crop = src.crop((x, y, x + w, y + h))
                if crop.getchannel("A").getbbox() is not None:
                    break
            mask = Image.new("L", crop.size, 0)
            points = [
                (rng.randint(0, crop.width // 3), rng.randint(0, crop.height // 3)),
                (rng.randint(crop.width // 2, crop.width), rng.randint(0, crop.height // 3)),
                (rng.randint(crop.width // 2, crop.width), rng.randint(crop.height // 2, crop.height)),
                (rng.randint(0, crop.width // 3), rng.randint(crop.height // 2, crop.height)),
            ]
            ImageDraw.Draw(mask).polygon(points, fill=255)
            alpha = Image.composite(crop.getchannel("A"), Image.new("L", crop.size, 0), mask)
            crop.putalpha(alpha)
            crop = crop.rotate(rng.uniform(-34, 34), expand=True, resample=Image.Resampling.BICUBIC)
            save(crop, ASSETS / "debris" / f"car_fragment_{index:02d}.png")
            index += 1

    for i in range(1, 11):
        size = rng.randint(54, 96)
        img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
        draw = ImageDraw.Draw(img)
        points = [
            (rng.randint(4, size // 2), rng.randint(2, size // 3)),
            (rng.randint(size // 2, size - 3), rng.randint(5, size // 2)),
            (rng.randint(size // 2, size - 3), rng.randint(size // 2, size - 4)),
            (rng.randint(2, size // 2), rng.randint(size // 2, size - 3)),
        ]
        draw.polygon(points, fill=(130, 210, 235, 112), outline=(218, 248, 255, 185))
        draw.line(points[:2], fill=(255, 255, 255, 170), width=2)
        draw.line((points[0], points[2]), fill=(255, 255, 255, 92), width=1)
        save(img, ASSETS / "debris" / f"glass_shard_{i:02d}.png")

    for i in range(1, 9):
        size = rng.randint(54, 94)
        img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
        draw = ImageDraw.Draw(img)
        color = rng.choice([(95, 104, 110), (145, 151, 153), (56, 64, 68)])
        points = [
            (rng.randint(5, size // 3), rng.randint(5, size // 2)),
            (rng.randint(size // 2, size - 4), rng.randint(2, size // 3)),
            (rng.randint(size // 2, size - 3), rng.randint(size // 2, size - 4)),
            (rng.randint(3, size // 2), rng.randint(size // 2, size - 3)),
        ]
        draw.polygon(points, fill=color + (230,), outline=(220, 226, 224, 130))
        draw.line(points[:2], fill=(255, 255, 255, 75), width=2)
        save(img, ASSETS / "debris" / f"metal_shard_{i:02d}.png")


def make_icon(name: str, draw_fn) -> None:
    img = Image.new("RGBA", (128, 128), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    draw_fn(draw)
    save(img, ASSETS / "icons" / f"{name}.png")


def make_icons() -> None:
    def coin(draw: ImageDraw.ImageDraw) -> None:
        draw.ellipse((19, 19, 109, 109), fill=(248, 181, 32, 255), outline=(116, 76, 17, 255), width=6)
        draw.ellipse((35, 32, 93, 93), fill=(255, 214, 76, 255), outline=(187, 118, 20, 255), width=5)
        draw.text((56, 47), "C", fill=(102, 64, 12, 255), anchor="mm")

    def lightning(draw: ImageDraw.ImageDraw) -> None:
        draw.ellipse((10, 10, 118, 118), fill=(31, 89, 111, 220), outline=(87, 207, 255, 255), width=5)
        draw.polygon([(72, 18), (39, 69), (62, 69), (51, 112), (90, 56), (66, 56)], fill=(76, 219, 255, 255))

    def trophy(draw: ImageDraw.ImageDraw) -> None:
        draw.rounded_rectangle((44, 31, 84, 79), radius=12, fill=(250, 188, 43, 255), outline=(121, 80, 15, 255), width=5)
        draw.arc((19, 32, 57, 71), 269, 91, fill=(250, 188, 43, 255), width=7)
        draw.arc((71, 32, 109, 71), 89, 271, fill=(250, 188, 43, 255), width=7)
        draw.rectangle((59, 78, 69, 101), fill=(169, 104, 19, 255))
        draw.rounded_rectangle((39, 98, 89, 112), radius=6, fill=(250, 188, 43, 255))

    def damage(draw: ImageDraw.ImageDraw) -> None:
        draw.polygon([(25, 101), (41, 32), (61, 67), (79, 21), (101, 98), (70, 79), (55, 111)], fill=(252, 93, 48, 255))
        draw.line((37, 96, 86, 41), fill=(255, 205, 67, 255), width=6)

    for name, fn in [("coin", coin), ("lightning", lightning), ("trophy", trophy), ("damage", damage)]:
        make_icon(name, fn)


def make_road() -> None:
    w, h = 512, 1024
    img = radial_gradient((w, h), (82, 86, 88), (28, 31, 34)).convert("RGBA")
    draw = ImageDraw.Draw(img)
    lane_w = w / 4
    for i in range(1, 4):
        x = int(i * lane_w)
        for y in range(-40, h, 112):
            draw.rounded_rectangle((x - 5, y, x + 5, y + 58), radius=3, fill=(228, 198, 83, 210))
    for x in (36, w - 36):
        draw.line((x, 0, x, h), fill=(211, 221, 220, 110), width=5)
    for y in range(0, h, 80):
        draw.line((0, y, w, y + 34), fill=(255, 255, 255, 10), width=2)
    save(img, ASSETS / "ui" / "road_lane.png")


def make_garage_floor() -> None:
    w, h = 900, 600
    img = radial_gradient((w, h), (35, 49, 58), (7, 14, 18)).convert("RGBA")
    draw = ImageDraw.Draw(img)
    for x in range(0, w, 72):
        draw.line((x, 0, x - 150, h), fill=(255, 255, 255, 13), width=1)
    for y in range(80, h, 92):
        draw.line((0, y, w, y), fill=(255, 255, 255, 17), width=1)
    for x in (160, 450, 740):
        draw.rounded_rectangle((x - 75, 44, x + 75, 53), radius=4, fill=(134, 193, 213, 180))
        glow = Image.new("RGBA", (w, h), (0, 0, 0, 0))
        gdraw = ImageDraw.Draw(glow)
        gdraw.ellipse((x - 130, 30, x + 130, 220), fill=(85, 171, 211, 28))
        img.alpha_composite(glow.filter(ImageFilter.GaussianBlur(18)))
    save(img, ASSETS / "ui" / "garage_floor.png")


def make_app_icon() -> Image.Image:
    img = Image.new("RGBA", (1024, 1024), (7, 13, 17, 255))
    draw = ImageDraw.Draw(img)
    for i in range(70):
        x = (i * 83) % 1024
        y = (i * 151) % 1024
        draw.line((x, y, min(1024, x + 90), max(0, y - 40)), fill=(255, 154, 36, 65), width=4)
    badge = radial_gradient((900, 900), (23, 60, 73), (8, 13, 16)).convert("RGBA")
    mask = rounded_rectangle_mask((900, 900), 190)
    img.paste(badge, (62, 62), mask)
    # Prefer the realistic generated sprite when it exists; fall back to the
    # deterministic procedural car when regenerating from a clean checkout.
    car_path = ASSETS / "cars" / "realistic_muscle_orange.png"
    if not car_path.exists():
        car_path = ASSETS / "cars" / "muscle_orange.png"
    car = Image.open(car_path).convert("RGBA").resize((365, 820), Image.Resampling.LANCZOS).rotate(-17, expand=True)
    img.alpha_composite(car, (335, 80))
    draw = ImageDraw.Draw(img)
    font_path = Path(r"C:\Windows\Fonts\impact.ttf")
    headline = ImageFont.truetype(str(font_path), 132) if font_path.exists() else ImageFont.load_default()
    title = ImageFont.truetype(str(font_path), 122) if font_path.exists() else ImageFont.load_default()
    draw.text((512, 838), "CRASH", fill=(242, 245, 238, 255), anchor="mm", font=headline)
    draw.text((512, 936), "CAR", fill=(250, 191, 44, 255), anchor="mm", font=title)
    return img


def write_icon_targets(icon: Image.Image) -> None:
    android_sizes = {
        "mipmap-mdpi": 48,
        "mipmap-hdpi": 72,
        "mipmap-xhdpi": 96,
        "mipmap-xxhdpi": 144,
        "mipmap-xxxhdpi": 192,
    }
    for folder, size in android_sizes.items():
        save(
            icon.resize((size, size), Image.Resampling.LANCZOS),
            ROOT / "android" / "app" / "src" / "main" / "res" / folder / "ic_launcher.png",
        )

    web_icons = {
        "favicon.png": 32,
        "icons/Icon-192.png": 192,
        "icons/Icon-maskable-192.png": 192,
        "icons/Icon-512.png": 512,
        "icons/Icon-maskable-512.png": 512,
    }
    for name, size in web_icons.items():
        save(icon.resize((size, size), Image.Resampling.LANCZOS), ROOT / "web" / name)


def copy_key_art() -> None:
    if not KEY_ART_SOURCE.exists():
        return
    dest = ASSETS / "key_art.png"
    shutil.copy2(KEY_ART_SOURCE, dest)
    art = Image.open(dest).convert("RGB")
    feature = art.resize((1024, 500), Image.Resampling.LANCZOS)
    save(feature, ASSETS / "store" / "feature_graphic.png")
    save(feature, ROOT / "android" / "fastlane" / "metadata" / "android" / "en-US" / "images" / "featureGraphic.png")


def main() -> None:
    ensure_dirs()
    make_car((224, 82, 24), (21, 23, 26), (245, 187, 39), "muscle_orange")
    make_car((38, 142, 206), (236, 241, 244), (37, 221, 255), "interceptor_blue")
    make_car((111, 206, 63), (19, 44, 28), (223, 255, 108), "rally_green")
    make_car((189, 57, 74), (246, 212, 67), (255, 255, 255), "stunt_red")
    make_crate()
    make_barrel((199, 55, 39), "red_barrel")
    make_barrel((54, 87, 99), "steel_barrel")
    make_cone()
    make_barricade()
    make_truck((219, 76, 46), "box_truck_red", (82, 92, 98))
    make_truck((49, 132, 193), "delivery_truck_blue", (214, 219, 210))
    make_bus()
    make_shop("corner_shop", (109, 82, 65), (227, 75, 43), (245, 188, 42))
    make_shop("repair_shop", (63, 77, 82), (49, 147, 194), (238, 234, 214))
    make_shop("market_stall", (84, 91, 63), (110, 190, 80), (250, 198, 51))
    make_debris()
    make_crash_fragments()
    make_icons()
    make_road()
    make_garage_floor()
    copy_key_art()
    icon = make_app_icon()
    save(icon, ASSETS / "store" / "app_icon_1024.png")
    write_icon_targets(icon)


if __name__ == "__main__":
    main()

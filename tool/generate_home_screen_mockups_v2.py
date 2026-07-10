from __future__ import annotations

import math
from pathlib import Path

from PIL import Image, ImageDraw, ImageEnhance, ImageFilter, ImageFont


ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "mockups" / "home_screen_options_v2"
OUT.mkdir(parents=True, exist_ok=True)


W, H = 1080, 1920


def font(name: str, size: int) -> ImageFont.FreeTypeFont:
    for candidate in [Path("C:/Windows/Fonts") / name, Path("C:/Windows/Fonts/arial.ttf")]:
        if candidate.exists():
            return ImageFont.truetype(str(candidate), size)
    return ImageFont.load_default()


F_TITLE = font("arialbd.ttf", 74)
F_H1 = font("arialbd.ttf", 52)
F_H2 = font("arialbd.ttf", 34)
F_BODY = font("arial.ttf", 25)
F_SMALL = font("arial.ttf", 20)
F_TINY = font("arialbd.ttf", 16)


def load(path: str) -> Image.Image:
    return Image.open(ROOT / path).convert("RGBA")


def cover(img: Image.Image, size=(W, H), focus_y=0.5) -> Image.Image:
    w, h = size
    scale = max(w / img.width, h / img.height)
    resized = img.resize((round(img.width * scale), round(img.height * scale)), Image.Resampling.LANCZOS)
    left = (resized.width - w) // 2
    top = round((resized.height - h) * focus_y)
    return resized.crop((left, top, left + w, top + h))


def contain(img: Image.Image, box: tuple[int, int]) -> Image.Image:
    scale = min(box[0] / img.width, box[1] / img.height)
    return img.resize((round(img.width * scale), round(img.height * scale)), Image.Resampling.LANCZOS)


def grad(size, top, bottom) -> Image.Image:
    g = Image.new("RGBA", size, (0, 0, 0, 0))
    px = g.load()
    for y in range(size[1]):
        t = y / max(1, size[1] - 1)
        c = tuple(round(top[i] * (1 - t) + bottom[i] * t) for i in range(4))
        for x in range(size[0]):
            px[x, y] = c
    return g


def panel(img: Image.Image, rect, fill=(2, 8, 12, 185), outline=(255, 255, 255, 36), radius=24, width=2) -> None:
    layer = Image.new("RGBA", img.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    d.rounded_rectangle(rect, radius=radius, fill=fill, outline=outline, width=width)
    img.alpha_composite(layer)


def write(d: ImageDraw.ImageDraw, xy, s, f, fill=(255, 255, 255, 255), anchor=None) -> None:
    d.text(xy, s, font=f, fill=fill, anchor=anchor)


def soft_shadow(img: Image.Image, rect, alpha=160, blur=26) -> None:
    sh = Image.new("RGBA", img.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(sh)
    d.ellipse(rect, fill=(0, 0, 0, alpha))
    img.alpha_composite(sh.filter(ImageFilter.GaussianBlur(blur)))


def top_bar(img: Image.Image, option: str) -> None:
    d = ImageDraw.Draw(img)
    panel(img, (36, 32, 1044, 112), (0, 0, 0, 115), (255, 255, 255, 26), 22, 1)
    write(d, (62, 58), option, F_TINY, (255, 207, 55, 255))
    write(d, (838, 58), "36648", F_BODY, (255, 207, 55, 255))
    d.ellipse((792, 54, 820, 82), fill=(255, 207, 55, 255))
    d.ellipse((800, 62, 812, 74), fill=(130, 85, 12, 255))


def logo_block(d: ImageDraw.ImageDraw, x, y, compact=False) -> None:
    scale = 0.72 if compact else 1.0
    f = font("arialbd.ttf", round(76 * scale))
    write(d, (x + 3, y + 3), "CRASH", f, (0, 0, 0, 180))
    write(d, (x, y), "CRASH", f, (248, 249, 246, 255))
    write(d, (x + 3, y + round(64 * scale) + 3), "CAR", f, (0, 0, 0, 180))
    write(d, (x, y + round(64 * scale)), "CAR", f, (255, 205, 43, 255))


def action_button(img, rect, label, color):
    panel(img, rect, color, None, 22, 0)
    d = ImageDraw.Draw(img)
    write(d, ((rect[0] + rect[2]) // 2, (rect[1] + rect[3]) // 2), label, F_H2, (3, 7, 8, 255), "mm")


def car_overlay(img, asset, box, shadow=True):
    car = contain(load(asset), (box[2] - box[0], box[3] - box[1]))
    x = box[0] + ((box[2] - box[0]) - car.width) // 2
    y = box[1] + ((box[3] - box[1]) - car.height) // 2
    if shadow:
        soft_shadow(img, (x + 30, y + car.height - 80, x + car.width - 30, y + car.height + 30), 165, 24)
    img.alpha_composite(car, (x, y))


def option_d_console() -> Image.Image:
    img = cover(load("assets/images/chase/impact_city_backdrop.png"), focus_y=0.5)
    img = ImageEnhance.Contrast(img).enhance(1.15)
    img.alpha_composite(grad((W, H), (3, 8, 12, 230), (3, 8, 12, 90)))
    d = ImageDraw.Draw(img)
    top_bar(img, "OPTION D")
    logo_block(d, 58, 156)
    write(d, (60, 332), "Main menu over live road", F_H2, (255, 255, 255, 245))
    write(d, (60, 374), "Less UI, bigger world, immediate Play.", F_BODY, (218, 229, 232, 230))

    car_overlay(img, "assets/images/chase/stunt_red_rear_game.png", (120, 760, 970, 1350))

    panel(img, (48, 1420, 1032, 1692), (2, 9, 13, 210), (124, 217, 87, 145), 28, 2)
    write(d, (86, 1460), "IMPACT RUN", F_H2, (124, 217, 87, 255))
    write(d, (86, 1502), "Rear chase collisions, slow motion, damage score", F_BODY, (230, 238, 238, 230))
    for i, label in enumerate(["GARAGE", "ARENAS", "SHOP"]):
        x = 84 + i * 296
        panel(img, (x, 1584, x + 250, 1648), (255, 255, 255, 24), (255, 255, 255, 48), 16, 1)
        write(d, (x + 125, 1616), label, F_SMALL, (255, 255, 255, 235), "mm")
    action_button(img, (62, 1760, 1018, 1852), "PLAY", (124, 217, 87, 255))
    return img


def option_e_garage() -> Image.Image:
    img = Image.new("RGBA", (W, H), (7, 10, 12, 255))
    d = ImageDraw.Draw(img)
    # Procedural garage bay
    for i in range(0, H, 18):
        a = 18 + int(30 * i / H)
        d.line((0, i, W, i), fill=(255, 255, 255, a // 8), width=1)
    for x in [135, 350, 730, 945]:
        d.rectangle((x, 0, x + 4, H), fill=(255, 255, 255, 18))
    d.rectangle((0, 1120, W, H), fill=(12, 15, 16, 255))
    for i in range(12):
        y = 1140 + i * 64
        d.line((120 - i * 35, y, 960 + i * 35, y), fill=(255, 255, 255, 20), width=2)
    for x in [160, 390, 690, 920]:
        d.rounded_rectangle((x, 124, x + 120, 146), radius=11, fill=(255, 235, 170, 170))
        d.rounded_rectangle((x - 40, 146, x + 160, 260), radius=40, fill=(255, 225, 120, 24))

    img.alpha_composite(grad((W, H), (3, 8, 12, 115), (3, 8, 12, 195)))
    top_bar(img, "OPTION E")
    write(d, (60, 170), "GARAGE", F_TITLE, (255, 255, 255, 255))
    write(d, (60, 244), "Choose your crash machine.", F_BODY, (220, 230, 230, 230))

    car_overlay(img, "assets/images/cars/realistic_stunt_red.png", (70, 425, 1010, 1005))
    panel(img, (64, 1048, 1016, 1218), (2, 8, 12, 190), (255, 201, 42, 130), 26, 2)
    write(d, (100, 1084), "STUNTLINE", F_H1, (255, 255, 255, 255))
    write(d, (100, 1146), "Speed 96    Handling 68    Impact 88", F_BODY, (230, 238, 238, 230))

    y = 1270
    for label, color, val in [("SPEED", (255, 201, 42, 255), 0.96), ("HANDLING", (66, 199, 255, 255), 0.68), ("IMPACT", (124, 217, 87, 255), 0.88)]:
        write(d, (86, y), label, F_SMALL, (220, 230, 230, 235))
        d.rounded_rectangle((86, y + 34, 994, y + 56), radius=11, fill=(255, 255, 255, 35))
        d.rounded_rectangle((86, y + 34, 86 + round(908 * val), y + 56), radius=11, fill=color)
        y += 98

    panel(img, (64, 1592, 1016, 1695), (2, 8, 12, 205), (66, 199, 255, 120), 24, 2)
    write(d, (98, 1628), "Downtown Strip selected", F_H2, (66, 199, 255, 255))
    write(d, (98, 1670), "Traffic-heavy city chase arena", F_SMALL, (225, 235, 237, 225))
    action_button(img, (62, 1760, 1018, 1852), "START IMPACT RUN", (255, 201, 42, 255))
    return img


def option_f_live_modes() -> Image.Image:
    img = cover(load("assets/images/key_art.png"), focus_y=0.58)
    img = ImageEnhance.Brightness(img).enhance(0.65)
    img.alpha_composite(grad((W, H), (3, 8, 12, 205), (3, 8, 12, 235)))
    d = ImageDraw.Draw(img)
    top_bar(img, "OPTION F")
    write(d, (60, 162), "SELECT MODE", F_TITLE, (255, 255, 255, 255))
    write(d, (62, 238), "Three ways to crash. One clear home.", F_BODY, (220, 230, 230, 230))

    rows = [
        ("IMPACT RUN", "Rear chase camera, traffic hits, slow motion", "assets/images/chase/stunt_red_rear_game.png", (124, 217, 87, 255)),
        ("RAMP LAUNCH", "Build speed, launch into intersections", "assets/images/traffic/delivery_truck_blue.png", (66, 199, 255, 255)),
        ("CLASSIC ARENA", "Top-down smash score mode", "assets/images/cars/realistic_muscle_orange.png", (255, 201, 42, 255)),
    ]
    y = 360
    for title, sub, asset, accent in rows:
        panel(img, (48, y, 1032, y + 386), (2, 8, 12, 218), accent[:3] + (150,), 28, 3)
        write(d, (92, y + 46), title, F_H2, accent)
        write(d, (92, y + 92), sub, F_SMALL, (228, 236, 237, 230))
        car_overlay(img, asset, (520, y + 80, 980, y + 292), shadow=False)
        action_button(img, (92, y + 274, 378, y + 336), "PLAY", accent)
        y += 432
    panel(img, (48, 1688, 1032, 1818), (0, 0, 0, 150), (255, 255, 255, 36), 24, 1)
    write(d, (92, 1730), "Garage", F_H2, (255, 255, 255, 245))
    write(d, (376, 1730), "Shop", F_H2, (255, 255, 255, 245))
    write(d, (640, 1730), "Settings", F_H2, (255, 255, 255, 245))
    return img


def option_g_crash_feed() -> Image.Image:
    img = cover(load("assets/images/key_art.png"), focus_y=0.52)
    img = ImageEnhance.Contrast(img).enhance(1.15)
    img.alpha_composite(grad((W, H), (1, 6, 9, 230), (1, 6, 9, 160)))
    d = ImageDraw.Draw(img)
    top_bar(img, "OPTION G")
    logo_block(d, 58, 148, compact=True)
    write(d, (60, 296), "Crash Feed", F_H2, (255, 255, 255, 245))
    write(d, (60, 338), "Recent wrecks, rewards, then play.", F_BODY, (220, 230, 230, 230))

    car_overlay(img, "assets/images/cars/wrecked_realistic_stunt_red.png", (80, 520, 1000, 1000))
    for i, (label, val, accent) in enumerate([
        ("TODAY'S DAMAGE", "84%", (255, 122, 48, 255)),
        ("BEST COMBO", "x12", (255, 201, 42, 255)),
        ("COINS WON", "875", (124, 217, 87, 255)),
    ]):
        x = 58 + i * 330
        panel(img, (x, 1040, x + 300, 1160), (2, 8, 12, 202), accent[:3] + (120,), 20, 2)
        write(d, (x + 24, 1072), label, F_TINY, (220, 230, 230, 235))
        write(d, (x + 24, 1104), val, F_H2, accent)

    panel(img, (58, 1235, 1022, 1588), (2, 8, 12, 210), (255, 255, 255, 38), 28, 2)
    write(d, (96, 1280), "NEXT RUN", F_TINY, (255, 201, 42, 255))
    write(d, (96, 1320), "Downtown Strip Impact", F_H1, (255, 255, 255, 255))
    write(d, (96, 1384), "Heavy traffic, shops, buses, slow-motion chain scoring.", F_BODY, (220, 230, 230, 230))
    action_button(img, (96, 1472, 984, 1550), "PLAY NOW", (124, 217, 87, 255))

    panel(img, (58, 1640, 1022, 1814), (2, 8, 12, 185), (66, 199, 255, 100), 26, 2)
    write(d, (96, 1682), "Garage upgrades available", F_H2, (66, 199, 255, 255))
    write(d, (96, 1726), "Improve speed, handling, and crash durability", F_SMALL, (226, 236, 238, 230))
    return img


def sheet(options: list[tuple[str, Image.Image, str]]) -> Image.Image:
    out = Image.new("RGBA", (2160, 1320), (5, 9, 13, 255))
    d = ImageDraw.Draw(out)
    write(d, (64, 54), "Crash Car Home Screen Mockups - Round 2", font("arialbd.ttf", 50), (255, 255, 255, 255))
    write(d, (64, 112), "New directions: fewer panels, more premium game-screen feel. Choose D, E, F, or G.", F_BODY, (220, 230, 230, 235))
    thumb_w, thumb_h = 420, 746
    x = 64
    for label, img, _ in options:
        thumb = img.resize((thumb_w, thumb_h), Image.Resampling.LANCZOS)
        d.rounded_rectangle((x - 18, 176, x + thumb_w + 18, 1000), radius=44, fill=(0, 0, 0, 255), outline=(255, 255, 255, 65), width=3)
        out.alpha_composite(thumb, (x, 216))
        write(d, (x + thumb_w // 2, 1060), label, F_H2, (255, 255, 255, 255), "mm")
        x += 520
    return out


def main() -> None:
    options = [
        ("D - Console Hero", option_d_console(), "home_option_d_console_hero.png"),
        ("E - Garage Bay", option_e_garage(), "home_option_e_garage_bay.png"),
        ("F - Live Mode Select", option_f_live_modes(), "home_option_f_live_modes.png"),
        ("G - Crash Feed", option_g_crash_feed(), "home_option_g_crash_feed.png"),
    ]
    for _, img, name in options:
        img.convert("RGB").save(OUT / name, quality=96)
    sheet(options).convert("RGB").save(OUT / "home_options_round2_contact_sheet.png", quality=96)


if __name__ == "__main__":
    main()

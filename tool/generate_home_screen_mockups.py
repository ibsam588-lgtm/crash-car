from __future__ import annotations

import math
from pathlib import Path

from PIL import Image, ImageDraw, ImageEnhance, ImageFilter, ImageFont


ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "mockups" / "home_screen_options"
OUT.mkdir(parents=True, exist_ok=True)


def font(name: str, size: int) -> ImageFont.FreeTypeFont:
    candidates = [
        Path("C:/Windows/Fonts") / name,
        Path("C:/Windows/Fonts/arial.ttf"),
    ]
    for path in candidates:
        if path.exists():
            return ImageFont.truetype(str(path), size)
    return ImageFont.load_default()


FONT_BLACK = font("arialbd.ttf", 34)
FONT_BOLD = font("arialbd.ttf", 28)
FONT_MED = font("arial.ttf", 24)
FONT_SMALL = font("arial.ttf", 18)
FONT_TINY = font("arialbd.ttf", 15)


def load(path: str) -> Image.Image:
    return Image.open(ROOT / path).convert("RGBA")


def cover(img: Image.Image, size: tuple[int, int], focus_y: float = 0.5) -> Image.Image:
    w, h = size
    scale = max(w / img.width, h / img.height)
    resized = img.resize((round(img.width * scale), round(img.height * scale)), Image.Resampling.LANCZOS)
    left = (resized.width - w) // 2
    top = round((resized.height - h) * focus_y)
    return resized.crop((left, top, left + w, top + h))


def contain(img: Image.Image, size: tuple[int, int]) -> Image.Image:
    w, h = size
    scale = min(w / img.width, h / img.height)
    return img.resize((round(img.width * scale), round(img.height * scale)), Image.Resampling.LANCZOS)


def overlay_gradient(base: Image.Image, top_alpha: int = 210, bottom_alpha: int = 120) -> None:
    grad = Image.new("RGBA", base.size, (0, 0, 0, 0))
    px = grad.load()
    for y in range(base.height):
        t = y / max(1, base.height - 1)
        alpha = round(top_alpha * (1 - t) + bottom_alpha * t)
        for x in range(base.width):
            px[x, y] = (3, 8, 12, alpha)
    base.alpha_composite(grad)


def rounded_panel(img: Image.Image, rect: tuple[int, int, int, int], fill, outline=None, width: int = 2, radius: int = 24) -> None:
    layer = Image.new("RGBA", img.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    d.rounded_rectangle(rect, radius=radius, fill=fill, outline=outline, width=width)
    img.alpha_composite(layer)


def text(draw: ImageDraw.ImageDraw, xy, value: str, fnt, fill=(255, 255, 255, 255), anchor=None) -> None:
    draw.text(xy, value, font=fnt, fill=fill, anchor=anchor)


def draw_logo(draw: ImageDraw.ImageDraw, x: int, y: int, scale: float = 1.0) -> None:
    big = font("arialbd.ttf", round(82 * scale))
    yellow = (255, 201, 42, 255)
    shadow = (0, 0, 0, 190)
    draw.text((x + 4, y + 4), "CRASH", font=big, fill=shadow)
    draw.text((x, y), "CRASH", font=big, fill=(245, 247, 247, 255))
    draw.text((x + 4, y + round(70 * scale) + 4), "CAR", font=big, fill=shadow)
    draw.text((x, y + round(70 * scale)), "CAR", font=big, fill=yellow)


def draw_pill(img: Image.Image, xy: tuple[int, int], label: str, value: str, accent=(255, 201, 42, 255)) -> None:
    x, y = xy
    rounded_panel(img, (x, y, x + 170, y + 82), (0, 0, 0, 145), (255, 255, 255, 38), 2, 16)
    d = ImageDraw.Draw(img)
    text(d, (x + 16, y + 12), label.upper(), FONT_TINY, (205, 215, 220, 240))
    text(d, (x + 16, y + 36), value, FONT_BOLD, accent)


def draw_cta(img: Image.Image, y: int, label: str, color=(255, 201, 42, 255)) -> None:
    rounded_panel(img, (78, y, 1002, y + 88), color, None, 0, 22)
    d = ImageDraw.Draw(img)
    text(d, (540, y + 44), label, FONT_BOLD, (5, 8, 8, 255), "mm")


def draw_mode_chip(img: Image.Image, rect, label: str, subtitle: str, accent) -> None:
    rounded_panel(img, rect, (5, 12, 17, 204), accent[:3] + (125,), 2, 22)
    d = ImageDraw.Draw(img)
    x1, y1, _, _ = rect
    text(d, (x1 + 26, y1 + 22), label, FONT_BOLD, accent)
    text(d, (x1 + 26, y1 + 58), subtitle, FONT_SMALL, (220, 230, 230, 230))


def option_a() -> Image.Image:
    img = cover(load("assets/images/chase/impact_city_backdrop.png"), (1080, 1920), 0.48)
    img = ImageEnhance.Contrast(img).enhance(1.08)
    overlay_gradient(img, 230, 150)
    d = ImageDraw.Draw(img)
    draw_logo(d, 58, 70, 0.86)
    text(d, (62, 258), "Cinematic chase home", FONT_BOLD, (255, 255, 255, 245))
    text(d, (62, 294), "Pick a car, build speed, smash the city.", FONT_MED, (218, 230, 232, 230))

    car = contain(load("assets/images/chase/stunt_red_rear_game.png"), (720, 470))
    shadow = Image.new("RGBA", img.size, (0, 0, 0, 0))
    sd = ImageDraw.Draw(shadow)
    sd.ellipse((210, 1260, 890, 1395), fill=(0, 0, 0, 160))
    shadow = shadow.filter(ImageFilter.GaussianBlur(22))
    img.alpha_composite(shadow)
    img.alpha_composite(car, (180, 1020))

    draw_pill(img, (62, 390), "Best", "451355")
    draw_pill(img, (252, 390), "Coins", "36648", (124, 217, 87, 255))
    draw_pill(img, (442, 390), "Modes", "3", (66, 199, 255, 255))

    draw_mode_chip(img, (62, 1460, 1018, 1560), "Impact Run", "Rear chase camera, moving traffic, slow-motion crashes", (124, 217, 87, 255))
    draw_mode_chip(img, (62, 1586, 530, 1688), "Classic Arena", "Top-down crash score mode", (255, 201, 42, 255))
    draw_mode_chip(img, (550, 1586, 1018, 1688), "Ramp Launch", "Speed ramp into intersections", (66, 199, 255, 255))
    draw_cta(img, 1760, "PLAY IMPACT RUN", (124, 217, 87, 255))
    text(d, (62, 36), "OPTION A", FONT_TINY, (255, 201, 42, 255))
    return img


def option_b() -> Image.Image:
    img = cover(load("assets/images/key_art.png"), (1080, 1920), 0.52)
    img = ImageEnhance.Brightness(img).enhance(0.72)
    img = ImageEnhance.Contrast(img).enhance(1.12)
    overlay_gradient(img, 235, 190)
    d = ImageDraw.Draw(img)
    text(d, (62, 58), "OPTION B", FONT_TINY, (255, 201, 42, 255))
    draw_logo(d, 62, 98, 0.72)
    text(d, (62, 262), "Garage first", FONT_BOLD, (255, 255, 255, 245))
    text(d, (62, 298), "Choose the machine before the mayhem.", FONT_MED, (218, 230, 232, 230))

    rounded_panel(img, (56, 390, 1024, 1088), (3, 8, 12, 190), (255, 255, 255, 50), 2, 30)
    car = contain(load("assets/images/cars/realistic_stunt_red.png"), (760, 430))
    img.alpha_composite(car, (170, 530))
    sd = ImageDraw.Draw(img)
    sd.line((170, 980, 910, 980), fill=(255, 201, 42, 165), width=5)
    sd.line((260, 1015, 820, 1015), fill=(66, 199, 255, 90), width=3)
    text(d, (92, 420), "STUNTLINE", font("arialbd.ttf", 40), (255, 255, 255, 255))
    text(d, (92, 468), "Speed 96%   Handling 68%   Impact 88%", FONT_SMALL, (220, 230, 230, 225))

    cards_y = 1140
    for i, (name, path, accent) in enumerate([
        ("Roadbreaker", "assets/images/cars/realistic_muscle_orange.png", (255, 201, 42, 255)),
        ("Interceptor", "assets/images/cars/realistic_interceptor_blue.png", (66, 199, 255, 255)),
        ("Rally Fang", "assets/images/cars/realistic_rally_green.png", (124, 217, 87, 255)),
    ]):
        x = 58 + i * 330
        rounded_panel(img, (x, cards_y, x + 304, cards_y + 330), (6, 13, 18, 218), accent[:3] + (150,), 2, 24)
        c = contain(load(path), (250, 150))
        img.alpha_composite(c, (x + 26, cards_y + 70))
        text(d, (x + 152, cards_y + 250), name, FONT_SMALL, (255, 255, 255, 245), "mm")

    draw_mode_chip(img, (62, 1515, 1018, 1618), "Select Arena", "Downtown Strip, Docks, Night Market, Construction Yard", (66, 199, 255, 255))
    draw_cta(img, 1760, "START RUN", (255, 201, 42, 255))
    return img


def option_c() -> Image.Image:
    img = Image.new("RGBA", (1080, 1920), (5, 10, 14, 255))
    bg = cover(load("assets/images/chase/impact_city_backdrop.png"), (1080, 1180), 0.5)
    bg = ImageEnhance.Brightness(bg).enhance(0.82)
    img.alpha_composite(bg, (0, 0))
    overlay_gradient(img, 190, 230)
    d = ImageDraw.Draw(img)
    text(d, (62, 58), "OPTION C", FONT_TINY, (255, 201, 42, 255))
    draw_logo(d, 62, 92, 0.66)
    text(d, (62, 246), "Choose a mode", FONT_BOLD, (255, 255, 255, 245))
    text(d, (62, 282), "Big cards, fast entry, action-first home.", FONT_MED, (218, 230, 232, 230))

    mode_data = [
        ("CLASSIC ARENA", "Smash props and traffic", "assets/images/cars/realistic_muscle_orange.png", (255, 201, 42, 255)),
        ("RAMP LAUNCH", "Charge speed, launch into the city", "assets/images/traffic/delivery_truck_blue.png", (66, 199, 255, 255)),
        ("IMPACT RUN", "Rear chase crashes and slow motion", "assets/images/chase/stunt_red_rear_game.png", (124, 217, 87, 255)),
    ]
    y = 390
    for title, sub, asset, accent in mode_data:
        rounded_panel(img, (56, y, 1024, y + 360), (5, 11, 16, 218), accent[:3] + (135,), 3, 32)
        text(d, (92, y + 38), title, font("arialbd.ttf", 34), accent)
        text(d, (92, y + 84), sub, FONT_MED, (225, 235, 237, 230))
        car = contain(load(asset), (440, 220))
        img.alpha_composite(car, (560, y + 88))
        rounded_panel(img, (92, y + 250, 378, y + 316), accent, None, 0, 18)
        text(d, (235, y + 284), "PLAY", FONT_BOLD, (5, 8, 8, 255), "mm")
        y += 408

    rounded_panel(img, (56, 1640, 1024, 1818), (0, 0, 0, 165), (255, 255, 255, 42), 2, 26)
    text(d, (92, 1672), "Daily Rewards", FONT_BOLD, (255, 255, 255, 245))
    text(d, (92, 1714), "Coins, premium cars, upgrades", FONT_MED, (220, 230, 230, 225))
    text(d, (940, 1730), "875", font("arialbd.ttf", 42), (255, 201, 42, 255), "rm")
    return img


def phone_frame(src: Image.Image, label: str) -> Image.Image:
    frame = Image.new("RGBA", (500, 940), (0, 0, 0, 0))
    d = ImageDraw.Draw(frame)
    d.rounded_rectangle((20, 20, 480, 880), radius=46, fill=(0, 0, 0, 255), outline=(255, 255, 255, 70), width=3)
    inner = src.resize((420, 746), Image.Resampling.LANCZOS)
    frame.alpha_composite(inner, (40, 70))
    text(d, (250, 912), label, FONT_BOLD, (255, 255, 255, 255), "mm")
    return frame


def main() -> None:
    options = [
        ("A - Cinematic Chase", option_a(), "home_option_a_cinematic.png"),
        ("B - Garage First", option_b(), "home_option_b_garage.png"),
        ("C - Mode Cards", option_c(), "home_option_c_modes.png"),
    ]
    for _, image, filename in options:
        image.convert("RGB").save(OUT / filename, quality=96)

    sheet = Image.new("RGBA", (1680, 1080), (5, 9, 13, 255))
    d = ImageDraw.Draw(sheet)
    text(d, (60, 48), "Crash Car Home Screen Mockups", font("arialbd.ttf", 44), (255, 255, 255, 255))
    text(d, (60, 100), "Pick A, B, or C and I will wire that direction into Flutter.", FONT_MED, (220, 230, 230, 230))
    x = 60
    for label, image, _ in options:
        frame = phone_frame(image, label)
        sheet.alpha_composite(frame, (x, 130))
        x += 540
    sheet.convert("RGB").save(OUT / "home_options_contact_sheet.png", quality=96)


if __name__ == "__main__":
    main()

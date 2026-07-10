from __future__ import annotations

import math
import random
import shutil
import subprocess
from pathlib import Path

from PIL import Image, ImageDraw, ImageEnhance, ImageFilter, ImageFont


ROOT = Path(__file__).resolve().parents[1]
OUT_DIR = ROOT / "mockups"
OUT_MP4 = OUT_DIR / "crash_car_gameplay_mockup.mp4"
OUT_POSTER = OUT_DIR / "crash_car_gameplay_mockup_poster.png"

W, H = 720, 1280
FPS = 30
DURATION = 11.5
FRAME_COUNT = int(FPS * DURATION)


def asset(path: str) -> Path:
    return ROOT / path


def load_rgba(path: str) -> Image.Image:
    return Image.open(asset(path)).convert("RGBA")


def font(name: str, size: int) -> ImageFont.FreeTypeFont | ImageFont.ImageFont:
    candidates = [
        Path("C:/Windows/Fonts") / name,
        Path("C:/Windows/Fonts") / "arialbd.ttf",
        Path("C:/Windows/Fonts") / "arial.ttf",
    ]
    for candidate in candidates:
        try:
            return ImageFont.truetype(str(candidate), size)
        except OSError:
            continue
    return ImageFont.load_default()


FONT_TITLE = font("impact.ttf", 76)
FONT_BIG = font("arialbd.ttf", 50)
FONT_MED = font("arialbd.ttf", 30)
FONT_SMALL = font("arialbd.ttf", 22)
FONT_TINY = font("arialbd.ttf", 16)

BACKDROP = load_rgba("assets/images/chase/impact_city_backdrop.png")
PLAYER = load_rgba("assets/images/chase/stunt_red_rear_game.png")
ROADBREAKER = load_rgba("assets/images/chase/player_muscle_rear.png")
BLUE_CAR = load_rgba("assets/images/chase/interceptor_rear.png")
RALLY = load_rgba("assets/images/chase/rally_rear.png")
TRUCK = load_rgba("assets/images/chase/delivery_truck_rear.png")
BUS = load_rgba("assets/images/chase/city_bus_rear.png")
COIN = load_rgba("assets/images/icons/coin.png")

DEBRIS = [
    load_rgba(f"assets/images/debris/car_fragment_{i:02d}.png")
    for i in range(1, 15)
]
DEBRIS += [
    load_rgba(f"assets/images/debris/glass_shard_{i:02d}.png")
    for i in range(1, 10)
]


def clamp(value: float, low: float = 0.0, high: float = 1.0) -> float:
    return max(low, min(high, value))


def lerp(a: float, b: float, t: float) -> float:
    return a + (b - a) * t


def smoothstep(edge0: float, edge1: float, x: float) -> float:
    t = clamp((x - edge0) / (edge1 - edge0))
    return t * t * (3 - 2 * t)


def cover(img: Image.Image, zoom: float, x_shift: float = 0) -> Image.Image:
    scale = max(W / img.width, H / img.height) * zoom
    size = (math.ceil(img.width * scale), math.ceil(img.height * scale))
    resized = img.resize(size, Image.Resampling.LANCZOS)
    max_x = max(0, resized.width - W)
    max_y = max(0, resized.height - H)
    left = int(max_x * (0.5 + x_shift * 0.5))
    top = int(max_y * 0.5)
    return resized.crop((left, top, left + W, top + H))


def composite_sprite(
    frame: Image.Image,
    sprite: Image.Image,
    center_x: float,
    bottom_y: float,
    width: float,
    angle: float = 0,
    opacity: float = 1,
) -> tuple[float, float]:
    height = width * sprite.height / sprite.width
    scaled = sprite.resize((max(1, int(width)), max(1, int(height))), Image.Resampling.LANCZOS)
    if opacity < 1:
        alpha = scaled.getchannel("A").point(lambda p: int(p * opacity))
        scaled.putalpha(alpha)
    if angle:
        scaled = scaled.rotate(angle, resample=Image.Resampling.BICUBIC, expand=True)
    x = int(center_x - scaled.width / 2)
    y = int(bottom_y - scaled.height)
    frame.alpha_composite(scaled, (x, y))
    return x + scaled.width / 2, y + scaled.height / 2


def shadow(frame: Image.Image, cx: float, cy: float, width: float, alpha: int = 95) -> None:
    layer = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    d.ellipse(
        (cx - width * 0.33, cy - width * 0.055, cx + width * 0.33, cy + width * 0.055),
        fill=(0, 0, 0, alpha),
    )
    frame.alpha_composite(layer.filter(ImageFilter.GaussianBlur(10)))


def rounded_box(
    draw: ImageDraw.ImageDraw,
    xy: tuple[float, float, float, float],
    fill: tuple[int, int, int, int],
    outline: tuple[int, int, int, int] | None = None,
    radius: int = 14,
    width: int = 1,
) -> None:
    draw.rounded_rectangle(xy, radius=radius, fill=fill, outline=outline, width=width)


def draw_text_center(
    draw: ImageDraw.ImageDraw,
    text: str,
    y: float,
    font_obj: ImageFont.ImageFont,
    fill: tuple[int, int, int, int],
) -> None:
    box = draw.textbbox((0, 0), text, font=font_obj)
    draw.text(((W - (box[2] - box[0])) / 2, y), text, font=font_obj, fill=fill)


def draw_glow_text(
    frame: Image.Image,
    text: str,
    xy: tuple[float, float],
    font_obj: ImageFont.ImageFont,
    fill: tuple[int, int, int, int],
    glow: tuple[int, int, int, int],
    blur: int = 8,
) -> None:
    layer = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    d.text(xy, text, font=font_obj, fill=glow)
    frame.alpha_composite(layer.filter(ImageFilter.GaussianBlur(blur)))
    ImageDraw.Draw(frame).text(xy, text, font=font_obj, fill=fill)


def draw_lanes(frame: Image.Image, t: float, slow: float) -> None:
    draw = ImageDraw.Draw(frame)
    horizon = H * 0.46
    speed = lerp(3.2, 0.55, slow)
    phase = (t * speed) % 1
    for lane in [-0.54, -0.18, 0.18, 0.54]:
        for i in range(-2, 13):
            z0 = clamp((i / 9 + phase * 0.16) % 1)
            z1 = clamp(z0 + 0.045)
            y0 = horizon + (H - horizon) * (z0**1.85)
            y1 = horizon + (H - horizon) * (z1**1.85)
            spread0 = lerp(72, W * 0.82, z0)
            spread1 = lerp(72, W * 0.82, z1)
            x0 = W / 2 + lane * spread0
            x1 = W / 2 + lane * spread1
            alpha = int(38 + 120 * z0)
            draw.line((x0, y0, x1, y1), fill=(255, 255, 255, alpha), width=max(2, int(2 + z0 * 7)))

    for i in range(18):
        z = ((i * 0.072 + t * speed * 0.22) % 1)
        y = horizon + (H - horizon) * (z**1.75)
        side = -1 if i % 2 else 1
        x = W / 2 + side * lerp(70, W * 0.48, z)
        draw.line((x, y, x + side * 18, y + 70 * z), fill=(255, 255, 255, int(22 + z * 78)), width=2)


def draw_hud(frame: Image.Image, t: float, crash: float, score_boost: int) -> None:
    draw = ImageDraw.Draw(frame)
    rounded_box(draw, (22, 22, 150, 114), (18, 27, 35, 158), radius=14)
    draw.text((42, 38), "SCORE", font=FONT_TINY, fill=(235, 245, 255, 220))
    score = 5350 + score_boost + int(max(0, t - 5.2) * 490)
    draw.text((42, 58), f"{score:,}", font=FONT_MED, fill=(255, 255, 255, 255))

    speed = int(lerp(58, 154, smoothstep(1.2, 4.9, t)) - crash * 26)
    rounded_box(draw, (538, 1020, 690, 1170), (14, 20, 26, 128), outline=(255, 255, 255, 75), radius=76)
    draw.arc((550, 1032, 678, 1160), start=120, end=120 + speed * 1.55, fill=(246, 230, 79, 255), width=10)
    draw.text((582, 1072), str(speed), font=FONT_BIG, fill=(255, 255, 255, 255))
    draw.text((586, 1124), "KM/H", font=FONT_TINY, fill=(255, 255, 255, 220))

    rounded_box(draw, (30, 1098, 172, 1148), (15, 25, 31, 132), radius=12)
    draw.text((42, 1114), "BOOST", font=FONT_TINY, fill=(255, 255, 255, 230))
    draw.rounded_rectangle((42, 1158, 146, 1168), radius=5, fill=(255, 255, 255, 54))
    boost_w = 104 * (1 - clamp((t - 5.0) / 4.5))
    draw.rounded_rectangle((42, 1158, 42 + boost_w, 1168), radius=5, fill=(112, 239, 255, 255))

    for x in (46, 614):
        rounded_box(draw, (x - 40, 1192, x + 40, 1272), (8, 12, 15, 168), outline=(255, 255, 255, 70), radius=40)
    draw.text((33, 1206), "<", font=FONT_BIG, fill=(255, 255, 255, 240))
    draw.text((602, 1206), ">", font=FONT_BIG, fill=(255, 255, 255, 240))
    rounded_box(draw, (298, 1184, 422, 1278), (17, 72, 84, 188), outline=(112, 239, 255, 210), radius=48, width=3)
    draw.text((333, 1208), "GAS", font=FONT_SMALL, fill=(112, 239, 255, 255))


def draw_traffic(frame: Image.Image, t: float, crash: float) -> None:
    horizon = H * 0.46
    targets = [
        (BLUE_CAR, -0.42, 0.24, 96, -5),
        (RALLY, 0.36, 0.36, 104, 4),
        (TRUCK, 0.04, 0.48, 136, 1),
        (BUS, -0.12, 0.62, 176, -2),
    ]
    for sprite, lane, offset, base, angle in targets:
        z = 0.18 + ((t * 0.068 + offset) % 0.68)
        if t > 5.1 and sprite == BUS:
            z = lerp(0.78, 0.92, smoothstep(5.1, 5.9, t))
        y = horizon + (H - horizon) * (z**1.8)
        road_w = lerp(100, W * 0.86, z)
        x = W / 2 + lane * road_w + math.sin(t * 1.8 + offset * 8) * 10
        width = base * lerp(0.28, 1.34, z)
        if sprite == BUS and t > 5.65:
            angle += lerp(0, -16, smoothstep(5.65, 6.8, t))
            x += lerp(0, -34, smoothstep(5.65, 7.1, t))
        shadow(frame, x, y + 5, width, int(45 + z * 60))
        composite_sprite(frame, sprite, x, y, width, angle=angle, opacity=1 - crash * 0.03)


def draw_crash_effects(frame: Image.Image, t: float) -> int:
    if t < 5.45:
        return 0
    p = clamp((t - 5.45) / 2.4)
    random.seed(42)
    cx = W * 0.48
    cy = H * 0.76
    layer = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)

    flash = max(0.0, 1 - p * 3.0)
    if flash > 0:
        d.ellipse((cx - 250, cy - 190, cx + 250, cy + 190), fill=(255, 243, 158, int(150 * flash)))

    for i, piece in enumerate(DEBRIS):
        angle = -2.6 + i * 0.31
        speed = 80 + (i % 7) * 24
        arc = math.sin(p * math.pi) * (60 + (i % 5) * 12)
        x = cx + math.cos(angle) * speed * p * 2.6
        y = cy + math.sin(angle) * speed * p * 1.45 - arc
        w = 28 + (i % 5) * 9
        opacity = int(255 * (1 - clamp((p - 0.72) / 0.28)))
        if opacity <= 0:
            continue
        composite_sprite(layer, piece, x, y, w, angle=angle * 38 + p * 420, opacity=opacity / 255)

    for i in range(26):
        angle = random.uniform(-math.pi, math.pi)
        dist = random.uniform(40, 250) * p
        x = cx + math.cos(angle) * dist
        y = cy + math.sin(angle) * dist * 0.55
        d.line((cx, cy, x, y), fill=(255, 210, 76, int(90 * (1 - p))), width=2)

    frame.alpha_composite(layer)
    return int(3200 * smoothstep(5.45, 8.6, t))


def draw_score_popups(frame: Image.Image, t: float) -> None:
    draw = ImageDraw.Draw(frame)
    popups = [
        ("SLOW MOTION", "BUS HIT", 5.35, (246, 249, 255), (112, 239, 255)),
        ("+1,250", "IMPACT", 6.15, (255, 244, 104), (255, 255, 255)),
        ("+780", "CHAIN HIT", 6.95, (255, 244, 104), (255, 255, 255)),
        ("x3", "COMBO", 7.55, (140, 255, 119), (255, 255, 255)),
    ]
    for index, (main, sub, start, c1, c2) in enumerate(popups):
        life = clamp((t - start) / 1.2)
        if life <= 0 or life >= 1:
            continue
        y = lerp(418 + index * 22, 342 + index * 18, life)
        alpha = int(255 * math.sin(life * math.pi))
        box_w = 270 if len(main) > 4 else 170
        rounded_box(
            draw,
            (W / 2 - box_w / 2, y - 42, W / 2 + box_w / 2, y + 62),
            (22, 31, 39, int(alpha * 0.62)),
            outline=(112, 239, 255, int(alpha * 0.6)),
            radius=14,
        )
        main_font = FONT_MED if len(main) > 4 else FONT_BIG
        box = draw.textbbox((0, 0), main, font=main_font)
        draw.text((W / 2 - (box[2] - box[0]) / 2, y - 24), main, font=main_font, fill=(*c1, alpha))
        box = draw.textbbox((0, 0), sub, font=FONT_TINY)
        draw.text((W / 2 - (box[2] - box[0]) / 2, y + 22), sub, font=FONT_TINY, fill=(*c2, alpha))


def draw_summary(frame: Image.Image, t: float) -> None:
    p = smoothstep(9.0, 10.0, t)
    if p <= 0:
        return
    overlay = Image.new("RGBA", (W, H), (0, 0, 0, int(188 * p)))
    frame.alpha_composite(overlay)
    draw = ImageDraw.Draw(frame)
    y = lerp(840, 300, p)
    rounded_box(draw, (70, y, 650, y + 486), (12, 20, 26, int(230 * p)), outline=(255, 197, 51, int(190 * p)), radius=20, width=2)
    draw_text_center(draw, "LEVEL COMPLETE", y + 48, FONT_MED, (255, 197, 51, int(255 * p)))
    for i, x in enumerate([242, 360, 478]):
        draw_text_center(draw, "*" if i != 1 else "*", y + 96, FONT_BIG, (255, 197, 51, int(255 * p)))
        draw.text((x - 20, y + 94), "*", font=FONT_BIG, fill=(255, 197, 51, int(255 * p)))
    draw_text_center(draw, "TOTAL SCORE", y + 174, FONT_SMALL, (255, 255, 255, int(210 * p)))
    draw_text_center(draw, "8,750", y + 204, FONT_BIG, (255, 255, 255, int(255 * p)))
    rows = [("Crash damage", "3,400"), ("Objects hit", "2,420"), ("Max speed", "1,150"), ("Combo bonus", "1,780")]
    for i, (label, value) in enumerate(rows):
        yy = y + 284 + i * 38
        draw.text((130, yy), label, font=FONT_SMALL, fill=(255, 255, 255, int(190 * p)))
        box = draw.textbbox((0, 0), value, font=FONT_SMALL)
        draw.text((590 - (box[2] - box[0]), yy), value, font=FONT_SMALL, fill=(255, 255, 255, int(230 * p)))
    coin = COIN.resize((36, 36), Image.Resampling.LANCZOS)
    frame.alpha_composite(coin, (277, int(y + 425)))
    draw.text((322, y + 425), "875", font=FONT_MED, fill=(255, 197, 51, int(255 * p)))


def draw_intro(frame: Image.Image, t: float) -> None:
    if t > 2.0:
        return
    p = 1 - smoothstep(1.35, 2.0, t)
    overlay = Image.new("RGBA", (W, H), (0, 0, 0, int(125 * p)))
    frame.alpha_composite(overlay)
    draw = ImageDraw.Draw(frame)
    draw_glow_text(frame, "CRASH", (48, 98), FONT_TITLE, (255, 255, 255, int(255 * p)), (255, 255, 255, int(110 * p)))
    draw_glow_text(frame, "CAR", (52, 174), FONT_TITLE, (255, 197, 51, int(255 * p)), (255, 197, 51, int(120 * p)))
    draw.text((55, 278), "Impact Run gameplay mockup", font=FONT_MED, fill=(255, 255, 255, int(235 * p)))
    draw.text((55, 322), "Speed up. Pick a target. Crash in slow motion.", font=FONT_SMALL, fill=(235, 245, 255, int(210 * p)))


def draw_player(frame: Image.Image, t: float, crash: float) -> None:
    steer = math.sin(t * 1.65) * 38 * (1 - crash)
    x = W / 2 + steer + lerp(0, -58, crash)
    bottom = lerp(1125, 1028, smoothstep(4.7, 5.55, t)) + math.sin(t * 32) * 2 * crash
    width = lerp(288, 330, smoothstep(2.0, 5.0, t))
    angle = lerp(0, -15, crash) + math.sin(t * 24) * 2.8 * crash
    shadow(frame, x, bottom + 6, width, 115)
    composite_sprite(frame, PLAYER, x, bottom, width, angle=angle)


def draw_frame(frame_no: int) -> Image.Image:
    t = frame_no / FPS
    crash = smoothstep(5.45, 6.75, t)
    slow = smoothstep(5.25, 5.7, t) * (1 - smoothstep(7.45, 8.25, t))
    shake = 0 if t < 5.45 else math.sin(t * 75) * 0.01 * (1 - clamp((t - 5.45) / 2.3))
    bg = cover(BACKDROP, 1.14 + t * 0.004 + crash * 0.025, math.sin(t * 0.28) * 0.2 + shake)
    bg = ImageEnhance.Contrast(bg).enhance(1.08)
    frame = bg.convert("RGBA")

    haze = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    hd = ImageDraw.Draw(haze)
    for y in range(0, 260):
        alpha = int(80 * (1 - y / 260))
        hd.line((0, y, W, y), fill=(5, 13, 18, alpha))
    for y in range(970, H):
        alpha = int(56 * ((y - 970) / (H - 970)))
        hd.line((0, y, W, y), fill=(5, 10, 12, alpha))
    frame.alpha_composite(haze)

    draw_lanes(frame, t, slow)
    draw_traffic(frame, t, crash)
    draw_player(frame, t, crash)
    score_boost = draw_crash_effects(frame, t)
    draw_hud(frame, t, crash, score_boost)
    draw_score_popups(frame, t)
    draw_intro(frame, t)
    draw_summary(frame, t)
    return frame.convert("RGB")


def render() -> None:
    OUT_DIR.mkdir(exist_ok=True)
    ffmpeg = shutil.which("ffmpeg")
    if ffmpeg is None:
        raise RuntimeError("ffmpeg was not found on PATH")
    cmd = [
        ffmpeg,
        "-y",
        "-f",
        "rawvideo",
        "-vcodec",
        "rawvideo",
        "-pix_fmt",
        "rgb24",
        "-s",
        f"{W}x{H}",
        "-r",
        str(FPS),
        "-i",
        "-",
        "-an",
        "-c:v",
        "libx264",
        "-pix_fmt",
        "yuv420p",
        "-preset",
        "medium",
        "-crf",
        "18",
        "-movflags",
        "+faststart",
        str(OUT_MP4),
    ]
    proc = subprocess.Popen(cmd, stdin=subprocess.PIPE)
    assert proc.stdin is not None
    poster_frame = int(FPS * 6.1)
    for frame_no in range(FRAME_COUNT):
        frame = draw_frame(frame_no)
        if frame_no == poster_frame:
            frame.save(OUT_POSTER)
        proc.stdin.write(frame.tobytes())
    proc.stdin.close()
    code = proc.wait()
    if code != 0:
        raise RuntimeError(f"ffmpeg exited with code {code}")
    print(f"Wrote {OUT_MP4}")
    print(f"Wrote {OUT_POSTER}")


if __name__ == "__main__":
    render()

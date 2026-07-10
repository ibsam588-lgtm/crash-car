from __future__ import annotations

import math
import random
import shutil
import subprocess
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter, ImageFont


ROOT = Path(__file__).resolve().parents[1]
KEYFRAMES = ROOT / "mockups" / "ps2_keyframes"
OUT_DIR = ROOT / "mockups"
OUT_MP4 = OUT_DIR / "crash_car_ps2_style_mockup.mp4"
OUT_POSTER = OUT_DIR / "crash_car_ps2_style_mockup_poster.png"

W, H = 960, 720
FPS = 30
DURATION = 12.0
FRAME_COUNT = int(FPS * DURATION)


def font(name: str, size: int) -> ImageFont.FreeTypeFont | ImageFont.ImageFont:
    for candidate in (
        Path("C:/Windows/Fonts") / name,
        Path("C:/Windows/Fonts") / "arialbd.ttf",
        Path("C:/Windows/Fonts") / "arial.ttf",
    ):
        try:
            return ImageFont.truetype(str(candidate), size)
        except OSError:
            pass
    return ImageFont.load_default()


FONT_BIG = font("arialbd.ttf", 44)
FONT_MED = font("arialbd.ttf", 26)
FONT_SMALL = font("arialbd.ttf", 18)
FONT_TINY = font("arialbd.ttf", 14)

SCENES = [
    Image.open(KEYFRAMES / "01_chase.png").convert("RGB"),
    Image.open(KEYFRAMES / "02_impact.png").convert("RGB"),
    Image.open(KEYFRAMES / "03_chain.png").convert("RGB"),
    Image.open(KEYFRAMES / "04_aftermath.png").convert("RGB"),
]


def clamp(value: float, low: float = 0.0, high: float = 1.0) -> float:
    return max(low, min(high, value))


def lerp(a: float, b: float, t: float) -> float:
    return a + (b - a) * t


def smoothstep(edge0: float, edge1: float, value: float) -> float:
    x = clamp((value - edge0) / (edge1 - edge0))
    return x * x * (3 - 2 * x)


def ease_out(t: float) -> float:
    return 1 - (1 - t) ** 3


def cover_crop(img: Image.Image, zoom: float, pan_x: float, pan_y: float) -> Image.Image:
    scale = max(W / img.width, H / img.height) * zoom
    rw, rh = int(img.width * scale), int(img.height * scale)
    resized = img.resize((rw, rh), Image.Resampling.BICUBIC)
    max_x = max(0, rw - W)
    max_y = max(0, rh - H)
    left = int(max_x * clamp(0.5 + pan_x, 0, 1))
    top = int(max_y * clamp(0.5 + pan_y, 0, 1))
    return resized.crop((left, top, left + W, top + H)).convert("RGBA")


def scene_at(t: float) -> tuple[Image.Image, float]:
    if t < 3.35:
        local = t / 3.35
        return SCENES[0], local
    if t < 5.9:
        local = (t - 3.35) / 2.55
        return SCENES[1], local
    if t < 8.75:
        local = (t - 5.9) / 2.85
        return SCENES[2], local
    local = (t - 8.75) / (DURATION - 8.75)
    return SCENES[3], local


def draw_scanlines(frame: Image.Image) -> None:
    overlay = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    draw = ImageDraw.Draw(overlay)
    for y in range(0, H, 3):
        draw.line((0, y, W, y), fill=(0, 0, 0, 18))
    frame.alpha_composite(overlay)


def draw_hud(frame: Image.Image, t: float) -> None:
    draw = ImageDraw.Draw(frame)
    score = 4200 + int(t * 275)
    if t > 4.2:
        score += int(1800 * smoothstep(4.2, 5.4, t))
    if t > 6.1:
        score += int(2400 * smoothstep(6.1, 7.4, t))
    if t > 8.0:
        score += int(950 * smoothstep(8.0, 9.0, t))

    def panel(x1: int, y1: int, x2: int, y2: int, alpha: int = 162) -> None:
        draw.rectangle((x1 + 4, y1 + 4, x2 + 4, y2 + 4), fill=(0, 0, 0, 80))
        draw.rounded_rectangle((x1, y1, x2, y2), radius=7, fill=(11, 18, 20, alpha), outline=(225, 220, 180, 80))

    panel(18, 18, 172, 88)
    draw.text((32, 29), "SCORE", font=FONT_TINY, fill=(220, 224, 216, 220))
    draw.text((32, 48), f"{score:,}", font=FONT_MED, fill=(255, 255, 244, 255))

    speed = int(lerp(74, 162, smoothstep(0.4, 3.3, t)))
    if t > 4.25:
        speed = int(lerp(speed, 94, smoothstep(4.25, 6.4, t)))
    if t > 8.4:
        speed = int(lerp(speed, 0, smoothstep(8.4, 10.5, t)))
    panel(W - 174, 18, W - 18, 88)
    draw.text((W - 156, 29), "SPEED", font=FONT_TINY, fill=(220, 224, 216, 220))
    draw.text((W - 156, 48), f"{speed:03d}", font=FONT_MED, fill=(255, 226, 76, 255))
    draw.text((W - 94, 56), "MPH", font=FONT_TINY, fill=(220, 224, 216, 220))

    damage = int(lerp(0, 92, smoothstep(3.9, 8.1, t)))
    panel(316, 18, 644, 70, 146)
    draw.text((334, 32), "DAMAGE", font=FONT_SMALL, fill=(232, 232, 218, 230))
    draw.rectangle((456, 37, 620, 51), outline=(255, 255, 255, 80), fill=(35, 41, 37, 180))
    fill_color = (114, 212, 70, 255) if damage < 45 else (255, 198, 54, 255) if damage < 75 else (255, 80, 52, 255)
    draw.rectangle((458, 39, 458 + int(160 * damage / 100), 49), fill=fill_color)
    draw.text((625, 32), f"{damage}%", font=FONT_SMALL, fill=(255, 255, 245, 240))


def draw_impact_text(frame: Image.Image, t: float) -> None:
    draw = ImageDraw.Draw(frame)
    events = [
        (3.92, "SLOW MOTION", "BUS HIT", (255, 236, 90)),
        (5.2, "+1,250", "IMPACT BONUS", (255, 236, 90)),
        (6.35, "+2,420", "CHAIN COLLISION", (255, 236, 90)),
        (7.5, "x3", "WRECK COMBO", (129, 255, 104)),
    ]
    for start, title, subtitle, color in events:
        life = clamp((t - start) / 1.15)
        if life <= 0 or life >= 1:
            continue
        alpha = int(255 * math.sin(life * math.pi))
        y = int(126 - 32 * ease_out(life))
        box_w = 360 if len(title) > 4 else 210
        x1, x2 = W // 2 - box_w // 2, W // 2 + box_w // 2
        draw.rounded_rectangle((x1, y, x2, y + 88), radius=10, fill=(8, 13, 15, int(178 * alpha / 255)), outline=(255, 226, 90, int(150 * alpha / 255)))
        title_box = draw.textbbox((0, 0), title, font=FONT_MED)
        sub_box = draw.textbbox((0, 0), subtitle, font=FONT_TINY)
        draw.text((W / 2 - (title_box[2] - title_box[0]) / 2, y + 18), title, font=FONT_MED, fill=(*color, alpha))
        draw.text((W / 2 - (sub_box[2] - sub_box[0]) / 2, y + 55), subtitle, font=FONT_TINY, fill=(240, 242, 230, alpha))


def draw_summary(frame: Image.Image, t: float) -> None:
    p = smoothstep(9.45, 10.55, t)
    if p <= 0:
        return
    overlay = Image.new("RGBA", (W, H), (0, 0, 0, int(130 * p)))
    frame.alpha_composite(overlay)
    draw = ImageDraw.Draw(frame)
    y = int(lerp(760, 148, p))
    draw.rounded_rectangle((238, y, 722, y + 420), radius=10, fill=(8, 13, 15, int(222 * p)), outline=(255, 205, 62, int(210 * p)), width=2)
    title = "RUN COMPLETE"
    box = draw.textbbox((0, 0), title, font=FONT_MED)
    draw.text((W / 2 - (box[2] - box[0]) / 2, y + 36), title, font=FONT_MED, fill=(255, 205, 62, int(255 * p)))
    score = "12,480"
    box = draw.textbbox((0, 0), score, font=FONT_BIG)
    draw.text((W / 2 - (box[2] - box[0]) / 2, y + 108), score, font=FONT_BIG, fill=(255, 255, 245, int(255 * p)))
    rows = [("Impact damage", "4,200"), ("Traffic hit", "3,050"), ("Chain bonus", "2,420"), ("Max speed", "1,850")]
    for i, (label, val) in enumerate(rows):
        yy = y + 188 + i * 38
        draw.text((304, yy), label, font=FONT_SMALL, fill=(238, 238, 224, int(232 * p)))
        box = draw.textbbox((0, 0), val, font=FONT_SMALL)
        draw.text((656 - (box[2] - box[0]), yy), val, font=FONT_SMALL, fill=(255, 255, 245, int(245 * p)))
    draw.rounded_rectangle((336, y + 350, 624, y + 390), radius=5, fill=(255, 205, 62, int(245 * p)))
    btn = "NEXT RUN"
    box = draw.textbbox((0, 0), btn, font=FONT_SMALL)
    draw.text((W / 2 - (box[2] - box[0]) / 2, y + 361), btn, font=FONT_SMALL, fill=(10, 12, 8, int(255 * p)))


def draw_particles(frame: Image.Image, t: float) -> None:
    if t < 3.75 or t > 8.4:
        return
    p = clamp((t - 3.75) / 4.6)
    random.seed(12)
    layer = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    draw = ImageDraw.Draw(layer)
    center = (W * 0.44, H * 0.58)
    for i in range(42):
        start = 3.75 + (i % 9) * 0.09
        life = clamp((t - start) / 1.25)
        if life <= 0 or life >= 1:
            continue
        angle = random.uniform(-2.8, 0.4)
        dist = random.uniform(40, 260) * ease_out(life)
        x = center[0] + math.cos(angle) * dist
        y = center[1] + math.sin(angle) * dist * 0.55 + life * 75
        size = random.randint(2, 7)
        alpha = int(180 * (1 - life))
        draw.rectangle((x, y, x + size, y + size // 2 + 1), fill=(55, 48, 40, alpha))
    if 3.85 < t < 4.25:
        draw.ellipse((center[0] - 210, center[1] - 115, center[0] + 210, center[1] + 115), fill=(255, 226, 120, int(70 * (1 - abs(t - 4.05) / 0.2))))
    frame.alpha_composite(layer.filter(ImageFilter.GaussianBlur(0.4)))


def render_frame(frame_no: int) -> Image.Image:
    t = frame_no / FPS
    scene, local = scene_at(t)
    base_zoom = 1.0 + 0.035 * local
    if t < 3.35:
        pan_x, pan_y = -0.02 + 0.045 * local, 0.03 - 0.04 * local
    elif t < 5.9:
        pan_x, pan_y = -0.03 + 0.02 * local, 0.02
    elif t < 8.75:
        pan_x, pan_y = 0.02 - 0.025 * local, 0.0
    else:
        pan_x, pan_y = -0.01 + 0.015 * local, -0.02

    shake = 0.0
    if 3.8 < t < 6.6:
        shake = (1 - clamp((t - 3.8) / 2.8)) * 0.012
        pan_x += math.sin(t * 63) * shake
        pan_y += math.cos(t * 71) * shake
        base_zoom += shake * 1.2

    frame = cover_crop(scene, base_zoom, pan_x, pan_y)

    if 3.35 < t < 3.72:
        # Hard impact flash into the bus-hit keyframe.
        white = Image.new("RGBA", (W, H), (255, 238, 170, int(150 * math.sin((t - 3.35) / 0.37 * math.pi))))
        frame.alpha_composite(white)

    vignette = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    vd = ImageDraw.Draw(vignette)
    edge = 120
    for i in range(edge):
        alpha = int(((edge - i) / edge) ** 2 * 72)
        vd.line((0, i, W, i), fill=(0, 0, 0, alpha))
        vd.line((0, H - 1 - i, W, H - 1 - i), fill=(0, 0, 0, alpha))
        vd.line((i, 0, i, H), fill=(0, 0, 0, alpha))
        vd.line((W - 1 - i, 0, W - 1 - i, H), fill=(0, 0, 0, alpha))
    frame.alpha_composite(vignette)

    draw_particles(frame, t)
    draw_hud(frame, t)
    draw_impact_text(frame, t)
    draw_summary(frame, t)
    draw_scanlines(frame)

    # Slight sixth-generation-console softness.
    low = frame.resize((640, 480), Image.Resampling.BILINEAR)
    return low.resize((W, H), Image.Resampling.BILINEAR).convert("RGB")


def render() -> None:
    OUT_DIR.mkdir(exist_ok=True)
    ffmpeg = shutil.which("ffmpeg")
    if not ffmpeg:
        raise RuntimeError("ffmpeg is required")

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
    poster = int(FPS * 4.35)
    for n in range(FRAME_COUNT):
        frame = render_frame(n)
        if n == poster:
            frame.save(OUT_POSTER)
        proc.stdin.write(frame.tobytes())
    proc.stdin.close()
    code = proc.wait()
    if code != 0:
        raise RuntimeError(f"ffmpeg exited with {code}")
    print(f"Wrote {OUT_MP4}")
    print(f"Wrote {OUT_POSTER}")


if __name__ == "__main__":
    render()

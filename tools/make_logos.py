"""Малює логотипи проєктів для CurseForge: без ігрового арту й торгових марок."""
from pathlib import Path
import random

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
SIZE = 800
CELL = 40
FONTS = "C:/Windows/Fonts/"


def font(name: str, size: int) -> ImageFont.FreeTypeFont:
    return ImageFont.truetype(FONTS + name, size)


def centered(draw: ImageDraw.ImageDraw, box, text, fnt, fill) -> None:
    left, top, right, bottom = draw.textbbox((0, 0), text, font=fnt)
    x = box[0] + (box[2] - box[0] - (right - left)) / 2 - left
    y = box[1] + (box[3] - box[1] - (bottom - top)) / 2 - top
    draw.text((x, y), text, font=fnt, fill=fill)


def make(path: Path, label: str, base, accent, seed: int) -> None:
    rng = random.Random(seed)
    img = Image.new("RGB", (SIZE, SIZE), base)
    draw = ImageDraw.Draw(img)

    # піксельне тло: квадрати світлішають до правого нижнього кута
    for y in range(0, SIZE, CELL):
        for x in range(0, SIZE, CELL):
            t = (x + y) / (2 * SIZE) + rng.uniform(-0.06, 0.06)
            shade = tuple(int(b + (a - b) * max(0, min(1, t))) for b, a in zip(base, accent))
            draw.rectangle((x, y, x + CELL - 1, y + CELL - 1), fill=shade)

    # хмарка діалогу з двома абетками: латиниця і кирилиця
    bubble = (90, 90, 710, 530)
    tail = [(190, 510), (190, 640), (340, 510)]
    shadow = (14, 16, 30)
    draw.rounded_rectangle([c + 14 for c in bubble], radius=70, fill=shadow)
    draw.polygon([(x + 14, y + 14) for x, y in tail], fill=shadow)
    draw.rounded_rectangle(bubble, radius=70, fill=(255, 250, 238))
    draw.polygon(tail, fill=(255, 250, 238))

    glyphs = font("segoeuib.ttf", 230)
    centered(draw, (110, 110, 400, 500), "Aa", glyphs, base)
    centered(draw, (400, 110, 690, 500), "Її", glyphs, accent)

    # стрічка з платформою: щоб не сплутати два моди в списку
    draw.rectangle((0, 640, SIZE, SIZE), fill=(20, 22, 38))
    centered(draw, (0, 640, SIZE, SIZE), label, font("segoeuib.ttf", 96), (255, 250, 238))

    path.parent.mkdir(exist_ok=True)
    img.save(path, optimize=True)
    print("logo:", path.relative_to(ROOT))


make(ROOT / "assets/logo/custom-language-fixes.png", "ANDROID", (28, 88, 112), (214, 96, 58), 1)
make(ROOT / "assets/logo/custom-language-bundle-fix.png", "PC", (64, 58, 130), (46, 140, 104), 2)

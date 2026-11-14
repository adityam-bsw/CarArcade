#!/usr/bin/env python3
"""
Retro Arcade Workshop: HOME hero-screen art (label-free, procedural, no generative AI).

    python workshop_home.py --output <dir>

Smooth plates are drawn at 3x and downsampled. The backdrop is real pixel art at 384x216 (shown at 5x, Point
filter) with a transparent garage-door opening so the live game world and the showcase car show through it.
All labels are TextMeshPro in Unity, so nothing here contains text.
"""
import argparse
import math
from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter

PAL = {
    "charcoal": "#252B36", "orange": "#F4A340", "red": "#D94B40", "cream": "#E7E0CF", "teal": "#6B9B87",
    "ink": "#18202A", "steel": "#59636D", "steel_dark": "#3B434D", "steel_light": "#8A939B",
    "orange_dark": "#B5701F", "orange_light": "#F9C77D", "red_dark": "#9C342C", "teal_dark": "#4A7262",
    "cream_dark": "#BDB59F", "paper": "#D9C9A5",
}
S = 3  # supersample


def c(name, a=255):
    if isinstance(name, tuple):
        return name if len(name) == 4 else name + (a,)
    h = PAL.get(name, name).lstrip("#")
    return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), a)


class Canvas:
    def __init__(self, w, h):
        self.w, self.h = w, h
        self.im = Image.new("RGBA", (w * S, h * S), (0, 0, 0, 0))
        self.d = ImageDraw.Draw(self.im)

    def pts(self, pts, dx=0, dy=0):
        return [((x + dx) * S, (y + dy) * S) for x, y in pts]

    def poly(self, pts, fill, outline=None, width=3, dx=0, dy=0):
        p = self.pts(pts, dx, dy)
        f = c(fill) if isinstance(fill, (str, tuple)) else fill
        if f[3] > 0:
            self.d.polygon(p, fill=f)
        if outline:
            self.d.line(p + [p[0]], fill=c(outline), width=width * S, joint="curve")

    def ellipse(self, box, fill, outline=None, width=2):
        b = [v * S for v in box]
        self.d.ellipse(b, fill=c(fill) if isinstance(fill, str) else fill,
                       outline=c(outline) if outline else None, width=width * S)

    def line(self, pts, fill, width=2):
        self.d.line(self.pts(pts), fill=c(fill) if isinstance(fill, str) else fill, width=int(width * S), joint="curve")

    def masked(self, mask_pts, painter):
        """Run painter(Canvas) on a transparent layer and keep only what falls inside the polygon."""
        layer = Canvas(self.w, self.h)
        painter(layer)
        mask = Image.new("L", self.im.size, 0)
        ImageDraw.Draw(mask).polygon(self.pts(mask_pts), fill=255)
        self.im.paste(layer.im, (0, 0), Image.composite(layer.im.split()[3], Image.new("L", self.im.size, 0), mask))

    def rivet(self, x, y, r=7):
        self.ellipse((x - r, y - r + 1.5, x + r, y + r + 1.5), "ink")
        self.ellipse((x - r, y - r, x + r, y + r), "steel_light", "ink", 2)
        self.ellipse((x - r * 0.45, y - r * 0.55, x + r * 0.35, y + r * 0.25), "cream")

    def out(self, path, rotate=0):
        im = self.im.resize((self.w, self.h), Image.Resampling.LANCZOS)
        im.save(path)


def notched(x0, y0, x1, y1, n=18, skew=0):
    return [(x0 + n + skew, y0), (x1 - n + skew, y0), (x1 + skew, y0 + n), (x1, y1 - n), (x1 - n, y1),
            (x0 + n, y1), (x0, y1 - n), (x0 + skew, y0 + n)]


# ------------------------------------------------------------------ buttons
def arcade_button(path, state):
    W, H = 360, 140
    palette = {"normal": ("orange", "orange_light", "orange_dark", "red_dark"),
               "pressed": ("orange_dark", "orange", "red_dark", "ink"),
               "disabled": ("steel", "steel_light", "steel_dark", "ink")}[state]
    face, hi, lo, base = palette
    cv = Canvas(W, H)
    lip = {"normal": 18, "pressed": 6, "disabled": 10}[state]
    top = 6 + (18 - lip)
    body = notched(8, top, W - 8, top + 100, 22, 8)
    cv.poly(body, base, "ink", 4, dy=lip)
    cv.poly(body, face, "ink", 4)
    # bevel: light top band, dark lower band, hazard stripes at the left end, racing stripe across
    def paint(l):
        l.poly([(0, top), (W, top), (W, top + 12), (0, top + 12)], hi)
        l.poly([(0, top + 78), (W, top + 78), (W, top + 100), (0, top + 100)], lo)
        for k in range(-2, 6):
            x = 22 + k * 22
            l.poly([(x, top + 14), (x + 11, top + 14), (x + 1, top + 76), (x - 10, top + 76)],
                   "ink" if state != "disabled" else "steel_dark")
    cv.masked(body, lambda l: paint(l) if False else None)
    cv.masked([(8, top + 14), (60, top + 14), (48, top + 92), (8, top + 92)], paint)
    cv.line([(76, top + 20), (W - 40, top + 20)], hi, 3)
    cv.line([(76, top + 82), (W - 40, top + 82)], lo, 3)
    cv.rivet(30, top + 50, 6)
    cv.rivet(W - 22, top + 50, 6)
    cv.out(path)


def plate_button(path, state):
    W, H = 300, 112
    lip = {"normal": 12, "pressed": 4}[state]
    top = 6 + (12 - lip)
    body = notched(8, top, W - 8, top + 82, 14)
    face = "steel" if state == "normal" else "steel_dark"
    cv = Canvas(W, H)
    cv.poly(body, "ink", "ink", 4, dy=lip)
    cv.poly(body, face, "ink", 4)
    cv.masked(body, lambda l: (l.poly([(0, top), (W, top), (W, top + 10), (0, top + 10)], "steel_light"),
                               l.poly([(0, top + 66), (W, top + 66), (W, top + 82), (0, top + 82)], "steel_dark")))
    cv.poly([(26, top + 70), (W - 26, top + 70), (W - 30, top + 76), (30, top + 76)], "teal")
    for x in (28, W - 28):
        cv.rivet(x, top + 41, 6)
    cv.out(path)


def hex_badge(path):
    W = H = 120
    cv = Canvas(W, H)
    pts = [(60 + 52 * math.cos(math.radians(a)), 60 + 52 * math.sin(math.radians(a))) for a in range(30, 390, 60)]
    cv.poly(pts, "ink", "ink", 3, dy=6)
    cv.poly(pts, "steel", "ink", 4)
    inner = [(60 + 40 * math.cos(math.radians(a)), 60 + 40 * math.sin(math.radians(a))) for a in range(30, 390, 60)]
    cv.poly(inner, "steel_dark", "steel_light", 2)
    for a in range(0, 360, 60):
        cv.rivet(60 + 46 * math.cos(math.radians(a + 30)), 60 + 46 * math.sin(math.radians(a + 30)), 3.5)
    # engraved gear
    for a in range(0, 360, 45):
        r1, r2 = 15, 25
        cv.line([(60 + r1 * math.cos(math.radians(a)), 60 + r1 * math.sin(math.radians(a))),
                 (60 + r2 * math.cos(math.radians(a)), 60 + r2 * math.sin(math.radians(a)))], "cream", 9)
    cv.ellipse((38, 38, 82, 82), "cream", "ink", 3)
    cv.ellipse((50, 50, 70, 70), "steel_dark", "ink", 3)
    cv.out(path)


# ------------------------------------------------------------------ plates
def title_plate(path):
    W, H = 1000, 200
    cv = Canvas(W, H)
    # chains up
    for x in (150, 850):
        for y in range(0, 40, 14):
            cv.ellipse((x - 7, y, x + 7, y + 20), (0, 0, 0, 0), "steel_light", 3)
    body = notched(20, 36, W - 20, H - 14, 34)
    cv.poly(body, "ink", None, 1, dy=10)
    cv.poly(body, "steel", "ink", 5)
    inner = notched(44, 58, W - 44, H - 38, 26)
    cv.poly(inner, "charcoal", "steel_light", 3)
    cv.masked(inner, lambda l: l.poly([(0, 58), (W, 58), (W, 84), (0, 84)], (255, 255, 255, 14)))
    cv.line([(70, H - 62), (W - 70, H - 62)], "orange", 3)
    for x, y in ((46, 52), (W - 46, 52), (46, H - 30), (W - 46, H - 30), (W // 2, 46), (W // 2, H - 24)):
        cv.rivet(x, y, 8)
    cv.out(path)


def counter_plate(path):
    W, H = 360, 104
    cv = Canvas(W, H)
    body = notched(8, 6, W - 8, H - 12, 16)
    cv.poly(body, "ink", None, 1, dy=8)
    cv.poly(body, "steel", "ink", 4)
    cv.poly(notched(100, 20, W - 24, H - 26, 10), "ink", "steel_light", 2)  # recessed readout
    cv.line([(112, H - 30), (W - 36, H - 30)], "teal_dark", 2)
    # coin medallion standing proud on the left
    cv.ellipse((18, 10, 92, 90), "ink")
    cv.ellipse((18, 6, 92, 86), "orange", "ink", 4)
    cv.ellipse((28, 16, 82, 76), "orange_light", "orange_dark", 3)
    star = []
    for i in range(10):
        a = math.radians(-90 + i * 36)
        r = 17 if i % 2 == 0 else 7
        star.append((55 + math.cos(a) * r, 46 + math.sin(a) * r))
    cv.poly(star, "orange_dark")
    cv.out(path)


def license_plate(path):
    W, H = 400, 104
    cv = Canvas(W, H)
    body = notched(8, 6, W - 8, H - 12, 12)
    cv.poly(body, "ink", None, 1, dy=8)
    cv.poly(body, "cream", "ink", 4)
    cv.poly(notched(18, 16, W - 18, H - 22, 8), (0, 0, 0, 0), "orange", 4)
    cv.poly([(26, 24), (W - 26, 24), (W - 26, 30), (26, 30)], "teal")
    cv.rivet(30, 52, 6)
    cv.rivet(W - 30, 52, 6)
    cv.out(path)


def tape(path):
    cv = Canvas(130, 40)
    cv.poly([(4, 8), (124, 4), (126, 12), (122, 20), (126, 30), (6, 36), (10, 24), (2, 16)], (231, 224, 207, 215),
            (189, 181, 159, 255), 2)
    cv.out(path)


def star_sticker(path):
    cv = Canvas(110, 110)
    pts = []
    for i in range(10):
        a = math.radians(-90 + i * 36)
        r = 48 if i % 2 == 0 else 22
        pts.append((55 + math.cos(a) * r, 55 + math.sin(a) * r))
    cv.poly(pts, "cream", "ink", 3)
    inner = [(55 + (p[0] - 55) * 0.62, 55 + (p[1] - 55) * 0.62) for p in pts]
    cv.poly(inner, "red", "red_dark", 3)
    cv.out(path)


# ------------------------------------------------------------------ pixel backdrop (384x216)
W, H = 384, 216
OPEN = (104, 74, 280, 148)  # x0, y0, x1, y1 of the transparent door opening


def hsh(x, y, seed=0):
    n = (x * 374761393 + y * 668265263 + seed * 2147483647) & 0xFFFFFFFF
    n = (n ^ (n >> 13)) * 1274126177 & 0xFFFFFFFF
    return ((n ^ (n >> 16)) & 255) / 255.0


def shade(col, k):
    return tuple(max(0, min(255, int(v * k))) for v in col[:3]) + (255,)


def _tool(w, h, fn):
    im = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    fn(ImageDraw.Draw(im), im)
    return im


def tool_wrench():
    def f(d, im):
        d.rectangle([10, 4, 31, 8], fill=c("steel_light"))
        d.line([10, 8, 31, 8], fill=c("steel"))
        d.ellipse([0, 0, 12, 12], fill=c("steel_light"))
        d.rectangle([0, 4, 5, 8], fill=(0, 0, 0, 0))
        d.ellipse([27, 1, 35, 11], fill=c("steel_light"))
        d.ellipse([30, 4, 32, 8], fill=(0, 0, 0, 0))
    return _tool(36, 13, f)


def tool_hammer():
    def f(d, im):
        d.rectangle([0, 0, 21, 8], fill=c("steel_light"))
        d.rectangle([0, 6, 21, 8], fill=c("steel"))
        d.rectangle([0, 0, 3, 8], fill=c("steel"))
        d.rectangle([8, 9, 13, 33], fill=c("paper"))
        d.rectangle([12, 9, 13, 33], fill=c("cream_dark"))
        d.rectangle([8, 9, 13, 11], fill=c("steel"))
    return _tool(22, 34, f)


def tool_screwdriver():
    def f(d, im):
        d.rectangle([0, 0, 12, 8], fill=c("red"))
        d.rectangle([0, 6, 12, 8], fill=c("red_dark"))
        for x in (3, 6, 9):
            d.line([x, 1, x, 7], fill=c("red_dark"))
        d.rectangle([13, 3, 30, 5], fill=c("steel_light"))
        d.rectangle([31, 3, 34, 5], fill=c("steel"))
    return _tool(35, 9, f)


def tool_saw():
    def f(d, im):
        d.polygon([(0, 0), (22, 0), (22, 11), (20, 14), (18, 11), (16, 14), (14, 11), (12, 14), (10, 11),
                   (8, 14), (6, 11), (4, 14), (2, 11), (0, 14)], fill=c("steel_light"))
        d.rectangle([0, 0, 22, 2], fill=c("cream"))
        d.rectangle([22, 0, 32, 20], fill=c("orange"))
        d.rectangle([29, 0, 32, 20], fill=c("orange_dark"))
        d.rectangle([25, 6, 28, 12], fill=(0, 0, 0, 0))
    return _tool(33, 21, f)


def tool_gear():
    def f(d, im):
        for ang in range(0, 360, 45):
            x = 9 + 8 * math.cos(math.radians(ang))
            y = 9 + 8 * math.sin(math.radians(ang))
            d.rectangle([x - 2, y - 2, x + 2, y + 2], fill=c("orange"))
        d.ellipse([2, 2, 16, 16], fill=c("orange"))
        d.ellipse([3, 8, 15, 16], fill=c("orange_dark"))
        d.ellipse([2, 2, 16, 12], fill=c("orange"))
        d.ellipse([6, 6, 12, 12], fill=(0, 0, 0, 0))
    return _tool(19, 19, f)


def tool_toolbox():
    def f(d, im):
        d.rectangle([9, 0, 21, 3], fill=c("steel_light"))
        d.rectangle([12, 1, 18, 3], fill=(0, 0, 0, 0))
        d.rectangle([0, 4, 29, 19], fill=c("red"))
        d.rectangle([0, 4, 29, 8], fill="#C9473C")
        d.rectangle([0, 17, 29, 19], fill=c("red_dark"))
        d.rectangle([12, 8, 17, 12], fill=c("cream"))
        d.rectangle([0, 9, 29, 9], fill=c("red_dark"))
    return _tool(30, 20, f)


def paste_tool(base, tool, x, y):
    alpha = tool.split()[3]
    outline_mask = alpha.filter(ImageFilter.MaxFilter(3))
    shadow = Image.new("RGBA", tool.size, (0, 0, 0, 0))
    big = Image.new("RGBA", (tool.width + 4, tool.height + 4), (0, 0, 0, 0))
    big.paste(Image.new("RGBA", tool.size, c("ink")), (1, 1), outline_mask)
    base.alpha_composite(Image.merge("RGBA", (Image.new("L", big.size, 8), Image.new("L", big.size, 10),
                                              Image.new("L", big.size, 14), big.split()[3].point(lambda v: 150 if v else 0))),
                         (x + 1, y + 2))                     # cast shadow
    base.alpha_composite(big, (x - 1, y - 1))                # ink outline
    base.alpha_composite(tool, (x, y))


def backdrop(path, props=True, door=True):
    im = Image.new("RGBA", (W, H), (0, 0, 0, 255))
    px = im.load()
    d = ImageDraw.Draw(im)
    wall = c("charcoal")
    # wall: vertical planks with grain and dents
    for y in range(0, 150):
        for x in range(W):
            plank = (x // 24)
            k = 0.9 + 0.1 * hsh(plank, 0, 3) + (hsh(x, y, 1) - 0.5) * 0.07
            if x % 24 == 0:
                k *= 0.6
            if y % 40 == 0 and hsh(plank, y // 40, 5) > 0.5:
                k *= 0.8
            px[x, y] = shade(wall, k)
    # floor: concrete with seams, hazard line and oil stains
    floor = c("steel_dark")
    for y in range(148, H):
        for x in range(W):
            k = 0.8 + (hsh(x, y, 7) - 0.5) * 0.08 + (y - 148) / 68 * -0.18
            if (x // 48 + y // 24) % 2 == 0:
                k *= 1.05
            px[x, y] = shade(floor, k)
    for x in range(0, W):
        d.rectangle([x, 148, x, 151], fill=c("orange") if (x // 8) % 2 == 0 else c("ink"))
    for (sx, sy, r) in ((50, 190, 9), (330, 196, 12), (210, 204, 7)):
        d.ellipse([sx - r, sy - r // 2, sx + r, sy + r // 2], fill=c("ink"))

    # ceiling beam + lamps with warm glow
    d.rectangle([0, 0, W, 8], fill=c("ink"))
    d.rectangle([0, 8, W, 10], fill=c("steel_dark"))
    if props:
        for lx in (46, 338):
            d.line([lx, 10, lx, 22], fill=c("steel_light"))
            d.polygon([(lx - 12, 32), (lx + 12, 32), (lx + 6, 22), (lx - 6, 22)], fill=c("steel"), outline=c("ink"))
            d.rectangle([lx - 10, 32, lx + 10, 34], fill=c("orange_light"))
            for y in range(34, 100):
                for x in range(lx - 46, lx + 47):
                    if 0 <= x < W and px[x, y][0:3] != (0, 0, 0):
                        dist = math.hypot((x - lx) * 0.8, y - 34)
                        if dist < 64:
                            t = (1 - dist / 64) ** 2 * 0.38
                            r, g, b, _ = px[x, y]
                            px[x, y] = (min(255, int(r + 244 * t)), min(255, int(g + 163 * t)), min(255, int(b + 64 * t)), 255)

    if props:
        # pegboard with tools (left): each tool is a small sprite with an auto outline and a cast shadow
        d.rectangle([14, 42, 92, 134], fill=c("steel_dark"), outline=c("ink"))
        d.rectangle([15, 43, 91, 44], fill=c("steel"))
        for y in range(50, 130, 6):
            for x in range(20, 88, 6):
                px[x, y] = c("ink")
        paste_tool(im, tool_wrench(), 20, 50)
        paste_tool(im, tool_hammer(), 64, 48)
        paste_tool(im, tool_screwdriver(), 20, 72)
        paste_tool(im, tool_saw(), 52, 86)
        paste_tool(im, tool_gear(), 22, 96)
        paste_tool(im, tool_toolbox(), 50, 111)

    if props:
        # poster and clock (right wall) and a taped race ticket
        d.rectangle([296, 36, 346, 84], fill=c("teal_dark"), outline=c("ink"))
        d.rectangle([302, 42, 340, 60], fill=c("ink"))
        d.polygon([(306, 56), (322, 46), (336, 56)], fill=c("orange"))
        for i in range(4):
            d.rectangle([302 + i * 10, 66, 308 + i * 10, 72], fill=c("ink") if i % 2 == 0 else c("cream"))
        d.rectangle([312, 33, 330, 38], fill=c("cream_dark"))
        d.ellipse([352, 40, 372, 60], fill=c("cream"), outline=c("ink"))
        d.line([362, 50, 362, 43], fill=c("ink"))
        d.line([362, 50, 367, 52], fill=c("red"))

    if props:
        # shelves with cans and tyres
        d.rectangle([292, 100, 372, 104], fill=c("steel_light"))
        d.rectangle([292, 104, 372, 106], fill=c("ink"))
        for i, col in enumerate(("red", "orange", "teal", "cream")):
            d.rectangle([298 + i * 14, 86, 306 + i * 14, 100], fill=c(col), outline=c("ink"))
        for ty in (118, 130):
            d.ellipse([296, ty, 336, ty + 18], fill=c("ink"), outline=c("steel_dark"))
            d.ellipse([308, ty + 4, 324, ty + 14], fill=c("steel"))
        d.rectangle([346, 112, 372, 146], fill=c("steel"), outline=c("ink"))         # oil drum
        d.line([346, 122, 372, 122], fill=c("ink"))
        d.line([346, 136, 372, 136], fill=c("ink"))

    if door:
        # roll-up door slats over the opening, then the frame
        x0, y0, x1, y1 = OPEN
        for y in range(58, y0):
            for x in range(x0 - 6, x1 + 6):
                k = 1.0 if (y % 6) else 0.65
                px[x, y] = shade(c("steel_light"), k * (0.85 + 0.15 * hsh(x, y, 9)))
        d.rectangle([x0 - 6, 54, x1 + 5, 58], fill=c("ink"))
        d.rectangle([x0 - 6, 54, x1 + 5, 56], fill=c("orange"))
        for x in range(x0 + 4, x1, 22):
            d.rectangle([x, 58, x + 1, y0 - 1], fill=c("steel_dark"))
        # frame
        for t, col in enumerate(("ink", "steel", "steel_light", "ink")):
            d.rectangle([x0 - 6 + t, y0 - 0, x0 - 6 + t, y1 + 1], fill=c(col))
            d.rectangle([x1 + 5 - t, y0 - 0, x1 + 5 - t, y1 + 1], fill=c(col))
        for y in range(y0 + 4, y1, 14):
            px[x0 - 4, y] = c("cream")
            px[x1 + 3, y] = c("cream")
        # transparent opening (the live world shows through)
        for y in range(y0, y1):
            for x in range(x0, x1):
                px[x, y] = (0, 0, 0, 0)
        # threshold ramp below the opening
        d.rectangle([x0 - 6, y1, x1 + 5, y1 + 3], fill=c("ink"))
        d.rectangle([x0 - 6, y1 + 1, x1 + 5, y1 + 1], fill=c("steel_light"))
    im.save(path)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--output", default="Generated_Home")
    out = Path(ap.parse_args().output)
    out.mkdir(parents=True, exist_ok=True)
    for st in ("normal", "pressed", "disabled"):
        arcade_button(out / f"UI_Button_Arcade_Primary_{st.title()}.png", st)
    for st in ("normal", "pressed"):
        plate_button(out / f"UI_Button_Plate_Secondary_{st.title()}.png", st)
    hex_badge(out / "UI_Badge_Hex_Gear.png")
    title_plate(out / "UI_Plate_Title_Main.png")
    counter_plate(out / "UI_Plate_Counter_Coin.png")
    license_plate(out / "UI_Plate_License_Best.png")
    tape(out / "UI_Deco_Tape_01.png")
    star_sticker(out / "UI_Deco_Sticker_Star.png")
    backdrop(out / "UI_Background_Workshop_Home.png")
    print("written", len(list(out.glob("*.png"))), "files to", out.resolve())


if __name__ == "__main__":
    main()

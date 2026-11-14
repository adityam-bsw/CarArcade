#!/usr/bin/env python3
"""
Retro Arcade Workshop: the garage background as SEPARATE, resizable parts (pixel art, 1x, shown at 5x with Point filter).

    python workshop_bg.py --output <dir>

Tileable strips (wall, floor, hazard line, ceiling beam) repeat sideways to fill any screen width; the door is one centred
sprite with a transparent opening. Props are separate (workshop_props.py) and anchor to the left or right screen edge.
"""
import argparse
from pathlib import Path
from PIL import Image, ImageDraw

import workshop_home as H
from workshop_home import c


def wall_tile(path):
    W, Hh = 96, 140
    im = Image.new("RGBA", (W, Hh), (0, 0, 0, 255))
    px = im.load()
    wall = c("charcoal")
    for y in range(Hh):
        for x in range(W):
            plank = x // 24
            k = 0.9 + 0.1 * H.hsh(plank, 0, 3) + (H.hsh(x, y, 1) - 0.5) * 0.07
            if x % 24 == 0:
                k *= 0.6
            if y % 40 == 0 and H.hsh(plank, y // 40, 5) > 0.5:
                k *= 0.8
            px[x, y] = H.shade(wall, k)
    im.save(path)


def floor_tile(path):
    W, Hh = 96, 68
    im = Image.new("RGBA", (W, Hh), (0, 0, 0, 255))
    px = im.load()
    floor = c("steel_dark")
    for y in range(Hh):
        for x in range(W):
            k = 0.8 + (H.hsh(x, y, 7) - 0.5) * 0.08 + y / 68 * -0.18
            if (x // 48 + y // 24) % 2 == 0:
                k *= 1.05
            px[x, y] = H.shade(floor, k)
    im.save(path)


def hazard_tile(path):
    im = Image.new("RGBA", (16, 4), (0, 0, 0, 255))
    d = ImageDraw.Draw(im)
    d.rectangle([0, 0, 7, 3], fill=c("orange"))
    d.rectangle([8, 0, 15, 3], fill=c("ink"))
    im.save(path)


def beam_tile(path):
    im = Image.new("RGBA", (16, 10), (0, 0, 0, 255))
    d = ImageDraw.Draw(im)
    d.rectangle([0, 0, 15, 7], fill=c("ink"))
    d.rectangle([0, 8, 15, 9], fill=c("steel_dark"))
    d.point([(5, 3), (11, 5)], fill=c("steel_dark"))
    im.save(path)


def door(path):
    """188x102: orange header, steel slats, riveted frame, threshold. The opening (x 6..181, y 20..93) is transparent."""
    W, Hh = 188, 102
    im = Image.new("RGBA", (W, Hh), (0, 0, 0, 0))
    px = im.load()
    d = ImageDraw.Draw(im)
    ox0, oy0, ox1, oy1 = 6, 20, 182, 94
    for y in range(4, oy0):
        for x in range(0, W):
            k = 1.0 if (y % 6) else 0.65
            px[x, y] = H.shade(c("steel_light"), k * (0.85 + 0.15 * H.hsh(x, y, 9)))
    d.rectangle([0, 0, W - 1, 4], fill=c("ink"))
    d.rectangle([0, 0, W - 1, 2], fill=c("orange"))
    for x in range(10, W - 6, 22):
        d.rectangle([x, 4, x + 1, oy0 - 1], fill=c("steel_dark"))
    for t, col in enumerate(("ink", "steel", "steel_light", "ink")):
        d.rectangle([t, oy0, t, oy1 + 1], fill=c(col))
        d.rectangle([W - 1 - t, oy0, W - 1 - t, oy1 + 1], fill=c(col))
    for y in range(oy0 + 4, oy1, 14):
        px[2, y] = c("cream")
        px[W - 3, y] = c("cream")
    d.rectangle([0, oy1, W - 1, oy1 + 3], fill=c("ink"))
    d.rectangle([0, oy1 + 1, W - 1, oy1 + 1], fill=c("steel_light"))
    im.save(path)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--output", default="Generated_Bg")
    out = Path(ap.parse_args().output)
    out.mkdir(parents=True, exist_ok=True)
    wall_tile(out / "UI_Bg_Wall_Tile.png")
    floor_tile(out / "UI_Bg_Floor_Tile.png")
    hazard_tile(out / "UI_Bg_Hazard_Tile.png")
    beam_tile(out / "UI_Bg_Beam_Tile.png")
    door(out / "UI_Bg_Door.png")
    print("written 5 parts to", out.resolve())


if __name__ == "__main__":
    main()

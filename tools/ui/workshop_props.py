#!/usr/bin/env python3
"""
Retro Arcade Workshop: stand-alone pixel props (every object is its own sprite so it can be animated) and the clean
garage backdrops they sit on.   python workshop_props.py --output <dir>

Pixel art at 1x (384x216 grid, shown at 5x with Point filter). Props include their ink outline and cast shadow.
Origin of every prop is its top-left corner on the 384x216 grid; the positions are listed in PLACEMENT.
"""
import argparse
import math
from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter

import workshop_home as H
from workshop_home import c

# name -> (x, y) top-left on the 384x216 grid (used by UIPrefabBuilder, see PropPlacement there)
PLACEMENT = {}


def outlined(tool, shadow=True):
    """Add a 1 px ink outline and a cast shadow around a drawn tool, like paste_tool does, but return the sprite."""
    pad = 2
    a = tool.split()[3]
    out = Image.new("RGBA", (tool.width + pad * 2 + 1, tool.height + pad * 2 + 1), (0, 0, 0, 0))
    grown = a.filter(ImageFilter.MaxFilter(3))
    if shadow:
        sh = Image.new("RGBA", tool.size, (10, 14, 20, 150))
        out.paste(sh, (pad + 1, pad + 2), grown)
    out.paste(Image.new("RGBA", tool.size, c("ink")), (pad, pad), grown)
    out.alpha_composite(tool, (pad + 1, pad + 1))
    return out


def sprite(w, h, fn):
    im = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    fn(ImageDraw.Draw(im), im)
    return im


def pegboard():
    def f(d, im):
        d.rectangle([0, 0, 78, 92], fill=c("steel_dark"), outline=c("ink"))
        d.rectangle([1, 1, 77, 2], fill=c("steel"))
        px = im.load()
        for y in range(8, 88, 6):
            for x in range(6, 74, 6):
                px[x, y] = c("ink")
    return sprite(79, 93, f)


def lamp():
    def f(d, im):
        d.line([13, 0, 13, 12], fill=c("steel_light"))
        d.polygon([(2, 22), (24, 22), (18, 12), (8, 12)], fill=c("steel"), outline=c("ink"))
        d.rectangle([4, 22, 22, 24], fill=c("orange_light"))
        d.rectangle([6, 14, 12, 16], fill=c("steel_light"))
    return sprite(27, 26, f)


def lamp_glow():
    W, Hh = 100, 76
    im = Image.new("RGBA", (W, Hh), (0, 0, 0, 0))
    px = im.load()
    for y in range(Hh):
        for x in range(W):
            dist = math.hypot((x - W / 2) * 0.8, y)
            if dist < 62:
                t = (1 - dist / 62) ** 2
                px[x, y] = (244, 163, 64, int(95 * t))
    return im


def clock_face():
    def f(d, im):
        d.ellipse([0, 0, 20, 20], fill=c("cream"), outline=c("ink"))
        for a in range(0, 360, 90):
            x = 10 + 8 * math.cos(math.radians(a)); y = 10 + 8 * math.sin(math.radians(a))
            d.point((int(x), int(y)), fill=c("ink"))
    return sprite(21, 21, f)


def hand(length, col):
    def f(d, im):
        d.rectangle([0, 0, 0, length - 1], fill=c(col))
    return sprite(1, length, f)


def robot_body():
    def f(d, im):
        # legs, torso, head with a dark screen face
        d.rectangle([8, 44, 14, 56], fill=c("steel"))
        d.rectangle([28, 44, 34, 56], fill=c("steel"))
        d.rectangle([5, 54, 16, 58], fill=c("steel_dark"))
        d.rectangle([26, 54, 37, 58], fill=c("steel_dark"))
        d.rectangle([4, 26, 38, 46], fill=c("teal_dark"))
        d.rectangle([4, 26, 38, 29], fill=c("teal"))
        d.rectangle([4, 42, 38, 46], fill="#35574A")
        d.rectangle([12, 31, 30, 40], fill=c("ink"))
        for i, col in enumerate(("red", "orange", "cream", "teal")):
            d.rectangle([14 + i * 4, 34, 16 + i * 4, 37], fill=c(col))
        d.rectangle([8, 4, 34, 24], fill=c("teal_dark"))
        d.rectangle([8, 4, 34, 7], fill=c("teal"))
        d.rectangle([11, 9, 31, 22], fill=c("ink"))
        d.rectangle([17, 24, 25, 26], fill=c("steel"))
    return sprite(43, 59, f)


def robot_eyes():
    def f(d, im):
        d.rectangle([0, 0, 4, 5], fill=c("orange_light"))
        d.rectangle([14, 0, 18, 5], fill=c("orange_light"))
        d.rectangle([5, 9, 13, 10], fill=c("orange"))
    return sprite(19, 11, f)


def robot_antenna():
    def f(d, im):
        d.rectangle([4, 4, 5, 12], fill=c("steel_light"))
        d.ellipse([1, 0, 8, 7], fill=c("red"), outline=c("ink"))
        d.point((3, 2), fill=c("cream"))
    return sprite(10, 13, f)


def robot_arm():
    def f(d, im):
        d.rectangle([1, 0, 6, 16], fill=c("steel"))
        d.rectangle([1, 0, 2, 16], fill=c("steel_light"))
        d.rectangle([0, 16, 7, 20], fill=c("orange"))
    return sprite(8, 21, f)


def shelf():
    def f(d, im):
        d.rectangle([0, 0, 79, 3], fill=c("steel_light"))
        d.rectangle([0, 4, 79, 5], fill=c("ink"))
        d.rectangle([4, 6, 8, 12], fill=c("steel_dark"))
        d.rectangle([71, 6, 75, 12], fill=c("steel_dark"))
    return sprite(80, 13, f)


def can(col):
    def f(d, im):
        d.rectangle([0, 0, 8, 14], fill=c(col), outline=c("ink"))
        d.rectangle([0, 5, 8, 8], fill=c("cream"))
        d.rectangle([1, 1, 2, 13], fill=(255, 255, 255, 60))
    return sprite(9, 15, f)


def tyre():
    def f(d, im):
        d.ellipse([0, 0, 39, 17], fill=c("ink"), outline=c("steel_dark"))
        d.ellipse([11, 4, 28, 13], fill=c("steel"))
        d.ellipse([16, 6, 23, 11], fill=c("steel_dark"))
    return sprite(40, 18, f)


def drum():
    def f(d, im):
        d.rectangle([0, 0, 25, 33], fill=c("steel"), outline=c("ink"))
        d.rectangle([1, 1, 4, 32], fill=c("steel_light"))
        for y in (8, 22):
            d.line([0, y, 25, y], fill=c("ink"))
        d.rectangle([9, 12, 16, 18], fill=c("orange"))
    return sprite(26, 34, f)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--output", default="Generated_Props")
    out = Path(ap.parse_args().output)
    props = out / "Props"
    props.mkdir(parents=True, exist_ok=True)

    H.backdrop(out / "UI_Background_Workshop_Home.png", props=False, door=True)
    H.backdrop(out / "UI_Background_Workshop_Wall.png", props=False, door=False)

    items = {
        "pegboard": pegboard(), "wrench": outlined(H._tool(36, 13, H.tool_wrench.__closure__ and None) if False else H.tool_wrench()),
        "hammer": outlined(H.tool_hammer()), "screwdriver": outlined(H.tool_screwdriver()), "saw": outlined(H.tool_saw()),
        "gear": outlined(H.tool_gear()), "toolbox": outlined(H.tool_toolbox()),
        "lamp": lamp(), "lamp_glow": lamp_glow(), "clock_face": clock_face(),
        "clock_hand_minute": hand(9, "ink"), "clock_hand_hour": hand(6, "red"),
        "robot_body": outlined(robot_body()), "robot_eyes": robot_eyes(), "robot_antenna": robot_antenna(),
        "robot_arm": outlined(robot_arm()), "shelf": outlined(shelf()),
        "can_red": outlined(can("red")), "can_orange": outlined(can("orange")), "can_teal": outlined(can("teal")),
        "tyre": outlined(tyre()), "drum": outlined(drum()),
    }
    for name, im in items.items():
        im.save(props / f"UI_Prop_{name}.png")
    print("written", len(items), "props and 2 backdrops to", out.resolve())


if __name__ == "__main__":
    main()

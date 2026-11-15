#!/usr/bin/env python3
"""Draws the small pixel icons for the new pickups (gem, shield, magnet, x2, slow-mo, ghost) into
Assets/Resources/RoadBuilder/Pickup_*.png. Procedural, no generative AI, palette from the workshop UI. Needs Pillow."""
import pathlib
from PIL import Image

OUT = pathlib.Path(__file__).resolve().parents[2] / "Assets/Resources/RoadBuilder"
INK = (24, 32, 42, 255); CREAM = (231, 224, 207, 255); ORANGE = (244, 163, 64, 255); RED = (217, 75, 64, 255)
TEAL = (107, 155, 135, 255); STEEL = (112, 124, 140, 255); LIGHT = (176, 188, 202, 255); BLUE = (92, 140, 220, 255)
CY = (96, 214, 230, 255); PURPLE = (160, 110, 220, 255); WHITE = (250, 250, 250, 255)

def canvas(rows, pal):
    im = Image.new("RGBA", (16, 16), (0, 0, 0, 0))
    for y, row in enumerate(rows):
        for x, ch in enumerate(row):
            if ch in pal: im.putpixel((x, y), pal[ch])
    return im

ICONS = {
 "Gem": (["................", "....oooooooo....", "...oCCWCCCCo....".replace("....","...",1)[:16], "..oCWCCCCCCCo...", "..oCCCCCCCCCo...", "...oCCCCCCCo....", "....oCCCCCo.....", ".....oCCCo......", "......oCo.......", ".......o........", "................", "................", "................", "................", "................", "................"], {"o": INK, "C": CY, "W": WHITE}),
 "Shield": (["................", "..oooooooooooo..", "..oBBBBBBBBBBo..", "..oBWWBBBBBBBo..", "..oBWBBBBBBBBo..", "..oBBBBBBBBBBo..", "..oBBBBBBBBBBo..", "...oBBBBBBBBo...", "...oBBBBBBBBo...", "....oBBBBBBo....", ".....oBBBBo.....", "......oBBo......", ".......oo.......", "................", "................", "................"], {"o": INK, "B": BLUE, "W": WHITE}),
 "Magnet": (["................", "...oooo..oooo...", "..oRRRRooRRRRo..".replace("..oRRRRooRRRRo..", "..oRRRo..oRRRo.."), "..oRRRo..oRRRo..", "..oRRRo..oRRRo..", "..oRRRo..oRRRo..", "..oRRRo..oRRRo..", "..oRRRoooRRRRo..".replace("RRRRo..", "RRRo..."), "..oRRRRRRRRRo...", "...oRRRRRRRo....", "....ooooooo.....", "..oWWo....oWWo..", "..oWWo....oWWo..", "..oooo....oooo..", "................", "................"], {"o": INK, "R": RED, "W": LIGHT}),
 "Double": (["................", "..ooooo..o...o..", ".oOOOOOo.oo.oo..", ".ooooOOo..ooo...", "....oOOo..ooo...", "...oOOo..oo.oo..", "..oOOo...o...o..", ".oOOOOOo........", ".ooooooo........", "................", "................", "................", "................", "................", "................", "................"], {"o": INK, "O": ORANGE}),
 "SlowMo": (["................", ".....oooooo.....", "...ooCCCCCCoo...", "..oCCCCoCCCCCo..", ".oCCCCCoCCCCCCo.", ".oCCCCCoCCCCCCo.", ".oCCCCCooooCCCo.", ".oCCCCCCCCCCCCo.", ".oCCCCCCCCCCCCo.", "..oCCCCCCCCCCo..", "...ooCCCCCCoo...", ".....oooooo.....", "................", "................", "................", "................"], {"o": INK, "C": CREAM}),
 "Ghost": (["................", "....oooooooo....", "...oPPPPPPPPo...", "..oPPPPPPPPPPo..", "..oPPWWPPWWPPo..", "..oPPWoPPWoPPo..", "..oPPPPPPPPPPo..", "..oPPPPPPPPPPo..", "..oPPPPPPPPPPo..", "..oPPPPPPPPPPo..", "..oPoPPoPPoPPo..", "..ooooooooooo...", "................", "................", "................", "................"], {"o": INK, "P": PURPLE, "W": WHITE}),
}

for name, (rows, pal) in ICONS.items():
    rows = [r.ljust(16, ".")[:16] for r in rows] + ["." * 16] * (16 - len(rows))
    canvas(rows[:16], pal).save(OUT / ("Pickup_%s.png" % name))
def double_icon():
    """An orange x2 with an ink outline, from a 3x5 pixel font scaled by 2."""
    glyphs = {"x": ["101", "101", "010", "101", "101"], "2": ["111", "001", "111", "100", "111"]}
    im = Image.new("RGBA", (16, 16), (0, 0, 0, 0))
    mask = set()
    for gi, ch in enumerate("x2"):
        for y, row in enumerate(glyphs[ch]):
            for x, bit in enumerate(row):
                if bit == "1":
                    for dy in range(2):
                        for dx in range(2):
                            mask.add((1 + gi * 8 + x * 2 + dx, 3 + y * 2 + dy))
    for (x, y) in mask:
        for ox in (-1, 0, 1):
            for oy in (-1, 0, 1):
                px, py = x + ox, y + oy
                if 0 <= px < 16 and 0 <= py < 16 and (px, py) not in mask: im.putpixel((px, py), INK)
    for (x, y) in mask: im.putpixel((x, y), ORANGE)
    return im

double_icon().save(OUT / "Pickup_Double.png")
print("wrote", len(ICONS), "icons to", OUT)

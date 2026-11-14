#!/usr/bin/env python3
"""
Retro Arcade Workshop: art for Garage, Worlds, HUD, Result, Settings and Collection (label-free, procedural).

    python workshop_screens.py --output <dir>

Builds on workshop_home.py (same palette and Canvas helpers). No text is drawn: labels are TextMeshPro in Unity.
"""
import argparse
import math
from pathlib import Path
from PIL import Image, ImageDraw

import workshop_home as H
from workshop_home import Canvas, c, notched


def window(cv, box, inset="ink"):
    x0, y0, x1, y1 = box
    cv.poly([(x0, y0), (x1, y0), (x1, y1), (x0, y1)], inset, "ink", 3)
    cv.poly([(x0 + 3, y0 + 3), (x1 - 3, y0 + 3), (x1 - 3, y0 + 8), (x0 + 3, y0 + 8)], (255, 255, 255, 26))


def small_title(path):
    W, Hh = 640, 160
    cv = Canvas(W, Hh)
    for x in (110, 530):
        for y in range(0, 30, 12):
            cv.ellipse((x - 6, y, x + 6, y + 17), (0, 0, 0, 0), "steel_light", 3)
    body = notched(14, 26, W - 14, Hh - 12, 26)
    cv.poly(body, "ink", None, 1, dy=8)
    cv.poly(body, "steel", "ink", 5)
    cv.poly(notched(34, 44, W - 34, Hh - 30, 20), "charcoal", "steel_light", 3)
    cv.line([(60, Hh - 50), (W - 60, Hh - 50)], "orange", 3)
    for x, y in ((34, 40), (W - 34, 40), (34, Hh - 26), (W - 34, Hh - 26)):
        cv.rivet(x, y, 7)
    cv.out(path)


def back_badge(path):
    W = 120
    cv = Canvas(W, W)
    pts = [(60 + 52 * math.cos(math.radians(a)), 60 + 52 * math.sin(math.radians(a))) for a in range(30, 390, 60)]
    cv.poly(pts, "ink", None, 1, dy=6)
    cv.poly(pts, "steel", "ink", 4)
    inner = [(60 + 40 * math.cos(math.radians(a)), 60 + 40 * math.sin(math.radians(a))) for a in range(30, 390, 60)]
    cv.poly(inner, "steel_dark", "steel_light", 2)
    cv.poly([(28, 60), (58, 34), (58, 50), (92, 50), (92, 70), (58, 70), (58, 86)], "cream", "ink", 3)
    cv.out(path)


def upgrade_plate(path):
    W, Hh = 440, 214
    cv = Canvas(W, Hh)
    body = notched(8, 6, W - 8, Hh - 14, 20)
    cv.poly(body, "ink", None, 1, dy=8)
    cv.poly(body, "steel", "ink", 4)
    cv.masked(body, lambda l: (l.poly([(0, 6), (W, 6), (W, 16), (0, 16)], "steel_light"),
                               l.poly([(0, Hh - 40), (W, Hh - 40), (W, Hh), (0, Hh)], "steel_dark")))
    cv.poly(notched(28, 24, W - 28, 70, 10), "ink", "steel_light", 2)      # header strip
    cv.poly(notched(28, 84, W - 28, 112, 8), "charcoal", "ink", 2)        # pip rail
    for x, y in ((26, 22), (W - 26, 22), (26, Hh - 36), (W - 26, Hh - 36)):
        cv.rivet(x, y, 6)
    cv.out(path)


def pip(path, on):
    cv = Canvas(44, 26)
    pts = [(8, 2), (42, 2), (36, 24), (2, 24)]
    cv.poly(pts, "orange" if on else "charcoal", "ink", 2)
    if on:
        cv.poly([(10, 4), (38, 4), (36, 9), (8, 9)], "orange_light")
    cv.out(path)


def name_plate(path):
    W, Hh = 600, 112
    cv = Canvas(W, Hh)
    body = notched(8, 6, W - 8, Hh - 12, 16)
    cv.poly(body, "ink", None, 1, dy=8)
    cv.poly(body, "charcoal", "ink", 4)
    cv.poly(notched(18, 16, W - 18, Hh - 22, 10), (0, 0, 0, 0), "orange", 4)
    cv.rivet(34, 50, 6)
    cv.rivet(W - 34, 50, 6)
    cv.out(path)


def card_frame(path, size, notch=14):
    W, Hh = size
    cv = Canvas(W, Hh)
    cv.poly(notched(2, 2, W - 2, Hh - 2, notch), (0, 0, 0, 0), "orange", 8)
    cv.poly(notched(8, 8, W - 8, Hh - 8, notch - 4), (0, 0, 0, 0), "orange_light", 3)
    cv.out(path)


def vehicle_card(path):
    W, Hh = 300, 224
    cv = Canvas(W, Hh)
    body = notched(8, 6, W - 8, Hh - 12, 18)
    cv.poly(body, "ink", None, 1, dy=8)
    cv.poly(body, "cream", "ink", 4)
    cv.poly([(24, 18), (W - 24, 18), (W - 24, 24), (24, 24)], "teal")
    window(cv, (24, 30, W - 24, 140))
    cv.line([(24, 154), (W - 24, 154)], "cream_dark", 2)
    cv.rivet(18, 178, 5)
    cv.rivet(W - 18, 178, 5)
    cv.out(path)


def postcard(path):
    W, Hh = 340, 252
    cv = Canvas(W, Hh)
    body = [(8, 10), (W - 8, 6), (W - 6, Hh - 12), (10, Hh - 8)]
    cv.poly(body, "ink", None, 1, dy=8)
    cv.poly(body, "cream", "ink", 4)
    window(cv, (20, 20, W - 20, 172))
    # stamp with perforated edge in the corner, overlapping the picture
    cv.poly([(W - 78, 8), (W - 14, 8), (W - 14, 62), (W - 78, 62)], "orange", "ink", 3)
    cv.poly([(W - 70, 16), (W - 22, 16), (W - 22, 54), (W - 70, 54)], "orange_light", "orange_dark", 2)
    for i in range(7):
        cv.ellipse((W - 80 + i * 9, 2, W - 74 + i * 9, 8), "ink")
    cv.line([(26, 196), (W - 26, 196)], "cream_dark", 2)
    cv.out(path)


def ticket(path):
    W, Hh = 300, 208
    cv = Canvas(W, Hh)
    body = [(8, 8), (W - 8, 8), (W - 8, Hh - 14), (W - 22, Hh - 8), (W - 36, Hh - 14), (W - 50, Hh - 8), (W - 64, Hh - 14),
            (8, Hh - 14)]
    cv.poly(body, "ink", None, 1, dy=7)
    cv.poly(body, "paper", "ink", 4)
    cv.poly([(8, 8), (30, 8), (30, Hh - 14), (8, Hh - 14)], "orange_dark")
    for y in range(22, Hh - 20, 18):
        cv.ellipse((19, y, 27, y + 9), "ink")
    window(cv, (44, 18, W - 22, 128))
    cv.line([(44, 144), (W - 22, 144)], "cream_dark", 2)
    cv.out(path)


def lock_tag(path):
    cv = Canvas(72, 92)
    cv.line([(36, 4), (36, 28)], "steel_light", 4)
    cv.poly([(10, 30), (62, 30), (66, 40), (66, 86), (6, 86), (6, 40)], "red", "ink", 3)
    cv.poly([(10, 34), (62, 34), (62, 42), (10, 42)], "red_dark")
    cv.ellipse((24, 4, 48, 40), (0, 0, 0, 0), "cream", 5)
    cv.poly([(28, 52), (44, 52), (44, 56), (40, 62), (40, 74), (32, 74), (32, 62), (28, 56)], "cream", "ink", 2)
    cv.out(path)


def certificate(path):
    W, Hh = 920, 640
    cv = Canvas(W, Hh)
    body = [(10, 16), (W - 12, 10), (W - 8, Hh - 22), (14, Hh - 14)]
    cv.poly(body, "ink", None, 1, dy=10)
    cv.poly(body, "paper", "ink", 5)
    cv.poly(notched(34, 38, W - 34, Hh - 42, 24), (0, 0, 0, 0), "orange_dark", 4)
    cv.poly(notched(46, 50, W - 46, Hh - 54, 18), (0, 0, 0, 0), "cream_dark", 2)
    for x, y in ((34, 38), (W - 34, 38), (34, Hh - 42), (W - 34, Hh - 42)):
        cv.ellipse((x - 14, y - 14, x + 14, y + 14), "orange_dark", "ink", 3)
    cv.poly([(W // 2 - 190, 56), (W // 2 + 190, 56), (W // 2 + 210, 100), (W // 2 + 190, 144), (W // 2 - 190, 144), (W // 2 - 210, 100)],
            "red", "ink", 4)
    # wax seal with ribbon tails at the bottom left
    cv.poly([(110, Hh - 120), (150, Hh - 40), (126, Hh - 52), (110, Hh - 30)], "red_dark", "ink", 3)
    cv.poly([(170, Hh - 120), (130, Hh - 40), (154, Hh - 52), (170, Hh - 30)], "red", "ink", 3)
    cv.ellipse((88, Hh - 190, 192, Hh - 86), "red", "ink", 4)
    cv.ellipse((104, Hh - 174, 176, Hh - 102), "red_dark", "ink", 3)
    star = []
    for i in range(10):
        a = math.radians(-90 + i * 36)
        r = 24 if i % 2 == 0 else 10
        star.append((140 + math.cos(a) * r, Hh - 138 + math.sin(a) * r))
    cv.poly(star, "orange_light")
    cv.out(path)


def record_stamp(path):
    W, Hh = 340, 120
    cv = Canvas(W, Hh)
    cv.poly(notched(8, 8, W - 8, Hh - 8, 14), (0, 0, 0, 0), "red", 8)
    cv.poly(notched(20, 20, W - 20, Hh - 20, 10), (0, 0, 0, 0), "red", 3)
    cv.out(path)


def board(path):
    W, Hh = 1100, 660
    cv = Canvas(W, Hh)
    body = notched(14, 26, W - 14, Hh - 14, 40)
    cv.poly(body, "ink", None, 1, dy=12)
    cv.poly(body, "steel", "ink", 6)
    cv.poly(notched(42, 52, W - 42, Hh - 44, 30), "charcoal", "steel_light", 3)
    cv.masked(notched(42, 52, W - 42, Hh - 44, 30), lambda l: l.poly([(0, 52), (W, 52), (W, 80), (0, 80)], (255, 255, 255, 14)))
    for x, y in ((44, 52), (W - 44, 52), (44, Hh - 44), (W - 44, Hh - 44), (W // 2, 44)):
        cv.rivet(x, y, 9)
    cv.out(path)


def toggle(path, on):
    W, Hh = 170, 84
    cv = Canvas(W, Hh)
    cv.poly(notched(6, 6, W - 6, Hh - 10, 14), "ink", "ink", 3, dy=4)
    cv.poly(notched(6, 6, W - 6, Hh - 10, 14), "teal_dark" if on else "steel_dark", "ink", 4)
    kx = W - 52 if on else 52
    cv.ellipse((kx - 34, 8, kx + 34, 76), "ink")
    cv.ellipse((kx - 32, 6, kx + 32, 70), "cream" if on else "steel_light", "ink", 3)
    cv.ellipse((kx - 18, 18, kx + 18, 54), "orange" if on else "steel", "ink", 2)
    cv.out(path)


def odometer(path):
    W, Hh = 380, 124
    cv = Canvas(W, Hh)
    body = notched(8, 6, W - 8, Hh - 14, 16)
    cv.poly(body, "ink", None, 1, dy=8)
    cv.poly(body, "steel", "ink", 4)
    cv.poly(notched(24, 20, W - 24, Hh - 30, 8), "ink", "steel_light", 2)
    for x in range(52, W - 40, 56):
        cv.line([(x, 24), (x, Hh - 34)], "steel_dark", 2)
    cv.rivet(18, 50, 4)
    cv.rivet(W - 18, 50, 4)
    cv.out(path)


def speedo(path):
    W = 260
    cv = Canvas(W, W)
    cv.ellipse((10, 14, W - 10, W - 6), "ink")
    cv.ellipse((10, 8, W - 10, W - 12), "steel", "ink", 5)
    cv.ellipse((30, 28, W - 30, W - 32), "charcoal", "steel_light", 3)
    for k in range(0, 11):
        a = math.radians(150 + k * 24)
        r1, r2 = 84, 100
        col = "red" if k >= 8 else "cream"
        cv.line([(W / 2 + r1 * math.cos(a), W / 2 - 6 + r1 * math.sin(a)),
                 (W / 2 + r2 * math.cos(a), W / 2 - 6 + r2 * math.sin(a))], col, 4 if k % 5 == 0 else 2)
    cv.out(path)


def needle(path):
    cv = Canvas(24, 120)
    cv.poly([(12, 0), (20, 100), (12, 116), (4, 100)], "red", "ink", 2)
    cv.out(path)


def hub(path):
    cv = Canvas(48, 48)
    cv.ellipse((2, 2, 46, 46), "steel_light", "ink", 4)
    cv.ellipse((14, 14, 34, 34), "steel", "ink", 3)
    cv.out(path)


def heart(path, on):
    W = 80
    cv = Canvas(W, W)
    pts = [(40, 72), (6, 34), (6, 20), (16, 8), (30, 8), (40, 18), (50, 8), (64, 8), (74, 20), (74, 34)]
    cv.poly(pts, "red" if on else "steel_dark", "ink", 4)
    if on:
        cv.poly([(14, 16), (26, 14), (30, 20), (18, 24)], (255, 255, 255, 110))
    else:
        cv.poly([(24, 18), (56, 50)], (0, 0, 0, 0))
    cv.out(path)


def ink_frame(path):
    W, Hh = 640, 72
    cv = Canvas(W, Hh)
    body = notched(6, 6, W - 6, Hh - 12, 14)
    cv.poly(body, "ink", None, 1, dy=6)
    cv.poly(body, "steel", "ink", 4)
    cv.poly(notched(20, 18, W - 20, Hh - 24, 6), "charcoal", "ink", 3)
    for x in range(60, W - 40, 56):
        cv.line([(x, 8), (x, 15)], "cream", 2)
    cv.out(path)


def ink_fill(path):
    cv = Canvas(120, 26)
    cv.poly([(2, 2), (118, 2), (112, 24), (2, 24)], "teal", "teal_dark", 2)
    cv.poly([(4, 4), (116, 4), (114, 9), (4, 9)], (255, 255, 255, 70))
    cv.out(path)


def tab(path, selected):
    W, Hh = 230, 88
    cv = Canvas(W, Hh)
    lift = 0 if selected else 10
    body = [(10, 18 + lift), (28, 6 + lift), (W - 28, 6 + lift), (W - 10, 18 + lift), (W - 10, Hh - 6), (10, Hh - 6)]
    cv.poly(body, "ink", None, 1, dy=5)
    cv.poly(body, "orange" if selected else "steel", "ink", 4)
    cv.poly([(24, 12 + lift), (W - 24, 12 + lift), (W - 26, 20 + lift), (26, 20 + lift)],
            "orange_light" if selected else "steel_light")
    cv.rivet(26, Hh - 24, 4)
    cv.rivet(W - 26, Hh - 24, 4)
    cv.out(path)


def backdrop_variant(path, variant):
    """Garage: clean side walls for plates. Wall: no door opening (certificate and rails)."""
    H.backdrop(path)
    im = Image.open(path).convert("RGBA")
    d = ImageDraw.Draw(im)
    px = im.load()
    wall = H.c("charcoal")
    # repaint both side walls clean (planks only) and keep lamps
    for y in range(40, 148):
        for x in list(range(0, 100)) + list(range(284, H.W)):
            if variant == "wall" and 100 <= x < 284:
                continue
            k = 0.9 + 0.1 * H.hsh(x // 24, 0, 3) + (H.hsh(x, y, 1) - 0.5) * 0.07
            if x % 24 == 0:
                k *= 0.6
            px[x, y] = H.shade(wall, k)
    if variant == "wall":
        for y in range(40, 148):
            for x in range(100, 284):
                k = 0.9 + 0.1 * H.hsh(x // 24, 0, 3) + (H.hsh(x, y, 1) - 0.5) * 0.07
                if x % 24 == 0:
                    k *= 0.6
                px[x, y] = H.shade(wall, k)
        for y in range(40, 56):
            for x in range(98, 286):
                k = 0.9 + 0.1 * H.hsh(x // 24, 0, 3)
                px[x, y] = H.shade(wall, k)
        d.rectangle([98, 54, 285, 150], fill=None)
        for y in range(56, 150):
            for x in range(96, 290):
                k = 0.9 + 0.1 * H.hsh(x // 24, 0, 3) + (H.hsh(x, y, 1) - 0.5) * 0.07
                if x % 24 == 0:
                    k *= 0.6
                px[x, y] = H.shade(wall, k)
        # redraw the floor edge strip across the middle
        for x in range(96, 290):
            d.rectangle([x, 148, x, 151], fill=H.c("orange") if (x // 8) % 2 == 0 else H.c("ink"))
    # small props so the clean walls are not empty: a wall clock and a hanging sign bracket
    d.ellipse([22, 52, 46, 76], fill=H.c("cream"), outline=H.c("ink"))
    d.line([34, 64, 34, 56], fill=H.c("ink"))
    d.line([34, 64, 40, 66], fill=H.c("red"))
    d.rectangle([338, 54, 362, 62], fill=H.c("steel_light"), outline=H.c("ink"))
    d.rectangle([346, 62, 354, 66], fill=H.c("steel_dark"))
    im.save(path)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--output", default="Generated_Screens")
    out = Path(ap.parse_args().output)
    out.mkdir(parents=True, exist_ok=True)
    small_title(out / "UI_Plate_Title_Small.png")
    back_badge(out / "UI_Badge_Hex_Back.png")
    upgrade_plate(out / "UI_Plate_Upgrade.png")
    pip(out / "UI_Pip_On.png", True)
    pip(out / "UI_Pip_Off.png", False)
    name_plate(out / "UI_Plate_Name.png")
    vehicle_card(out / "UI_Card_Vehicle_Plate.png")
    postcard(out / "UI_Card_World_Postcard.png")
    ticket(out / "UI_Card_Road_Ticket.png")
    card_frame(out / "UI_Card_Frame_Vehicle.png", (316, 240))
    card_frame(out / "UI_Card_Frame_World.png", (356, 268))
    card_frame(out / "UI_Card_Frame_Road.png", (316, 224))
    lock_tag(out / "UI_Icon_Lock_Tag.png")
    certificate(out / "UI_Cert_Paper.png")
    record_stamp(out / "UI_Stamp_Record.png")
    board(out / "UI_Panel_Board.png")
    toggle(out / "UI_Toggle_On.png", True)
    toggle(out / "UI_Toggle_Off.png", False)
    odometer(out / "UI_HUD_Odometer.png")
    speedo(out / "UI_HUD_Speedo.png")
    needle(out / "UI_HUD_Needle.png")
    hub(out / "UI_HUD_Hub.png")
    heart(out / "UI_HUD_Heart_On.png", True)
    heart(out / "UI_HUD_Heart_Off.png", False)
    ink_frame(out / "UI_HUD_Ink_Frame.png")
    ink_fill(out / "UI_HUD_Ink_Fill.png")
    tab(out / "UI_Tab_Metal_Normal.png", False)
    tab(out / "UI_Tab_Metal_Selected.png", True)
    backdrop_variant(out / "UI_Background_Workshop_Garage.png", "garage")
    backdrop_variant(out / "UI_Background_Workshop_Wall.png", "wall")
    print("written", len(list(out.glob("*.png"))), "files to", out.resolve())


if __name__ == "__main__":
    main()

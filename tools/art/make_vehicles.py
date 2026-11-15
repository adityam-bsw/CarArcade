#!/usr/bin/env python3
"""Smooth versions of the three slice vehicles (ClassicRed, SportCyan, MonsterTruck): body + wheel as separate PNGs.
Own code, procedural, no generative AI, no downloads. Python 3.9 standard library only.

    python3 tools/art/make_vehicles.py --output <dir> [--only classic,sport,monster,wheels]

Canvas sizes, pivot (centre) and silhouette extents follow the sprites the game really loads (Resources/RoadBuilder/CarBody,
SportCarBody, MonsterTruckBody, Wheel), because RoadCar.cs stretches each body to a fixed world box (2.4x0.9, 2.4x0.8, 2.6x1.1
units) and fits the wheel to 2*radius*1.03. Outline: 0.04 world units from the DISPLAY density (about 8 px on the bodies).
Wheel arches are centred on the physics wheel anchors (back/front at 0.2/0.8 of the width) so the drawn wheel sits in them.
"""
import argparse, math, os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from smoothart import *

def T(*hexes):
    return tuple(hexrgb(h) for h in hexes)

# ------------------------------------------------------------------ wheels (160x160, circle centred, shown at 0.84*1.03 units)
def wheel(kind, path):
    S = 160; ppu = S / (0.84 * 1.03)
    sp = Sprite(S, S, ppu); c = S / 2.0
    R = 76 - sp.outline_px            # silhouette radius; outline brings it to 76 (4 px bleed margin)
    P = PAL
    if kind == "monster":
        core = R - 11                  # lugs stand 11 px out of the carcass
        lugs = []
        n = 16
        def sil(m):
            m.circle(c, c, core)
            for i in range(n):
                a = 2 * math.pi * (i + 0.5) / n
                pts = []
                w0, w1 = 0.115, 0.085      # lug half-angle at root and tip (trapezoid block)
                for (rad, w) in ((core - 4, w0), (R, w1)):
                    pass
                a0, a1 = a - 0.115, a + 0.115; b0, b1 = a - 0.078, a + 0.078
                m.polygon(round_poly([(c + (core - 4) * math.cos(a0), c + (core - 4) * math.sin(a0), 0), (c + R * math.cos(b0), c + R * math.sin(b0), 5),
                                      (c + R * math.cos(b1), c + R * math.sin(b1), 5), (c + (core - 4) * math.cos(a1), c + (core - 4) * math.sin(a1), 0)], 5))
        tyre = sp.shape(sil)
        sp.cel(tyre, T(P["TyreLight"], P["TyreMid"], P["TyreDeep"]), direction=(c - R * 0.7, c - R * 0.7, c + R * 0.9, c + R * 0.9), soft=0.25)
        sp.rim(tyre.cov(), P["TyreLight"], 3)
        # inner tread shadow ring between lugs, sidewall bead
        for rr, col, op in ((core - 2, P["TyreDeep"], 0.55),):
            ring = Mask(S, S); ring.circle(c, c, rr); inner = Mask(S, S); inner.circle(c, c, rr - 4)
            sp.paint(ring.cov().minus(inner.cov()), hexrgb(col), op)
        rimR = 40
        rim = sp.shape(lambda m: m.circle(c, c, rimR), in_silhouette=False)
        sp.cel(rim, T("#FFC066", "#F28C1E", "#B24E12"), direction=(c - rimR, c - rimR, c + rimR, c + rimR), rim=3)
        dish = Mask(S, S); dish.circle(c, c, 29)
        sp.cel(dish, T("#B24E12", "#8C3A0C", "#5A220A"), direction=(c - 29, c - 29, c + 29, c + 29), soft=0.3)
        for i in range(6):                       # beadlock bolts: one is bright (rotation marker)
            a = 2 * math.pi * i / 6
            b = Mask(S, S); b.circle(c + 34.5 * math.cos(a), c + 34.5 * math.sin(a), 3.6 if i else 4.6)
            sp.paint(b, hexrgb("#FFF6E0") if i == 0 else hexrgb("#3B2410"))
        for i in range(5):
            a = 2 * math.pi * i / 5 + 0.3
            sp.line([(c + 8 * math.cos(a), c + 8 * math.sin(a)), (c + 25 * math.cos(a), c + 25 * math.sin(a))], 4.6, "#FFC066")
        hub = Mask(S, S); hub.circle(c, c, 11)
        sp.cel(hub, T("#E7E0CF", "#9AA3AD", "#59636D"), rim=2)
    else:
        sport = kind == "sport"
        def sil(m): m.circle(c, c, R)
        tyre = sp.shape(sil)
        sp.cel(tyre, T(P["TyreLight"], P["TyreMid"], P["TyreDeep"]), direction=(c - R * 0.7, c - R * 0.7, c + R * 0.9, c + R * 0.9), soft=0.25)
        sp.rim(tyre.cov(), P["TyreLight"], 3)
        n = 22 if sport else 18                 # tread blocks: notches around the outer band (show the spin)
        for i in range(n):
            a = 2 * math.pi * (i + 0.5) / n
            sp.line([(c + (R - 12) * math.cos(a), c + (R - 12) * math.sin(a)), (c + (R - 2.5) * math.cos(a), c + (R - 2.5) * math.sin(a))], 2.6, P["TyreDeep"])
        # sidewall: a lighter ring so the tyre reads as a tyre, then the rim
        rimR = 49 if sport else 42
        side = Mask(S, S); side.circle(c, c, rimR + 7); sin2 = Mask(S, S); sin2.circle(c, c, rimR + 3)
        sp.paint(side.cov().minus(sin2.cov()), hexrgb(P["TyreLight"]), 0.55)
        rim = sp.shape(lambda m: m.circle(c, c, rimR), in_silhouette=False)
        if sport:
            sp.cel(rim, T("#E8F7FF", "#B7C4D0", "#6F7E8C"), direction=(c - rimR, c - rimR, c + rimR, c + rimR), rim=3)
        else:
            sp.cel(rim, T("#C9D2DA", "#8A939B", "#59636D"), direction=(c - rimR, c - rimR, c + rimR, c + rimR), rim=3)
        dr = rimR - 9
        dish = Mask(S, S); dish.circle(c, c, dr)
        sp.cel(dish, T("#59636D", "#3B434D", "#252B36"), direction=(c - dr, c - dr, c + dr, c + dr), soft=0.3)
        ns = 6 if sport else 5; sw = 3.6 if sport else 5.4
        for i in range(ns):
            a = 2 * math.pi * i / ns - math.pi / 2 * (0 if sport else 1) + (0.2 if sport else 0)
            pts = [(c + 7 * math.cos(a), c + 7 * math.sin(a)), (c + (rimR - 9) * math.cos(a), c + (rimR - 9) * math.sin(a))]
            sp.line(pts, sw + 1.2, "#252B36"); sp.line(pts, sw, "#E7EDF2" if sport else "#C9D2DA")
        hub = Mask(S, S); hub.circle(c, c, 11)
        hubc = ("#8CE6FF", "#18B8E8", "#0C6A9A") if sport else ("#F27A6E", "#D8323C", "#7E1F2E")
        sp.cel(hub, T(*hubc), rim=2)
        mk = Mask(S, S); mk.circle(c + rimR * 0.72, c, 4.4)        # single bright bolt: rotation marker
        sp.paint(mk, hexrgb("#FFF6E0"))
        mo = Mask(S, S); mo.circle(c + rimR * 0.72, c, 4.4)
    sp.finish(path)
    return sp

# ------------------------------------------------------------------ shared body pieces
def pane(sp, pts, body_deep, clip=None, streak=None):
    """Window: dark body-shade frame line (0.02 units), cool vertical gradient glass, one soft diagonal streak."""
    m = sp.mask(); m.polygon(round_poly(pts, 6))
    frame = m.dilated(4.5).minus(m)
    sp.paint(frame, hexrgb(body_deep))
    cov = m.cov(); y0, y1 = cov.bb[1], cov.bb[3]
    sp.img.paint(cov, vgrad(y0, y1, Ramp([(0, "#E4F6FF"), (0.45, PAL["GlassSky"]), (1, PAL["GlassSkyDeep"])])))
    if streak:
        sp.line(streak, 4.0, "#FFFFFF", clip=cov, opacity=0.55)
    return cov

def arch(sp, cx, cy, r, envelope, body_deep):
    """Wheel arch: charcoal liner behind the drawn wheel with a darker body-colour lip, clipped to the body envelope."""
    env = envelope.cov()
    out = sp.mask(); out.circle(cx, cy, r + 7); mid = sp.mask(); mid.circle(cx, cy, r)
    sp.paint(out.cov().minus(mid.cov()), hexrgb(body_deep), clip=env)
    sp.paint(mid, hexrgb("#1E242C"), clip=env)
    inner = sp.mask(); inner.circle(cx, cy, r - 10)
    sp.paint(mid.cov().minus(inner.cov()), hexrgb("#2C333D"), 0.8, clip=env)

def lamp(sp, x0, y0, x1, y1, rad, cols):
    m = sp.mask(); m.rrect(x0, y0, x1, y1, rad)
    sp.paint(m.dilated(3).minus(m), INK)
    sp.cel(m, T(*cols), direction=(x0, y0, x1, y1), rim=0)
    return m

def head(sp, cx, cy, cap):
    h = sp.mask(); h.circle(cx, cy, 15)
    sp.paint(h.dilated(3.5).minus(h), INK)
    sp.cel(h, T("#FFE0C0", "#F7C09A", "#D08C6A"))
    capm = sp.mask(); capm.polygon(round_poly([(cx - 17, cy - 3, 0), (cx - 11, cy - 17, 7), (cx + 10, cy - 18, 7), (cx + 17, cy - 3, 0), (cx + 30, cy - 3, 3), (cx + 30, cy + 1, 3), (cx + 6, cy + 1, 0), (cx - 17, cy + 1, 0)], 5))
    sp.paint(capm.dilated(3).minus(capm), INK)
    sp.cel(capm, T(*cap))
    eye = sp.mask(); eye.circle(cx + 7, cy + 4, 2.6); sp.paint(eye, INK)

def classic(path):
    W, H = 480, 180
    sp = Sprite(W, H, 200)
    red = ("#F27A6E", PAL["CarRed"], PAL["CarRedDeep"])
    prof = [(17, 147, 14), (15, 100, 18), (28, 90, 10), (126, 86, 10), (190, 22, 26), (326, 20, 26), (392, 84, 14), (440, 92, 26), (464, 112, 22), (462, 147, 14)]
    body = sp.shape(lambda m: m.polygon(round_poly(prof, 9)))
    bc = sp.cel(body, T(*red), direction=(150, 14, 260, 147), soft=0.16, split=(0.45, 0.86), rim=3.5)
    # sill band (darker) and racing-cream belt stripe, both clipped to the body
    sill = sp.mask(); sill.rrect(0, 130, W, 160, 0)
    sp.paint(sill, hexrgb(PAL["CarRedDeep"]), 0.55, clip=bc)
    sp.line([(34, 112), (446, 112)], 4.5, "#FFF1D6", clip=bc)
    # door seams and handle (inner lines, darker shade of the body)
    for x in (140, 262, 372):
        sp.line([(x, 90), (x, 134)], 1.8, PAL["CarRedDeep"], clip=bc, opacity=0.9)
    sp.line([(228, 100), (250, 100)], 4.5, "#8E2A2A", clip=bc); sp.line([(228, 99), (250, 99)], 2.4, "#FFF1D6", clip=bc)
    pane(sp, [(136, 80, 0), (196, 34, 6), (250, 34, 6), (250, 80, 0)], PAL["CarRedDeep"], streak=[(168, 74), (200, 42)])
    pane(sp, [(262, 80, 0), (262, 34, 0), (322, 34, 8), (378, 80, 0)], PAL["CarRedDeep"], streak=[(286, 72), (318, 42)])
    head(sp, 296, 62, ("#FFE27A", "#F6C945", "#B88A1E"))
    arch(sp, 96, 174, 88, body, PAL["CarRedDeep"]); arch(sp, 384, 174, 88, body, PAL["CarRedDeep"])
    # bumpers: steel, front and rear; lamps
    for (x0, x1) in ((15, 38), (446, 466)):
        pass
    lamp(sp, 442, 90, 462, 104, 5, ("#FFFBE0", "#FFE27A", "#E9A92B"))
    lamp(sp, 18, 98, 34, 112, 4, ("#FF9A8A", "#E8434A", "#7E1F2E"))
    sp.gloss(bc, 250, 30, 55, 9, 0.35, clip=bc)
    sp.finish(path); return sp

def sport(path):
    W, H = 480, 160
    sp = Sprite(W, H, 200)
    cy = ("#8CE6FF", PAL["CarCyan"], PAL["CarCyanDeep"])
    prof = [(17, 135, 14), (15, 98, 16), (36, 88, 12), (146, 82, 8), (216, 28, 26), (320, 30, 22), (402, 78, 16), (450, 88, 26), (466, 106, 20), (462, 135, 14)]
    # spoiler: pylon + wing (drawn first so the body sits in front of the pylon base)
    pylon = sp.shape(lambda m: m.polygon([(44, 90), (44, 52), (60, 52), (60, 90)]))
    wing = sp.shape(lambda m: m.rrect(20, 34, 96, 52, 7))
    sp.cel(pylon, T("#59636D", "#3B434D", "#252B36"), direction=(44, 52, 60, 90))
    sp.cel(wing, T("#6B7885", "#3B434D", "#252B36"), direction=(20, 34, 40, 52), rim=2.5)
    sp.line([(32, 43), (84, 43)], 2.6, PAL["CarCyan"], clip=wing)
    body = sp.shape(lambda m: m.polygon(round_poly(prof, 9)))
    bc = sp.cel(body, T(*cy), direction=(150, 26, 260, 135), soft=0.16, split=(0.45, 0.86), rim=3.5)
    sill = sp.mask(); sill.rrect(0, 118, W, 150, 0)
    sp.paint(sill, hexrgb(PAL["CarCyanDeep"]), 0.55, clip=bc)
    sp.line([(36, 100), (450, 100)], 4.0, "#FFFFFF", clip=bc, opacity=0.95)       # racing stripe
    sp.line([(36, 109), (450, 109)], 2.2, "#FFFFFF", clip=bc, opacity=0.8)
    for x in (270, ):
        sp.line([(x, 84), (x, 124)], 1.8, PAL["CarCyanDeep"], clip=bc, opacity=0.9)
    sp.line([(300, 92), (320, 92)], 4.0, "#0C6A9A", clip=bc)
    pane(sp, [(152, 76, 0), (220, 38, 8), (258, 38, 0), (258, 76, 0)], PAL["CarCyanDeep"], streak=[(180, 70), (214, 46)])
    pane(sp, [(268, 76, 0), (268, 38, 0), (312, 38, 8), (378, 76, 0)], PAL["CarCyanDeep"], streak=[(292, 70), (316, 46)])
    head(sp, 316, 62, ("#FF8A7A", "#E8434A", "#7E1F2E"))
    arch(sp, 100, 156, 88, body, PAL["CarCyanDeep"]); arch(sp, 380, 156, 88, body, PAL["CarCyanDeep"])
    lamp(sp, 438, 92, 462, 100, 4, ("#FFFBE0", "#FFE27A", "#E9A92B"))
    lamp(sp, 18, 98, 32, 112, 4, ("#FF9A8A", "#E8434A", "#7E1F2E"))
    sp.gloss(bc, 300, 40, 55, 7, 0.35, clip=bc)
    sp.finish(path); return sp

def monster(path):
    W, H = 480, 200
    sp = Sprite(W, H, 184)
    og = ("#FFC066", PAL["CarOrange"], PAL["CarOrangeDeep"])
    # exhaust stacks behind the cab (steel pipes, a distinctive tall rear silhouette)
    for x in (146, 168):
        st = sp.shape(lambda m, x=x: m.rrect(x - 8, 28, x + 8, 92, 8))
        sp.cel(st, T("#E7EDF2", "#8A939B", "#3B434D"), direction=(x - 8, 28, x + 8, 28), soft=0.25, rim=0)
        cap = sp.mask(); cap.ellipse(x, 30, 8, 5); sp.paint(cap, hexrgb("#252B36"))
    prof = [(30, 150, 14), (25, 90, 16), (36, 84, 10), (176, 82, 8), (196, 40, 14), (336, 36, 14), (392, 80, 14), (438, 86, 22), (456, 104, 18), (454, 150, 14)]
    body = sp.shape(lambda m: m.polygon(round_poly(prof, 9)))
    bc = sp.cel(body, T(*og), direction=(150, 36, 250, 150), soft=0.16, split=(0.45, 0.86), rim=3.5)
    sill = sp.mask(); sill.rrect(0, 128, W, 170, 0)
    sp.paint(sill, hexrgb("#3B434D"), 0.9, clip=bc)                                 # chassis rail
    sp.paint(sill.dilated(0).minus(sill), hexrgb("#3B434D"), 0.0)
    flame = sp.mask(); flame.polygon([(214, 122), (440, 100), (440, 112), (330, 128), (214, 128)])
    sp.paint(flame, hexrgb(PAL["CarOrangeDeep"]), 0.85, clip=bc)
    sp.line([(222, 118), (420, 102)], 2.6, "#FFF1D6", clip=bc, opacity=0.9)
    for x in (270, 386):
        sp.line([(x, 90), (x, 124)], 1.8, PAL["CarOrangeDeep"], clip=bc, opacity=0.9)
    sp.line([(280, 92), (300, 92)], 4.5, "#B24E12", clip=bc)
    sp.line([(36, 100), (170, 100)], 2.0, PAL["CarOrangeDeep"], clip=bc, opacity=0.9)  # bed panel line
    pane(sp, [(208, 78, 0), (214, 48, 8), (268, 48, 0), (268, 78, 0)], PAL["CarOrangeDeep"], streak=[(224, 72), (244, 54)])
    pane(sp, [(278, 78, 0), (278, 48, 0), (326, 48, 8), (378, 78, 0)], PAL["CarOrangeDeep"], streak=[(296, 72), (318, 54)])
    head(sp, 312, 66, ("#8CE6FF", "#18B8E8", "#0C6A9A"))
    arch(sp, 96, 187, 100, body, PAL["CarOrangeDeep"]); arch(sp, 384, 187, 100, body, PAL["CarOrangeDeep"])
    # roof light bar with three warm lamps; bull bar at the nose
    bar = sp.shape(lambda m: m.rrect(224, 22, 306, 38, 6))
    sp.cel(bar, T("#6B7885", "#3B434D", "#252B36"), direction=(224, 22, 224, 38), rim=0)
    for x in (238, 265, 292):
        lamp(sp, x - 10, 10, x + 10, 26, 8, ("#FFFBE0", "#FFE27A", "#E9A92B"))
    bull = sp.shape(lambda m: m.rrect(436, 108, 462, 150, 8))
    sp.cel(bull, T("#C9D2DA", "#8A939B", "#3B434D"), direction=(436, 108, 462, 150), rim=0)
    lamp(sp, 432, 92, 452, 106, 5, ("#FFFBE0", "#FFE27A", "#E9A92B"))
    sp.gloss(bc, 300, 46, 60, 7, 0.35, clip=bc)
    sp.finish(path); return sp

if __name__ == "__main__":
    ap = argparse.ArgumentParser(); ap.add_argument("--output", required=True); ap.add_argument("--only", default="classic,sport,monster,wheels")
    a = ap.parse_args(); os.makedirs(a.output, exist_ok=True); only = a.only.split(",")
    if "wheels" in only:
        for k in ("classic", "sport", "monster"):
            wheel(k, os.path.join(a.output, "wheel_%s.png" % k)); print("wheel", k)
    for k, fn in (("classic", classic), ("sport", sport), ("monster", monster)):
        if k in only:
            fn(os.path.join(a.output, "body_%s.png" % k)); print("body", k)

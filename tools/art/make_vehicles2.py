#!/usr/bin/env python3
"""Smooth restyle of the CATALOGUE (garage) vehicles. Own code, procedural, no generative AI, no downloads. Python 3.9 stdlib only.

    python3 tools/art/make_vehicles2.py --output <dir> [--only vehicle-police-car,vehicle-car]

For every vehicle it writes   bodies/<id>.png   (body only, wheel arches empty: the game draws the spinning wheels on top)
                              previews/<id>.png (body + wheels, for the catalogue card and as a drop-in for the old preview)
                              wheels/<id>_w<i>.png (round wheel cut-outs, size = old size * S)
Canvas = old canvas * S (S integer per vehicle, see vehicles2_inventory.json), wheel anchors = old anchors * S, silhouette
(outline included) fills the old content box. Outline 0.04 units from the display density (about 5 px), cel3 shading, 4 px bleed.
Designs are in OLD pixel units (floats) on the old canvas, so a design can be checked against the old picture directly.
"""
import argparse, os, sys, math
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from veh2lib import *

DESIGNS = {}
def design(vid, wheel_kind="car", accent="#D8323C", **wkw):
    def deco(fn):
        DESIGNS[vid] = (fn, wheel_kind, accent, wkw)
        return fn
    return deco

def window_cols(): return ("#E4F6FF", PAL["GlassSky"], PAL["GlassSkyDeep"])

def sedan_profile(rear, deck, cab_r0, roof_r, roof_f, cab_f0, roof_y, hood_y, nose_y, bot, front=None, rear_bot=None):
    """Old-px profile of a three-box car. rear/front are x extremes (1 and W-1)."""
    x1 = front
    return [(rear, bot, 9), (rear, deck, 14), (cab_r0, deck - 0.2, 6), (roof_r, roof_y, 12), (roof_f, roof_y, 12), (cab_f0, hood_y - 0.2, 6),
            (x1, hood_y + 0.25, 20), (x1, nose_y + 1.2, 12), (x1, bot, 9)]

# ================================================================== police car (35 x 16)
@design("vehicle-police-car", accent="#2A6CD8")
def police(c):
    W = c.W
    prof = sedan_profile(1.0, 8.9, 7.6, 11.0, 19.0, 23.6, 3.0, 8.2, 9.4, 14.55, front=34.0)
    cov, body = c.body(prof, T("#FFFFFF", "#E6ECF2", "#A9B6C6"), rim=3.5, split=(0.55, 0.93))
    # black door/quarter livery, a blue stripe along the belt
    low = c.sp.mask(); low.polygon([(c.X(0), c.Y(11.4)), (c.X(40), c.Y(11.4)), (c.X(40), c.Y(16)), (c.X(0), c.Y(16))])
    c.sp.paint(low, hexrgb("#2B3340"), 1.0, clip=cov)
    door = c.sp.mask(); door.polygon(c.poly([(8.2, 8.6, 3), (21.6, 8.6, 3), (21.6, 11.4, 0), (8.2, 11.4, 0)]))
    c.sp.paint(door, hexrgb("#2B3340"), 1.0, clip=cov)
    c.line([(1.2, 11.4), (33.8, 11.4)], 4.0, "#2A6CD8", clip=cov)
    for x in (8.2, 15.0, 21.6):
        c.line([(x, 8.7), (x, 11.2)], 1.6, "#FFFFFF", clip=cov, opacity=0.45)
    c.pane([(8.6, 7.6, 0), (11.4, 3.9, 4), (14.7, 3.9, 0), (14.7, 7.6, 0)], "#9AA9BA", streak=[(9.8, 7.0), (11.9, 4.6)])
    c.pane([(15.6, 7.6, 0), (15.6, 3.9, 0), (18.6, 3.9, 4), (22.6, 7.6, 0)], "#9AA9BA", streak=[(16.5, 7.0), (18.0, 4.6)])
    # light bar: steel base, red and blue domes
    c.rect(12.3, 1.0, 18.7, 3.0, T("#C9D2DA", "#8A939B", "#3B434D"), rad=4, ink=0)
    c.lamp(12.6, 1.0, 15.3, 2.7, ("#FF9A8A", "#E8434A", "#8E2A2A"), rad=6)
    c.lamp(15.7, 1.0, 18.4, 2.7, ("#9CC8FF", "#2A6CD8", "#1A3F8A"), rad=6)
    c.arches(cov, "#9AA9BA")
    c.lamp(32.7, 8.7, 33.95, 10.1, ("#FFFBE0", "#FFE27A", "#E9A92B"), rad=5)
    c.lamp(1.05, 8.9, 2.2, 10.2, ("#FF9A8A", "#E8434A", "#7E1F2E"), rad=4)
    c.rect(32.8, 11.2, 34.0, 13.4, T("#6B7885", "#3B434D", "#252B36"), rad=4)         # push bar
    c.gloss(cov, 15, 3.6, 22, 3, 0.35)

# ================================================================== car, green hatch (31 x 15)
@design("vehicle-car", accent="#2F8F4A")
def car(c):
    prof = sedan_profile(1.0, 8.0, 6.4, 9.2, 17.2, 21.0, 2.0, 7.6, 8.8, 13.55, front=30.0)
    cov, body = c.body(prof, T("#8DE08A", "#3FAE5A", "#1F6B3A"), rim=3.5)
    c.line([(1.2, 10.5), (29.8, 10.5)], 3.2, "#E7F6D8", clip=cov)
    low = c.sp.mask(); low.polygon([(c.X(0), c.Y(11.6)), (c.X(40), c.Y(11.6)), (c.X(40), c.Y(16)), (c.X(0), c.Y(16))])
    c.sp.paint(low, hexrgb("#1F6B3A"), 0.55, clip=cov)
    for x in (13.5, 19.0):
        c.line([(x, 7.8), (x, 11.0)], 1.5, "#1F6B3A", clip=cov, opacity=0.8)
    c.pane([(7.0, 7.1, 0), (9.6, 3.2, 4), (13.0, 3.2, 0), (13.0, 7.1, 0)], "#1F6B3A", streak=[(8.3, 6.5), (10.2, 3.9)])
    c.pane([(14.0, 7.1, 0), (14.0, 3.2, 0), (16.8, 3.2, 4), (20.2, 7.1, 0)], "#1F6B3A", streak=[(14.8, 6.5), (16.2, 3.9)])
    c.arches(cov, "#1F6B3A")
    c.lamp(28.6, 8.4, 29.9, 9.8, ("#FFFBE0", "#FFE27A", "#E9A92B"), rad=5)
    c.lamp(1.05, 8.4, 2.2, 9.8, ("#FF9A8A", "#E8434A", "#7E1F2E"), rad=4)
    c.gloss(cov, 13, 3.0, 18, 3, 0.35)

# ================================================================== sports car (35 x 14)
@design("vehicle-sports-car", wheel_kind="sport", accent="#A877E8")
def sports(c):
    prof = [(1.0, 12.55, 9), (1.0, 6.6, 12), (6.5, 6.0, 8), (11.6, 2.4, 14), (18.8, 2.4, 14), (23.8, 6.3, 8),
            (33.0, 7.2, 20), (34.0, 8.6, 12), (34.0, 12.55, 9)]
    cov, body = c.body(prof, T("#C79CFF", "#7B3FC4", "#4A1F86"), rim=3.5)
    c.line([(1.2, 9.0), (33.8, 9.0)], 3.0, "#F4ECFF", clip=cov, opacity=0.95)
    c.line([(1.2, 10.0), (33.8, 10.0)], 1.5, "#F4ECFF", clip=cov, opacity=0.8)
    low = c.sp.mask(); low.polygon([(c.X(0), c.Y(10.6)), (c.X(40), c.Y(10.6)), (c.X(40), c.Y(16)), (c.X(0), c.Y(16))])
    c.sp.paint(low, hexrgb("#4A1F86"), 0.55, clip=cov)
    c.pane([(8.5, 5.9, 0), (11.9, 3.0, 6), (15.2, 3.0, 0), (15.2, 5.9, 0)], "#4A1F86", streak=[(9.9, 5.4), (12.0, 3.5)])
    c.pane([(16.1, 5.9, 0), (16.1, 3.0, 0), (18.5, 3.0, 6), (22.4, 5.9, 0)], "#4A1F86", streak=[(16.8, 5.4), (18.0, 3.5)])
    # rear wing: pylon and blade (drawn into the silhouette)
    c.rect(2.6, 4.0, 3.6, 6.6, T("#59636D", "#3B434D", "#252B36"), rad=1)
    c.rect(1.0, 3.3, 6.2, 4.6, T("#6B7885", "#3B434D", "#252B36"), rad=3)
    c.arches(cov, "#4A1F86")
    c.lamp(31.8, 7.5, 33.9, 8.4, ("#FFFBE0", "#FFE27A", "#E9A92B"), rad=4)
    c.lamp(1.05, 6.9, 2.0, 7.9, ("#FF9A8A", "#E8434A", "#7E1F2E"), rad=3)
    c.gloss(cov, 17, 3.2, 20, 2.5, 0.35)

# ================================================================== monster truck (40 x 30)
@design("vehicle-monster-truck", wheel_kind="lug", accent="#F28C1E")
def monster(c):
    # frame rails + axles are drawn first; a tall cab body sits on top; the huge tyres are the wheels (drawn separately)
    rail = c.rect(1.0, 15.6, 39.0, 17.2, T("#6B7885", "#3B434D", "#252B36"), rad=4)
    for x in (8.5, 25.5):
        c.rect(x - 0.8, 14.6, x + 0.8, 21.0, T("#8A939B", "#59636D", "#3B434D"), rad=3)
    prof = [(2.0, 15.8, 8), (1.0, 8.2, 12), (3.6, 7.2, 8), (10.0, 6.8, 8), (13.0, 3.8, 10), (22.0, 2.6, 10), (26.0, 7.0, 6),
            (36.0, 7.6, 18), (39.0, 9.4, 12), (38.6, 15.8, 8)]
    cov, body = c.body(prof, T("#FFC066", "#F28C1E", "#B24E12"), rim=3.5)
    c.pane([(13.6, 7.0, 0), (14.2, 4.3, 4), (18.5, 4.3, 0), (18.5, 7.0, 0)], "#B24E12", streak=[(15.0, 6.5), (16.4, 4.8)])
    c.pane([(19.4, 7.0, 0), (19.4, 4.3, 0), (22.0, 4.0, 4), (24.8, 7.0, 0)], "#B24E12", streak=[(20.2, 6.5), (21.4, 4.8)])
    flame = c.sp.mask(); flame.polygon([(c.X(14), c.Y(14.6)), (c.X(38), c.Y(11.0)), (c.X(38), c.Y(12.6)), (c.X(22), c.Y(14.8))])
    c.sp.paint(flame, hexrgb("#B24E12"), 0.9, clip=cov)
    c.line([(15, 13.9), (36, 11.0)], 1.6, "#FFF1D6", clip=cov, opacity=0.9)
    c.line([(14, 8.2), (26, 8.2)], 1.2, "#B24E12", clip=cov, opacity=0.9)
    c.arches(cov, "#B24E12", extra=0.5)
    c.lamp(37.2, 8.8, 38.9, 10.4, ("#FFFBE0", "#FFE27A", "#E9A92B"), rad=5)
    c.lamp(1.05, 8.9, 2.4, 10.2, ("#FF9A8A", "#E8434A", "#7E1F2E"), rad=3)
    # roof light bar
    c.rect(15.0, 1.0, 22.0, 2.6, T("#6B7885", "#3B434D", "#252B36"), rad=3)
    for x in (16.2, 18.4, 20.6):
        c.lamp(x - 0.7, 1.0, x + 0.7, 2.4, ("#FFFBE0", "#FFE27A", "#E9A92B"), rad=4)
    c.gloss(cov, 18, 4.2, 12, 2, 0.35)


def sill(c, cov, y, color, op=0.55):
    m = c.sp.mask(); m.polygon([(c.X(-5), c.Y(y)), (c.X(80), c.Y(y)), (c.X(80), c.Y(60)), (c.X(-5), c.Y(60))])
    c.sp.paint(m, hexrgb(color), op, clip=cov)

def band(c, cov, y0, y1, color, op=1.0):
    m = c.sp.mask(); m.polygon([(c.X(-5), c.Y(y0)), (c.X(120), c.Y(y0)), (c.X(120), c.Y(y1)), (c.X(-5), c.Y(y1))])
    c.sp.paint(m, hexrgb(color), op, clip=cov)

HEAD = ("#FFFBE0", "#FFE27A", "#E9A92B"); TAIL = ("#FF9A8A", "#E8434A", "#7E1F2E")
STEEL = T("#C9D2DA", "#8A939B", "#3B434D"); DSTEEL = T("#6B7885", "#3B434D", "#252B36")

# ================================================================== taxi (35 x 16)
@design("vehicle-taxi", accent="#252B36")
def taxi(c):
    prof = sedan_profile(1.0, 9.3, 7.6, 10.6, 20.0, 24.2, 3.6, 8.9, 9.7, 14.6, front=34.0)
    cov, body = c.body(prof, T("#FFE88A", "#F6C24A", "#B8841E"), rim=3.5, split=(0.5, 0.9))
    sill(c, cov, 12.0, "#B8841E", 0.45)
    # checker band on the belt
    for i in range(34):
        x = 1.0 + i * 1.0
        if i % 2 == 0: c.line([(x, 10.7), (x + 1.0, 10.7)], 2.2, "#252B36", clip=cov)
        else: c.line([(x, 10.7), (x + 1.0, 10.7)], 2.2, "#FFFFFF", clip=cov)
    for x in (13.2, 19.8):
        c.line([(x, 8.7), (x, 12.2)], 1.5, "#B8841E", clip=cov, opacity=0.85)
    c.pane([(8.4, 8.2, 0), (11.2, 4.4, 4), (14.4, 4.4, 0), (14.4, 8.2, 0)], "#B8841E", streak=[(9.6, 7.6), (11.8, 5.0)])
    c.pane([(15.4, 8.2, 0), (15.4, 4.4, 0), (19.2, 4.4, 4), (23.0, 8.2, 0)], "#B8841E", streak=[(16.2, 7.6), (17.8, 5.0)])
    c.rect(12.6, 1.0, 17.8, 3.7, T("#FFFFFF", "#F4F1E4", "#B8B2A0"), rad=3)          # roof sign, no text
    c.rect(13.2, 1.7, 17.2, 3.0, T("#FFE27A", "#F6C945", "#B88A1E"), rad=2)
    c.arches(cov, "#B8841E")
    c.lamp(32.7, 8.8, 33.95, 10.2, HEAD, rad=5); c.lamp(1.05, 9.0, 2.2, 10.3, TAIL, rad=4)
    c.gloss(cov, 16, 3.9, 20, 3, 0.35)

# ================================================================== race car (35 x 11)
@design("vehicle-race-car", wheel_kind="sport", accent="#FFFFFF")
def race(c):
    prof = [(7.5, 8.0, 6), (9.0, 4.6, 8), (13.0, 4.0, 8), (16.0, 2.3, 10), (19.0, 4.4, 8), (26.0, 5.8, 8), (34.0, 7.6, 3), (34.0, 8.9, 3), (27.0, 9.2, 6), (7.5, 9.2, 4)]
    cov, body = c.body(prof, T("#8EB6FF", "#2F6FDB", "#173F8A"), rim=3.0)
    c.rect(1.0, 1.0, 5.4, 2.6, T("#C9D2DA", "#59636D", "#252B36"), rad=2)              # rear wing
    c.rect(2.4, 2.4, 3.6, 7.0, T("#6B7885", "#3B434D", "#252B36"), rad=1)
    c.rect(1.0, 6.7, 6.4, 7.8, DSTEEL, rad=2)
    c.rect(27.5, 9.0, 34.0, 10.0, T("#C9D2DA", "#59636D", "#252B36"), rad=2)             # front wing
    c.disc(15.2, 4.0, 1.5 * c.S / 2, T("#FFF6E0", "#F2C94C", "#B88A1E"), ink=1.8)          # helmet (gold-ish)
    c.line([(10.0, 6.4), (30.0, 7.6)], 1.8, "#FFFFFF", clip=cov, opacity=0.95)
    c.disc(19.5, 6.6, 1.5 * c.S / 2, T("#FFFFFF", "#E8EEF4", "#A0AEBE"), ink=0, rim=0)
    sill(c, cov, 8.2, "#173F8A", 0.5)
    c.lamp(33.0, 7.9, 33.95, 8.7, HEAD, rad=3)
    c.arches(cov, "#173F8A", extra=0.5)

# ================================================================== dune buggy (25 x 16)
@design("vehicle-dune-buggy", wheel_kind="lug", accent="#FFC066")
def buggy(c):
    prof = [(1.0, 13.0, 6), (1.0, 9.5, 8), (8.0, 9.0, 6), (15.0, 8.8, 6), (19.0, 8.0, 10), (23.5, 9.2, 8), (24.0, 10.6, 8), (24.0, 13.0, 6)]
    # cage first (behind), then the tub in front
    for pts in ([(4.0, 9.2), (4.5, 3.0), (11.0, 1.6), (14.5, 9.0)], [(4.5, 3.0), (3.0, 9.3)], [(8.0, 2.3), (9.2, 9.0)]):
        c.tube(pts, 0.85, T("#DDE6EE", "#8A939B", "#3B434D"))
    c.rect(6.0, 5.0, 11.5, 9.4, T("#4A5560", "#2B333D", "#171C22"), rad=5)                 # seat shell
    cov, body = c.body(prof, T("#8CCBFF", "#3F9AE0", "#1F5C98"), rim=3.0, split=(0.5, 0.9))
    c.line([(3.0, 11.0), (23.5, 11.0)], 1.4, "#FFFFFF", clip=cov, opacity=0.9)
    c.part([(13.4, 8.8, 0), (15.2, 6.6, 3), (16.4, 6.6, 3), (16.4, 8.8, 0)], T("#C9E8FF", "#9ED0F5", "#5FA6CC"), ink=1.4, in_sil=False)   # little screen
    sill(c, cov, 12.2, "#1F5C98", 0.5)
    c.arches(cov, "#1F5C98", extra=1.5)
    c.lamp(22.4, 9.2, 23.9, 10.4, HEAD, rad=4)
    c.tube([(3.0, 12.5), (3.0, 9.3)], 0.6, STEEL, in_sil=False)

# ================================================================== semi-truck cab (listed as pickup) (41 x 26)
@design("vehicle-pickup", wheel_kind="truck", accent="#3A78C9")
def pickup(c):
    c.rect(1.0, 17.2, 40.0, 20.4, DSTEEL, rad=3)                                       # chassis rails
    c.rect(2.5, 15.6, 15.5, 17.4, STEEL, rad=3)                                        # fifth wheel plate
    for x in (21.6, 24.2):                                                              # exhaust stacks
        c.rect(x - 0.7, 1.0, x + 0.7, 9.5, T("#F2F5F8", "#B7C4D0", "#59636D"), rad=3)
    prof = [(20.5, 17.4, 5), (20.5, 6.0, 8), (23.0, 4.6, 8), (30.0, 4.4, 10), (32.5, 9.8, 8), (39.0, 10.8, 12), (40.0, 12.4, 10), (40.0, 17.4, 5)]
    cov, body = c.body(prof, T("#8FBFFF", "#3A78C9", "#1B467F"), rim=3.2, split=(0.5, 0.9))
    c.pane([(24.0, 9.8, 0), (24.0, 6.2, 3), (29.2, 6.2, 3), (31.4, 9.8, 0)], "#1B467F", streak=[(25.0, 9.2), (27.0, 6.8)])
    sill(c, cov, 14.4, "#1B467F", 0.45)
    c.line([(21.0, 12.4), (39.5, 12.4)], 1.4, "#FFFFFF", clip=cov, opacity=0.9)
    c.line([(24.0, 11.2), (24.0, 17.0)], 1.3, "#1B467F", clip=cov, opacity=0.85)
    c.rect(37.6, 12.9, 40.0, 16.8, T("#C9D2DA", "#8A939B", "#3B434D"), rad=3)           # grille
    c.lamp(38.6, 10.8, 39.9, 12.0, HEAD, rad=4)
    c.rect(7.0, 14.8, 17.0, 17.4, DSTEEL, rad=2)
    c.arches(cov, "#1B467F", extra=0.5)
    c.gloss(cov, 27, 5.4, 9, 1.6, 0.3)

# ================================================================== city bus (47 x 23)
@design("vehicle-bus", wheel_kind="truck", accent="#1FA59B")
def bus(c):
    prof = [(1.0, 19.6, 8), (1.0, 3.8, 12), (3.0, 2.2, 8), (42.0, 2.2, 10), (46.0, 5.4, 14), (46.0, 19.6, 8)]
    cov, body = c.body(prof, T("#6FE0D2", "#1FA59B", "#0E5F63"), rim=3.2, split=(0.5, 0.9))
    band(c, cov, 3.0, 4.6, "#FFF1D6", 1.0)
    sill(c, cov, 15.2, "#0E5F63", 0.7)
    band(c, cov, 14.8, 15.4, "#FFF1D6", 1.0)
    xs = [3.0, 10.0, 17.0, 24.0, 31.0]
    for i, x0 in enumerate(xs):
        c.pane([(x0, 12.8, 2), (x0, 5.6, 2), (x0 + 6.2, 5.6, 2), (x0 + 6.2, 12.8, 2)], "#0E5F63", streak=[(x0 + 1.0, 12.0), (x0 + 3.2, 6.4)])
    c.pane([(38.4, 14.4, 2), (38.4, 5.6, 2), (42.2, 5.6, 2), (42.2, 14.4, 2)], "#0E5F63", streak=[(39.2, 13.4), (40.8, 6.4)])   # door
    c.pane([(43.2, 12.6, 2), (43.2, 5.6, 2), (45.7, 6.0, 6), (45.8, 12.6, 2)], "#0E5F63", streak=[(43.8, 11.6), (44.8, 6.8)])    # windscreen
    c.line([(37.8, 5.0), (37.8, 15.0)], 1.4, "#0E5F63", clip=cov, opacity=0.9)
    c.rect(41.0, 1.0, 44.0, 2.4, DSTEEL, rad=2)                                         # destination box
    c.arches(cov, "#0E5F63", extra=1.0)
    c.lamp(44.6, 15.4, 45.9, 16.8, HEAD, rad=4); c.lamp(1.05, 15.0, 2.2, 16.6, TAIL, rad=4)
    c.gloss(cov, 24, 3.6, 36, 1.8, 0.3)

# ================================================================== fire truck (46 x 23)
@design("vehicle-fire-truck", wheel_kind="truck", accent="#FFF1D6")
def fire(c):
    # design was authored over x 1..46 but the 46 px canvas content box is x 1..45 (front touched the border): squeeze x by 44/45
    _X = c.X
    c.X = lambda x: _X(1.0 + (x - 1.0) * 44.0 / 45.0)
    # ladder on the roof (drawn first, sits behind the body top)
    c.rect(2.5, 1.0, 31.0, 3.3, T("#F2F5F8", "#C9D2DA", "#6B7885"), rad=2)
    c.line([(3.2, 2.15), (30.3, 2.15)], 0.6, "#8A939B", opacity=0.9)
    for i in range(11):
        c.line([(3.6 + i * 2.7, 1.1), (3.6 + i * 2.7, 3.2)], 0.9, "#3B434D", opacity=0.95)
    prof = [(1.0, 19.6, 8), (1.0, 5.2, 8), (32.0, 5.2, 6), (33.0, 8.2, 6), (36.0, 5.0, 10), (42.0, 5.0, 8), (45.0, 11.0, 10), (46.0, 13.0, 6), (46.0, 19.6, 8)]
    cov, body = c.body(prof, T("#FF8A7A", "#D94B40", "#8E2A2A"), rim=3.2, split=(0.5, 0.9))
    band(c, cov, 12.0, 13.0, "#F2F5F8", 1.0)
    sill(c, cov, 15.4, "#8E2A2A", 0.6)
    band(c, cov, 15.1, 15.9, "#F2F5F8", 1.0)
    c.rect(4.0, 6.6, 13.5, 14.4, T("#6B7885", "#4A5560", "#2B333D"), rad=3, ink=0)       # roller door
    for yy in (8.0, 9.6, 11.2, 12.8): c.line([(4.4, yy), (13.1, yy)], 0.6, "#59636D", opacity=0.8)
    c.rect(15.0, 6.6, 21.0, 14.4, T("#6B7885", "#4A5560", "#2B333D"), rad=3)
    c.rect(22.5, 6.6, 31.0, 14.4, T("#6B7885", "#4A5560", "#2B333D"), rad=3)
    c.pane([(35.2, 11.0, 0), (36.4, 6.4, 3), (41.4, 6.4, 3), (41.4, 11.0, 0)], "#8E2A2A", streak=[(36.4, 10.2), (38.0, 7.0)])
    c.pane([(42.4, 11.0, 0), (42.4, 6.4, 0), (43.4, 6.4, 3), (44.8, 11.0, 0)], "#8E2A2A")
    c.rect(36.2, 1.0, 41.0, 2.8, T("#C9D2DA", "#8A939B", "#3B434D"), rad=3)             # light bar
    c.lamp(36.5, 1.0, 38.4, 2.6, TAIL, rad=4); c.lamp(38.8, 1.0, 40.7, 2.6, ("#9CC8FF", "#2A6CD8", "#1A3F8A"), rad=4)
    c.arches(cov, "#8E2A2A", extra=1.0)
    c.lamp(44.7, 12.4, 45.9, 14.0, HEAD, rad=4); c.lamp(1.05, 15.8, 2.2, 17.4, TAIL, rad=4)
    c.rect(44.2, 15.3, 46.0, 17.6, STEEL, rad=3)
    c.gloss(cov, 20, 5.6, 26, 1.6, 0.3)

# ================================================================== camper van (37 x 21)
@design("vehicle-camper", wheel_kind="truck", accent="#F4A340")
def camper(c):
    prof = [(1.0, 18.4, 8), (1.0, 5.4, 12), (4.0, 3.0, 10), (27.0, 3.0, 10), (31.0, 8.6, 8), (35.0, 10.2, 12), (36.0, 12.0, 8), (36.0, 18.4, 8)]
    cov, body = c.body(prof, T("#FFF6E0", "#F2E4C4", "#B79E6C"), rim=3.2, split=(0.55, 0.92))
    low = c.sp.mask(); low.polygon([(c.X(-5), c.Y(11.4)), (c.X(80), c.Y(11.4)), (c.X(80), c.Y(60)), (c.X(-5), c.Y(60))])
    c.sp.paint(low, T("#FFC066", "#F4A340", "#B24E12")[1], 1.0, clip=cov)                   # orange lower half (flat) + shade below
    sill(c, cov, 15.2, "#B24E12", 0.55)
    band(c, cov, 11.2, 12.0, "#FFFFFF", 0.95)
    c.pane([(3.2, 9.6, 3), (3.2, 5.4, 3), (10.0, 5.4, 3), (10.0, 9.6, 3)], "#B79E6C", streak=[(4.0, 8.8), (6.0, 6.0)])
    c.pane([(12.0, 9.6, 3), (12.0, 5.4, 3), (19.0, 5.4, 3), (19.0, 9.6, 3)], "#B79E6C", streak=[(12.8, 8.8), (14.8, 6.0)])
    c.pane([(22.0, 9.6, 0), (22.0, 5.4, 3), (26.2, 5.4, 3), (29.4, 9.6, 0)], "#B79E6C", streak=[(22.8, 8.8), (24.4, 6.0)])
    c.line([(21.0, 4.4), (21.0, 15.0)], 1.3, "#B79E6C", clip=cov, opacity=0.9)
    c.rect(8.5, 1.0, 17.5, 3.4, T("#DDE6EE", "#9AA3AD", "#59636D"), rad=4)             # roof vent / box
    c.arches(cov, "#B24E12", extra=1.0)
    c.lamp(34.8, 12.6, 35.9, 14.0, HEAD, rad=4); c.lamp(1.05, 12.6, 2.1, 14.2, TAIL, rad=4)
    c.gloss(cov, 15, 3.8, 18, 1.4, 0.3)

# ================================================================== box trailer (listed as semi-truck) (50 x 26)
@design("vehicle-semi-truck", wheel_kind="truck", accent="#3A78C9")
def semi(c):
    prof = [(1.0, 20.6, 4), (1.0, 3.0, 8), (4.0, 1.0, 6), (46.0, 1.0, 6), (49.0, 3.0, 8), (49.0, 20.6, 4)]
    cov, body = c.body(prof, T("#FFFFFF", "#E8EEF4", "#A0AEBE"), rim=3.2, split=(0.55, 0.92))
    band(c, cov, 13.0, 15.6, "#3A78C9", 1.0)
    band(c, cov, 13.0, 13.7, "#8FBFFF", 1.0)
    for x in (10.0, 20.0, 30.0, 40.0):
        c.line([(x, 1.5), (x, 20.4)], 1.0, "#A0AEBE", clip=cov, opacity=0.7)
    c.rect(1.0, 3.0, 3.6, 20.0, T("#C9D2DA", "#8A939B", "#59636D"), rad=2)             # rear door frame
    c.rect(2.0, 9.0, 2.8, 14.0, DSTEEL, rad=1)
    c.rect(1.0, 20.4, 49.0, 23.2, DSTEEL, rad=3)                                        # frame
    c.rect(38.0, 22.8, 40.0, 25.0, DSTEEL, rad=1)                                       # landing leg
    c.rect(33.0, 24.0, 45.0, 25.0, DSTEEL, rad=1)
    c.lamp(1.05, 17.6, 2.3, 19.4, TAIL, rad=3)
    c.arches(cov, "#A0AEBE", extra=0.5)
    c.gloss(cov, 25, 2.2, 40, 0.9, 0.35)

# ================================================================== tractor (26 x 20)
@design("vehicle-tractor", wheel_kind="tractor", accent="#6B4426")
def tractor(c):
    # canopy on posts, exhaust stack, then the body
    c.tube([(3.4, 8.0), (3.4, 2.6)], 0.9, STEEL); c.tube([(10.0, 8.0), (10.0, 2.6)], 0.9, STEEL)
    c.rect(1.4, 1.0, 11.6, 2.8, T("#B8E070", "#8FCB3A", "#4F7F1E"), rad=4)
    c.rect(17.0, 2.4, 18.4, 9.0, T("#E7EDF2", "#8A939B", "#3B434D"), rad=3)
    c.rect(4.0, 5.2, 8.6, 8.4, T("#6B4426", "#4A2D18", "#2A1810"), rad=3)               # seat
    prof = [(3.4, 14.0, 6), (3.2, 8.6, 8), (11.0, 8.2, 6), (13.0, 6.8, 6), (22.0, 8.0, 8), (24.0, 10.0, 8), (24.0, 14.0, 6)]
    cov, body = c.body(prof, T("#C9F08A", "#8FCB3A", "#4F7F1E"), rim=3.2, split=(0.5, 0.9))
    for x in (19.8, 21.0, 22.2): c.line([(x, 9.0), (x, 13.2)], 0.7, "#2F5413", clip=cov, opacity=0.9)
    c.rect(22.8, 9.0, 24.0, 13.6, DSTEEL, rad=2)
    sill(c, cov, 12.2, "#4F7F1E", 0.55)
    c.lamp(22.4, 8.2, 23.6, 9.2, HEAD, rad=3)
    c.rect(10.0, 13.4, 22.0, 14.6, DSTEEL, rad=2)                                        # axle beam
    c.gloss(cov, 15, 7.4, 6, 1.2, 0.3)

# ================================================================== quad ATV (46 x 27)
@design("vehicle-quad-atv", wheel_kind="lug", accent="#F4A340")
def quad(c):
    c.tube([(33.6, 20.4), (35.0, 3.4)], 1.5, T("#C9D2DA", "#8A939B", "#3B434D"))        # handlebar post
    c.tube([(31.6, 2.4), (38.6, 1.9)], 1.3, DSTEEL)
    c.disc(40.6, 2.8, 2.2 * c.S / 2.0, T("#FFFBE0", "#FFE27A", "#E9A92B"), ink=1.8)
    c.rect(1.0, 9.0, 11.6, 12.0, T("#6B7885", "#3B434D", "#252B36"), rad=3)             # rear rack
    c.rect(12.0, 9.6, 25.0, 13.2, T("#4A5560", "#2B333D", "#171C22"), rad=5)            # seat
    prof = [(5.0, 21.6, 6), (4.6, 14.6, 8), (14.0, 14.0, 8), (28.0, 13.6, 8), (36.0, 12.6, 8), (44.0, 15.0, 10), (45.0, 18.0, 6), (43.0, 22.0, 6)]
    cov, body = c.body(prof, T("#FF9A8A", "#E8433A", "#8E2A2A"), rim=3.2, split=(0.5, 0.9))
    c.line([(6.0, 18.2), (43.0, 18.2)], 1.6, "#FFF1D6", clip=cov, opacity=0.95)
    sill(c, cov, 19.6, "#8E2A2A", 0.55)
    c.rect(8.0, 21.0, 40.0, 23.0, DSTEEL, rad=3)
    c.lamp(42.4, 14.4, 44.0, 16.2, HEAD, rad=4)
    c.gloss(cov, 22, 14.6, 14, 1.2, 0.3)

# ================================================================== scooter (17 x 11)
@design("vehicle-scooter", wheel_kind="cart", accent="#FFC1CC")
def scooter(c):
    c.tube([(12.4, 8.0), (12.6, 2.2)], 1.1, T("#E7EDF2", "#8A939B", "#3B434D"))
    c.tube([(11.4, 1.7), (15.0, 1.5)], 1.0, DSTEEL)
    prof = [(1.5, 7.6, 4), (1.2, 4.8, 8), (5.6, 4.2, 6), (8.4, 6.6, 3), (11.6, 6.4, 2), (13.0, 3.2, 4), (14.2, 3.0, 4), (13.8, 8.0, 3), (3.0, 8.4, 4)]
    cov, body = c.body(prof, T("#FFA8BC", "#F0506E", "#B02A4A"), rim=2.6, split=(0.5, 0.9))
    c.rect(2.4, 3.2, 7.4, 4.8, T("#4A5560", "#2B333D", "#171C22"), rad=3)             # seat
    sill(c, cov, 7.2, "#B02A4A", 0.55)
    c.lamp(14.2, 1.0, 15.6, 2.2, HEAD, rad=3)

# ================================================================== bicycle (38 x 20)
@design("vehicle-bicycle", wheel_kind="wire", accent="#D94B40")
def bicycle(c):
    fr = T("#FF9A8A", "#D94B40", "#8E2A2A")
    rear, front, bb, seat, head = (8.5, 11.5), (29.5, 11.5), (17.5, 12.6), (15.0, 5.6), (26.0, 5.8)
    c.tube([rear, bb], 0.9, fr); c.tube([rear, seat], 0.9, fr); c.tube([seat, bb], 1.0, fr)
    c.tube([seat, head], 1.0, fr); c.tube([bb, head], 1.1, fr); c.tube([head, front], 0.9, T("#E7EDF2", "#8A939B", "#3B434D"))
    c.tube([head, (27.0, 2.6)], 0.9, DSTEEL); c.tube([(25.2, 2.4), (31.6, 2.2)], 1.0, DSTEEL)
    c.tube([seat, (14.6, 2.4)], 0.8, STEEL)
    c.rect(11.4, 1.0, 17.2, 2.8, T("#4A5560", "#2B333D", "#171C22"), rad=4)          # saddle
    c.tube([(15.0, 13.6), (20.0, 11.6)], 0.8, STEEL)                                   # crank
    c.disc(17.5, 12.6, 1.9 * c.S / 4, T("#E7EDF2", "#8A939B", "#59636D"), ink=1.6)
    c.disc(20.2, 11.5, 0.9 * c.S / 4, T("#F4A340", "#E9A92B", "#B24E12"), ink=1.4)     # pedal
    c.lamp(30.3, 1.9, 31.7, 3.0, HEAD, rad=3)

# ================================================================== motorcycle (83 x 45)
@design("vehicle-motorcycle", wheel_kind="wire", accent="#2F6FDB")
def moto(c):
    bl = T("#8EB6FF", "#2F6FDB", "#173F8A")
    # fork, swing arm, frame, engine
    c.tube([(67.5, 31.5), (58.0, 10.5)], 2.6, T("#F2F5F8", "#B7C4D0", "#59636D"))
    c.tube([(12.5, 32.5), (36.0, 31.0)], 2.4, DSTEEL)
    c.tube([(30.0, 30.0), (37.0, 14.0), (52.0, 12.0)], 3.0, STEEL)
    c.rect(26.0, 22.0, 46.0, 36.0, T("#8A939B", "#59636D", "#3B434D"), rad=8)            # engine block
    c.rect(33.0, 16.0, 43.0, 24.0, T("#6B7885", "#3B434D", "#252B36"), rad=5)
    c.tube([(44.0, 34.0), (21.0, 38.0), (8.0, 34.0)], 2.4, T("#E7EDF2", "#8A939B", "#59636D"))   # exhaust
    # rear fender, seat, tank, headlight number plate
    c.part([(4.0, 18.0, 3), (9.5, 15.5, 8), (26.0, 14.0, 6), (30.0, 17.0, 4), (14.0, 19.5, 8)], bl, ink=0, rim=2.0)
    c.part([(22.0, 11.5, 6), (35.0, 10.0, 8), (42.0, 12.6, 4), (36.0, 15.5, 4), (23.0, 15.0, 4)], T("#4A5560", "#2B333D", "#171C22"))
    c.part([(36.0, 8.0, 10), (52.0, 6.0, 12), (58.0, 12.0, 8), (52.0, 18.0, 6), (40.0, 17.0, 4)], bl, rim=3.0)
    c.part([(54.0, 4.0, 4), (61.5, 3.2, 8), (64.0, 9.5, 6), (58.0, 11.0, 4)], T("#FFFFFF", "#E8EEF4", "#A0AEBE"), rim=2.0)   # number plate
    c.tube([(55.5, 6.5), (60.5, 5.0), (66.0, 2.5)], 2.2, DSTEEL)                         # bars
    c.tube([(51.0, 4.0), (58.0, 3.0)], 2.2, DSTEEL)
    c.disc(66.2, 11.4, 1.7 * c.S, T("#FFFBE0", "#FFE27A", "#E9A92B"), ink=1.8)
    c.part([(60.0, 24.0, 6), (78.0, 19.4, 8), (81.4, 24.0, 4), (64.0, 28.0, 4)], bl, rim=2.0)  # front fender (over the front wheel)

# ================================================================== golf cart (32 x 19)
@design("vehicle-golf-cart", wheel_kind="cart", accent="#3FAE5A")
def golf(c):
    for x in (3.4, 27.0):
        c.tube([(x, 9.8), (x, 3.8)], 0.9, STEEL)
    c.rect(1.4, 1.0, 29.6, 4.0, T("#8DE08A", "#3FAE5A", "#1F6B3A"), rad=6)
    prof = [(1.5, 16.6, 6), (1.2, 10.6, 8), (10.0, 10.0, 6), (22.0, 9.8, 4), (28.0, 9.6, 6), (31.0, 11.6, 8), (31.0, 16.6, 6)]
    cov, body = c.body(prof, T("#FFFFFF", "#E6ECF2", "#9AA9BA"), rim=3.0, split=(0.55, 0.92))
    c.rect(4.0, 6.6, 11.0, 11.4, T("#6B4426", "#4A2D18", "#2A1810"), rad=5)              # seat back
    c.tube([(21.5, 10.0), (23.2, 5.8)], 0.8, STEEL); c.tube([(21.0, 5.6), (24.6, 6.2)], 0.9, DSTEEL)   # steering wheel
    sill(c, cov, 14.2, "#9AA9BA", 0.6)
    band(c, cov, 12.6, 13.4, "#3FAE5A", 1.0)
    c.arches(cov, "#9AA9BA", extra=1.0)
    c.lamp(29.7, 11.4, 30.9, 12.8, HEAD, rad=3)

# ================================================================== tuk-tuk (33 x 19)
@design("vehicle-tuk-tuk", wheel_kind="cart", accent="#F6C945")
def tuk(c):
    for x in (4.0, 21.0):
        c.tube([(x, 10.0), (x, 4.4)], 0.9, STEEL)
    c.part([(1.4, 4.0, 6), (3.0, 1.0, 8), (22.0, 1.0, 8), (25.4, 4.0, 6)], T("#FFE88A", "#F6C945", "#B88A1E"), rim=3.0)    # roof
    c.rect(4.6, 5.6, 20.4, 12.0, T("#4A5560", "#2B333D", "#171C22"), rad=4)             # open cabin shade
    prof = [(1.5, 16.6, 6), (1.2, 11.4, 8), (14.0, 11.0, 6), (22.0, 10.0, 8), (28.0, 8.6, 8), (31.0, 10.2, 8), (31.0, 16.6, 6)]
    cov, body = c.body(prof, T("#8DE08A", "#3FAE5A", "#1F6B3A"), rim=3.0, split=(0.55, 0.92))
    c.pane([(23.0, 10.0, 0), (24.4, 6.0, 3), (27.4, 5.6, 3), (28.2, 10.0, 0)], "#1F6B3A")
    sill(c, cov, 14.4, "#1F6B3A", 0.55)
    c.arches(cov, "#1F6B3A", extra=1.0)
    c.lamp(29.8, 10.0, 30.9, 11.4, HEAD, rad=3)

# ================================================================== shopping cart (28 x 17)
@design("vehicle-shopping-cart", wheel_kind="cart", accent="#D94B40")
def shop(c):
    basket = c.part([(3.0, 2.0, 3), (26.0, 2.8, 3), (23.0, 10.2, 4), (6.0, 10.0, 4)], T("#E7F0F6", "#B7C4D0", "#6F7E8C"), rim=2.4)[0]
    for i in range(1, 8):                                                               # wire grid
        c.line([(3.0 + i * 3.0 * 0.96, 2.4), (6.0 + i * 2.4, 9.8)], 0.5, "#59636D", clip=basket, opacity=0.9)
    for yy in (4.5, 7.0):
        c.line([(3.5, yy), (25.5, yy + 0.4)], 0.5, "#59636D", clip=basket, opacity=0.9)
    c.line([(3.0, 2.3), (26.0, 3.1)], 1.4, "#E7EDF2", clip=basket)
    c.tube([(2.0, 3.0), (1.2, 1.2), (4.0, 1.0)], 0.9, DSTEEL)                           # handle
    c.tube([(5.0, 10.0), (7.5, 13.0)], 0.8, STEEL); c.tube([(23.0, 10.0), (21.5, 12.0)], 0.8, STEEL)
    c.tube([(7.5, 12.6), (21.5, 11.8)], 0.8, STEEL)


def rider(c, hx, hy, shirt=("#8CCBFF", "#3F9AE0", "#1F5C98"), cap=("#FF8A7A", "#E8434A", "#7E1F2E"), s=1.0):
    """Small cartoon rider seen from the side facing right: cap, face, torso, arm."""
    c.part([(hx - 1.6 * s, hy + 7.0 * s, 4), (hx - 1.9 * s, hy + 2.4 * s, 6), (hx + 1.8 * s, hy + 2.0 * s, 6), (hx + 2.0 * s, hy + 7.0 * s, 4)], shirt, rim=2.0)
    c.disc(hx, hy, 2.5 * s * c.S / 2.0, T("#FFE0C0", "#F7C09A", "#D08C6A"), ink=1.8)
    c.part([(hx - 2.0 * s, hy - 0.3 * s, 2), (hx - 1.2 * s, hy - 2.0 * s, 5), (hx + 1.4 * s, hy - 2.1 * s, 5), (hx + 2.0 * s, hy - 0.3 * s, 2),
            (hx + 3.6 * s, hy - 0.3 * s, 2), (hx + 3.6 * s, hy + 0.3 * s, 2), (hx - 2.0 * s, hy + 0.3 * s, 0)], cap, ink=1.6)
    c.line([(hx + 0.4 * s, hy + 3.4 * s), (hx + 3.4 * s, hy + 4.4 * s)], 1.3 * s, "#FFE0C0")

# ================================================================== tank (32 x 15)
@design("vehicle-tank")
def tank(c):
    G = T("#B4DA7C", "#6B9640", "#34581F")
    c.rect(20.0, 3.4, 31.0, 4.9, STEEL, rad=3)                                           # barrel
    c.rect(29.4, 2.8, 31.0, 5.5, DSTEEL, rad=2)
    c.part([(8.4, 6.9, 0), (9.8, 3.6, 7), (14.0, 2.4, 7), (20.4, 3.0, 7), (22.2, 6.9, 0)], G, rim=3.0)
    c.part([(3.0, 10.0, 3), (4.8, 6.6, 6), (27.2, 6.6, 6), (29.0, 10.0, 3)], G, rim=3.0)
    c.disc(15.2, 2.8, 1.3 * c.S / 2.0, T("#8A939B", "#59636D", "#3B434D"), ink=1.8)      # hatch
    trk = c.rect(1.0, 9.0, 31.0, 14.0, T("#59636D", "#3B434D", "#171C22"), rad=7, ink=0)
    for i in range(30):
        c.line([(2.2 + i * 0.98, 9.4), (2.2 + i * 0.98, 13.6)], 0.5, "#171C22", clip=trk[0], opacity=0.6)
    for x in (5.4, 10.2, 15.0, 19.8, 24.6):
        c.disc(x, 11.5, 3.0 * c.S / 2.0, T("#C9D2DA", "#8A939B", "#3B434D"), ink=1.6)
    c.disc(3.6, 11.5, 2.0 * c.S / 2.0, T("#C9D2DA", "#8A939B", "#3B434D"), ink=1.4); c.disc(28.4, 11.5, 2.0 * c.S / 2.0, T("#C9D2DA", "#8A939B", "#3B434D"), ink=1.4)

# ================================================================== moon rover (49 x 37)
@design("vehicle-moon-rover", wheel_kind="lug", accent="#8CCBFF")
def rover(c):
    c.tube([(24.0, 9.0), (24.0, 3.0)], 1.2, STEEL)                                       # mast
    c.part([(17.0, 3.2, 4), (30.0, 1.4, 6), (32.0, 5.0, 3), (21.0, 6.0, 3)], T("#F2F5F8", "#B7C4D0", "#59636D"), rim=2.0)      # dish
    c.rect(29.0, 8.6, 39.0, 15.0, T("#FFC066", "#F28C1E", "#B24E12"), rad=4, rim=2.0)    # cargo crate
    c.tube([(9.0, 28.0), (20.0, 21.0), (39.0, 28.0)], 2.0, DSTEEL)                       # suspension arms
    cov, body = c.body([(4.0, 24.0, 8), (4.0, 15.5, 10), (8.0, 13.6, 8), (41.0, 13.6, 8), (45.0, 15.8, 10), (45.0, 24.0, 8), (24.0, 27.0, 14)], T("#E7EDF2", "#B7C4D0", "#6F7E8C"), rim=3.2, split=(0.5, 0.9))
    c.pane([(8.0, 20.8, 6), (8.0, 15.0, 8), (22.0, 15.0, 8), (22.0, 20.8, 6)], "#3B434D", glass=("#8CE6FF", "#18B8E8", "#0C6A9A"), streak=[(9.6, 20.0), (13.0, 15.8)])
    band(c, cov, 22.6, 23.6, "#18B8E8", 1.0)
    c.disc(42.4, 16.5, 1.0 * c.S / 2.0, T("#FFFBE0", "#FFE27A", "#E9A92B"), ink=1.6)
    c.disc(27.0, 18.4, 0.8 * c.S / 2.0, T("#FF9A8A", "#E8434A", "#7E1F2E"), ink=1.4)
    c.wheel_at(9.8, 28.0, 8.0, "lug", "#8CCBFF"); c.wheel_at(39.2, 28.0, 8.0, "lug", "#8CCBFF")

# ================================================================== handcar (38 x 24)
@design("vehicle-handcar")
def handcar(c):
    c.tube([(30.0, 3.0), (11.0, 8.4)], 1.6, DSTEEL)                                      # pump lever (long beam)
    c.tube([(32.4, 2.2), (28.6, 3.4)], 1.6, T("#FFE88A", "#F6C945", "#B88A1E"))
    c.rect(16.0, 6.8, 18.6, 14.0, T("#FF9A8A", "#D94B40", "#8E2A2A"), rad=2)             # post
    c.part([(3.0, 15.0, 3), (35.0, 15.0, 3), (35.0, 19.0, 3), (3.0, 19.0, 3)], T("#FFE88A", "#F6C945", "#B88A1E"), rim=2.6)   # deck
    c.rect(4.0, 19.0, 34.0, 21.2, T("#6B7885", "#3B434D", "#252B36"), rad=2)
    for x in (9.5, 28.5):
        c.disc(x, 20.8, 3.2 * c.S / 2.0, T("#C9D2DA", "#8A939B", "#3B434D"), ink=1.7)
    c.rect(1.0, 22.0, 37.0, 23.0, T("#8A939B", "#59636D", "#3B434D"), rad=1)             # rail
    c.rect(6.0, 14.0, 12.0, 15.4, T("#6B4426", "#4A2D18", "#2A1810"), rad=1)

# ================================================================== snowmobile (53 x 20)
@design("vehicle-snowmobile")
def snowmobile(c):
    c.rect(28.0, 17.0, 52.0, 19.0, T("#E7EDF2", "#B7C4D0", "#59636D"), rad=2)           # ski
    c.part([(48.0, 17.0, 0), (51.0, 13.2, 3), (52.0, 15.0, 2), (51.4, 19.0, 0)], T("#E7EDF2", "#B7C4D0", "#59636D"))
    c.tube([(37.0, 15.0), (36.0, 18.4)], 1.1, DSTEEL)
    trk = c.rect(1.0, 11.6, 25.0, 19.0, T("#8A939B", "#59636D", "#2B333D"), rad=3.5)      # track pod
    for i in range(22):
        c.line([(2.4 + i * 1.05, 12.2), (2.2 + i * 1.05, 18.6)], 0.55, "#171C22", clip=trk[0], opacity=0.55)
    c.rect(5.0, 4.6, 17.0, 8.0, T("#4A5560", "#2B333D", "#171C22"), rad=3)              # seat
    cov, body = c.body([(4.0, 13.6, 6), (5.0, 8.0, 8), (22.0, 7.4, 8), (31.0, 7.2, 8), (40.0, 10.2, 6), (48.0, 11.0, 8), (49.0, 13.6, 4), (38.0, 15.4, 8), (8.0, 15.4, 8)],
                       T("#FF9A8A", "#E8433A", "#8E2A2A"), rim=3.0, split=(0.5, 0.9))
    c.part([(32.0, 8.0, 0), (37.0, 3.0, 4), (38.6, 3.0, 2), (37.2, 8.4, 0)], T("#E4F6FF", "#BFE6F7", "#5FA6CC"), ink=1.5)      # screen
    c.line([(8.0, 11.2), (46.0, 11.8)], 1.5, "#FFF1D6", clip=cov, opacity=0.95)
    c.disc(47.6, 11.6, 1.0 * c.S / 2.0, T("#FFFBE0", "#FFE27A", "#E9A92B"), ink=1.5)
    c.tube([(26.0, 6.4), (26.0, 4.4), (29.0, 4.0)], 0.9, DSTEEL)

# ================================================================== sled (41 x 23)
@design("vehicle-sled")
def sled(c):
    c.rect(1.0, 20.0, 39.0, 22.0, T("#E7EDF2", "#B7C4D0", "#59636D"), rad=1.5)          # runners
    c.part([(36.0, 20.0, 0), (39.0, 17.2, 3), (40.0, 19.4, 2), (39.0, 22.0, 0)], T("#E7EDF2", "#B7C4D0", "#59636D"))
    for x in (7.0, 28.0): c.tube([(x, 15.0), (x, 20.4)], 1.4, T("#FF9A8A", "#D94B40", "#8E2A2A"))
    rider(c, 12.0, 5.2, s=1.45)
    c.part([(3.0, 15.0, 3), (35.0, 15.0, 3), (35.0, 16.8, 2), (3.0, 16.8, 2)], T("#C98A5A", "#9A6B3E", "#6B4426"), rim=2.4)
    c.part([(6.0, 11.6, 3), (21.0, 11.6, 3), (22.0, 15.0, 0), (5.4, 15.0, 0)], T("#8CCBFF", "#3F9AE0", "#1F5C98"), rim=2.0)
    c.line([(35.0, 15.6), (39.0, 12.6)], 1.2, "#E7E0CF")                                 # rope

# ================================================================== toboggan (51 x 20)
@design("vehicle-toboggan")
def toboggan(c):
    c.part([(1.5, 18.6, 3), (1.0, 14.0, 6), (40.0, 14.0, 4), (46.0, 13.4, 3), (49.0, 6.0, 5), (50.0, 3.0, 4), (49.0, 2.0, 2), (46.4, 6.0, 3), (42.0, 11.0, 5), (40.0, 18.6, 3)],
           T("#FFE88A", "#F6C945", "#B88A1E"), rim=3.0, split=(0.5, 0.9))
    c.line([(3.0, 15.8), (40.0, 15.8)], 1.4, "#B88A1E", opacity=0.9)
    rider(c, 17.0, 6.0, s=1.5)
    c.part([(9.0, 9.0, 3), (28.0, 9.0, 3), (28.4, 14.0, 0), (8.6, 14.0, 0)], T("#8CCBFF", "#3F9AE0", "#1F5C98"), rim=2.0)

# ================================================================== snowboard (38 x 21)
@design("vehicle-snowboard")
def snowboard(c):
    c.part([(1.0, 16.2, 2), (3.0, 18.2, 8), (35.0, 18.6, 8), (37.0, 16.0, 2), (35.0, 19.6, 4), (3.0, 19.6, 4)], T("#9CC8FF", "#2F6FDB", "#173F8A"), rim=2.4)
    c.part([(2.0, 17.2, 4), (36.0, 17.0, 4), (36.0, 19.0, 3), (2.0, 19.0, 3)], T("#9CC8FF", "#2F6FDB", "#173F8A"), rim=2.4)
    for x in (10.5, 24.5): c.rect(x - 1.4, 15.8, x + 1.4, 17.4, T("#6B7885", "#3B434D", "#252B36"), rad=2)
    for x in (15.4, 19.0):                                                               # legs
        c.part([(x, 16.0, 1), (x + 0.2, 11.0, 3), (x + 2.0, 11.0, 3), (x + 2.2, 16.0, 1)], T("#6B7885", "#3B434D", "#252B36"), rim=1.5)
    rider(c, 18.6, 3.2, shirt=T("#8CCBFF", "#3F9AE0", "#1F5C98"), s=1.25)

# ================================================================== skateboard (31 x 14)
@design("vehicle-skateboard", wheel_kind="cart", accent="#FFC066")
def skateboard(c):
    c.part([(1.0, 2.2, 2), (3.4, 6.0, 10), (27.6, 6.0, 10), (30.0, 2.2, 2), (30.0, 3.4, 2), (28.2, 8.8, 6), (2.8, 8.8, 6), (1.0, 3.4, 2)], T("#FFC066", "#F28C1E", "#B24E12"), rim=2.8, split=(0.5, 0.9))
    c.line([(4.0, 6.6), (27.0, 6.6)], 1.4, "#FFF1D6", opacity=0.9)
    for x in (7.0, 24.0):
        c.rect(x - 2.0, 8.6, x + 2.0, 10.0, T("#C9D2DA", "#8A939B", "#3B434D"), rad=1.5)
        c.disc(x - 0.4, 11.5, 2.6 * c.S / 2.0, T("#FFFBE0", "#E7E0CF", "#9AA3AD"), ink=1.8)
        c.disc(x - 0.4, 11.5, 1.0 * c.S / 2.0, T("#8A939B", "#59636D", "#3B434D"), ink=0)

# ================================================================== driver
def render(vid, out, only_wheels=False):
    fn, wk, accent, wkw = DESIGNS[vid]
    for sub in ("bodies", "previews", "wheels"):
        os.makedirs(os.path.join(out, sub), exist_ok=True)
    for with_wheels, sub in ((False, "bodies"), (True, "previews")):
        c = Ctx(vid, with_wheels)
        fn(c)
        if with_wheels:
            c.draw_wheels(wk, accent, **wkw)
        c.save(os.path.join(out, sub, vid + ".png"))
    for i in range(len(INV[vid]["wheels"])):
        wheel_sprite(vid, i, os.path.join(out, "wheels", "%s_w%d.png" % (vid, i)), wk, accent, **wkw)

if __name__ == "__main__":
    ap = argparse.ArgumentParser(); ap.add_argument("--output", required=True); ap.add_argument("--only", default="")
    a = ap.parse_args()
    ids = a.only.split(",") if a.only else list(DESIGNS)
    for v in ids:
        render(v, a.output); print("done", v, flush=True)

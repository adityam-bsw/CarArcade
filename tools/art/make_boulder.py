#!/usr/bin/env python3
"""Rolling Boulder rock sprite (QA12-07): lumpy grey-brown rock, cel shaded, dark outline, cracks and pebbles. Own code, procedural, no
generative AI. Usage: python3 tools/art/make_boulder.py <output png>   (shipped at Assets/Resources/StageFx/boulder.png)"""
import math, os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from smoothart import *
OUT = sys.argv[1]
os.makedirs(os.path.dirname(os.path.abspath(OUT)), exist_ok=True)
N = 128; C = 64
sp = Sprite(N, N, N / 2.2)
def lump(i):
    h = math.sin(i * 12.9898 + 4.1) * 43758.5453; return h - math.floor(h)
pts = []
for i in range(28):
    a = i / 28 * math.tau
    r = 57 - 5 * lump(i) + 2.5 * math.sin(a * 3 + 1.0)
    pts.append((C + math.cos(a) * r, C + math.sin(a) * r))
m = sp.shape(lambda mm: mm.polygon(round_poly([(x, y, 6) for x, y in pts], 5)))
rock = ("#B9AB98", "#8A7B6B", "#4F4439")
cov = sp.cel(m, rock, direction=(24, 18, 100, 112), soft=.25, split=(.3, .72), rim=3)
for cx, cy, rx, ry, col, o in ((44, 40, 16, 9, "#D9CCB8", .55), (86, 84, 18, 12, "#3E352C", .35), (40, 92, 10, 7, "#5E5246", .3)):
    sp.gloss(cov, cx, cy, rx, ry, o, color=col, clip=cov)
for poly in ([(70, 20), (62, 44), (72, 58), (66, 80)], [(30, 70), (48, 76), (54, 92)], [(92, 40), (84, 56), (96, 66)]):
    sp.line(poly, 1.8, "#3A3027", clip=cov, opacity=.9)
for x, y, r in ((50, 58, 4), (80, 30, 3), (28, 46, 3), (96, 96, 4)):
    pm = sp.mask(); pm.circle(x, y, r); sp.paint(pm, hexrgb("#6B5E50"), .8, clip=cov)
sp.finish(OUT)

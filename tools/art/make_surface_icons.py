#!/usr/bin/env python3
"""Surface tutorial icons (section C5): one smooth badge per surface behaviour, 128 px, own code (smoothart), no generative AI, no downloads.

    python3 tools/art/make_surface_icons.py --output <dir>

Each icon is a round cel-shaded badge (colour = category) with a cream glyph that says the verb of the surface. File names are the hintIcon ids
used by SurfaceBehaviourSetSO (icon-<surface>). Python 3.9 standard library only.
"""
import argparse, math, os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from smoothart import *

CREAM = "#FFF6E0"
CAT = {  # category -> (light, mid, deep)
    "cold": ("#BFE8FF", "#6FB6E8", "#2F6FB0"), "soft": ("#D9B27A", "#A9773E", "#6B4426"), "break": ("#F7D27A", "#D9893A", "#8E4A1E"),
    "bounce": ("#9BEAA8", "#44B86A", "#1E7A44"), "hot": ("#FFB27A", "#E5563A", "#8E2A2A"), "tech": ("#C8A8FF", "#7B5FD0", "#3E2A86"),
    "toxic": ("#D6F27A", "#8FBF2A", "#4A6E14"), "dark": ("#B8B0D8", "#5E5890", "#2B2650"), "water": ("#9FE4EE", "#3FA9C8", "#1B6A8C"),
}

def cap(m, x0, y0, x1, y1, r): m.capsule(x0, y0, x1, y1, r)
def flake(m, cx, cy, L, r):
    for k in range(3):
        a = math.pi * k / 3; dx, dy = math.cos(a) * L, math.sin(a) * L
        cap(m, cx - dx, cy - dy, cx + dx, cy + dy, r)
def arrow(m, x0, y0, x1, y1, r, head):
    cap(m, x0, y0, x1, y1, r)
    a = math.atan2(y1 - y0, x1 - x0)
    pts = [(x1 + math.cos(a) * head, y1 + math.sin(a) * head)] + [(x1 + math.cos(a + s) * head * 0.9, y1 + math.sin(a + s) * head * 0.9) for s in (2.4, -2.4)]
    m.polygon(pts)
def bolt(m, cx, cy, k):
    m.polygon([(cx + 6 * k, cy - 30 * k), (cx - 14 * k, cy + 4 * k), (cx - 1 * k, cy + 4 * k), (cx - 8 * k, cy + 30 * k), (cx + 14 * k, cy - 6 * k), (cx + 1 * k, cy - 6 * k)])
def drop(m, cx, cy, k):
    m.circle(cx, cy + 8 * k, 16 * k); m.polygon([(cx, cy - 26 * k), (cx - 14 * k, cy + 2 * k), (cx + 14 * k, cy + 2 * k)])
def wave(m, cx, cy, w, h, r, n=2):
    pts = [(cx - w + 2 * w * i / 40.0, cy + math.sin(i / 40.0 * n * 2 * math.pi) * h) for i in range(41)]
    m.polyline(pts, r)
def spring(m, cx, cy, w, h, r, n=4):
    pts = [(cx + (w if i % 2 else -w), cy - h + 2 * h * i / (2 * n)) for i in range(2 * n + 1)]
    m.polyline(pts, r)
def cloud(m, cx, cy, k):
    m.circle(cx - 14 * k, cy + 4 * k, 12 * k); m.circle(cx + 2 * k, cy - 6 * k, 16 * k); m.circle(cx + 18 * k, cy + 4 * k, 12 * k); m.rrect(cx - 26 * k, cy + 2 * k, cx + 30 * k, cy + 16 * k, 7 * k)

# id -> (category, glyph(mask), optional cut(mask) drawn in the deep colour)
def g_ice(m): flake(m, 64, 64, 30, 4.5)
def g_ice_bridge(m): flake(m, 64, 58, 24, 4); cap(m, 28, 94, 100, 94, 5)
def g_snow(m): flake(m, 64, 64, 22, 4); [m.circle(x, y, 4) for x, y in ((30, 36), (98, 40), (34, 96), (96, 92))]
def g_mud(m): m.circle(46, 74, 14); m.circle(68, 66, 18); m.circle(88, 76, 12); m.rrect(30, 76, 102, 96, 9)
def g_glass(m): m.polygon([(64, 24), (96, 56), (64, 104), (32, 56)])
def c_glass(m): m.polyline([(64, 24), (58, 52), (70, 66), (62, 104)], 2.2)
def g_cloud(m): cloud(m, 64, 62, 1.0)
def c_cloud(m): [m.circle(x, 96, 2.8) for x in (44, 64, 84)]
def g_rope(m): wave(m, 64, 62, 38, 10, 4.5, 1); cap(m, 28, 82, 100, 82, 3.5); [cap(m, x, 56 + 6 * math.sin((x - 26) / 76.0 * math.pi * 2), x, 84, 2.6) for x in (36, 52, 68, 84, 98)]
def g_neon(m): bolt(m, 64, 64, 1.1)
def g_sand(m): m.polygon([(26, 92), (52, 52), (70, 74), (88, 44), (104, 92)])
def g_wood(m): m.polygon([(64, 22), (88, 58), (80, 100), (48, 100), (40, 58), (58, 64)])
def c_wood(m): m.circle(64, 84, 9)
def g_spring(m): spring(m, 64, 62, 22, 30, 4.5, 4)
def g_magnet(m): m.polyline([(36, 36), (36, 70), (50, 94), (78, 94), (92, 70), (92, 36)], 9)
def c_magnet(m): cap(m, 27, 28, 45, 28, 5); cap(m, 83, 28, 101, 28, 5)
def g_gravel(m): [m.circle(x, y, r) for x, y, r in ((40, 74, 12), (62, 62, 14), (86, 76, 12), (52, 92, 9), (76, 94, 8), (92, 52, 8))]
def g_water(m): wave(m, 64, 50, 36, 8, 5, 2); wave(m, 64, 76, 36, 8, 5, 2)
def g_acid(m): drop(m, 64, 60, 1.2)
def c_acid(m): m.circle(60, 70, 4); m.circle(70, 78, 3)
def g_oil(m): drop(m, 64, 62, 1.15); m.ellipse(64, 100, 28, 6)
def g_cobble(m): [m.circle(x, y, 13) for x, y in ((40, 48), (68, 44), (90, 56), (48, 76), (76, 76), (60, 98), (88, 94))]
def g_conveyor(m): arrow(m, 24, 64, 100, 64, 7, 22); m.rrect(24, 84, 104, 94, 5)
def g_lava_river(m): wave(m, 64, 56, 38, 9, 5, 2); m.polygon([(36, 100), (48, 74), (60, 100)]); m.polygon([(68, 100), (80, 74), (92, 100)])
def g_sticky(m): m.circle(64, 58, 22); m.polygon([(52, 76), (60, 100), (68, 76)]); m.polygon([(70, 76), (78, 96), (84, 74)])
def g_honey(m): [m.polygon([(64 + 22 * math.cos(a), 64 + 22 * math.sin(a)) for a in [math.pi / 3 * i + math.pi / 6 for i in range(6)]])]; m.circle(64, 104, 6)
def c_honey(m): m.circle(64, 64, 8)
def g_plates(m): m.rrect(26, 38, 62, 62, 4); m.rrect(66, 38, 102, 62, 4); m.rrect(26, 68, 62, 92, 4); m.rrect(66, 68, 102, 92, 4)
def g_crystals(m): m.polygon([(36, 98), (48, 40), (60, 98)]); m.polygon([(56, 98), (72, 24), (88, 98)]); m.polygon([(80, 98), (92, 54), (102, 98)])
def g_rocks(m): m.circle(50, 76, 17); m.circle(80, 58, 15); arrow(m, 98, 98, 98, 74, 3.5, 12); arrow(m, 30, 40, 30, 62, 3.5, 12)
def g_ghost(m): m.circle(64, 54, 26); m.rrect(38, 54, 90, 98, 3); [m.circle(x, 98, 8.5) for x in (47, 64, 81)]
def c_ghost(m): m.circle(54, 54, 5); m.circle(74, 54, 5)
def g_electric(m): bolt(m, 52, 60, 0.95); cap(m, 82, 36, 82, 92, 4); cap(m, 98, 36, 98, 92, 4)
def g_paper(m): m.polygon([(34, 28), (88, 28), (98, 40), (98, 100), (34, 100)])
def c_paper(m): m.polyline([(34, 64), (50, 58), (62, 70), (76, 60), (98, 66)], 2.4)
def g_lava(m): m.circle(64, 62, 26); m.polygon([(40, 96), (52, 78), (64, 96)]); m.polygon([(64, 96), (76, 78), (88, 96)])
def c_lava(m): m.polyline([(56, 50), (66, 62), (58, 76)], 2.4)
def g_sphere(m): m.circle(64, 62, 24)
def g_electromag(m): bolt(m, 64, 52, 0.7); m.polyline([(36, 66), (36, 86), (50, 100), (78, 100), (92, 86), (92, 66)], 7)
def g_rope_strong(m): wave(m, 64, 60, 38, 5, 5, 1); cap(m, 28, 80, 100, 80, 5); cap(m, 28, 92, 100, 92, 5)
def g_asphalt(m): cap(m, 26, 80, 102, 80, 8); [cap(m, x, 80, x + 12, 80, 3) for x in ()]
def g_coral(m): cap(m, 64, 98, 64, 40, 5); cap(m, 64, 76, 40, 52, 4.5); cap(m, 64, 66, 90, 42, 4.5)

ICONS = {
    "icon-ice": ("cold", g_ice, None), "icon-ice-bridge": ("cold", g_ice_bridge, None), "icon-snow": ("cold", g_snow, None),
    "icon-mud": ("soft", g_mud, None), "icon-glass": ("break", g_glass, c_glass), "icon-cloud": ("break", g_cloud, c_cloud),
    "icon-rope-bridge": ("soft", g_rope, None), "icon-neon-grid": ("tech", g_neon, None), "icon-sand": ("soft", g_sand, None),
    "icon-wooden-bridge": ("hot", g_wood, c_wood), "icon-spring-road": ("bounce", g_spring, None), "icon-magnetic-rail": ("tech", g_magnet, c_magnet),
    "icon-gravel": ("soft", g_gravel, None), "icon-sea-water": ("water", g_water, None), "icon-acid": ("toxic", g_acid, c_acid),
    "icon-oil": ("dark", g_oil, None), "icon-cobblestone": ("soft", g_cobble, None), "icon-conveyor": ("tech", g_conveyor, None),
    "icon-lava-river": ("hot", g_lava_river, None), "icon-sticky-candy": ("bounce", g_sticky, None), "icon-honey": ("break", g_honey, c_honey),
    "icon-metal-plates": ("tech", g_plates, None), "icon-ice-crystals": ("cold", g_crystals, None), "icon-floating-islands": ("water", g_rocks, None),
    "icon-haunted-shadow": ("dark", g_ghost, c_ghost), "icon-electric-rail": ("tech", g_electric, None), "icon-fragile-paper": ("break", g_paper, c_paper),
    "icon-lava-crust": ("hot", g_lava, c_lava), "icon-magma-bridge": ("hot", g_lava, c_lava), "icon-glass-heavy": ("break", g_glass, c_glass),
    "icon-candy": ("bounce", g_sphere, None), "icon-moon-dust": ("tech", g_sphere, None), "icon-river": ("water", g_water, None),
    "icon-beach": ("soft", g_sand, None), "icon-electromagnetic": ("tech", g_electromag, None), "icon-rope-bridge-strong": ("soft", g_rope_strong, None),
    "icon-asphalt": ("soft", g_asphalt, None), "icon-coral": ("water", g_coral, None),
}

def badge(path, cat, glyph, cut):
    S = 128; sp = Sprite(S, S, 150.0)
    c = S / 2; R = S / 2 - 4 - sp.outline_px
    L, M, D = (hexrgb(h) for h in CAT[cat])
    disc = sp.shape(lambda m: m.circle(c, c, R))
    sp.cel(disc, (L, M, D), direction=(c - R * 0.8, c - R * 0.8, c + R * 0.9, c + R * 0.9), soft=0.22, split=(0.4, 0.8), rim=3.0)
    sh = sp.mask(); glyph(sh); sp.paint(shift_cov(sh.cov(), 2, 3), D, 0.55)          # soft drop shadow of the glyph
    g = sp.mask(); glyph(g); sp.paint(g, hexrgb(CREAM))
    if cut is not None:
        k = sp.mask(); cut(k); sp.paint(k, D, 0.9)
    sp.finish(path)

def main():
    ap = argparse.ArgumentParser(); ap.add_argument("--output", required=True); ap.add_argument("--only", default="")
    a = ap.parse_args(); os.makedirs(a.output, exist_ok=True)
    only = set(x for x in a.only.split(",") if x)
    for name, (cat, glyph, cut) in ICONS.items():
        if only and name not in only: continue
        badge(os.path.join(a.output, name + ".png"), cat, glyph, cut)
        print("wrote", name)

if __name__ == "__main__":
    main()

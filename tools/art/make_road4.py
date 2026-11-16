#!/usr/bin/env python3
"""Smooth road strips, batch 4 (QA7-06): the 12 surfaces that had no smooth strip. Own code, no generative AI, no downloads.
    python3 tools/art/make_road4.py --output <dir> [--surface lava_crust,sea_water,...|all]
1024 x 192, seamless in X, OPAQUE body from the lip to the bottom row (no skirt fade; solidify() guarantees it), lip rows measured by check_roads2.py.
  lava_crust basalt + glowing cracks     magma_bridge obsidian blocks over a magma glow    sea_water waves + foam     river streaks + pebbles
  candy frosting over cake + sprinkles   cloud puffy white                                 coral branching coral crest  beach pale sand, shells, wet band
  floating_islands grass over layered rock  ice_bridge staggered ice bricks, snow cap        rope_bridge planks + twisted ropes  salt_flat hex crust cracks
"""
import argparse, math, os, random, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from smoothart import *
import make_road
from make_road import W, H, SS, INK, periodic_dist, strip_cov, column_cache, mottled, lip_shadow, stones, solidify, preview
from make_road2 import bumps_fn
from make_road3 import M, slab_top, zig
make_road.OPAQUE_BODY = True

def PR(stops):
    """Ramp whose stop positions are pixel rows 0..192 (vgrad wants 0..1)."""
    return Ramp([(p / 192.0, c) for p, c in stops])

def hl_line(img, top, rows, color, a):
    """Soft highlight just under the lip."""
    c = Cov(W, H); c.bb = (0, 0, W, H)
    for x in range(W):
        t = top(x)
        for y in range(max(0, int(t)), int(t) + rows): c.d[y * W + x] = max(0.0, 1 - (y + .5 - t) / (rows - 0.5)) if y + .5 >= t else 0
    img.paint(c, color, a)

def lava_crust():
    rnd = random.Random(301); top = column_cache(lambda x: 4.5 + 1.6 * wave_fbm(x, W, 311, octaves=3, base=4)); img = Image(W, H)
    slab_top(img, top, Ramp([(0, "#8A7478"), (3, "#5A4A50"), (14, "#443840"), (60, "#32282C"), (192, "#1C1618")]), 0.18, 0.12, 315)
    # basalt plates: darker polygon slabs
    pl = M()
    for i in range(14):
        x = i * 74 + rnd.uniform(-8, 8); y = rnd.uniform(26, 130); pl.polygon([(x + rnd.uniform(-30, 0), y + rnd.uniform(-14, 0)), (x + rnd.uniform(40, 70), y + rnd.uniform(-14, 0)), (x + rnd.uniform(40, 70), y + rnd.uniform(14, 30)), (x + rnd.uniform(-30, 0), y + rnd.uniform(14, 30))])
    img.paint(pl.cov(), "#0E0A0B", 0.3)
    # cracks: long zigzags with orange core, soft glow halo, branches
    core = M(); halo = M(); hot = M()
    for k in range(11):
        x0 = k * 93 + rnd.uniform(0, 30); y0 = rnd.uniform(14, 40); pts = [(x0, y0)]; x, y = x0, y0
        for s in range(9):
            x += rnd.uniform(14, 30) * rnd.choice((1, 1, -1)); y += rnd.uniform(8, 17); pts.append((x, min(y, 180)))
        core.polyline(pts, 1.5); halo.polyline(pts, 5.5); hot.polyline(pts, 0.6)
        j = rnd.randrange(2, 6); bx, by = pts[j]; br = zig(rnd, bx, by, bx + rnd.uniform(-45, 45), by + rnd.uniform(10, 40), 4, 4); core.polyline(br, 1.0); halo.polyline(br, 4)
    img.paint(halo.cov().blur(3, 3, True), "#FF5A12", 0.55); img.paint(core.cov(), vgrad(0, 192, PR([(0, "#FFB02E"), (1, "#FF5A12")]))); img.paint(hot.cov(), "#FFF0A0", 0.9)
    gl = M()                                                                                   # ember specks
    for _ in range(60): gl.circle(rnd.uniform(0, W), rnd.uniform(20, 180), rnd.uniform(0.8, 1.6))
    img.paint(gl.cov(), "#FF9A2A", 0.8)
    hl_line(img, top, 3, "#C9B0A0", 0.5); lip_shadow(img, top, 12, 26, 0.2, "#000000")
    return img

def magma_bridge():
    rnd = random.Random(311); top = column_cache(lambda x: 4.0); img = Image(W, H)
    img.paint(strip_cov(top), vgrad(0, 192, PR([(0, "#2A1A1C"), (30, "#4A2018"), (50, "#B0300E"), (90, "#FF6A14"), (140, "#FF9A22"), (192, "#FFC64A")])))
    # lava ripples glowing under the deck
    r = M(); b = M()
    for _ in range(34):
        x = rnd.uniform(0, W); y = rnd.uniform(70, 185); L = rnd.uniform(40, 120); r.polyline([(x + L * i / 8, y + 3 * math.sin(i * 1.3 + x)) for i in range(9)], 1.4)
    for _ in range(18): b.circle(rnd.uniform(0, W), rnd.uniform(60, 180), rnd.uniform(3, 8))
    img.paint(r.cov(), "#FFE27A", 0.7); img.paint(b.cov(), "#FF4A0A", 0.5)
    # obsidian deck blocks 128 wide with glowing seams, lower support beam
    beam = M(); beam.rrect(-10, 30, W + 10, 46, 2); img.paint(beam.cov(), vgrad(30, 46, Ramp([(0, "#3A2A2E"), (1, "#1A1214")])))
    deck = [M() for _ in range(2)]; hi = M(); seam = M()
    for i in range(8):
        xa = i * 128 + 3; xb = xa + 122; deck[i % 2].rrect(xa, 4, xb, 30, 5); hi.rrect(xa + 6, 5, xb - 6, 8.5, 1.6); seam.rrect(xa - 5, 8, xa, 30, 1)
    img.paint(seam.cov().blur(3, 3, True), "#FF7A1A", 0.8)
    for m, (a, c) in zip(deck, (("#5A4A56", "#2A2028"), ("#4C3E4A", "#241A22"))): img.paint(m.cov(), vgrad(4, 30, Ramp([(0, a), (1, c)])))
    img.paint(hi.cov(), "#D8C0D8", 0.45)
    cr = M()
    for i in range(8):
        x = i * 128 + rnd.uniform(30, 90); cr.polyline(zig(rnd, x, 8, x + rnd.uniform(-14, 14), 28, 4, 3), 0.8)
    img.paint(cr.cov(), "#FF8A2A", 0.85)
    return img

def sea_water():
    top = column_cache(lambda x: 8 + 3.2 * math.sin(x / W * 2 * math.pi * 8) + 1.2 * math.sin(x / W * 2 * math.pi * 21 + 1) + 1.0 * wave_fbm(x, W, 321, 2, 6)); img = Image(W, H); rnd = random.Random(321)
    slab_top(img, top, Ramp([(0, "#8FE6F0"), (6, "#2FB4D8"), (30, "#1A7FBE"), (90, "#0F5A9C"), (192, "#0A3470")]), 0.1, 0.06, 325)
    # wave bands (sine crest lines) and foam caps on the crests
    ln = M(); dk = M(); foam = M(); fd = M()
    for k in range(9):
        y0 = 26 + k * 18; ph = rnd.uniform(0, 6.28); n = rnd.choice((6, 7, 8))
        pts = [(x, y0 + 3.2 * math.sin(x / W * 2 * math.pi * n + ph)) for x in range(0, W + 1, 8)]; ln.polyline(pts, 1.1); dk.polyline([(x, y + 3) for x, y in pts], 2.2)
    img.paint(dk.cov().blur(1, 2, True), "#08306A", 0.35); img.paint(ln.cov(), "#7ADCF4", 0.45)
    for x in range(0, W, 4):
        t = top(x)
        if math.sin(x / W * 2 * math.pi * 8) > 0.55: foam.circle(x, t + 1, rnd.uniform(2.6, 4.6))
    for _ in range(90): foam.circle(rnd.uniform(0, W), rnd.uniform(14, 34), rnd.uniform(0.9, 2.0))
    for _ in range(40): fd.circle(rnd.uniform(0, W), rnd.uniform(40, 170), rnd.uniform(1.5, 3.6))
    img.paint(fd.cov(), "#C8F4FF", 0.35); img.paint(foam.cov(), "#FFFFFF", 0.95)
    return img

def river():
    top = column_cache(lambda x: 6 + 0.9 * wave_fbm(x, W, 331, 2, 5)); img = Image(W, H); rnd = random.Random(331)
    slab_top(img, top, Ramp([(0, "#B6F0E4"), (5, "#58C8B8"), (30, "#2E9C9A"), (100, "#1E7A7E"), (192, "#175A60")]), 0.08, 0.1, 335)
    fl = M(); dk = M(); sp = M()
    for _ in range(46):
        x = rnd.uniform(0, W); y = rnd.uniform(16, 130); L = rnd.uniform(60, 170); fl.capsule(x, y, x + L, y + rnd.uniform(-1, 1), rnd.uniform(0.8, 1.6)); dk.capsule(x, y + 4, x + L * 0.8, y + 4, 1.8)
    for _ in range(70): sp.circle(rnd.uniform(0, W), rnd.uniform(12, 90), rnd.uniform(0.8, 1.4))
    img.paint(dk.cov(), "#0F4A52", 0.3); img.paint(fl.cov(), "#D8FFF4", 0.5); img.paint(sp.cov(), "#FFFFFF", 0.7)
    # pebble bed at the bottom (what separates it from the sea)
    stones(img, rnd, 40, 140, 184, (8, 16), (5, 9), "#0C3A40", Ramp([(0, "#9DB6A0"), (1, "#6C8A78")]), shade_a=0.6, lit_a=0.9)
    hl_line(img, top, 3, "#FFFFFF", 0.5)
    return img

def candy():
    rnd = random.Random(341); N = 16
    # frosting: scalloped lip of round pink bulges
    items = [(i * 64 + 32, 31, 7.0) for i in range(N)]; top = column_cache(bumps_fn(11.0, items, 0.6)); img = Image(W, H)
    img.paint(strip_cov(top), vgrad(0, 192, PR([(0, "#FFC3DF"), (20, "#FF8FC0"), (40, "#F060A0"), (44, "#7A4A3C"), (100, "#6A3A2E"), (192, "#4A2620")])))
    # cake sponge crumbs and chocolate swirl
    cr = M()
    for _ in range(150): cr.ellipse(rnd.uniform(0, W), rnd.uniform(52, 188), rnd.uniform(1.5, 4), rnd.uniform(1, 2.5))
    img.paint(cr.cov(), "#9A6A4A", 0.5)
    sw = M()
    for k in range(8): x = k * 128 + 20; sw.polyline([(x + i * 11, 100 + 16 * math.sin(i * 0.9 + k)) for i in range(11)], 3)
    img.paint(sw.cov(), "#2E1812", 0.5)
    # frosting drips below the scallops, shine, sprinkles
    dr = M(); sh = M()
    for i in range(N):
        x = i * 64 + 32 + rnd.uniform(-10, 10); L = rnd.uniform(6, 18); dr.polygon([(x - 8, 36), (x + 8, 36), (x + 4, 38 + L), (x - 4, 38 + L)]); dr.circle(x, 38 + L, 5.2)
        sh.capsule(x - 14, 8, x - 6, 6, 1.4)
    img.paint(dr.cov(), vgrad(30, 66, Ramp([(0, "#F060A0"), (1, "#E04890")]))); img.paint(sh.cov(), "#FFFFFF", 0.8)
    sp = [M() for _ in range(5)]; cols = ("#FFFFFF", "#6BE0FF", "#FFE45A", "#7CE070", "#B07CFF")
    for _ in range(110):
        x = rnd.uniform(0, W); y = rnd.uniform(14, 30); a = rnd.uniform(0, 3.14); L = 3.6; sp[rnd.randrange(5)].capsule(x, y, x + L * math.cos(a), y + L * math.sin(a), 1.1)
    for m, c in zip(sp, cols): img.paint(m.cov(), c)
    return img

def cloud():
    rnd = random.Random(351); items = []
    for i in range(22): items.append((i * (W / 22) + rnd.uniform(-8, 8), rnd.uniform(22, 36), rnd.uniform(9, 18)))
    top = column_cache(bumps_fn(20.0, items, 0.8)); img = Image(W, H)
    img.paint(strip_cov(top), vgrad(0, 192, PR([(0, "#FFFFFF"), (20, "#F4F8FF"), (60, "#D6E4F8"), (120, "#B4CBEA"), (192, "#98B4DE")])))
    pf = M(); pd = M()
    for _ in range(38):
        x = rnd.uniform(0, W); y = rnd.uniform(34, 170); r = rnd.uniform(14, 34); pf.circle(x - 3, y - 4, r * 0.85); pd.circle(x + 5, y + 7, r)
    img.paint(pd.cov().blur(5, 3, True), "#7C9ACC", 0.22); img.paint(pf.cov().blur(4, 3, True), "#FFFFFF", 0.4)
    hl_line(img, top, 5, "#FFFFFF", 0.9)
    return img

def coral():
    rnd = random.Random(361); items = []
    for i in range(30):
        x = i * (W / 30) + rnd.uniform(-5, 5); items.append((x, rnd.uniform(6, 11), rnd.uniform(6, 20)))
    top = column_cache(bumps_fn(24.0, items, 0.45)); img = Image(W, H)
    img.paint(strip_cov(top), vgrad(0, 192, PR([(0, "#FFB59A"), (12, "#FF7E6E"), (50, "#E0507A"), (110, "#B03C80"), (192, "#6E2A6C")])))
    po = M(); pl = M(); ho = M()
    for _ in range(160):
        x = rnd.uniform(0, W); y = rnd.uniform(34, 185); po.circle(x, y, rnd.uniform(1.6, 3.2)); pl.circle(x - 0.6, y - 0.7, 1.0)
    for _ in range(22): ho.ellipse(rnd.uniform(0, W), rnd.uniform(50, 170), rnd.uniform(5, 11), rnd.uniform(3, 6))
    img.paint(ho.cov(), "#4A1858", 0.6); img.paint(po.cov(), "#FFD8A8", 0.45); img.paint(pl.cov(), "#FFFFFF", 0.6)
    br = M()                                                                                   # branch lines inside body
    for k in range(10):
        x = rnd.uniform(0, W); br.polyline(zig(rnd, x, 28, x + rnd.uniform(-30, 30), rnd.uniform(70, 120), 5, 6), 1.8)
    img.paint(br.cov(), "#FFC2A0", 0.4); hl_line(img, top, 4, "#FFE4D0", 0.55)
    return img

def beach():
    rnd = random.Random(371); top = column_cache(lambda x: 6.5 + 1.2 * wave_fbm(x, W, 371, 2, 3)); img = Image(W, H)
    slab_top(img, top, Ramp([(0, "#FFFAEA"), (4, "#FBE9C2"), (30, "#F3D6A0"), (80, "#DDB982"), (110, "#B08A60"), (150, "#8A6A4C"), (192, "#74583F")]), 0.04, 0.05, 375)
    rp = M()
    for k in range(10):
        x0 = rnd.uniform(0, W); y0 = rnd.uniform(24, 70); L = rnd.uniform(80, 200); rp.polyline([(x0 + L * i / 10, y0 + 1.5 * math.sin(i + k)) for i in range(11)], 0.8)
    img.paint(rp.cov(), "#FFF8E0", 0.5)
    gr = M(); gd = M()
    for _ in range(380): x = rnd.uniform(0, W); y = rnd.uniform(12, 170); (gr if rnd.random() < .5 else gd).circle(x, y, rnd.uniform(0.6, 1.3))
    img.paint(gr.cov(), "#FFFFFF", 0.8); img.paint(gd.cov(), "#B58E5E", 0.6)
    # shells (fans with ribs) and starfish
    shell = M(); rib = M(); sh = M(); star = M(); stl = M()
    for i in range(9):
        cx = i * 113 + rnd.uniform(0, 40); cy = rnd.uniform(34, 100); R = rnd.uniform(8, 13); a0 = rnd.uniform(-0.3, 0.3)
        pts = [(cx, cy + 3)] + [(cx + R * math.cos(-math.pi * (0.1 + 0.8 * k / 12) + a0), cy + 3 + R * math.sin(-math.pi * (0.1 + 0.8 * k / 12) + a0) * 0.85) for k in range(13)]
        sh.polygon([(x + 1.5, y + 1.8) for x, y in pts]); shell.polygon(pts)
        for k in range(5): a = -math.pi * (0.2 + 0.6 * k / 4) + a0; rib.polyline([(cx, cy + 3), (cx + R * 0.95 * math.cos(a), cy + 3 + R * 0.8 * math.sin(a))], 0.5)
    for cx, cy in ((190, 118), (700, 124), (930, 104)):
        pts = [(cx + (11 if k % 2 == 0 else 4.5) * math.cos(-1.57 + k * 0.628), cy + (11 if k % 2 == 0 else 4.5) * math.sin(-1.57 + k * 0.628)) for k in range(10)]
        star.polygon(pts); stl.polygon([(x - 0.8, y - 0.8) for x, y in zip(*zip(*[(cx + 0.4 * (p[0] - cx), cy + 0.4 * (p[1] - cy)) for p in pts]))])
        for k in range(5): stl.circle(cx + 6 * math.cos(-1.57 + k * 1.2566), cy + 6 * math.sin(-1.57 + k * 1.2566), 0.9)
    img.paint(sh.cov(), "#8A6A4C", 0.5); img.paint(shell.cov(), vgrad(24, 110, Ramp([(0, "#FFE8E0"), (1, "#F2B8A8")]))); img.paint(rib.cov(), "#C98A7A", 0.8)
    img.paint(star.cov(), "#F07A4A"); img.paint(stl.cov(), "#FFC090", 0.9)
    wet = M(); wet.rrect(-10, 148, W + 10, 160, 5); img.paint(wet.cov().blur(5, 3, True), "#3E2C20", 0.25)
    hl_line(img, top, 3, "#FFFFFF", 0.5)
    return img

def floating_islands():
    top = column_cache(lambda x: 4.0 + 1.5 * wave_fbm(x, W, 381, 3, 6)); img = Image(W, H); rnd = random.Random(381)
    img.paint(strip_cov(top), lambda x, y: (Ramp([(0, "#9A8068"), (60, "#7A6250"), (120, "#5E4A3E"), (192, "#40322E")])(y)))
    # rock strata and cracks
    st = M(); sd = M()
    for k in range(7):
        y0 = 32 + k * 24; ph = rnd.uniform(0, 6.28); n = rnd.choice((3, 4, 5)); pts = [(x, y0 + 3 * math.sin(x / W * 6.2832 * n + ph)) for x in range(0, W + 1, 8)]; st.polyline(pts, 0.9); sd.polyline([(x, y + 2) for x, y in pts], 1.6)
    img.paint(sd.cov(), "#2A2020", 0.4); img.paint(st.cov(), "#B09A82", 0.45)
    ck = M()
    for k in range(14): x = rnd.uniform(0, W); ck.polyline(zig(rnd, x, rnd.uniform(34, 60), x + rnd.uniform(-20, 20), rnd.uniform(90, 180), 5, 4), 0.9)
    img.paint(ck.cov(), "#241A1A", 0.6)
    stones(img, rnd, 18, 40, 180, (7, 16), (4, 8), "#241A1A", Ramp([(0, "#9A8470"), (1, "#7C6652")]), shade_a=0.5, lit_a=0.6)
    # grass cap with scalloped lower edge and blade tufts
    gt = column_cache(lambda x: top(x) + 13 + 3.5 * math.sin(x / W * 6.2832 * 32) + 2 * math.sin(x / W * 6.2832 * 13 + 1))
    cap = Cov(W, H); cap.bb = (0, 0, W, 40)
    for x in range(W):
        for y in range(0, 40): cap.d[y * W + x] = max(0.0, min(1.0, y + 1 - top(x))) * max(0.0, min(1.0, gt(x) - y))
    img.paint(cap, vgrad(0, 24, Ramp([(0, "#A8E858"), (0.4, "#5EBE3A"), (1, "#2E8A2E")])))
    bl = M(); hl = M()
    for i in range(96):
        x = i * (W / 96) + rnd.uniform(-2, 2); t = top(x); bl.polygon([(x - 2, t + 1), (x, t - rnd.uniform(2, 3.5)), (x + 2, t + 1)])
    img.paint(bl.cov(), "#7CD64A"); hl_line(img, top, 3, "#E4FFB0", 0.6); lip_shadow(img, gt, 0, 6, 0.35, "#1E3A1E")
    return img

def ice_bridge():
    top = column_cache(lambda x: 4.5 + 0.8 * wave_fbm(x, W, 391, 2, 4)); img = Image(W, H); rnd = random.Random(391)
    slab_top(img, top, Ramp([(0, "#FFFFFF"), (3, "#E8F8FF"), (20, "#B4E2F6"), (100, "#7CBEE4"), (192, "#4A8AC0")]), 0.06, 0.05, 395)
    bk = M(); hi = M(); lo = M(); ck = M()
    for r in range(4):
        y0 = 4 + r * 46; off = 0 if r % 2 == 0 else 64
        for i in range(9):
            xa = i * 128 + off - 64 + 2; xb = xa + 124; bk.rrect(xa, y0 + 4, xb, y0 + 44, 7); hi.rrect(xa + 5, y0 + 6.5, xb - 5, y0 + 10, 1.5); lo.rrect(xa + 4, y0 + 36, xb - 2, y0 + 44, 3)
            if rnd.random() < 0.6: x = xa + rnd.uniform(20, 100); ck.polyline(zig(rnd, x, y0 + 14, x + rnd.uniform(-12, 12), y0 + 38, 4, 3), 0.8)
    gap = M(); gap.rrect(-10, 0, W + 10, 192, 0)
    img.paint(bk.cov(), vgrad(0, 192, PR([(0, "#DDF6FF"), (0.5, "#9ED8F2"), (1, "#5AA4D8")])), 0.85)
    img.paint(lo.cov(), "#3A78B8", 0.35); img.paint(hi.cov(), "#FFFFFF", 0.7); img.paint(ck.cov(), "#FFFFFF", 0.8)
    # snow cap with drift bumps along the lip
    sc = M(); items = [(rnd.uniform(0, W), rnd.uniform(14, 36), rnd.uniform(2, 4)) for _ in range(30)]
    cap = Cov(W, H); cap.bb = (0, 0, W, 20); f = bumps_fn(9.0, items, 0.8)
    for x in range(W):
        for y in range(0, 20): cap.d[y * W + x] = max(0.0, min(1.0, y + 1 - top(x))) * max(0.0, min(1.0, f(x) - y)) if False else max(0.0, min(1.0, y + 1 - (top(x) - max(0, 9 - f(x))))) * max(0.0, min(1.0, 9 + 3 * math.sin(x / W * 6.2832 * 24) * 0.6 - y))
    img.paint(cap, vgrad(0, 12, Ramp([(0, "#FFFFFF"), (1, "#DCEAF6")])))
    return img

def rope_bridge():
    top = column_cache(lambda x: 4.0); img = Image(W, H); rnd = random.Random(401); N = 16
    img.paint(strip_cov(column_cache(lambda x: 9.0)), vgrad(0, 192, PR([(0, "#4A3626"), (30, "#38281C"), (192, "#241810")])))   # dark under-structure
    # beam grain + hanger ropes down to the bottom
    gr = M(); hang = M(); hk = M()
    for _ in range(70): x = rnd.uniform(0, W); y = rnd.uniform(40, 185); gr.capsule(x, y, x + rnd.uniform(20, 60), y + rnd.uniform(-.5, .5), 0.7)
    for i in range(8):
        x = i * 128 + 64; hang.rrect(x - 4, 28, x + 4, 192, 2)
        for y in range(34, 190, 7): hk.polyline([(x - 4, y), (x + 4, y + 4)], 0.7)
    img.paint(gr.cov(), "#6A4C34", 0.5); img.paint(hang.cov(), vgrad(28, 192, Ramp([(0, "#C8A268"), (1, "#8A6A40")]))); img.paint(hk.cov(), "#5A4228", 0.85)
    # planks with 5 px gaps (gaps show the dark structure at y = 9)
    pl = [M() for _ in range(3)]; hl = M(); gn = M(); knot = M(); sd = M()
    for i in range(N):
        xa = i * 64 + 2.5; xb = xa + 59; ya = 4.0 + rnd.uniform(-0.3, 0.3); yb = 20 + rnd.uniform(-1, 1)
        pl[rnd.randrange(3)].rrect(xa, ya, xb, yb, 4); sd.rrect(xa + 1, ya + 2, xb + 1, yb + 3, 4); hl.rrect(xa + 3, ya + 1.2, xb - 3, ya + 3.8, 1.4)
        for _ in range(2): gx = rnd.uniform(xa + 6, xb - 24); gy = rnd.uniform(ya + 6, yb - 3); gn.capsule(gx, gy, gx + rnd.uniform(10, 22), gy + rnd.uniform(-.6, .6), 0.6)
    img.paint(sd.cov(), INK, 0.5)
    for m, (a, b) in zip(pl, (("#D9A868", "#A87840"), ("#CC9858", "#9A6C38"), ("#C08A4E", "#8E6030"))): img.paint(m.cov(), vgrad(3, 22, Ramp([(0, a), (1, b)])))
    img.paint(hl.cov(), "#FFE8BC", 0.55); img.paint(gn.cov(), "#6E4A28", 0.55)
    # twisted rope along the front (y 21..31) with diagonal twist marks and knots at the hangers
    rp = M(); tw = M(); rs = M()
    rp.rrect(-10, 21, W + 10, 31, 5)
    for x in range(0, W, 10): tw.polyline([(x, 31), (x + 6, 21)], 0.9)
    rs.rrect(-10, 29, W + 10, 33, 2)
    img.paint(rs.cov(), INK, 0.3); img.paint(rp.cov(), vgrad(21, 31, Ramp([(0, "#EAD29A"), (0.5, "#C8A868"), (1, "#8A6A3C")])))
    img.paint(tw.cov(), "#6E5028", 0.6)
    for i in range(8): knot.circle(i * 128 + 64, 26, 7.5)
    img.paint(knot.cov(), vgrad(18, 34, Ramp([(0, "#D8B878"), (1, "#7A5A30")])))
    return img

def salt_flat():
    top = column_cache(lambda x: 6.0 + 0.5 * wave_fbm(x, W, 411, 2, 3)); img = Image(W, H); rnd = random.Random(411)
    slab_top(img, top, Ramp([(0, "#FFFFFF"), (4, "#F6F2F4"), (30, "#E6E2EA"), (100, "#CDCADC"), (192, "#A8A8C4")]), 0.05, 0.05, 415)
    # hex crust: 64 px cells, rows of 34; ridge lines slightly raised (light) with a dark crack beside
    ck = M(); rl = M()
    R = 6
    for r in range(R):
        yc = 30 + r * 31
        for i in range(16):
            xc = i * 64 + (32 if r % 2 else 0); pts = [(xc + 36 * math.cos(math.radians(30 + 60 * k)) * 0.95 + rnd.uniform(-2, 2), yc + 22 * math.sin(math.radians(30 + 60 * k)) + rnd.uniform(-1.5, 1.5)) for k in range(7)]
            pts[-1] = pts[0]; ck.polyline(pts, 0.9); rl.polyline([(x - 1.2, y - 1.4) for x, y in pts], 0.9)
    img.paint(ck.cov(), "#8E8AA6", 0.55); img.paint(rl.cov(), "#FFFFFF", 0.9)
    sp = M(); pk = M()
    for _ in range(260): sp.circle(rnd.uniform(0, W), rnd.uniform(14, 185), rnd.uniform(0.6, 1.3))
    for _ in range(14): pk.ellipse(rnd.uniform(0, W), rnd.uniform(30, 170), rnd.uniform(30, 70), rnd.uniform(5, 10))
    img.paint(pk.cov().blur(5, 3, True), "#F4C8D8", 0.22); img.paint(sp.cov(), "#FFFFFF", 0.7)
    return img

SURFACES = {k: v for k, v in globals().items() if k in "lava_crust magma_bridge sea_water river candy cloud coral beach floating_islands ice_bridge rope_bridge salt_flat".split() and callable(v)}
ORDER = "lava_crust magma_bridge sea_water river candy cloud coral beach floating_islands ice_bridge rope_bridge salt_flat".split()

if __name__ == "__main__":
    ap = argparse.ArgumentParser(); ap.add_argument("--output", required=True); ap.add_argument("--surface", default="all"); ap.add_argument("--no-preview", action="store_true")
    a = ap.parse_args(); os.makedirs(a.output, exist_ok=True)
    for name in (ORDER if a.surface == "all" else a.surface.split(",")):
        im = solidify(SURFACES[name]()); write_png(im, os.path.join(a.output, "road_%s_1024x192.png" % name))
        print(name, "seam score:", round(tile_seam_score(im), 3), flush=True)
        if not a.no_preview: preview(im, os.path.join(a.output, "road_%s_preview_x3.png" % name))

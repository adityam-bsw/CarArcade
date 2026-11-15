#!/usr/bin/env python3
"""Smooth road strips, batch 3: the 13 Section C2 behaviour surfaces that had modules but no art. Same generator and conventions as
make_road.py / make_road2.py. Own code, no generative AI, no downloads.

    python3 tools/art/make_road3.py --output <dir> [--surface spring_road,magnetic_rail,...|all]

1024 x 192, seamless in X (4 px colour bleed added by write_png), each visual telegraphs the behaviour (Blueprint C2):
  spring_road     yellow launch deck with up-chevrons over red steel coils     magnetic_rail  red/silver pole blocks, copper coils, cyan sparks
  acid            lime slab, bubble domes on the lip, drips on the skirt       oil            near-black slick, rainbow sheen, thick drips
  conveyor(+_b)   hazard-chevron belt over steel frame and rollers; _b is the second animation frame (belt +32 px, roller spokes +45 deg)
  lava_river      glowing lava with dark crust islands that rise above it      sticky_candy   raspberry taffy, white stripes, stretched strings
  honey           amber slab with honeycomb cells and fat golden drips         metal_plates   riveted plates, heat gradient, glowing seams
  ice_crystals    pale base with a spiky crystal crest and shards              haunted_shadow dark violet, dissolving body, dashed ghost line, wisps
  electric_rail(+_off)  conductor rail: charged segments (arcs, cyan) alternate with safe segments (green lamp); _off = all dormant
  fragile_paper(+_torn) stacked torn-edge sheets with perforation lines;       _torn = three ragged tears through the whole strip
"""
import argparse, math, os, random, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from smoothart import *
from make_road import W, H, SS, INK, periodic_dist, strip_cov, column_cache, mottled, lip_shadow, stones, skirt_fade, preview
from make_road2 import blob, bumps_fn

def M(): return Mask(W, H, SS, True)

def slab_top(img, top, ramp, k1=0.05, k2=0.03, seed=5):
    img.paint(strip_cov(top), mottled(ramp, top, k1, k2, seed))

def zig(rnd, x0, y0, x1, y1, n, amp):
    pts = [(x0, y0)]
    for i in range(1, n):
        t = i / n; pts.append((x0 + (x1 - x0) * t, y0 + (y1 - y0) * t + rnd.uniform(-amp, amp)))
    pts.append((x1, y1)); return pts

def drips(img, rnd, xs, y0, lens, neck, bulb, ramp_fn, hi="#FFFFFF", hi_a=0.5):
    """Hanging drips: a capsule neck and a round bulb, painted over the (already faded) skirt, lit from the upper left."""
    body = M(); lit = M()
    for x, ln in zip(xs, lens):
        w0 = neck * 2.8; body.polygon([(x - w0, y0 - 6), (x + w0, y0 - 6), (x + neck, y0 + ln * 0.6), (x - neck, y0 + ln * 0.6)]); body.capsule(x, y0 + ln * 0.5, x, y0 + ln, neck); body.circle(x, y0 + ln, bulb)
        lit.capsule(x - bulb * 0.35, y0 + 4, x - bulb * 0.35, y0 + ln - 2, max(0.8, neck * 0.28)); lit.circle(x - bulb * 0.35, y0 + ln - bulb * 0.3, bulb * 0.28)
    img.paint(body.cov(), ramp_fn); img.paint(lit.cov(), hi, hi_a)

# ------------------------------------------------------------------------------------------------------------ spring road
def spring_road():
    rnd = random.Random(201); img = Image(W, H)
    # coils first (deck overlaps them); gaps between coil stacks stay transparent so the road reads as "on springs"
    back = M(); front = M(); hi = M(); core = M(); cast = M()
    for i in range(8):
        cx = i * 128 + 64; y = 34.0
        core.rrect(cx - 7, 28, cx + 7, 150, 3)
        for k in range(9):
            yy = y + k * 13.0
            back.polyline([(cx + 30, yy - 6), (cx - 30, yy + 2)], 3.6)                       # far strand (behind, darker)
            front.polyline([(cx - 30, yy + 2), (cx + 30, yy + 7.5)], 4.6)                    # near strand (the visible slant)
            hi.polyline([(cx - 24, yy + 0.4), (cx + 22, yy + 5)], 1.2)
        cast.rrect(cx - 34, 28, cx + 34, 33, 2)
    img.paint(core.cov(), vgrad(28, 150, Ramp([(0, "#3B3F4C"), (1, "#23262F")])))
    img.paint(back.cov(), vgrad(30, 150, Ramp([(0, "#9C2E2A"), (1, "#5E1A1C")])))
    img.paint(front.cov(), vgrad(30, 150, Ramp([(0, "#F0584A"), (0.5, "#CF3A33"), (1, "#8A2024")])))
    img.paint(hi.cov(), "#FFD0C0", 0.7)
    # deck: yellow plate with darker underside, up-chevrons (the launch cue) and bolts
    deck = M(); deck.rrect(-10, 2, W + 10, 24, 4); under = M(); under.rrect(-10, 20, W + 10, 30, 2)
    img.paint(under.cov(), vgrad(20, 30, Ramp([(0, "#5A4A22"), (1, "#2C2A2A")])))
    img.paint(deck.cov(), vgrad(2, 24, Ramp([(0, "#FFF08A"), (0.15, "#FFD93E"), (0.7, "#F0AE1E"), (1, "#C98410")])))
    ch = M(); ch2 = M(); bolts = M()
    for i in range(16):
        cx = i * 64 + 32
        ch.polygon([(cx - 14, 17), (cx, 6.5), (cx + 14, 17), (cx + 14, 12.5), (cx, 2.5 + 4.5 - 0.0), (cx - 14, 12.5)])
        bolts.circle(cx - 28, 13, 1.5); bolts.circle(cx + 28, 13, 1.5)
    img.paint(ch.cov(), "#B25A10", 0.9); img.paint(bolts.cov(), "#8A5A14", 0.9)
    gl = M(); gl.rrect(-10, 3, W + 10, 5.2, 1); img.paint(gl.cov(), "#FFFFFF", 0.55)
    skirt_fade(img, 98, 190)
    return img

# --------------------------------------------------------------------------------------------------------- magnetic rail
def magnetic_rail():
    rnd = random.Random(211); TOP = 4.0; top = column_cache(lambda x: TOP)
    img = Image(W, H)
    slab_top(img, top, Ramp([(0, "#B9C6D6"), (3, "#6F86A3"), (14, "#4A6080"), (30, "#3A4C69"), (100, "#2E3C57"), (192, "#1A2236")]), 0.05, 0.04, 215)
    # horseshoe field lines under the pole blocks (cyan, faint): arcs linking neighbouring poles
    fl = M()
    for i in range(16):
        x0 = i * 64 + 32
        for r in (14, 22, 30):
            pts = [(x0 + r * 2.1 * (0.5 - 0.5 * math.cos(math.pi * t / 12)) - 0, 18 + r * 0.9 * math.sin(math.pi * t / 12)) for t in range(13)]
            fl.polyline([(x0 + (px - x0) * (64 / (r * 2.1)) if False else px, py) for px, py in pts], 0.8)
    img.paint(fl.cov(), "#6FE8FF", 0.35)
    # pole blocks along the top: red (N) / silver (S) alternating every 64 px with N / S bevels
    red = M(); sil = M(); hl = M()
    for i in range(16):
        xa, xb = i * 64 + 3, (i + 1) * 64 - 3
        (red if i % 2 == 0 else sil).rrect(xa, 1.5, xb, 20, 3.5); hl.rrect(xa + 3, 2.6, xb - 3, 5.4, 1.4)
    img.paint(red.cov(), vgrad(1.5, 20, Ramp([(0, "#FF6A55"), (0.5, "#D6372F"), (1, "#8E1E22")])))
    img.paint(sil.cov(), vgrad(1.5, 20, Ramp([(0, "#F4F7FA"), (0.5, "#C4CDD6"), (1, "#8590A0")])))
    img.paint(hl.cov(), "#FFFFFF", 0.6)
    # copper winding blocks below, every 128 px: tight vertical wire turns
    blk = M(); wires = M(); wl = M()
    for i in range(8):
        xa = i * 128 + 22; xb = xa + 84
        blk.rrect(xa, 34, xb, 98, 6)
        for k in range(21):
            x = xa + 4 + k * 3.8; wires.rrect(x, 36, x + 2.4, 96, 1.0); wl.rrect(x, 36, x + 0.9, 96, 0.4)
    img.paint(blk.cov(), "#3B2418")
    img.paint(wires.cov(), vgrad(34, 98, Ramp([(0, "#FFB066"), (0.5, "#D9772A"), (1, "#8E4A18")])))
    img.paint(wl.cov(), "#FFE2B8", 0.55)
    # sparks: little bright zig-zag bolts jumping across the pole gaps (on the lip), plus star glints
    sp = M(); gl = M(); st = M()
    for i in range(16):
        if i % 3 == 0:
            x = i * 64 + 64; pts = zig(rnd, x - 9, 4, x + 9, 16, 5, 2.4); sp.polyline(pts, 0.9); gl.polyline(pts, 2.6)
            r = 3.2; st.polygon([(x, 22 - r), (x + r * .3, 22 - r * .3), (x + r, 22), (x + r * .3, 22 + r * .3), (x, 22 + r), (x - r * .3, 22 + r * .3), (x - r, 22), (x - r * .3, 22 - r * .3)])
    img.paint(gl.cov().blur(2, 2, True), "#5FE0FF", 0.6); img.paint(sp.cov(), "#E8FFFF"); img.paint(st.cov(), "#FFFFFF", 0.9)
    lip_shadow(img, top, 20, 32, 0.18, "#0C1220")
    skirt_fade(img, 90, 190)
    return img

# ------------------------------------------------------------------------------------------------------------------ acid
def acid():
    rnd = random.Random(221)
    items = [(rnd.uniform(0, W), rnd.uniform(7, 15), rnd.uniform(3, 9)) for _ in range(38)]            # bubble domes bulging up
    top = column_cache(bumps_fn(11.0, items, 0.7))
    img = Image(W, H)
    slab_top(img, top, Ramp([(0, "#D8F466"), (4, "#A2D434"), (14, "#6EAE1A"), (40, "#3F8014"), (100, "#256012"), (192, "#123812")]), 0.12, 0.06, 225)
    # bubbles inside the slab: translucent discs with bright rims and a glint (the "green bubbling" cue)
    fill = M(); ring = M(); glint = M(); dark = M()
    for _ in range(46):
        x = rnd.uniform(0, W); y = rnd.uniform(24, 130); r = rnd.uniform(4, 13)
        fill.circle(x, y, r); inner = M()
        ring.circle(x, y, r); dark.circle(x + 1.5, y + 1.5, r * 0.8); glint.circle(x - r * 0.35, y - r * 0.38, max(1.0, r * 0.2))
    img.paint(fill.cov(), "#D9FF7A", 0.22)
    rc = ring.cov(); inner = M()
    for _ in range(0): pass
    img.paint(rc.blur(1, 1, True), "#F4FFC0", 0.18)
    # crisp rings: draw each ring as an outline polyline circle
    rg = M()
    rnd2 = random.Random(222)
    for _ in range(46):
        x = rnd2.uniform(0, W); y = rnd2.uniform(24, 130); r = rnd2.uniform(4, 13)
        rg.polyline([(x + r * math.cos(a * 0.3927), y + r * math.sin(a * 0.3927)) for a in range(17)], 0.8)
    img.paint(rg.cov(), "#F2FFC4", 0.8); img.paint(glint.cov(), "#FFFFFF", 0.9)
    # darker toxic mottling
    tox = M()
    for _ in range(26): tox.ellipse(rnd.uniform(0, W), rnd.uniform(30, 120), rnd.uniform(18, 50), rnd.uniform(5, 12))
    img.paint(tox.cov().blur(5, 3, True), "#246A1A", 0.30)
    # dome highlights on the lip bubbles
    hd = M()
    for bx, hw, hh in items:
        if hh > 5: hd.ellipse(bx - hw * 0.3, 11 - hh * 0.55 + 2, hw * 0.28, hh * 0.22)
    img.paint(hd.cov(), "#FFFFFF", 0.7)
    lip_shadow(img, top, 16, 28, 0.15, "#1B501C")
    skirt_fade(img, 100, 176)
    xs = [i * 64 + rnd.uniform(12, 52) for i in range(16)]
    drips(img, rnd, xs, 122, [rnd.uniform(12, 44) for _ in xs], 4.0, 6.5, vgrad(122, 190, Ramp([(0, "#5DB41C"), (1, "#B5F23A")])), "#F4FFC0", 0.6)
    return img

# ------------------------------------------------------------------------------------------------------------------- oil
def oil():
    rnd = random.Random(231)
    top = column_cache(lambda x: 6.0 + 0.8 * wave_fbm(x, W, 233, octaves=2, base=3))
    img = Image(W, H)
    slab_top(img, top, Ramp([(0, "#5B5368"), (3, "#2B2634"), (16, "#17131F"), (60, "#110E18"), (192, "#08060C")]), 0.05, 0.03, 235)
    # rainbow sheen: long wavy bands of violet / teal / gold, soft
    for col, ya, yb, seed in (("#8A45E8", 26, 64, 1), ("#22C4B0", 40, 90, 2), ("#E2B83E", 56, 108, 3), ("#E0508E", 70, 120, 4)):
        m = M()
        for k in range(9):
            x0 = rnd.uniform(0, W); y0 = rnd.uniform(ya, yb); L = rnd.uniform(150, 320); ph = rnd.uniform(0, 6.28)
            m.polyline([(x0 + L * i / 14, y0 + 5 * math.sin(ph + i * 0.8)) for i in range(15)], rnd.uniform(2.5, 5.5))
        img.paint(m.cov().blur(3, 3, True), col, 0.42)
    # puddles: ellipses with a bright rim crescent top-left
    pud = M(); rim = M(); gloss = M()
    for _ in range(9):
        x = rnd.uniform(0, W); y = rnd.uniform(34, 118); rx = rnd.uniform(26, 62); ry = rx * rnd.uniform(.18, .28)
        pud.ellipse(x, y, rx, ry); rim.ellipse(x - 1, y - 1.4, rx + 1, ry + 1); gloss.capsule(x - rx * 0.55, y - ry * 0.35, x - rx * 0.1, y - ry * 0.6, 1.1)
    img.paint(rim.cov(), "#8A7CA8", 0.45); img.paint(pud.cov(), "#0A0710", 0.85); img.paint(gloss.cov(), "#FFFFFF", 0.7)
    # glossy streak on the lip: the "oil sheen" reads at a glance
    g = M(); g2 = M()
    for i in range(16):
        x = i * 64 + 6; g.capsule(x, 4.2, x + 34, 4.2, 1.0); g2.capsule(x + 40, 4.4, x + 50, 4.4, 0.8)
    img.paint(g.cov(), "#FFFFFF", 0.65); img.paint(g2.cov(), "#C9B8FF", 0.6)
    lip_shadow(img, top, 16, 28, 0.0)
    skirt_fade(img, 100, 170)
    xs = [i * 85 + rnd.uniform(10, 70) for i in range(12)]
    drips(img, rnd, xs, 118, [rnd.uniform(14, 44) for _ in xs], 4.6, 7.0, vgrad(118, 190, Ramp([(0, "#1A1522"), (1, "#0A070F")])), "#B9A8F0", 0.55)
    return img

# ------------------------------------------------------------------------------------------------------------------- conveyor
def conveyor(frame=0):
    img = Image(W, H); shift = 32 * frame; ang = frame * math.pi / 4
    # steel frame behind the belt, with side plates and rollers (circles every 128 px)
    plate = M(); plate.rrect(-10, 12, W + 10, 84, 2)
    img.paint(plate.cov(), vgrad(12, 84, Ramp([(0, "#7C8895"), (0.15, "#5E6A77"), (1, "#3B444F")])))
    ro = M(); rin = M(); hub = M(); spokes = M(); sh = M()
    for i in range(8):
        cx = i * 128 + 64; cy = 49
        sh.circle(cx + 1.5, cy + 2.2, 31); ro.circle(cx, cy, 30); rin.circle(cx, cy, 25); hub.circle(cx, cy, 8)
        for k in range(4):
            a = ang + k * math.pi / 2; spokes.capsule(cx + 9 * math.cos(a), cy + 9 * math.sin(a), cx + 23 * math.cos(a), cy + 23 * math.sin(a), 2.4)
    img.paint(sh.cov(), "#1C2128", 0.55)
    img.paint(ro.cov(), "#2F363F"); img.paint(rin.cov(), vgrad(24, 74, Ramp([(0, "#AEB9C4"), (1, "#69747F")])))
    img.paint(spokes.cov(), "#3F4852"); img.paint(hub.cov(), vgrad(41, 57, Ramp([(0, "#F2F5F8"), (1, "#8D98A4")])))
    # belt: dark rubber band on top, a thicker lip with yellow chevrons pointing right, tread ticks
    belt = M(); belt.rrect(-10, 2, W + 10, 15, 3.5)
    img.paint(belt.cov(), vgrad(2, 15, Ramp([(0, "#5C646E"), (0.25, "#3C434B"), (1, "#252A30")])))
    cv = M(); tk = M()
    for i in range(-1, 17):
        x = i * 64 + shift
        cv.polygon([(x + 8, 3.6), (x + 20, 3.6), (x + 31, 8.5), (x + 20, 13.4), (x + 8, 13.4), (x + 19, 8.5)])
        tk.rrect(x + 36, 4, x + 37.4, 13, 0.4); tk.rrect(x + 46, 4, x + 47.4, 13, 0.4)
    img.paint(tk.cov(), "#14181C", 0.7); img.paint(cv.cov(), vgrad(3, 14, Ramp([(0, "#FFE066"), (1, "#E6A21C")])))
    # belt return strand under the frame
    ret = M(); ret.rrect(-10, 76, W + 10, 88, 3)
    img.paint(ret.cov(), vgrad(76, 88, Ramp([(0, "#3A4048"), (1, "#1E2227")])))
    gl = M(); gl.rrect(-10, 2.4, W + 10, 3.8, 0.6); img.paint(gl.cov(), "#FFFFFF", 0.35)
    skirt_fade(img, 92, 188)
    return img

# ----------------------------------------------------------------------------------------------------------------- lava river
def lava_river():
    rnd = random.Random(241)
    isl = [(60 + i * 170 + rnd.uniform(-20, 20), rnd.uniform(34, 52), rnd.uniform(9, 13)) for i in range(6)]
    lavatop = lambda x: 16.0 + 1.2 * wave_fbm(x, W, 243, octaves=2, base=4)
    def topf(x):
        t = lavatop(x)
        for bx, hw, hh in isl:
            d = periodic_dist(x, bx)
            if d < hw: t = min(t, 16.0 - hh * min(1.0, (1 - d / hw) * 3.2) ** 0.7)
        return t
    top = column_cache(topf); lt = column_cache(lavatop)
    img = Image(W, H)
    img.paint(strip_cov(top), vgrad(0, 192, Ramp([(0, "#FFD070"), (0.10, "#FF9A28"), (0.18, "#F2601E"), (0.36, "#C83A18"), (0.68, "#8E2012"), (1, "#4A0E0C")])))
    # current: flowing bright veins and dark cooled crust flakes drifting in the lava body
    vn = M(); crust = M(); crm = M()
    for _ in range(18):
        x0 = rnd.uniform(0, W); y0 = rnd.uniform(26, 130); L = rnd.uniform(90, 220); ph = rnd.uniform(0, 6.28)
        vn.polyline([(x0 + L * i / 12, y0 + 4 * math.sin(ph + i * 0.7)) for i in range(13)], rnd.uniform(1.6, 3.0))
    for _ in range(40):
        x = rnd.uniform(0, W); y = rnd.uniform(28, 140); rx = rnd.uniform(10, 26); ry = rx * rnd.uniform(.28, .4)
        pts = blob(rnd, x, y, rx, ry, 6, .25); crust.polygon(pts); crm.polygon([(a - .8, b - 1.2) for a, b in pts])
    img.paint(vn.cov().blur(1, 2, True), "#FFE98A", 0.9)
    img.paint(crm.cov(), "#FF8A2E", 0.9); img.paint(crust.cov(), vgrad(26, 140, Ramp([(0, "#4A2C28"), (1, "#2A1414")])))
    # lip glow band where the lava surface is
    gl = Cov(W, H); gl.bb = (0, 0, W, 60)
    for x in range(W):
        t = lt(x)
        for y in range(0, 40): gl.d[y * W + x] = max(0.0, 1 - abs(y + .5 - t - 3) / 9.0) * (0.0 if top(x) < t - 0.5 else 1.0)
    img.paint(gl, "#FFF2A8", 0.55)
    # islands: dark basalt platforms standing above the lava, orange rim light underneath, brighter lit top edge
    ic = Cov(W, H); ic.bb = (0, 0, W, 64); ih = Cov(W, H); ih.bb = (0, 0, W, 64); ig = Cov(W, H); ig.bb = (0, 0, W, 80)
    for bx, hw, hh in isl:
        for x in range(int(bx - hw - 6), int(bx + hw + 6)):
            xi = x % W; d = abs(x + .5 - bx); t = top(xi)
            if d >= hw or t > 15.0: continue
            bot = 31.0 + 1.2 * math.sin((x + bx) * 0.21) - 10 * (d / hw) ** 3
            for y in range(max(0, int(t)), int(bot) + 2):
                a = min(1.0, max(0.0, y + 1 - t)) * min(1.0, max(0.0, bot + 1 - y))
                ic.d[y * W + xi] = max(ic.d[y * W + xi], a)
                if y < t + 3: ih.d[y * W + xi] = max(ih.d[y * W + xi], a * (1 - (y - t) / 3.5))
                if y > bot - 8: ig.d[y * W + xi] = max(ig.d[y * W + xi], a * (1 - (bot - y) / 9))
    img.paint(ic, vgrad(2, 34, Ramp([(0, "#7A5A52"), (0.25, "#4E3430"), (1, "#2E1A18")])))
    img.paint(ig, "#FF7A22", 0.75); img.paint(ih, "#C9A092", 0.8)
    # fissure glow on islands
    fz = M()
    for bx, hw, hh in isl:
        fz.polyline(zig(rnd, bx - hw * .5, 14, bx + hw * .4, 20, 5, 2.4), 0.8)
    img.paint(fz.cov().multiplied(ic), "#FF9A30", 0.9)
    skirt_fade(img, 94, 190)
    return img

# ------------------------------------------------------------------------------------------------------------ sticky candy
def sticky_candy():
    rnd = random.Random(251)
    bumps = [(rnd.uniform(0, W), rnd.uniform(16, 36), rnd.uniform(4, 10)) for _ in range(26)]
    def topf(x):
        t = 12.0 + 0.8 * wave_fbm(x, W, 253, octaves=2, base=4)
        for bx, hw, hh in bumps:
            d = periodic_dist(x, bx)
            if d < hw: t -= hh * (0.5 + 0.5 * math.cos(math.pi * d / hw)) ** 0.7
        return t
    top = column_cache(topf); img = Image(W, H)
    slab_top(img, top, Ramp([(0, "#FFEAF3"), (3, "#FFC6DE"), (16, "#FFA0C8"), (50, "#F57DB4"), (120, "#DC5E98"), (192, "#A03472")]), 0.06, 0.04, 255)
    # candy stripes: diagonal white bands, 64 px pitch (divides the width, so they wrap)
    st = M(); st2 = M()
    for i in range(-1, 18):
        x = i * 64 + 6; st.polygon([(x, 20), (x + 16, 20), (x + 46, 150), (x + 30, 150)])
        st2.polygon([(x + 22, 20), (x + 26, 20), (x + 56, 150), (x + 52, 150)])
    img.paint(st.cov(), "#FFFFFF", 0.38); img.paint(st2.cov(), "#8E2060", 0.20)
    # stretched taffy strings crossing the body: thin, sagging, bright
    sg = M(); shd = M()
    for _ in range(16):
        x0 = rnd.uniform(0, W); L = rnd.uniform(70, 160); y0 = rnd.uniform(36, 112); sag = rnd.uniform(6, 14)
        pts = [(x0 + L * t / 12, y0 + sag * math.sin(math.pi * t / 12)) for t in range(13)]
        sg.polyline(pts, 1.1); shd.polyline([(a, b + 2.2) for a, b in pts], 1.2)
    img.paint(shd.cov(), "#6E1648", 0.35); img.paint(sg.cov(), "#FFE6F1", 0.85)
    # gloss on lip bumps
    gl = M()
    for bx, hw, hh in bumps:
        if hh > 5: gl.capsule(bx - hw * .45, 12 - hh * .55 + 1.5, bx + hw * .05, 12 - hh * .8 + 1.5, 1.1)
    img.paint(gl.cov(), "#FFFFFF", 0.85)
    lip_shadow(img, top, 16, 28, 0.15, "#7C1C52")
    skirt_fade(img, 104, 172)
    # stretchy gooey drips ending in a bulb, with a pulled neck (the sticky cue)
    xs = [i * 64 + rnd.uniform(14, 50) for i in range(16)]; lens = [rnd.uniform(14, 52) for _ in xs]
    drips(img, rnd, xs, 122, lens, 3.4, 6.5, vgrad(122, 190, Ramp([(0, "#B83078"), (1, "#F27AB0")])), "#FFE6F1", 0.7)
    return img

# ------------------------------------------------------------------------------------------------------------------ honey
def honey():
    rnd = random.Random(261)
    top = column_cache(lambda x: 8.0 + 1.1 * wave_fbm(x, W, 263, octaves=2, base=3))
    img = Image(W, H)
    slab_top(img, top, Ramp([(0, "#F6D070"), (3, "#EAA92C"), (14, "#C98414"), (44, "#A46408"), (110, "#784202"), (192, "#442402")]), 0.05, 0.04, 265)
    # honeycomb cells: pointy-top hexagons, 64 px wide, 48 px row pitch, odd rows offset 32 (wraps)
    wall = M(); cellhi = M()
    R = 64 / math.sqrt(3)
    for r in range(0, 4):
        cy = 44 + r * 48 * 0.75 * 1.0 + 0
        cy = 40 + r * 48.0
        for i in range(-1, 17):
            cx = i * 64 + (32 if r % 2 else 0)
            pts = [(cx + R * 0.93 * math.cos(math.pi / 6 + k * math.pi / 3), cy + R * 0.93 * math.sin(math.pi / 6 + k * math.pi / 3) * 0.72) for k in range(7)]
            wall.polyline(pts, 1.6)
            cellhi.polyline(pts[2:5], 0.9)
    img.paint(wall.cov(), vgrad(30, 190, Ramp([(0, "#7A4308"), (1, "#3A1E04")])), 0.6)
    img.paint(cellhi.cov(), "#FFE08A", 0.55)
    # translucent glow patches and air bubbles
    gp = M(); bb = M(); bh = M()
    for _ in range(14): gp.ellipse(rnd.uniform(0, W), rnd.uniform(26, 110), rnd.uniform(24, 60), rnd.uniform(6, 14))
    for _ in range(40):
        x = rnd.uniform(0, W); y = rnd.uniform(22, 130); r = rnd.uniform(1.6, 4.4); bb.circle(x, y, r); bh.circle(x - r * .3, y - r * .3, r * .35)
    img.paint(gp.cov().blur(5, 3, True), "#FFD25A", 0.28); img.paint(bb.cov(), "#FFE9A8", 0.45); img.paint(bh.cov(), "#FFFFFF", 0.85)
    # wide glossy meniscus streaks along the lip
    g = M()
    for i in range(8): g.capsule(i * 128 + 20, 6.2, i * 128 + 88, 6.4, 1.4)
    img.paint(g.cov(), "#FFFBE0", 0.75)
    lip_shadow(img, top, 16, 28, 0.14, "#5E3406")
    skirt_fade(img, 104, 166)
    xs = [i * 128 + rnd.uniform(24, 100) for i in range(8)]
    drips(img, rnd, xs, 120, [rnd.uniform(14, 50) for _ in xs], 6.5, 10.5, vgrad(120, 190, Ramp([(0, "#9C5C0A"), (0.45, "#E8A218"), (1, "#FFD25A")])), "#FFF6CC", 0.75)
    return img

# ----------------------------------------------------------------------------------------------------------- metal plates
def metal_plates():
    rnd = random.Random(271); img = Image(W, H)
    # dark furnace behind, visible through the seams, glowing near the top
    back = M(); back.rrect(-10, 2, W + 10, 140, 1); img.paint(back.cov(), vgrad(2, 140, Ramp([(0, "#FF9A30"), (0.3, "#C2441A"), (1, "#2A1410")])))
    heat_c = Ramp([(0, "#8A94A0"), (0.35, "#A8805C"), (0.65, "#CC5A2C"), (1, "#E04818")])
    cold_c = Ramp([(0, "#9AA4AE"), (1, "#566170")])
    rows = ((3, 62, 0), (68, 126, 128))
    heats = [[0.0, 0.55, 1.0, 0.25], [0.7, 0.1, 0.4, 0.9]]
    for ri, (ya, yb, off) in enumerate(rows):
        for i in range(4):
            xa = off + i * 256 + 5; xb = xa + 246; h = heats[ri][i]
            yt = ya + (rnd.uniform(-0.6, 0.6) if ri == 0 else 0)
            pl = M(); pl.rrect(xa, yt, xb, yb, 6); sh = M(); sh.rrect(xa + 1, yt + 2, xb + 1.5, yb + 3, 6)
            # wrap handling for the offset row: Mask is wrap_x so a plate crossing x=W reappears at x=0
            img.paint(sh.cov(), "#150B08", 0.6)
            img.paint(pl.cov(), (lambda y0, y1, h=h: lambda x, y: lerp(lerp(cold_c(0.2 + 0.6 * (y - y0) / (y1 - y0)), heat_c(0.15 + 0.85 * (y - y0) / (y1 - y0)), min(1.0, h * 1.15)), (255, 210, 120), max(0.0, h - 0.8) * 1.4))(yt, yb))
            # rivets and a bevel highlight
            rv = M(); rh = M(); bv = M()
            for cx, cy in ((xa + 12, yt + 12), (xb - 12, yt + 12), (xa + 12, yb - 12), (xb - 12, yb - 12), ((xa + xb) / 2, yt + 12), ((xa + xb) / 2, yb - 12)):
                rv.circle(cx, cy, 3.6); rh.circle(cx - 1, cy - 1, 1.4)
            bv.rrect(xa + 4, yt + 1.2, xb - 4, yt + 4.2, 1.5)
            img.paint(rv.cov(), lerp((70, 78, 88), (140, 50, 20), h)); img.paint(rh.cov(), "#FFFFFF", 0.6); img.paint(bv.cov(), "#FFFFFF", 0.3 + 0.1 * (1 - h))
            if h > 0.5:                                # heat shimmer: bright seams and a hot core glow on the plate
                gl = M(); gl.ellipse((xa + xb) / 2, (yt + yb) / 2, 80, 14); img.paint(gl.cov().blur(5, 3, True), "#FFD070", 0.5 * h)
    # scorch marks and scratches
    sc = M()
    for _ in range(70): x = rnd.uniform(0, W); y = rnd.uniform(8, 120); sc.capsule(x, y, x + rnd.uniform(8, 34), y + rnd.uniform(-3, 3), 0.9)
    img.paint(sc.cov(), "#1A0E0A", 0.55)
    skirt_fade(img, 100, 176)
    return img

# ---------------------------------------------------------------------------------------------------------- ice crystals
def ice_crystals():
    rnd = random.Random(281); BASE = 26.0; top = column_cache(lambda x: BASE + 0.8 * wave_fbm(x, W, 283, octaves=2, base=4))
    img = Image(W, H)
    slab_top(img, top, Ramp([(0, "#D4F0FF"), (5, "#A8D6FA"), (24, "#82B4F0"), (70, "#6684DA"), (192, "#34459E")]), 0.06, 0.03, 285)
    # embedded shards in the body: angular lighter / darker facets
    for i in range(26):
        cx, cy = rnd.uniform(0, W), rnd.uniform(34, 140); R = rnd.uniform(14, 40); a0 = rnd.uniform(0, 6.28); n = rnd.choice((3, 4))
        pts = [(cx + R * rnd.uniform(.6, 1.1) * math.cos(a0 + 6.2832 * k / n), cy + R * .7 * rnd.uniform(.6, 1.1) * math.sin(a0 + 6.2832 * k / n)) for k in range(n)]
        m = M(); m.polygon(pts); img.paint(m.cov(), "#F0FAFF" if i % 2 == 0 else "#3E4EB0", 0.3 if i % 2 == 0 else 0.25)
    cr = M()
    for _ in range(10):
        x = rnd.uniform(0, W); y = rnd.uniform(30, 110); cr.polyline(zig(rnd, x, y, x + rnd.uniform(20, 50), y + rnd.uniform(-20, 20), 4, 5), 0.8)
    img.paint(cr.cov(), "#F4FBFF", 0.8)
    # crystal crest: hexagonal prisms, each a lit left facet and a shaded right facet with a bright tip; three heights per cluster
    lit = M(); shd = M(); tip = M(); edge = M(); back_l = M(); back_s = M()
    def crystal(cx, base, w, h, lean, L, S, T):
        tx = cx + lean; ty = base - h
        L.polygon([(cx - w, base + 3), (cx - w * .9 + lean * .2, base - h * .72), (tx, ty), (cx + lean * .1, base + 3)])
        S.polygon([(cx + lean * .1, base + 3), (tx, ty), (cx + w * .9 + lean * .2, base - h * .72), (cx + w, base + 3)])
        T.polygon([(tx, ty), (tx - w * .38, ty + h * .26), (tx + w * .38, ty + h * .26)])
    for i in range(34):
        cx = i * (W / 34.0) + rnd.uniform(-8, 8); base = BASE + rnd.uniform(0, 2)
        h = rnd.uniform(11, 21) if i % 5 else rnd.uniform(20, 26); w = rnd.uniform(5.5, 9.5)
        crystal(cx, base, w, min(h, base - 1.0), rnd.uniform(-3, 3), lit, shd, tip)
        if i % 3 == 0: crystal(cx + rnd.uniform(5, 9), base + 1, w * .8, h * .62, rnd.uniform(-2, 4), back_l, back_s, tip)
    img.paint(back_s.cov(), "#5876C8"); img.paint(back_l.cov(), "#9AC8F6")
    img.paint(shd.cov(), vgrad(0, 22, Ramp([(0, "#8FB6F0"), (1, "#4C68C0")])))
    img.paint(lit.cov(), vgrad(0, 22, Ramp([(0, "#FFFFFF"), (1, "#B8E0FF")])))
    img.paint(tip.cov(), "#FFFFFF", 0.95)
    sp = M()
    for _ in range(44):
        x = rnd.uniform(0, W); y = rnd.uniform(26, 126); r = rnd.uniform(1.6, 3.4)
        sp.polygon([(x, y - r), (x + r * .25, y - r * .25), (x + r, y), (x + r * .25, y + r * .25), (x, y + r), (x - r * .25, y + r * .25), (x - r, y), (x - r * .25, y - r * .25)])
    img.paint(sp.cov(), "#FFFFFF", 0.9)
    lip_shadow(img, top, 12, 24, 0.14, "#34459E")
    skirt_fade(img, 80, 176)
    return img

# ---------------------------------------------------------------------------------------------------------- haunted shadow
def haunted_shadow():
    rnd = random.Random(291)
    top = column_cache(lambda x: 8.0 + 1.0 * wave_fbm(x, W, 293, octaves=2, base=3))
    img = Image(W, H)
    slab_top(img, top, Ramp([(0, "#B2A0E0"), (3, "#6A56A4"), (16, "#46367E"), (60, "#33275C"), (192, "#1C1438")]), 0.10, 0.06, 295)
    # drifting pale mist bands and a ghostly lit core
    ms = M()
    for _ in range(16): ms.ellipse(rnd.uniform(0, W), rnd.uniform(28, 120), rnd.uniform(40, 110), rnd.uniform(5, 11))
    img.paint(ms.cov().blur(6, 3, True), "#C9B8FF", 0.26)
    # dashed ghost line along the lip (the "follow the ghost preview" cue)
    ds = M(); dg = M()
    for i in range(16): ds.capsule(i * 64 + 8, 13, i * 64 + 40, 13, 1.5); dg.capsule(i * 64 + 8, 13, i * 64 + 40, 13, 4)
    img.paint(dg.cov().blur(2, 2, True), "#C9B8FF", 0.45); img.paint(ds.cov(), "#F0E8FF", 0.9)
    # little spectral wisps rising on the body: S-curves with tapered ends, pale
    ws = M()
    for _ in range(14):
        x0 = rnd.uniform(0, W); y0 = rnd.uniform(40, 130); pts = [(x0 + 7 * math.sin(t * 0.9), y0 - t * 3.2) for t in range(11)]
        ws.polyline(pts, 1.2)
    img.paint(ws.cov().blur(1, 2, True), "#D8CCFF", 0.4)
    lip_shadow(img, top, 16, 28, 0.2, "#0E0820")
    # dissolve: the body fades in a periodic dip (4 per strip) with a noisy edge so the road is "half there"; the lip stays solid
    nz = ValueNoise2D(48, 12, 297)
    def a(x, y):
        dip = 0.5 - 0.5 * math.cos(2 * math.pi * 4 * x / W)
        k = smoothstep(10, 34, y); n = nz.at(x / W, y / H)
        return 1.0 - 0.62 * dip * k * smoothstep(0.25, 0.75, n * 0.6 + dip * 0.5 + 0.1)
    img.multiply_alpha(a)
    # ragged tendrils on the skirt instead of a plain fade
    xs = [rnd.uniform(0, W) for _ in range(30)]; ln = [rnd.uniform(18, 52) for _ in xs]; hw = [rnd.uniform(9, 18) for _ in xs]
    def bottom(x):
        b = 92.0 + 14 * wave_fbm(x, W, 299, 3, 6)
        for tx, l, w in zip(xs, ln, hw):
            d = periodic_dist(x, tx)
            if d < w: b = max(b, 92 + l * (1 - d / w) ** 1.2)
        return b
    bc = column_cache(bottom)
    img.multiply_alpha(lambda x, y: 1.0 - smoothstep(bc(x) - 14, bc(x) + 1, y))
    return img

# --------------------------------------------------------------------------------------------------------- electric rail
def electric_rail(on=True):
    rnd = random.Random(301); img = Image(W, H)
    # sleeper bed + hazard band
    bed = M(); bed.rrect(-10, 30, W + 10, 192, 2)
    img.paint(bed.cov(), vgrad(30, 192, Ramp([(0, "#4A5260"), (0.3, "#323946"), (1, "#181C24")])))
    hz = M(); hzb = M(); hzb.rrect(-10, 30, W + 10, 42, 1)
    for i in range(-2, 34): x = i * 32; hz.polygon([(x, 42), (x + 16, 42), (x + 28, 30), (x + 12, 30)])
    img.paint(hzb.cov(), "#17191F"); img.paint(hz.cov().multiplied(hzb.cov()), vgrad(30, 42, Ramp([(0, "#FFD93E"), (1, "#E0A818")])))
    st = M(); sth = M()
    for i in range(16): x = i * 64 + 32; st.rrect(x - 22, 52, x + 22, 76, 4); sth.rrect(x - 18, 53.5, x + 18, 57.5, 1.5)
    img.paint(st.cov(), vgrad(52, 76, Ramp([(0, "#59606E"), (1, "#2C323C")]))); img.paint(sth.cov(), "#8E96A6", 0.5)
    # insulator blocks (white-blue ceramics) every 64 px, rail on top
    ins = M(); insh = M()
    for i in range(16): x = i * 64 + 32; ins.rrect(x - 8, 20, x + 8, 32, 2); insh.rrect(x - 6, 20.5, x - 2, 31, 1)
    img.paint(ins.cov(), vgrad(20, 32, Ramp([(0, "#F4F6FA"), (1, "#9AA6BA")]))); img.paint(insh.cov(), "#FFFFFF", 0.6)
    # conductor rail: 4 segments of 256 px, 0 and 2 charged (when on), 1 and 3 safe; small 6 px gaps between segments
    for sgi in range(4):
        xa = sgi * 256 + 3; xb = xa + 250; charged = on and sgi % 2 == 0
        rl = M(); rl.rrect(xa, 11, xb, 22, 4); hh = M(); hh.rrect(xa + 4, 12.2, xb - 4, 14.6, 1.2)
        if charged:
            gl = M(); gl.rrect(xa - 3, 8, xb + 3, 25, 6)
            img.paint(gl.cov().blur(5, 3, True), "#4FD8FF", 0.65)
            img.paint(rl.cov(), vgrad(11, 22, Ramp([(0, "#FFFFFF"), (0.3, "#A8F0FF"), (1, "#2AA8D8")])))
        else:
            img.paint(rl.cov(), vgrad(11, 22, Ramp([(0, "#B6BEC8"), (0.35, "#808A96"), (1, "#4C545E")])))
        img.paint(hh.cov(), "#FFFFFF", 0.7 if charged else 0.4)
        # status lamp on the base: red-hot (charged), green (safe), grey (off)
        lamp = M(); lamp.circle((xa + xb) / 2, 36, 3.6)
        col = "#FF5A4A" if charged else ("#4DFF88" if on else "#5A6070")
        if on:
            lg = M(); lg.circle((xa + xb) / 2, 36, 9); img.paint(lg.cov().blur(3, 3, True), col, 0.6)
        img.paint(lamp.cov(), col)
        if charged:                                   # arcs: bright jagged bolts leaping above the rail and across the segment
            bolt = M(); bg = M()
            for k in range(7):
                x0 = xa + 18 + k * 34 + rnd.uniform(-6, 6); pts = zig(rnd, x0, 11, x0 + rnd.uniform(14, 30), 11, 7, 4.5)
                pts = [(x, min(11.0, max(0.8, y - 3.5 + 0.0 if abs(y - 11) > 0.01 else y))) for x, y in pts]; bolt.polyline(pts, 0.8); bg.polyline(pts, 2.6)
            img.paint(bg.cov().blur(2, 2, True), "#4FD8FF", 0.7); img.paint(bolt.cov(), "#FFFFFF")
    # end caps at segment gaps: yellow warning chevrons on the bed
    skirt_fade(img, 100, 190)
    return img

# --------------------------------------------------------------------------------------------------------- fragile paper
def fragile_paper(torn=False):
    rnd = random.Random(311); img = Image(W, H)
    def jag(x): return 5.0 + 2.2 * wave_fbm(x, W, 313, octaves=4, base=9) + 1.2 * math.sin(x * 1.7) * 0.4
    top = column_cache(jag)
    # three stacked sheets: kraft base, cream middle, near-white top, each with its own wavy bottom edge
    for ci, (yb, c0, c1, ph) in enumerate(((140, "#C8A472", "#9E7C4E", 1), (96, "#E4D2A2", "#C0A874", 2), (60, "#F6EED4", "#DACB9E", 3))):
        bot = column_cache(lambda x, yb=yb, ph=ph: yb + 4 * wave_fbm(x, W, 320 + ph, 3, 5))
        cov = Cov(W, H); cov.bb = (0, 0, W, 160)
        for x in range(W):
            t = top(x); b = bot(x)
            for y in range(0, 160):
                a = min(1.0, max(0.0, y + 1 - t)) * min(1.0, max(0.0, b + 1 - y))
                if a > 0: cov.d[y * W + x] = a
        sh = Cov(W, H); sh.bb = (0, 0, W, 170)
        for x in range(W):
            b = bot(x)
            for y in range(int(b) + 1, min(168, int(b) + 9)): sh.d[y * W + x] = math.exp(-((y - b - 2) / 3.0) ** 2)
        if ci > 0: img.paint(sh, "#3A2A14", 0.35 if ci == 1 else 0.28)
        img.paint(cov, vgrad(0, yb, Ramp([(0, c0), (1, c1)])))
    # ruled lines and a margin line on the top sheet, in faint blue and red
    ln = M()
    for y in (22, 34, 46): ln.rrect(-10, y, W + 10, y + 1.2, 0.5)
    img.paint(ln.cov().multiplied(strip_cov(lambda x: 10)), "#7FA6D8", 0.55)
    # perforation / tear lines: dotted vertical cuts every 128 px with small notches at the lip; fibres of white
    dots = M(); notch = M(); cut = M()
    for i in range(8):
        x = i * 128 + 64
        for k in range(14): dots.circle(x, 14 + k * 8.5, 1.5)
        notch.polygon([(x - 5, 0), (x + 5, 0), (x + 0.5, 14)]); cut.polyline(zig(rnd, x, 10, x + rnd.uniform(-6, 6), 50 + rnd.uniform(0, 30), 7, 2.5), 0.7)
    img.paint(dots.cov(), "#6A5230", 0.55); img.paint(cut.cov(), "#5A4426", 0.85)
    # crease lines (fold shadows) crossing the sheets
    cz = M(); czl = M()
    for i in range(8):
        x = i * 128 + 20; cz.polyline([(x, 8), (x + 16, 60), (x + 4, 124)], 0.9); czl.polyline([(x + 1.5, 8), (x + 17.5, 60), (x + 5.5, 124)], 0.7)
    img.paint(cz.cov(), "#8E7648", 0.5); img.paint(czl.cov(), "#FFFFFF", 0.5)
    # torn top edge: a thin white fibre fringe just inside the lip
    fib = Cov(W, H); fib.bb = (0, 0, W, 30)
    for x in range(W):
        t = top(x)
        for y in range(int(t), int(t) + 4): fib.d[y * W + x] = max(0.0, 1 - (y + .5 - t) / 2.6) * 0.9
    img.paint(fib, "#FFFFFF", 0.75)
    # ragged bottom: torn edge (jagged teeth) rather than a soft fade, with white fibre
    def bottom(x): return 118 + 9 * wave_fbm(x, W, 331, 3, 6)
    bc = column_cache(bottom)
    img.multiply_alpha(lambda x, y: 1.0 - smoothstep(bc(x) - 1.2, bc(x) + 1.2, y))
    if torn:
        # three tears right through the whole strip: a jagged centre line wandering with y, ~14 px wide, white fibre on both edges
        cxs = (170, 520, 850)
        def edge(x0, y):
            return x0 + 5 * math.sin(y * 0.09 + x0) + 2.5 * math.sin(y * 0.31 + x0 * 2)
        def gap(x, y):
            for cx in cxs:
                c = edge(cx, y); hw = 5.0 + 5.0 * (0.5 + 0.5 * math.sin(y * 0.11 + cx))
                d = abs(x - c)
                if d < hw: return 0.0
            return 1.0
        keep = Cov(W, H); fib2 = Cov(W, H); keep.bb = (0, 0, W, 170); fib2.bb = (0, 0, W, 170)
        for y in range(0, 170):
            for x in range(W):
                best = 99.0
                for cx in cxs:
                    c = edge(cx, y + .5); hw = 5.0 + 5.0 * (0.5 + 0.5 * math.sin((y + .5) * 0.11 + cx)); d = abs(x + .5 - c) - hw
                    if d < best: best = d
                if best < 3.0: keep.d[y * W + x] = min(1.0, max(0.0, best + 0.5)); fib2.d[y * W + x] = max(0.0, 1 - max(0.0, best) / 2.4) if best > -0.5 else 0.0
        img.paint(fib2, "#FFFFFF", 0.9)
        for y in range(img.h):
            for x in range(max(0, 0), W):
                k = keep.d[y * W + x] if (keep.bb and y < 170) else 1.0
                near = False
                for cx in cxs:
                    if abs(x - edge(cx, y)) < 22: near = True; break
                if near: img.a[y * W + x] *= k
    return img

SURFACES = {
    "spring_road": lambda: spring_road(), "magnetic_rail": lambda: magnetic_rail(), "acid": lambda: acid(), "oil": lambda: oil(),
    "conveyor": lambda: conveyor(0), "conveyor_b": lambda: conveyor(1), "lava_river": lambda: lava_river(), "sticky_candy": lambda: sticky_candy(),
    "honey": lambda: honey(), "metal_plates": lambda: metal_plates(), "ice_crystals": lambda: ice_crystals(), "haunted_shadow": lambda: haunted_shadow(),
    "electric_rail": lambda: electric_rail(True), "electric_rail_off": lambda: electric_rail(False),
    "fragile_paper": lambda: fragile_paper(False), "fragile_paper_torn": lambda: fragile_paper(True),
}

if __name__ == "__main__":
    ap = argparse.ArgumentParser(); ap.add_argument("--output", required=True); ap.add_argument("--surface", default="all"); ap.add_argument("--no-preview", action="store_true")
    a = ap.parse_args(); os.makedirs(a.output, exist_ok=True)
    for name in (SURFACES if a.surface == "all" else a.surface.split(",")):
        im = SURFACES[name](); write_png(im, os.path.join(a.output, "road_%s_1024x192.png" % name))
        print(name, "seam score (1.0 = invisible):", round(tile_seam_score(im), 3), flush=True)
        if not a.no_preview: preview(im, os.path.join(a.output, "road_%s_preview_x3.png" % name))
